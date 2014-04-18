using System;
using System.Collections.Generic;
using System.Linq;

namespace Earlvik.ArtiStereo
{
    /// <summary>
    /// Marker interface for room constructor parts
    /// </summary>
    interface IRoomObject { }
    /// <summary>
    /// Class that stores and provides information about premises where to model sound waves
    /// </summary>
    [Serializable]
   public class Room
    {
        
        //lists of room elements
        private List<ListenerPoint> _listeners;
        private List<SoundPoint> _sources;
        private List<Wall> _walls;
        //value for calculating reflection from ceiling

        public Room()
        {
            _walls = new List<Wall>();
            _sources = new List<SoundPoint>();
            _listeners = new List<ListenerPoint>();
        }

        public double CeilingHeight { set; get; }
        public List<ListenerPoint> Listeners { get { return _listeners; } }
        public List<SoundPoint> Sources { get { return _sources; } }
        public List<Wall> Walls { get { return _walls; } }

        /// <summary>
        /// Adds new listener to the room
        /// </summary>
        /// <param name="listener"></param>
        public ListenerPoint AddListener(ListenerPoint listener)
        {
            if (!ListenerNotExists(listener)) throw new Exception("Trying to add already existent listener");
            if (!PointNotOnWall(listener)) throw new Exception("Trying to add listener on a wall");
            _listeners.Add(listener);
            return listener;
        }

        /// <summary>
        /// Adds new sound source to the room
        /// </summary>
        /// <param name="source"></param>
        public SoundPoint AddSource(SoundPoint source)
        {
            if(!SourceNotExists(source)) throw new Exception("Trying to add already existent source");
            if(!PointNotOnWall(source)) throw new Exception("Trying to add source on a wall");
            _sources.Add(source);
            return source;
        }

        /// <summary>
        /// Adds new wall to the room
        /// </summary>
        /// <param name="wall"></param>
        public Wall AddWall(Wall wall)
        {
            if(!WallNotExists(wall)) throw new Exception("Trying to add already existent wall");
            _walls.Add(wall);
            return wall;
        }
        /// <summary>
        /// Main method. Calculates reflections, sound reduction and reverberation, records result sound in listeners
        /// </summary>
        public void CalculateSound()
        {
            
            double airSSpeed = Wall.Material.Air.SoundSpeed;
            foreach (SoundPoint source in Sources)
            {
                //double distance = Listeners.Min(x => Geometry.Distance(source, x));

                //double timeOffset = distance/airSSpeed*1000;
                double directSoundLevel = SoundReduction(Listeners.Min(x=>Geometry.Distance(source,x)));
                foreach (ListenerPoint listener in Listeners)
                {
                    listener.Sound = new Sound(1, source.Sound.DiscretionRate, source.Sound.BitsPerSample);
                    
                    //Direct sound
                    if (CheckPath(new Line(source, listener)))
                    {
                        double distance = Geometry.Distance(listener, source);
                        double time = distance/airSSpeed*1000;
                        double percentReduction = SoundReduction(distance);// / directSoundLevel;
                        if (listener.Directional)
                        {
                            percentReduction *= listener.GetReduction(new Line(source, listener));
                        }
                       
                        listener.Sound.Add(source.Sound.CopyWithVolume(percentReduction,0), 0, 0,
                            source.Sound.MillesecondsToSamples((int) time));
                    }
                    //Primary reflections
                    Sound snd;
                    foreach (Wall wall in Walls)
                    {
                        Point refPoint = Geometry.ReflectionPoint(wall, source, listener);
                        if (refPoint == null) continue;
                        if (
                            !(CheckPath(new Line(source, refPoint), wall) &&
                              CheckPath(new Line(refPoint, listener), wall))) continue;
                        snd = new Sound(source.Sound);
                        //snd.SetVolume(SoundReduction(Geometry.Distance(source, refPoint)),0);
                        Double angle = Geometry.Angle(new Line(source, refPoint), new Line(refPoint, listener),false) / 2;
                        Double refCoefft = wall.ReflectionCoefft(angle);
                        //snd.SetVolume(refCoefft,0);
                        Double reductionCoefft = (SoundReduction(Geometry.Distance(source, refPoint) +
                                                                 Geometry.Distance(refPoint, listener))) * refCoefft;
                        //reductionCoefft /= directSoundLevel; //Adjusting volume to prevent distortion
                        if (listener.Directional)
                        {
                            reductionCoefft *= listener.GetReduction(new Line(refPoint, listener));
                        }
                        snd.SetVolume(reductionCoefft, 0);
                        //Frequency-based reduction
                        snd.SetVolume(0,wall.WallMaterial.Low,wall.WallMaterial.Medium,wall.WallMaterial.High);
                        Double time = (Geometry.Distance(source, refPoint) + Geometry.Distance(refPoint, listener)) /
                                      airSSpeed * 1000;
                        listener.Sound.Add(snd, 0, 0, (int)time);
                        snd = null;
                    }
                }
                

               

            }
        }

