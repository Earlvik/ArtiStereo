using System;
using System.Collections.Generic;
using System.Linq;


namespace Earlvik.ArtiStereo
{
   
    /// <summary>
    /// Class that stores and provides information about premises where to model sound waves
    /// </summary>
    [Serializable]
   public class Room
    {
        //lists of room elements
        private List<Wall> _walls;
        private List<Point> _sources;
        private List<Point> _listeners;
        //value for calculating reflection from ceiling
        public double CeilingHeight { set; get; }

        public Room()
        {
            _walls = new List<Wall>();
            _sources = new List<Point>();
            _listeners = new List<Point>();
        }
        /// <summary>
        /// Adds new wall to the room
        /// </summary>
        /// <param name="wall"></param>
        public void AddWall(Wall wall)
        {
            if(!WallNotExists(wall)) throw new Exception("Trying to add already existent wall");
            _walls.Add(wall);
        }
        /// <summary>
        /// Adds new sound source to the room
        /// </summary>
        /// <param name="source"></param>
        public void AddSource(Point source)
        {
            if(!SourceNotExists(source)) throw new Exception("Trying to add already existent source");
            if(!PointNotOnWall(source)) throw new Exception("Trying to add source on a wall");
            _sources.Add(source);
        }
        /// <summary>
        /// Adds new listener to the room
        /// </summary>
        /// <param name="listener"></param>
        public void AddListener(Point listener)
        {
            if (!ListenerNotExists(listener)) throw new Exception("Trying to add already existent listener");
            if (!PointNotOnWall(listener)) throw new Exception("Trying to add listener on a wall");
            _listeners.Add(listener);
        }
        /// <summary>
        /// deletes certain wall
        /// </summary>
        /// <param name="wall"></param>
        public void RemoveWall(Wall wall)
        {
            _walls.Remove(wall);
        }
        /// <summary>
        /// deletes certain sound source
        /// </summary>
        /// <param name="source"></param>
        public void RemoveSource(Point source)
        {
            _sources.Remove(source);
        }
        /// <summary>
        /// deletes certain listener
        /// </summary>
        /// <param name="listener"></param>
        public void RemoveListener(Point listener)
        {
            _listeners.Remove(listener);
        }
        //Public accessors to the lists
        public List<Wall> Walls { get { return _walls; } }
        public List<Point> Sources { get { return _sources; } }
        public List<Point> Listeners { get { return _listeners; } }
        //Methods checking correctness of elements being added
        bool WallNotExists(Wall testwall)
        {
            if(testwall == null) throw new ArgumentNullException();
            return _walls.All(wall => testwall != wall);
        }

        bool SourceNotExists(Point testsource)
        {
            if(testsource == null) throw new ArgumentNullException();
           return _sources.All(source => testsource != source);
        }

        bool ListenerNotExists(Point testlistener)
        {
            if(testlistener == null) throw new ArgumentNullException();
            return _listeners.All(listener => testlistener != listener);
        }

        bool PointNotOnWall(Point point)
        {
            if(point == null) throw new ArgumentNullException();
            return _walls.All(wall => !(Geometry.OnLine(wall, point) && Geometry.OnSegment(wall, point)));
        }
        /// <summary>
        /// Checking validness of the room, germeticness etc.
        /// </summary>
        /// <returns></returns>
        public bool IsValid()
        {
            List<Point> surface = OuterSurface();
            for (int i = 0; i < surface.Count - 1; i++)
            {
                Line line = new Line(surface[i],surface[i+1]);
                if (_walls.All(wall => wall != line)) return false;
            }
            return true;
        }
        /// <summary>
        /// Calculates outer surface points of the room
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
    }

