using System;

namespace Earlvik.ArtiStereo
{
    /// <summary>
    /// Point with associated sound and microphone directional decrease function
    /// </summary>
    [Serializable]
    public class ListenerPoint:SoundPoint
    {
        public delegate double DirectionalDecrease(Line incomingRay, Line micDirection);

        public Sound.Channel Channel { set; get; }
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
            Channel = Sound.Channel.FrontLeft;

        }
        public ListenerPoint(double x, double y, Line direction, DirectionalDecrease decreaseFunction, double alt = 0) : base(x, y,alt)
        {
            mDirection = direction;
            mDecreaseFunction = decreaseFunction;
            Directional = true;
            Channel = Sound.Channel.FrontLeft;
        }

        public ListenerPoint(Point p)
            : base(p)
        {
            Directional = false;
            Channel = Sound.Channel.FrontLeft;
        }
        public ListenerPoint(double x, double y, double alt = 0)
            : base(x, y, alt)
        {
            Directional = false;
            Channel = Sound.Channel.FrontLeft;
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
}