using System;

namespace Earlvik.ArtiStereo
{
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
}