        /// <summary>
        /// Checking validness of the room, germeticness etc.
        /// </summary>
        /// <returns></returns>
        public bool IsValid()
        {
            if (_sources.Count == 0 || _listeners.Count == 0) return false;
            List<Point> surface = OuterSurface();
            for (int i = 0; i < surface.Count - 1; i++)
            {
                Line line = new Line(surface[i],surface[i+1]);
                if (_walls.All(wall => wall != line)) return false;
            }
            return true;
        }

        /// <summary>
        /// deletes certain listener
        /// </summary>
        /// <param name="listener"></param>
        public void RemoveListener(ListenerPoint listener)
        {
            _listeners.Remove(listener);
        }

        /// <summary>
        /// deletes certain sound source
        /// </summary>
        /// <param name="source"></param>
        public void RemoveSource(SoundPoint source)
        {
            _sources.Remove(source);
        }

        /// <summary>
        /// deletes certain wall
        /// </summary>
        /// <param name="wall"></param>
        public void RemoveWall(Wall wall)
        {
            _walls.Remove(wall);
        }

        bool CheckPath(Line line)
        {
            return Walls.All(wall => Geometry.SegmentIntersection(wall, line, false) == null);
        }

        bool CheckPath(Line line, Wall exclude)
        {
            return Walls.All(wall => Geometry.SegmentIntersection(wall, line, false) == null || wall == exclude); 
        }

        bool ListenerNotExists(Point testlistener)
        {
            if(testlistener == null) throw new ArgumentNullException();
            return _listeners.All(listener => testlistener != listener);
        }

        /// <summary>
        /// Calculates outer surface points of the room using Jarvis algorythm
        /// </summary>
        /// <returns>List of points in the outer surface polygon</returns>
        List<Point> OuterSurface()
        {
            List<Point> points = new List<Point>();
            List<Point> surface = new List<Point>();
            points.AddRange(_sources);
            points.AddRange(_listeners);
            foreach (Wall wall in _walls)
            {
                points.Add(wall.Start);
                points.Add(wall.End);
            }
            Point lefttop = points[0];
            foreach (Point point in points)
            {
                if (point.X < lefttop.X || point.Y < lefttop.Y) lefttop = point;
            }
            Point current = lefttop;
            Point pretendent = (points[0] == current) ? points[1] : points[0];
            while (pretendent != lefttop)
            {
                if (current != lefttop)
                {
                    pretendent = points.Find(point => surface.IndexOf(point) == -1);
                }
                for (int i = 0; i < points.Count; i++)
                {
                    if (points[i] == current) continue;
                    double vecprod = Geometry.VectorMultiplication(new Line(current, pretendent),
                        new Line(current, points[i]));
                    if (Geometry.EqualDouble(vecprod, 0))
                    {
                        if (Geometry.Distance(pretendent, current) > Geometry.Distance(points[i], current))
                        {
                            pretendent = points[i];
                        }
                        continue;
                    }
                    if (vecprod < 0)
                    {
                        pretendent = points[i];

                    }

                }
                surface.Add(pretendent);
                current = pretendent;
            }
            //surface.Add(lefttop);
            return surface;
        }

        bool PointNotOnWall(Point point)
        {
            if(point == null) throw new ArgumentNullException();
            return _walls.All(wall => !(Geometry.OnLine(wall, point) && Geometry.OnSegment(wall, point)));
        }

        double SoundReduction(double distance)
        {
           //double decibellReduction = Math.Log((distance ), 2) * (-3);
            //return Math.Pow(10, decibellReduction / 10.0);
            return 1/(distance * distance);
        }

        bool SourceNotExists(Point testsource)
        {
            if(testsource == null) throw new ArgumentNullException();
            return _sources.All(source => testsource != source);
        }

