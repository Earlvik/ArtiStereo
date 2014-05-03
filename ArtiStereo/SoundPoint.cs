using System;

namespace Earlvik.ArtiStereo
{
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
}