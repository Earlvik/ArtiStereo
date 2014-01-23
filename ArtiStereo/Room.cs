using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Earlvik.ArtiStereo
{
    public interface ILinear
    {
        Point GetIntersection(ILinear second);
    }

    [Serializable]
   public class Room
    {
    }

    [Serializable]
    public class Point
    {
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
    }

    [Serializable]
   public class Line:ILinear
    {
        private const double Eps = 0.00001;

        static double VectorMultiplication(Line first, Line second)
        {
            double x1 = first.End.X - first.Start.X;
            double y1 = first.End.Y - first.Start.Y;
            double x2 = second.End.X - second.Start.X;
            double y2 = second.End.Y - second.Start.Y;

            return x1*y2 - x2*y1;
        }
        static public double Direction(Line line, Point point)
        {
          Line toPoint = new Line(line.Start,point);
            return VectorMultiplication(line, toPoint);
        }

        static bool OnSegment(Line line, Point point)
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

        Point SegmentIntersection(Line second)
        {
            double d1 = Direction(second, this.Start);
            double d2 = Direction(second, this.End);
            double d3 = Direction(this, second.Start);
            double d4 = Direction(this, second.End);

            if (Math.Abs(d1) < Eps && OnSegment(second, this.Start))
            {
                return this.Start;
            }
            if(Math.Abs(d2)< Eps && OnSegment(second,this.End))
            {
                return this.End;
            }
            if (Math.Abs(d3) < Eps && OnSegment(this, second.Start))
            {
                return second.Start;
            }
            if (Math.Abs(d4) < Eps && OnSegment(this, second.End))
            {
                return second.End;
            }

            if ((d2*d1 < 0) && (d3*d4 < 0))
            {
                double x1 = Start.X,
                       x2 = End.X,
                       x3 = second.Start.X,
                       x4 = second.End.X,
                       y1 = Start.Y,
                       y2 = End.Y,
                       y3 = second.Start.Y,
                       y4 = second.End.Y;
                double coefftA = ((x4 - x3) * (y1 - y3) - (y4 - y3) * (x1 - x3)) / ((y4 - y3) * (x2 - x1) - (x4 - x3) * (y2 - y1));
               // double coefftB = ((x2 - x1) * (y1 - y3) - (y2 - y1) * (x1 - x3)) / ((y4 - y3) * (x2 - x1) - (x4 - x3) * (y2 - y1));
                return new Point(x1 + coefftA*(x2-x1),y1+coefftA*(y2-y1));
            }
            return null;

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

        public Point GetIntersection(ILinear second)
        {
            if (second is Line) return SegmentIntersection(second as Line);
            throw new NotImplementedException("Here will be intersection for a segment and an arc");
        }
    }
}
