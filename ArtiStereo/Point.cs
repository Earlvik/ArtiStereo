using System;

namespace Earlvik.ArtiStereo
{
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
            return Geometry.EqualDouble(mX,other.mX) && Geometry.EqualDouble(mY,other.mY);
        }
    }
}