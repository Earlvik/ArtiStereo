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
        public int ImageMaxDepth { set; get; } 
        
        //lists of room elements
        private readonly List<ListenerPoint> mListeners;
        private readonly List<SoundPoint> mSources;
        private readonly List<Wall> mWalls;

        public Room()
        {
            ImageMaxDepth = 3;
            mWalls = new List<Wall>();
            mSources = new List<SoundPoint>();
            mListeners = new List<ListenerPoint>();
        }

        public Room(Room room)
        {
            ImageMaxDepth = 3;
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
        public event EventHandler CalculationProgress;

// ReSharper disable once InconsistentNaming
        public enum RoomPreset {SmallSquare,BigSquare,SmallRect,LShape, Triangle,Trapezoid,Pentagon}

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
                    room.AddWall(new Wall(15, 0, 15, 15, mat));
                    room.AddWall(new Wall(15, 15, 0, 15, mat));
                    room.AddWall(new Wall(0, 15, 0, 0, mat));
                    break;
                case RoomPreset.SmallRect:
                    room.AddWall(new Wall(0, 0, 10, 0, mat));
                    room.AddWall(new Wall(10, 0, 10, 5, mat));
                    room.AddWall(new Wall(10, 5, 0, 5, mat));
                    room.AddWall(new Wall(0, 5, 0, 0, mat));
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
            Sound.Channel channel = Sound.Channel.FrontLeft;
            for (int i = 0; i < 9; i++)
            {
                if (mListeners.All(x => x.Channel != (Sound.Channel) i))
                {
                    channel = (Sound.Channel) i;
                    break;
                }
            }
            listener.Channel = channel;
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
        public void CalculateSound(double reflectionsVolume = 1)
        {
            
            double airSSpeed = Wall.Material.Air.SoundSpeed;
            reflectionsVolume /= mSources.Count;
            foreach (SoundPoint source in Sources)
            {
                //double distance = Listeners.Min(x => Geometry.Distance(source, x));

                //double timeOffset = distance/airSSpeed*1000;
                double directSoundLevel = SoundReduction(Listeners.Min(x=>Geometry.Distance(source,x)));
                foreach (ListenerPoint listener in Listeners)
                {
                    listener.Sound = null;
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
                        if (CalculationProgress != null)
                        {
                            CalculationProgress(this, null);
                        }
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
                                                                 Geometry.Distance(refPoint, listener))) * refCoefft*reflectionsVolume;
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
                        if (CalculationProgress != null)
                        {
                            CalculationProgress(this, null);
                        }
                        //snd = null;
                    }
                    //Ceiling reflection
                    snd.Copy(source.Sound);
                    double directDistance = Geometry.Distance(listener, source);
                    Wall ceiling = new Wall(0,0,directDistance,0,CeilingMaterial);
                    Point listenerTemp = new Point(0,CeilingHeight-listener.Altitude);
                    Point sourceImage = new Point(directDistance,source.Altitude-CeilingHeight);
                    double ceilingImageDistance = Geometry.Distance(listenerTemp, sourceImage);
                    double ceilingReductionCoefft = SoundReduction(ceilingImageDistance)*reflectionsVolume;
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
                    if (CalculationProgress != null)
                    {
                        CalculationProgress(this, null);
                    }


                    //Floor reflection
                    if (source.Altitude > 0 && listener.Altitude > 0)
                    {
                        snd.Copy(source.Sound);
                        Wall floor = new Wall(0, 0, directDistance, 0, FloorMaterial);
                        listenerTemp = new Point(0, listener.Altitude);
                        sourceImage = new Point(directDistance, -source.Altitude);
                        double floorImageDistance = Geometry.Distance(listenerTemp, sourceImage);
                        double floorReductionCoefft = SoundReduction(floorImageDistance)*reflectionsVolume;
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
                        if (CalculationProgress != null)
                        {
                            CalculationProgress(this, null);
                        }
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
                        if (CalculationProgress != null)
                        {
                            CalculationProgress(this, null);
                        }
                        RoomImage image = new RoomImage(this, wall, source);
                        for (int i = 1; i < image.ImageWalls.Length; i++)
                        {
                            RecursiveImageCalculation(image,image.ImageWalls[i],imageDepth,seenSources,copy,listener,source,snd,reflectionsVolume);
                        }
                    }
                }
                

               

            }
        }

        private void RecursiveImageCalculation(RoomImage image, Wall wall, int imageDepth, HashSet<Point> seenSources, Room copy, ListenerPoint listener, SoundPoint source, Sound snd, double reflectionsVolume)
        {
            imageDepth++;
            double airSSpeed = Wall.Material.Air.SoundSpeed;
            RoomImage newImage = new RoomImage(image,wall);
            if (!seenSources.Contains(newImage.Source))
            { 
                seenSources.Add(newImage.Source);
                foreach (Wall imageWall in newImage.ImageWalls)
                {
                    copy.AddWall(imageWall);
                }
                snd.Copy(source.Sound);
                double distance = newImage.GetTotalDistance(copy, listener);
                double percentReduction = SoundReduction(distance)*reflectionsVolume;
                double time = distance / airSSpeed * 1000;

                Console.WriteLine("SOUND");
                //TODO: find intersecting walls and add sound
                Line directLine = new Line(newImage.Source, listener);
                if (listener.Directional)
                {
                    percentReduction *= listener.GetReduction(directLine);
                }
                double low = 0, medium = 0, high = 0;
                foreach (Wall wayWall in copy.IntersectingWalls(directLine))
                {
                    double angle = Math.PI / 2 - Geometry.Angle(directLine, wayWall, false);
                    percentReduction *= wayWall.ReflectionCoefft(angle);
                    low += (1 - low)*wayWall.WallMaterial.Low;
                    medium += (1 - medium)*wayWall.WallMaterial.Medium;
                    high += (1 - high)*wayWall.WallMaterial.High;
                }
                snd.SetVolume(0,low,medium,high);
                listener.Sound.Add(snd, 0, 0, source.Sound.MillesecondsToSamples((int)time),
                    percentReduction);
                
            }
            if (imageDepth == ImageMaxDepth)
            {
                for (int j = 1; j < newImage.ImageWalls.Length; j++)
                {
                    if (!mWalls.Contains(newImage.ImageWalls[j]))
                    {
                        copy.RemoveWall(newImage.ImageWalls[j]);
                    }
                }
                return;
            }
            for (int i = 1; i < newImage.ImageWalls.Length;i++)
            {
                RecursiveImageCalculation(newImage,newImage.ImageWalls[i],imageDepth,seenSources,copy,listener,source,snd,reflectionsVolume);
            }

        }

        public Sound GetSoundFromListeners()
        {
            if(!IsValid() || mListeners.Any(x => x.Sound == null)) throw new ArgumentException("The room is not valid or the sound is not yet calculated");
            int channels = mListeners.Max(x=>(int)x.Channel)+1;
            int discretionRate = mListeners[0].Sound.DiscretionRate;
            int bitsPerSample = mListeners[0].Sound.BitsPerSample;
            Sound sound = new Sound(channels,discretionRate,bitsPerSample);
            foreach (ListenerPoint listener in mListeners)
            {
                int chanNum = (int)listener.Channel;
                sound.Add(listener.Sound,0,chanNum,0);
            }
            return sound;
        }

        /// <summary>
        /// Checking validness of the room, germeticness etc.
        /// </summary>
        /// <returns></returns>
        public bool IsValid(out string issues)
        {
            issues = "";
            if (mSources.Count == 0 || mListeners.Count == 0
                || mListeners.Count > 9)
            {
                issues = "Room should contain sources and listeners, but no more than 9 listeners";
                return false;
            }
            for (int i = 0; i < 9; i++)
            {
                Sound.Channel channel = (Sound.Channel)i;
                if (mListeners.Count(x => x.Channel == channel) > 1)
                {
                    issues = "Listeners should not have same channel labels";
                    return false;
                }
            }
            if (mSources.Any(x => x.Sound == null))
            {
                issues = "Sound is not assigned to one of sources";
                return false;
            }
            int discretionRate = mSources[0].Sound.DiscretionRate;
            int bitsPerSample = mSources[0].Sound.BitsPerSample;
            if (!mSources.All(x => x.Sound.DiscretionRate == discretionRate && x.Sound.BitsPerSample == bitsPerSample))
            {
                issues = "All sources should have sound with same parameters";
                return false;
            }
            if (Sources.Any(source => source.Altitude > CeilingHeight || source.Altitude < 0))
            {
                issues = "All of sources should have positive altitude less than ceiling height";
                return false;
            }
            if (Listeners.Any(listener => listener.Altitude > CeilingHeight || listener.Altitude < 0))
            {
                issues = "All of listeners should have positive altitude less than ceiling height";
                return false;
            }
            List<Point> surface = OuterSurface();
            for (int i = 0; i < surface.Count - 1; i++)
            {
                Line line = new Line(surface[i],surface[i+1]);
                if (line.Start == line.End) continue;
                if (mWalls.All(wall => wall != line))
                {
                    issues = "Room should have closed convex shape";
                    return false;
                }
            }
            return true;
        }

        public bool IsValid()
        {
            string s;
            return IsValid(out s);
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
                    pretendent = points.First(point => surface.IndexOf(point) == -1);
                }
                for (int i = 0; i < points.Count; i++)
                {
                    if (points[i] == current) continue;
                    double vecprod = Geometry.VectorMultiplication(new Line(current, pretendent),
                        new Line(current, points[i]));
                    if (Geometry.EqualDouble(vecprod, 0))
                    {
                        if (Geometry.Distance(pretendent, current) > Geometry.Distance(points[i], current) && (!surface.Contains(points[i]) || points[i]==lefttop))
                        {
                            pretendent = points[i];
                        }
                        continue;
                    }
                    if (vecprod < 0 && (!surface.Contains(points[i]) || points[i] == lefttop))
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
}
