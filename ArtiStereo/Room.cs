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
        private const int imageMaxDepth = 3;
        
        //lists of room elements
        private readonly List<ListenerPoint> mListeners;
        private readonly List<SoundPoint> mSources;
        private readonly List<Wall> mWalls;

        public Room()
        {
            mWalls = new List<Wall>();
            mSources = new List<SoundPoint>();
            mListeners = new List<ListenerPoint>();
        }

        public Room(Room room)
        {
            mWalls = new List<Wall>();
            foreach (Wall wall in room.Walls)
            {
                mWalls.Add(wall);
            }
            mSources = room.Sources;
            mListeners = room.Listeners;
        }


        public double CeilingHeight { set; get; }
        public Wall.Material CeilingMaterial { set; get; }
        public Wall.Material FloorMaterial { set; get; }
        public List<ListenerPoint> Listeners { get { return mListeners; } }
        public List<SoundPoint> Sources { get { return mSources; } }
        public List<Wall> Walls { get { return mWalls; } }

// ReSharper disable once InconsistentNaming
        public enum RoomPreset {SmallSquare,BigSquare,SmallRect,TShape,LShape, Triangle,Trapezoid,Pentagon}

        static public Room CreatePresetRoom(RoomPreset preset)
        {
            Room room = new Room();
            Wall.Material mat = Wall.Material.OakWood;
            room.CeilingHeight = 3;
            room.CeilingMaterial = mat;
            room.FloorMaterial = mat;
            switch (preset)
            {
                case RoomPreset.SmallSquare:
                    room.AddWall(new Wall(0, 0, 5, 0, mat));
                    room.AddWall(new Wall(5, 0, 5, 5, mat));
                    room.AddWall(new Wall(5, 5, 0, 5, mat));
                    room.AddWall(new Wall(0, 5, 0, 0, mat));
                    break;
                case RoomPreset.BigSquare:
                    room.AddWall(new Wall(0, 0, 15, 0, mat));
                    room.AddWall(new Wall(5, 0, 15, 15, mat));
                    room.AddWall(new Wall(15, 15, 0, 15, mat));
                    room.AddWall(new Wall(0, 15, 0, 0, mat));
                    break;
                case RoomPreset.SmallRect:
                    room.AddWall(new Wall(0, 0, 10, 0, mat));
                    room.AddWall(new Wall(10, 0, 10, 5, mat));
                    room.AddWall(new Wall(10, 5, 0, 5, mat));
                    room.AddWall(new Wall(0, 5, 0, 0, mat));
                    break;
                case RoomPreset.TShape:
                    room.AddWall(new Wall(0, 0, 3, 0, mat));
                    room.AddWall(new Wall(3, 0, 3, 5, mat));
                    room.AddWall(new Wall(3, 5, 9, 5, mat));
                    room.AddWall(new Wall(9, 5, 9, 10, mat));
                    room.AddWall(new Wall(9, 10, -6, 10, mat));
                    room.AddWall(new Wall(-6, 10, -6, 5, mat));
                    room.AddWall(new Wall(0, 5, 3, 5, mat));
                    room.AddWall(new Wall(3, 5, 0, 0, mat));
                    break;
                case RoomPreset.LShape:
                    room.AddWall(new Wall(0, 0, 3, 0, mat));
                    room.AddWall(new Wall(3, 0, 3, 5, mat));
                    room.AddWall(new Wall(3, 5, 9, 5, mat));
                    room.AddWall(new Wall(9, 5, 9, 10, mat));
                    room.AddWall(new Wall(9, 10, 0, 10, mat));
                    room.AddWall(new Wall(0, 10, 0, 0, mat));
                    break;
                case RoomPreset.Triangle:
                    room.AddWall(new Wall(0, 0, 7, 0, mat));
                    room.AddWall(new Wall(7, 0, 0, 5, mat));
                    room.AddWall(new Wall(0, 5, 0, 0, mat));
                    break;
                case RoomPreset.Trapezoid:
                    room.AddWall(new Wall(0, 0, 10, 0, mat));
                    room.AddWall(new Wall(10, 0, 7, 5, mat));
                    room.AddWall(new Wall(7, 5, 2, 5, mat));
                    room.AddWall(new Wall(2, 5, 0, 0, mat));
                    break;
                case RoomPreset.Pentagon:
                    room.AddWall(new Wall(0, 0, 4, 0, mat));
                    room.AddWall(new Wall(4, 0, 6, 4, mat));
                    room.AddWall(new Wall(6, 4, 2, 6, mat));
                    room.AddWall(new Wall(2, 6, -2, 4, mat));
                    room.AddWall(new Wall(-2, 4, 0, 0, mat));
                    break;
                default:
                    throw new ArgumentOutOfRangeException("preset");
            }
            return room;
        }

        /// <summary>
        /// Adds new listener to the room
        /// </summary>
        /// <param name="listener"></param>
        public ListenerPoint AddListener(ListenerPoint listener)
        {
            if (!ListenerNotExists(listener)) return null;
            if (!PointNotOnWall(listener)) throw new Exception("Trying to add listener on a wall");
            mListeners.Add(listener);
            return listener;
        }

        /// <summary>
        /// Adds new sound source to the room
        /// </summary>
        /// <param name="source"></param>
        public SoundPoint AddSource(SoundPoint source)
        {
            if(!SourceNotExists(source)) return null;
            if(!PointNotOnWall(source)) throw new Exception("Trying to add source on a wall");
            mSources.Add(source);
            return source;
        }

        /// <summary>
        /// Adds new wall to the room
        /// </summary>
        /// <param name="wall"></param>
        public Wall AddWall(Wall wall)
        {
            if(!WallNotExists(wall)) return null;
            mWalls.Add(wall);
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
                       
                        listener.Sound.Add(source.Sound, 0, 0,
                            source.Sound.MillesecondsToSamples((int) time),percentReduction);
                    }

                    //Primary reflections
                    Sound snd = new Sound();
                    foreach (Wall wall in Walls)
                    {
                        Point refPoint = Geometry.ReflectionPoint(wall, source, listener);
                        if (refPoint == null) continue;
                        if (
                            !(CheckPath(new Line(source, refPoint), wall) &&
                              CheckPath(new Line(refPoint, listener), wall))) continue;
                        snd.Copy(source.Sound);
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
                        double time = (Geometry.Distance(source, refPoint) + Geometry.Distance(refPoint, listener)) /
                                      airSSpeed * 1000;
                        listener.Sound.Add(snd, 0, 0, source.Sound.MillesecondsToSamples((int)time));
                        //snd = null;
                    }
                    //Ceiling reflection
                    snd.Copy(source.Sound);
                    double directDistance = Geometry.Distance(listener, source);
                    Wall ceiling = new Wall(0,0,directDistance,0,CeilingMaterial);
                    Point listenerTemp = new Point(0,CeilingHeight-listener.Altitude);
                    Point sourceImage = new Point(directDistance,source.Altitude-CeilingHeight);
                    double ceilingImageDistance = Geometry.Distance(listenerTemp, sourceImage);
                    double ceilingReductionCoefft = SoundReduction(ceilingImageDistance);
                    double ceilingAngle = Math.PI/2 -
                                          Geometry.Angle(ceiling, new Line(listenerTemp, sourceImage), false);
                    ceilingReductionCoefft *= ceiling.ReflectionCoefft(ceilingAngle);
                    if (listener.Directional)
                    {
                        ceilingReductionCoefft *= listener.GetReduction(new Line(source, listener));
                    }
                    snd.SetVolume(ceilingReductionCoefft,0);
                    snd.SetVolume(0,CeilingMaterial.Low,CeilingMaterial.Medium,CeilingMaterial.High);
                    double ceilingTime = ceilingImageDistance/airSSpeed*1000;
                    listener.Sound.Add(snd, 0, 0, source.Sound.MillesecondsToSamples((int)ceilingTime));


                    //Floor reflection
                    if (source.Altitude > 0 && listener.Altitude > 0)
                    {
                        snd.Copy(source.Sound);
                        Wall floor = new Wall(0, 0, directDistance, 0, FloorMaterial);
                        listenerTemp = new Point(0, listener.Altitude);
                        sourceImage = new Point(directDistance, -source.Altitude);
                        double floorImageDistance = Geometry.Distance(listenerTemp, sourceImage);
                        double floorReductionCoefft = SoundReduction(floorImageDistance);
                        double floorAngle = Math.PI/2 -
                                              Geometry.Angle(ceiling, new Line(listenerTemp, sourceImage), false);
                        floorReductionCoefft *= floor.ReflectionCoefft(floorAngle);
                        if (listener.Directional)
                        {
                            floorReductionCoefft *= listener.GetReduction(new Line(source, listener));
                        }
                        snd.SetVolume(floorReductionCoefft, 0);
                        snd.SetVolume(0, FloorMaterial.Low, FloorMaterial.Medium, FloorMaterial.High);
                        double floorTime = ceilingImageDistance/airSSpeed*1000;
                        listener.Sound.Add(snd, 0, 0, source.Sound.MillesecondsToSamples((int) floorTime));
                    }

                    //Secondary reflections
                    int imageDepth = 1;
                    var seenSources = new HashSet<Point>();
                    seenSources.Add(source);
                    Room copy = new Room(this);
                    foreach (Wall wall in Walls)
                    {
                        RoomImage image = new RoomImage(this, wall, source);
                        seenSources.Add(image.Source);
                    }
                    foreach (Wall wall in Walls)
                    {
                        imageDepth = 1;
                        //Console.WriteLine("Picked base Wall "+wall);
                        RoomImage firstImage = new RoomImage(this,wall,source);
                        
                        foreach (Wall imageWall in firstImage.ImageWalls)
                        {
                            copy.AddWall(imageWall);
                        }
                        RoomImage curImage = new RoomImage(this, wall, source);
                        bool flag = true;
                        int callBack = 0;
                        while (flag)
                        {
                            for (int i =1; i<curImage.ImageWalls.Length; i++)
                            {
                                if (!flag) break;
                                imageDepth++;
                                RoomImage newImage = new RoomImage(curImage,curImage.ImageWalls[i]);
                               
                                if (!seenSources.Contains(newImage.Source))
                                {

                                    seenSources.Add(newImage.Source);
                                    foreach (Wall imageWall in newImage.ImageWalls)
                                    {
                                        copy.AddWall(imageWall);
                                    }
                                    snd.Copy(source.Sound);
                                    double distance = newImage.GetTotalDistance(copy, listener);
                                    double percentReduction = SoundReduction(distance);
                                    double time = distance/airSSpeed*1000;

                                    Console.WriteLine("SOUND");
                                    //TODO: find intersecting walls and add sound
                                    Line directLine = new Line(newImage.Source,listener);
                                    if (listener.Directional)
                                    {
                                        percentReduction *= listener.GetReduction(directLine);
                                    }
                                    foreach (Wall wayWall in copy.IntersectingWalls(directLine))
                                    {
                                        double angle = Math.PI/2 - Geometry.Angle(directLine, wayWall, false);
                                        percentReduction *= wayWall.ReflectionCoefft(angle);
                                        snd.SetVolume(0,wayWall.WallMaterial.Low,wayWall.WallMaterial.Medium,wayWall.WallMaterial.High);
                                    }
                                    listener.Sound.Add(snd,0,0,source.Sound.MillesecondsToSamples((int)time),percentReduction);
                                }
                                if (imageDepth == imageMaxDepth)
                                {
                                    imageDepth--;
                                    for (int j = 1; j < newImage.ImageWalls.Length; j++)
                                    {
                                        if (!mWalls.Contains(newImage.ImageWalls[j]))
                                        {
                                            copy.RemoveWall(newImage.ImageWalls[j]);
                                        }
                                    }
                                    while (i == curImage.ImageWalls.Length - 1)
                                    {
                                        imageDepth--;
                                        for (int j = 1; j < curImage.ImageWalls.Length; j++)
                                        {
                                            if (!mWalls.Contains(curImage.ImageWalls[j]))
                                            {
                                                copy.RemoveWall(curImage.ImageWalls[j]);
                                            }
                                        }
                                        if (curImage.Parent == null)
                                        {
                                            flag = false;
                                            break;
                                        }
                                        curImage = curImage.Parent;
                                        i = callBack;
                                    }
                                }
                                else
                                {
                                    callBack = i;
                                    curImage = newImage;
                                    break;
                                    
                                }

                            }
                        }
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
            if (mSources.Count == 0 || mListeners.Count == 0) return false;
            if (Sources.Any(source => source.Altitude > CeilingHeight || source.Altitude < 0))
            {
                return false;
            }
            if (Listeners.Any(listener => listener.Altitude > CeilingHeight || listener.Altitude < 0))
            {
                return false;
            }
            List<Point> surface = OuterSurface();
            for (int i = 0; i < surface.Count - 1; i++)
            {
                Line line = new Line(surface[i],surface[i+1]);
                if (line.Start == line.End) continue;
                if (mWalls.All(wall => wall != line)) return false;
            }
            return true;
        }

        /// <summary>
        /// deletes certain listener
        /// </summary>
        /// <param name="listener"></param>
        public void RemoveListener(ListenerPoint listener)
        {
            mListeners.Remove(listener);
        }

        /// <summary>
        /// deletes certain sound source
        /// </summary>
        /// <param name="source"></param>
        public void RemoveSource(SoundPoint source)
        {
            mSources.Remove(source);
        }

        /// <summary>
        /// deletes certain wall
        /// </summary>
        /// <param name="wall"></param>
        public void RemoveWall(Wall wall)
        {
            mWalls.Remove(wall);
        }

        bool CheckPath(Line line)
        {
            return Walls.All(wall => Geometry.SegmentIntersection(wall, line, false) == null);
        }

        bool CheckPath(Line line, Wall exclude)
        {
            return Walls.All(wall => Geometry.SegmentIntersection(wall, line, false) == null || wall == exclude); 
        }

        IEnumerable<Wall> IntersectingWalls(Line line)
        {
            return Walls.Where(wall => line.GetIntersection(wall, false)!=null);
        }

        bool ListenerNotExists(Point testlistener)
        {
            if(testlistener == null) throw new ArgumentNullException();
            return mListeners.All(listener => testlistener != listener);
        }

        /// <summary>
        /// Calculates outer surface points of the room using Jarvis algorythm
        /// </summary>
        /// <returns>List of points in the outer surface polygon</returns>
        List<Point> OuterSurface()
        {
            List<Point> points = new List<Point>();
            List<Point> surface = new List<Point>();
            points.AddRange(mSources);
            points.AddRange(mListeners);
            foreach (Wall wall in mWalls)
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
            return mWalls.All(wall => !(Geometry.OnLine(wall, point) && Geometry.OnSegment(wall, point)));
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
            return mSources.All(source => testsource != source);
        }

        bool WallNotExists(Wall testwall)
        {
            if(testwall == null) throw new ArgumentNullException();
            return mWalls.All(wall => testwall != wall);
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

        public Point(Point point)
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
                
                mX = value;
            }
            get { return mX; }
        }
        public double Y
        {
            set
            {
                
                mY = value;
            }
            get { return mY; }
        }

        public static Point Vector(Point start, Point end)
        {
            return new Point(end.X-start.X,end.Y-start.Y);
        }

        public static Point operator +(Point first, Point second)
        {
            return new Point(first.X + second.X,first.Y + second.Y);
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

        public static Point operator -(Point first, Point second)
        {
            return new Point(first.X - second.X, first.Y - second.Y);
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

        public override string ToString()
        {
            return Start + " --> " + End;
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
            return (Start==other.Start && End == other.End) || (Start == other.End && End == other.Start);
        }
    }
    /// <summary>
    /// Point with associated sound 
    /// </summary>
    [Serializable]
    public class SoundPoint:Point,IRoomObject
    {
        [NonSerialized] private Sound mSound;

        public SoundPoint(Point p, double alt = 0) : base(p.X, p.Y)
        {
            Altitude = alt;
        }
        public SoundPoint(double x, double y, double alt = 0) : base(x, y)
        {
            Altitude = alt;
        }

        public Sound Sound
        {
            set { mSound = value; }
            get { return mSound; }
        }

        public double Altitude { set; get; }
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

        public ListenerPoint(Point p, Line direction, DirectionalDecrease decreaseFunction, double alt = 0) : base(p,alt)
        {
            mDirection = direction;
            mDecreaseFunction = decreaseFunction;
            Directional = true;

        }
        public ListenerPoint(double x, double y, Line direction, DirectionalDecrease decreaseFunction, double alt = 0) : base(x, y,alt)
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
        public ListenerPoint(double x, double y, double alt = 0)
            : base(x, y, alt)
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

            Material(double dens, double sspeed, double low = 0, double medium = 0, double high = 0)
            {
                Density = dens;
                SoundSpeed = sspeed;
                Low = low;
                Medium = medium;
                High = high;
            }

            public double Density { set; get; }
            public double High { set; get; }
            public double Low { set; get; }
            public double Medium { set; get; }
            public double SoundSpeed { set; get; }
        }
    }
    /// <summary>
    /// Image of room for calculating complex reflections
    /// </summary>
    public class RoomImage
    {
        
        public Wall[] ImageWalls;
        public RoomImage Parent;
        public Point Source;


        public RoomImage(Room room, Wall wall, Point source)
        {
            if(!room.Walls.Contains(wall) || !room.Sources.Contains(source)) throw new ArgumentException("Room elements are non-existent");
            ImageWalls = new Wall[room.Walls.Count];
            ImageWalls[0] = wall;
           
            int i = 1;
            foreach (Wall roomWall in room.Walls)
            {
                if (roomWall == wall) continue;
                Point startVector = Geometry.ParallelProjection(wall, roomWall.Start,true);
                startVector.X -= roomWall.Start.X;
                startVector.Y -= roomWall.Start.Y;

                Point endVector = Geometry.ParallelProjection(wall, roomWall.End, true);
                endVector.X -= roomWall.End.X;
                endVector.Y -= roomWall.End.Y;

                Point newStart = new Point(roomWall.Start.X+2*startVector.X,roomWall.Start.Y+2*startVector.Y);
                Point newEnd = new Point(roomWall.End.X + 2 * endVector.X, roomWall.End.Y + 2 * endVector.Y);
                ImageWalls[i] = new Wall(newStart,newEnd,roomWall.WallMaterial);
                i++;
            }
            Point sourceVector = Geometry.ParallelProjection(wall, source, true);
            sourceVector.X -= source.X;
            sourceVector.Y -= source.Y;
            Source = new Point(source.X+2*sourceVector.X,source.Y+2*sourceVector.Y);
          
        }

        public RoomImage(RoomImage image, Wall wall)
        {
            if(!image.ImageWalls.Contains(wall)) throw new ArgumentException("Room elements are non-existent");
            Parent = image;
            ImageWalls = new Wall[image.ImageWalls.Length];
            ImageWalls[0] = wall;
            int i = 1;
            foreach (Wall roomWall in image.ImageWalls)
            {
                if (roomWall == wall) continue;
                Point startVector = Geometry.ParallelProjection(wall, roomWall.Start, true);
                startVector.X -= roomWall.Start.X;
                startVector.Y -= roomWall.Start.Y;

                Point endVector = Geometry.ParallelProjection(wall, roomWall.End, true);
                endVector.X -= roomWall.End.X;
                endVector.Y -= roomWall.End.Y;

                Point newStart = new Point(roomWall.Start.X + 2 * startVector.X, roomWall.Start.Y + 2 * startVector.Y);
                Point newEnd = new Point(roomWall.End.X + 2 * endVector.X, roomWall.End.Y + 2 * endVector.Y);
                ImageWalls[i] = new Wall(newStart, newEnd, roomWall.WallMaterial);
                i++;
            }
            Point sourceVector = Geometry.ParallelProjection(wall, image.Source, true);
            sourceVector.X -= image.Source.X;
            sourceVector.Y -= image.Source.Y;
            Source = new Point(image.Source.X + 2 * sourceVector.X, image.Source.Y + 2 * sourceVector.Y);
         
        }
        /// <summary>
        /// Distance between image of source and listener
        /// </summary>
        /// <param name="room">Room where to find listener</param>
        /// <param name="listener">Listener to use</param>
        /// <returns></returns>
        public double GetTotalDistance(Room room, ListenerPoint listener)
        {
            if(!room.Listeners.Contains(listener)) throw new ArgumentException("Room elements are non-existent");
            return Geometry.Distance(listener, Source);
        }
    }
}