        bool WallNotExists(Wall testwall)
        {
            if(testwall == null) throw new ArgumentNullException();
            return _walls.All(wall => testwall != wall);
        }
    }
    /// <summary>
    /// Simple geometric point
    /// </summary>
    [Serializable]
    public class Point
    {
        private double mX ;
        private double mY;

        public Point(double x, double y)
        {
            X = x;
            Y = y;
        }

        public Point(System.Windows.Point point)
        {
            mX = point.X;
            mY = point.Y;
        }

        public Point()
        {
            X = 0;
            Y = 0;
        }

        public double X
        {
            set
            {
                //if(value < 0) throw new ArgumentException(" Point coordinate X should be non-negative, but was "+value);
                mX = value;
            }
            get { return mX; }
        }
        public double Y
        {
            set
            {
                //if (value < 0) throw new ArgumentException(" Point coordinate Y should be non-negative, but was " + value);
                mY = value;
            }
            get { return mY; }
        }

        public static bool operator ==(Point a, Point b)
        {
            if (((Object)a)==null && ((Object)b) == null) return true;
            if (((Object)a) == null || ((Object)b) == null) return false;
            return Geometry.EqualDouble(a.X, b.X) && Geometry.EqualDouble(a.Y, b.Y);
        }

        public static bool operator !=(Point a, Point b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Point) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (mX.GetHashCode()*397) ^ mY.GetHashCode();
            }
        }

        public override string ToString()
        {
            return "( " + X + " ; " + Y + " )";
        }

        protected bool Equals(Point other)
        {
            return mX.Equals(other.mX) && mY.Equals(other.mY);
        }
    }
    /// <summary>
    /// Simple geometric line
    /// </summary>
    [Serializable]
   public class Line
    {
        public Line(Point start, Point end)
        {
            Start = start;
            End = end;
        }

        public Line(double xstart, double ystart, double xend, double yend)
        {
            Start = new Point(xstart, ystart);
            End = new Point(xend, yend);
        }

        public Point End { set; get; }
        public Point Start { set; get; }

        static public bool operator ==(Line a, Line b)
        {
            if (((Object)a) == null && ((Object)b) == null) return true;
            if (((Object)a) == null || ((Object)b) == null) return false;
            return (a.Start == b.Start && a.End == b.End) || (a.Start == b.End && a.End == b.Start);
        }

        static public bool operator !=(Line a, Line b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Line) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Start != null ? Start.GetHashCode() : 0)*397) ^ (End != null ? End.GetHashCode() : 0);
            }
        }
        /// <summary>
        /// Returns point of intersection with segment or whole line
        /// </summary>
        /// <param name="second">Line to intersect with</param>
        /// <param name="wholeLine">Does out-of-segment intersections count</param>
        /// <returns>Intersection point OR null if none exists</returns>
        public Point GetIntersection(Line second, bool wholeLine)
        {
            return Geometry.SegmentIntersection(this,second, wholeLine);
            
        }

        protected bool Equals(Line other)
        {
            return Equals(Start, other.Start) && Equals(End, other.End);
        }
    }
    /// <summary>
    /// Point with associated sound 
    /// </summary>
    [Serializable]
    public class SoundPoint:Point,IRoomObject
    {
        [NonSerialized] private Sound mSound;

        public SoundPoint(Point p) : base(p.X, p.Y)
        {
           
        }
        public SoundPoint(double x, double y) : base(x, y)
        {
            
        }

        public Sound Sound
        {
            set { mSound = value; }
            get { return mSound; }
        }
    }
    /// <summary>
    /// Point with associated sound and microphone directional decrease function
    /// </summary>
    [Serializable]
    public class ListenerPoint:SoundPoint
    {
        public delegate double DirectionalDecrease(Line incomingRay, Line micDirection);
        /// <summary>
        /// "Heart-shaped" function that provides strond sound preception from the front of microphone, less from sides and very low from rear
        /// </summary>
        public static DirectionalDecrease Cardioid = delegate(Line incomingRay, Line micDirection)
        {
            double incomingAngle = Geometry.Angle(micDirection, incomingRay,true);
            double r = 0.5*(1 + Math.Cos(incomingAngle));
            double decibellReduction = 25*(r-1);
            double percentReduction = Math.Pow(10, decibellReduction/20.0);
            return percentReduction;
        };
       
        private readonly DirectionalDecrease mDecreaseFunction;
        private readonly Line mDirection;

        public ListenerPoint(Point p, Line direction, DirectionalDecrease decreaseFunction) : base(p)
        {
            mDirection = direction;
            mDecreaseFunction = decreaseFunction;
            Directional = true;

        }
        public ListenerPoint(double x, double y, Line direction, DirectionalDecrease decreaseFunction) : base(x, y)
        {
            mDirection = direction;
            mDecreaseFunction = decreaseFunction;
            Directional = true;

        }

        public ListenerPoint(Point p)
            : base(p)
        {
            Directional = false;
        }
        public ListenerPoint(double x, double y)
            : base(x, y)
        {
            Directional = false;
        }
        /// <summary>
        /// Angle between mic direction and positive direction of X-axis
        /// </summary>
        public double DirectionAngle
        {
            get { return Geometry.Angle(new Line(0, 0, 1, 0), mDirection, true); }
        }
        /// <summary>
        /// X-Coordinate of direction vector
        /// </summary>
        public double DirectionX
        {
            set { if(mDirection!=null) mDirection.End.X = value; }
            get { return mDirection==null?0:mDirection.End.X; }
        }
        /// <summary>
        /// Y-Coordinate of direction vector
        /// </summary>
        public double DirectionY
        {
            set { if(mDirection!=null) mDirection.End.Y = value; }
            get
            {
                return mDirection == null ? 0:mDirection.End.Y;
            }
        }
        /// <summary>
        /// Determines if mic is omnidirectional or not
        /// </summary>
        public bool Directional { private set; get; }
        /// <summary>
        /// Calculates sound reduction based on incoming sound direction
        /// </summary>
        /// <param name="incomingRay">Incoming sound direction</param>
        /// <returns>Percentage of result sound</returns>
        public double GetReduction(Line incomingRay)
        {
            if (mDecreaseFunction == null) return 1;
            return mDecreaseFunction(incomingRay, mDirection);
        }
    }

    [Serializable]
    public class Wall:Line,IRoomObject
    {
        public MaterialPreset MatPreset;
        /// <summary>
        /// List of predefined material options
        /// </summary>
        public enum MaterialPreset { OakWood, Air, Glass, Granite, Brick, Rubber, None}

        public Wall(Point start, Point end, Material material) : base(start, end)
        {
            WallMaterial = material;
            MatPreset = MaterialPreset.None;
        }

        public Wall(double xstart, double ystart, double xend, double yend, Material material)
            : base(xstart, ystart, xend, yend)
        {
            WallMaterial = material;
            MatPreset = MaterialPreset.None;
        }

        public Wall(Point start, Point end, MaterialPreset preset)
            : base(start, end)
        {
            MatPreset = preset;
            switch (preset)
            {
                case MaterialPreset.OakWood:
                    {
                        WallMaterial = Material.OakWood;
                        break;
                    }
                case MaterialPreset.Granite:
                    {
                        WallMaterial = Material.Granite;
                        break;
                    }
                case MaterialPreset.Glass:
                    {
                        WallMaterial = Material.Glass;
                        break;
                    }
                case MaterialPreset.Brick:
                    {
                        WallMaterial = Material.Brick;
                        break;
                    }
                case MaterialPreset.Rubber:
                    {
                        WallMaterial = Material.Rubber;
                        break;
                    }
                default:
                    {
                        WallMaterial = Material.OakWood;
                       break;
                    }
            }
        }
        public Wall(double xstart, double ystart, double xend, double yend, MaterialPreset preset) : this(new Point(xstart, ystart), new Point(xend, yend), preset) { }
        public Material WallMaterial { set; get; }
        /// <summary>
        /// Level of sound reduction after rflection from this wall
        /// </summary>
        /// <param name="angle">Angle of sound ray falling</param>
        /// <returns></returns>
        public double ReflectionCoefft(double angle)
       {
           //double densityPart = (WallMaterial.Density/Material.Air.Density)*Math.Cos(angle);
           //double speedPart =
           //    Math.Sqrt((Material.Air.SoundSpeed/WallMaterial.SoundSpeed)*
           //              (Material.Air.SoundSpeed/WallMaterial.SoundSpeed) - Math.Sin(angle)*Math.Sin(angle));
           //if (Geometry.EqualDouble(densityPart+speedPart,0) ) return 0;
           //return Math.Abs((densityPart - speedPart)/(densityPart + speedPart));
           double impedancepart = (WallMaterial.Density*WallMaterial.SoundSpeed)/
                                  (Material.Air.Density*Material.Air.SoundSpeed);
           double tangentPart =1 - ((WallMaterial.SoundSpeed/Material.Air.SoundSpeed)*
                          (WallMaterial.SoundSpeed/Material.Air.SoundSpeed) - 1)*Math.Tan(angle);
           if (tangentPart < 0) return 1;
           tangentPart = Math.Sqrt(tangentPart);
           double result = (impedancepart - tangentPart)/(impedancepart + tangentPart);
           return result;
       }
        [Serializable]
        public class Material
        {
            public static readonly Material Air = new Material(1.20, 340.29);
            public static readonly Material Brick = new Material(1800,3480,0.01,0.02,0.02);
            public static readonly Material Glass = new Material(2500, 5000,0.035,0.037,0.2);
            public static readonly Material Granite = new Material(2800, 3950,0.01,0.02,0.03);
            public static readonly Material OakWood = new Material(720,4000,0.09,0.08,0.1);
            public static readonly Material OakWoodCarpeted = new Material(720,4000,0.01,0.1,0.4);
            public static readonly Material Rubber = new Material(1050, 54);
            
            public double Density { set; get; }
            public double SoundSpeed { set; get; }
            public double Low { set; get; }
            public double Medium { set; get; }
            public double High { set; get; }
            Material(double dens, double sspeed, double low = 0, double medium = 0, double high = 0)
            {
                Density = dens;
                SoundSpeed = sspeed;
                Low = low;
                Medium = medium;
                High = high;
            }

            
        }
    }

    /// <summary>
    /// Class containing basic geometry methods for calculating the route of sound waves
    /// </summary>
    public static class Geometry
        {
            /// <summary>
            /// Constant used to compare real numbers
            /// </summary>
            private const double eps = 0.0000001;

        /// <summary>
        /// Calculates an angle value between two lines
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="asVector">Should method consider lines as directional vectors or as infinite lines</param>
        /// <returns>angle in radians</returns>
        public static double Angle(Line a, Line b, bool asVector)
        {
            //line equations parameters
            double A1 = a.Start.Y - a.End.Y;
            double B1 = a.End.X - a.Start.X;
            //double C1 = a.Start.X*a.End.Y - a.Start.Y*a.End.X;

            double A2 = b.Start.Y - b.End.Y;
            double B2 = b.End.X - b.Start.X;
            // double C2 = b.Start.X * b.End.Y - b.Start.Y * b.End.X;

            Point checkPoint = new Point(b.Start.X -B2,b.Start.Y+A2);
            //parallel and peprendicular lines
            if ((Math.Abs(B1) < eps && Math.Abs(B2) < eps) || (Math.Abs(A1) < eps && Math.Abs(A2) < eps) ||
                Math.Abs((A1/A2) - (B1/B2)) < eps)
            {
                if (!asVector || A1*A2 <0 || B1*B2 <0 ) return 0;
                return Math.PI;
            }
                    
               
            if (Math.Abs(A1*A2 + B1*B2) < eps)
            {
                if (!asVector || Direction(a, checkPoint) < 0) return Math.PI/2;
                return 3*Math.PI/2;
            }

            //other lines
            double denom = Math.Sqrt((A1 * A1 + B1 * B1) * (A2 * A2 + B2 * B2));
            double nom = A1*A2 + B1*B2;
            if (!asVector) nom = Math.Abs(nom);
            else nom *= -1;
            double result  = Math.Acos(nom / denom);
            if (asVector && Direction(a, checkPoint) > 0)
            {
                result = 2*Math.PI - result;
            }
            return result;
        }

        /// <summary>
            /// Calculates the distance between two points
            /// </summary>
            /// <param name="a"></param>
            /// <param name="b"></param>
            /// <returns></returns>
            public static double Distance(Point a, Point b)
            {
                return Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
            }
            /// <summary>
            /// Calculates the distance between a point and a line
            /// </summary>
            /// <param name="point"></param>
            /// <param name="line"></param>
            /// <returns></returns>
            public static double Distance(Point point, Line line)
            {
                //Line equation parameters
                double A = line.Start.Y - line.End.Y;
                double B = line.End.X - line.Start.X;
                double C = line.Start.X * line.End.Y - line.Start.Y * line.End.X;

                double denom = Math.Sqrt(A * A + B * B);
                if (Math.Abs(denom) < eps) throw new Exception("the line equation is illegal");
                return Math.Abs(A * point.X + B * point.Y + C) / denom;
            }

        static public bool EqualDouble(double a, double b)
            {
                return Math.Abs(a - b) < eps;
            }

            static public bool OnLine(Line line, Point point)
            {
                return EqualDouble((point.X - line.Start.X)/(line.End.X - line.Start.X), (point.Y - line.Start.Y)/(line.End.Y - line.Start.Y));
            }

        /// <summary>
        /// Decides if the given point is located on the given line segment
        /// </summary>
        /// <param name="line"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        static public bool OnSegment(Line line, Point point)
        {
            double xs = line.Start.X;
            double xe = line.End.X;
            double ys = line.Start.Y;
            double ye = line.End.Y;
            double xp = point.X;
            double yp = point.Y;

            return (Math.Min(xs, xe) < xp || EqualDouble(Math.Min(xs, xe), xp)) && (xp < Math.Max(xs, xe) || EqualDouble(Math.Max(xs, xe), xp)) &&
                   (Math.Min(ys, ye) < yp || EqualDouble(Math.Min(ys, ye), yp)) && (yp < Math.Max(ys, ye) || EqualDouble(Math.Max(ys, ye), yp));
        }

        /// <summary>
        /// Calculates the parallel projection of a point to a line  
        /// </summary>
        /// <param name="toLine">Line to project to</param>
        /// <param name="point">Point to project</param>
        /// <param name="wholeLine">Does out-of-segment points count</param>
        /// <returns></returns>
        public static Point ParallelProjection(Line toLine, Point point, bool wholeLine)
        {
            //double A1 = fromLine.Start.Y - fromLine.End.Y;
            //double B1 = fromLine.End.X - fromLine.Start.X;
            //double C1 = fromLine.Start.X*fromLine.End.Y - fromLine.Start.Y*fromLine.End.X;

            double A2 = toLine.Start.Y - toLine.End.Y;
            double B2 = toLine.End.X - toLine.Start.X;
            double C2 = toLine.Start.X * toLine.End.Y - toLine.Start.Y * toLine.End.X;

            if (EqualDouble(B2, 0))
            {
                Point projection =  new Point(-C2 / A2, point.Y);
                return (OnSegment(toLine, projection) || wholeLine) ? projection : null;
            }
            if (EqualDouble(A2, 0))
            {
                Point projection =  new Point(point.X, toLine.Start.Y);
                return (OnSegment(toLine, projection) || wholeLine) ? projection : null;
            }

            double angleCoefft = A2 / (-B2);
            //double offset = C1/(-B1);
               
            Point start = new Point(0, point.Y + (1 / angleCoefft) * point.X);
            Point end = new Point(1, point.Y + (1 / angleCoefft) * (point.X - 1));
            Line normal = new Line(start, end);
            Point intersection = toLine.GetIntersection(normal, true);
            return (OnSegment(toLine, intersection) || wholeLine) ? intersection : null;
        }

        /// <summary>
        /// Calculates the point of primary reflection
        /// </summary>
        /// <param name="wall">linear wall that reflects sound</param>
        /// <param name="source">location of the sound source</param>
        /// <param name="listener">location of the listener</param>
        /// <returns>null if there is no primary reflection, reflection point otherwise</returns>
        public static Point ReflectionPoint(Line wall, Point source, Point listener)
        {
            Line direct = new Line(source, listener);
            double angle = Angle(wall, direct,false);
            if (Math.Abs(angle) < eps) //the wall is parallel with the direct line
            {
                return ParallelProjection(wall, new Point((source.X + listener.X) / 2, (source.Y + listener.Y) / 2),false);
            }

            Point intersection = wall.GetIntersection(direct, true);
            if (OnSegment(direct, intersection)) return null;//the wall is between the source and the listener

            if (Math.Abs(angle - Math.PI / 2) < eps) return intersection;

            Point nearPoint, farPoint;
            double shortDist = Distance(listener, wall), longDist = Distance(source, wall);
            if (longDist > shortDist)
            {
                nearPoint = listener;
                farPoint = source;
            }
            else
            {
                nearPoint = source;
                farPoint = listener;
                double temp = shortDist;
                shortDist = longDist;
                longDist = temp;
            }

            Line farParallel;
            if (Math.Abs(wall.Start.X - wall.End.X) < eps)
            {
                farParallel = new Line(farPoint, new Point(farPoint.X, 0));
            }
            else
            {
                double wallAngleCoefft = (wall.Start.Y - wall.End.Y) / (wall.Start.X - wall.End.X);
                Point secondPoint = new Point(0, farPoint.Y + wallAngleCoefft * (-farPoint.X));
                farParallel = new Line(farPoint, secondPoint);

            }

            Point endOfFarProjection = ParallelProjection(farParallel, nearPoint,true);

            double similarityCoefft = (shortDist) / (longDist - shortDist);

            // Point on far parallel line which will be projected
            Point farResultProjection = new Point((farPoint.X + similarityCoefft * endOfFarProjection.X) / (1 + similarityCoefft), (farPoint.Y + similarityCoefft * endOfFarProjection.Y) / (1 + similarityCoefft));

            Point result = ParallelProjection(wall, farResultProjection,false);

            return result;

        }

        /// <summary>
        /// Calculates a point where one line intersects the other
        /// </summary>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <param name="wholeLine">false if only intersection of segments is needed, true otherwise</param>
        /// <returns>null if there is no intersection or the intersection point otherwise</returns>
        static public Point SegmentIntersection(Line first,Line second, bool wholeLine)
        {
            //Vector multiplication between the line and the endpoint of the other line
            double d1 = Direction(second, first.Start);
            double d2 = Direction(second, first.End);
            double d3 = Direction(first, second.Start);
            double d4 = Direction(first, second.End);
            //Checking if any of the endpoints is on the other line
            if (Math.Abs(d1) < eps && (OnSegment(second, first.Start) || wholeLine))
            {
                return first.Start;
            }
            if (Math.Abs(d2) < eps && (OnSegment(second, first.End) || wholeLine))
            {
                return first.End;
            }
            if (Math.Abs(d3) < eps && (OnSegment(first, second.Start) || wholeLine))
            {
                return second.Start;
            }
            if (Math.Abs(d4) < eps && (OnSegment(first, second.End) || wholeLine))
            {
                return second.End;
            }
            //Checking for intersections apart from the endpoints

            double x1 = first.Start.X,
                x2 = first.End.X,
                x3 = second.Start.X,
                x4 = second.End.X,
                y1 = first.Start.Y,
                y2 = first.End.Y,
                y3 = second.Start.Y,
                y4 = second.End.Y;
            if (Math.Abs((y4 - y3) * (x2 - x1) - (x4 - x3) * (y2 - y1)) < eps) return null;
            double coefftA = ((x4 - x3) * (y1 - y3) - (y4 - y3) * (x1 - x3)) / ((y4 - y3) * (x2 - x1) - (x4 - x3) * (y2 - y1));
            // double coefftB = ((x2 - x1) * (y1 - y3) - (y2 - y1) * (x1 - x3)) / ((y4 - y3) * (x2 - x1) - (x4 - x3) * (y2 - y1));
            if (wholeLine || (d2 * d1 < 0 && d3 * d4 < 0))
            {
                return new Point(x1 + coefftA * (x2 - x1), y1 + coefftA * (y2 - y1));
            }

            return null;

        }

        /// <summary>
        /// Calculates a value that shows the direction of cross products of vectors
        /// </summary>
        /// <param name="first">first line</param>
        /// <param name="second">second line</param>
        /// <returns>negative value if the second line goes clockwise from the first, or vice versa, or 0 if vectors are collinear</returns>
        public static double VectorMultiplication(Line first, Line second)
        {
            double x1 = first.End.X - first.Start.X;
            double y1 = first.End.Y - first.Start.Y;
            double x2 = second.End.X - second.Start.X;
            double y2 = second.End.Y - second.Start.Y;

            return x1 * y2 - x2 * y1;
        }
        /// <summary>
        /// Calculates the direction of cross product of given line vector and one of its ends to the given point vector
        /// </summary>
        /// <param name="line"></param>
        /// <param name="point"></param>
        /// <returns>negative value if the second line goes clockwise from the first, or vice versa, or 0 if vectors are collinear</returns>
        static double Direction(Line line, Point point)
        {
            Line toPoint = new Line(line.Start, point);
            return VectorMultiplication(line, toPoint);
        }
        }
}