    [Serializable]
    public class Point
    {
        protected bool Equals(Point other)
        {
            return _x.Equals(other._x) && _y.Equals(other._y);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Point) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (_x.GetHashCode()*397) ^ _y.GetHashCode();
            }
        }

        private double _x ;
        private double _y;
        public double X
        {
            set
            {
                if(value < 0) throw new ArgumentException(" Point coordinate X should be non-negative, but was "+value);
                _x = value;
            }
            get { return _x; }
        }
        public double Y
        {
            set
            {
                if (value < 0) throw new ArgumentException(" Point coordinate Y should be non-negative, but was " + value);
                _y = value;
            }
            get { return _y; }
        }
        public Point(double x, double y)
        {
            X = x;
            Y = y;
        }

        public Point()
        {
            X = 0;
            Y = 0;
        }
        public override string ToString()
        {
            return "( " + X + " ; " + Y + " )";
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
    }

    [Serializable]
   public class Line
    {
        protected bool Equals(Line other)
        {
            return Equals(Start, other.Start) && Equals(End, other.End);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Line) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Start != null ? Start.GetHashCode() : 0)*397) ^ (End != null ? End.GetHashCode() : 0);
            }
        }

        public Point Start { set; get; }
        public Point End { set; get; }
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

        public Point GetIntersection(Line second, bool wholeLine)
        {
            if (second is Line) return Geometry.SegmentIntersection(this,second as Line, wholeLine);
            throw new NotImplementedException("Here will be intersection for a segment and an arc or maybe not");
        }

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
    }

    [Serializable]
    public class Wall:Line
    {
        public Material WallMaterial { set; get; }

        public Wall(Point start, Point end, Material material) : base(start, end)
        {
            WallMaterial = material;
        }

        public Wall(double xstart, double ystart, double xend, double yend, Material material)
            : base(xstart, ystart, xend, yend)
        {
            WallMaterial = material;
        }

       public double ReflectionCoefft(double angle)
       {
           double densityPart = (WallMaterial.Density/Material.Air.Density)*Math.Cos(angle);
           double speedPart =
               Math.Sqrt((Material.Air.SoundSpeed/WallMaterial.SoundSpeed)*
                         (Material.Air.SoundSpeed/WallMaterial.SoundSpeed) - Math.Sin(angle)*Math.Sin(angle));
           if (Geometry.EqualDouble(densityPart+speedPart,0) ) return 0;
           return Math.Abs((densityPart - speedPart)/(densityPart + speedPart));
       }

        public struct Material
        {
            public double Density;
            public double SoundSpeed;
            Material(double dens, double sspeed)
            {
                Density = dens;
                SoundSpeed = sspeed;
            }
            public static readonly Material OakWood = new Material(720,4000); 
            public static readonly Material Air = new Material(1.20, 4000);
            public static readonly Material Glass = new Material(2500, 5000);
            public static readonly Material Granite = new Material(2800, 3950);
            public static readonly Material Brick = new Material(1800,3480);
            public static readonly Material Rubber = new Material(1050, 54);
           
                
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
            private const double Eps = 0.0000001;
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

                return (Math.Min(xs, xe) <= xp) && (xp <= Math.Max(xs, xe)) &&
                       (Math.Min(ys, ye) <= yp) && (yp <= Math.Max(ys, ye));
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
                if (Math.Abs(d1) < Eps && (OnSegment(second, first.Start) || wholeLine))
                {
                    return first.Start;
                }
                if (Math.Abs(d2) < Eps && (OnSegment(second, first.End) || wholeLine))
                {
                    return first.End;
                }
                if (Math.Abs(d3) < Eps && (OnSegment(first, second.Start) || wholeLine))
                {
                    return second.Start;
                }
                if (Math.Abs(d4) < Eps && (OnSegment(first, second.End) || wholeLine))
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
                if (Math.Abs((y4 - y3) * (x2 - x1) - (x4 - x3) * (y2 - y1)) < Eps) return null;
                double coefftA = ((x4 - x3) * (y1 - y3) - (y4 - y3) * (x1 - x3)) / ((y4 - y3) * (x2 - x1) - (x4 - x3) * (y2 - y1));
                double coefftB = ((x2 - x1) * (y1 - y3) - (y2 - y1) * (x1 - x3)) / ((y4 - y3) * (x2 - x1) - (x4 - x3) * (y2 - y1));
                if (wholeLine || (d2 * d1 < 0 && d3 * d4 < 0))
                {
                    return new Point(x1 + coefftA * (x2 - x1), y1 + coefftA * (y2 - y1));
                }

                return null;

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
            static double Distance(Point point, Line line)
            {
                //Line equation parameters
                double A = line.Start.Y - line.End.Y;
                double B = line.End.X - line.Start.X;
                double C = line.Start.X * line.End.Y - line.Start.Y * line.End.X;

                double denom = Math.Sqrt(A * A + B * B);
                if (Math.Abs(denom) < Eps) throw new Exception("the line equation is illegal");
                return Math.Abs(A * point.X + B * point.Y + C) / denom;
            }
            /// <summary>
            /// Calculates an angle value between two lines
            /// </summary>
            /// <param name="a"></param>
            /// <param name="b"></param>
            /// <returns>angle in radians</returns>
            public static double Angle(Line a, Line b)
            {
                //line equations parameters
                double A1 = a.Start.Y - a.End.Y;
                double B1 = a.End.X - a.Start.X;
                //double C1 = a.Start.X*a.End.Y - a.Start.Y*a.End.X;

                double A2 = b.Start.Y - b.End.Y;
                double B2 = b.End.X - b.Start.X;
                // double C2 = b.Start.X * b.End.Y - b.Start.Y * b.End.X;

                //parallel and peprendicular lines
                if ((Math.Abs(B1) < Eps && Math.Abs(B2) < Eps) || (Math.Abs(A1) < Eps && Math.Abs(A2) < Eps) || Math.Abs((A1 / A2) - (B1 / B2)) < Eps) return 0;
                if (Math.Abs(A1 * A2 + B1 * B2) < Eps) return Math.PI / 2;

                //other lines
                double denom = Math.Sqrt((A1 * A1 + B1 * B1) * (A2 * A2 + B2 * B2));
                return Math.Acos(Math.Abs(A1 * A2 + B1 * B2) / denom);
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
                double angle = Angle(wall, direct);
                if (Math.Abs(angle) < Eps) //the wall is parallel with the direct line
                {
                    return ParallelProjection(wall, new Point((source.X + listener.X) / 2, (source.Y + listener.Y) / 2));
                }

                Point intersection = wall.GetIntersection(direct, true);
                if (OnSegment(direct, intersection)) return null;//the wall is between the source and the listener

                if (Math.Abs(angle - Math.PI / 2) < Eps) return intersection;

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
                if (Math.Abs(wall.Start.X - wall.End.X) < Eps)
                {
                    farParallel = new Line(farPoint, new Point(farPoint.X, 0));
                }
                else
                {
                    double wallAngleCoefft = (wall.Start.Y - wall.End.Y) / (wall.Start.X - wall.End.X);
                    Point secondPoint = new Point(0, farPoint.Y + wallAngleCoefft * (-farPoint.X));
                    farParallel = new Line(farPoint, secondPoint);

                }

                Point endOfFarProjection = ParallelProjection(farParallel, nearPoint);

                double similarityCoefft = (shortDist) / (longDist - shortDist);

                // Point on far parallel line which will be projected
                Point farResultProjection = new Point((farPoint.X + similarityCoefft * endOfFarProjection.X) / (1 + similarityCoefft), (farPoint.Y + similarityCoefft * endOfFarProjection.Y) / (1 + similarityCoefft));

                Point result = ParallelProjection(wall, farResultProjection);

                return result;

            }
            /// <summary>
            /// Calculates the parallel projection of a point to a line  
            /// </summary>
            /// <param name="toLine"></param>
            /// <param name="point"></param>
            /// <returns></returns>
            public static Point ParallelProjection(Line toLine, Point point)
            {
                //double A1 = fromLine.Start.Y - fromLine.End.Y;
                //double B1 = fromLine.End.X - fromLine.Start.X;
                //double C1 = fromLine.Start.X*fromLine.End.Y - fromLine.Start.Y*fromLine.End.X;

                double A2 = toLine.Start.Y - toLine.End.Y;
                double B2 = toLine.End.X - toLine.Start.X;
                double C2 = toLine.Start.X * toLine.End.Y - toLine.Start.Y * toLine.End.X;

                if (Math.Abs(B2) < Eps) return new Point(-C2 / A2, point.Y);

                double angleCoefft = A2 / (-B2);
                //double offset = C1/(-B1);

                Point start = new Point(0, point.Y + (1 / angleCoefft) * point.X);
                Point end = new Point(1, point.Y + (1 / angleCoefft) * (point.X - 1));
                Line normal = new Line(start, end);
                Point intersection = toLine.GetIntersection(normal, true);
                return OnSegment(toLine, intersection) ? intersection : null;
            }

            static public bool EqualDouble(double a, double b)
            {
                return Math.Abs(a - b) < Eps;
            }

            static public bool OnLine(Line line, Point point)
            {
                return EqualDouble((point.X - line.Start.X)/(line.End.X - line.Start.X), (point.Y - line.Start.Y)/(line.End.Y - line.Start.Y));
            }

        }
}
