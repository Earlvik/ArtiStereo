using System;
using System.Linq;

namespace Earlvik.ArtiStereo
{
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