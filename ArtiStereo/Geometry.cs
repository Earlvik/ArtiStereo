using System;

namespace Earlvik.ArtiStereo
{
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
                return new Point(first.Start);
            }
            if (Math.Abs(d2) < eps && (OnSegment(second, first.End) || wholeLine))
            {
                return new Point(first.End);
            }
            if (Math.Abs(d3) < eps && (OnSegment(first, second.Start) || wholeLine))
            {
                return new Point(second.Start);
            }
            if (Math.Abs(d4) < eps && (OnSegment(first, second.End) || wholeLine))
            {
                return new Point(second.End);
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