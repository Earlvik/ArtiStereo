using System;
using System.Net.Mime;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AS = Earlvik.ArtiStereo;

namespace ArtiStereoTests
{
    [TestClass]
    public class GeometryTests
    {
        private const double Eps = 0.0000001;
        [TestMethod]
        public void InterSectionTest()
        {
            AS.Line a = new AS.Line(1,1,3,5);
            AS.Line b= new AS.Line(1,5,3,1);
            AS.Point p = a.GetIntersection(b,false);
            Assert.IsTrue(p!=null && Math.Abs(p.X)-2<Eps && Math.Abs(p.Y)-3<Eps,"The intersection point had to be (2,3) but was "+p);
        }

        [TestMethod]
        public void ParallelTest()
        {
            AS.Line a = new AS.Line(0,2,5,2);
            AS.Line b = new AS.Line(0,3,6,3);
            Assert.IsNull(a.GetIntersection(b,false),"There must be no intersections, but was "+a.GetIntersection(b,false));
        }

        [TestMethod]
        public void NonParallelNonIntersectingTest()
        {
            AS.Line a = new AS.Line(0,2,5,2);
            AS.Line b = new AS.Line(7,0,7,5);
            Assert.IsNull(a.GetIntersection(b,false),"There must be no intersections, but was "+a.GetIntersection(b,false));
        }

        [TestMethod]
        public void NonParallelLineInterectingTest()
        {
            AS.Line a = new AS.Line(0, 2, 5, 2);
            AS.Line b = new AS.Line(7, 0, 7, 5);
            AS.Point p = a.GetIntersection(b, true);
            Assert.IsTrue(p != null && Math.Abs(p.X) - 7 < Eps && Math.Abs(p.Y) - 2 < Eps, "The intersection point had to be (7,2) but was " + p);
        } 

        [TestMethod]
        public void TouchingTest()
        {
            AS.Line a = new AS.Line(0,0,5,0);
            AS.Line b = new AS.Line(2,0,2,5);
            AS.Point p = a.GetIntersection(b,false);
            Assert.IsTrue(p != null && Math.Abs(p.X - 2) < Eps && Math.Abs(p.Y) < Eps, "The intersection point had to be (2,0) but was " + p);
        }

        [TestMethod]
        public void PointDistanceTest()
        {
            AS.Point a = new AS.Point(1, 1);
            AS.Point b = new AS.Point(5,2.5);
            Assert.IsTrue(Math.Abs(AS.Geometry.Distance(a,b) - Math.Sqrt(18.25)) < Eps);
        }

        [TestMethod]
        public void AngleTest1()
        {
            AS.Line a = new AS.Line(1,2,2,6);
            AS.Line b = new AS.Line(2,2,6,1);
            double angle = AS.Geometry.Angle(a, b,false);
            Assert.IsTrue(Math.Abs(angle - Math.PI/2) <Eps,"Angle between lines had to be Pi/2, but was "+ angle);
        }

        [TestMethod]
        public void AngleTest2()
        {
            AS.Line a = new AS.Line(0,0,6,0);
            AS.Line b = new AS.Line(1,1,3,2);
            double trueResult = Math.Acos(2/Math.Sqrt(5));
            double angle = AS.Geometry.Angle(a, b,false);
            Assert.IsTrue(Math.Abs(angle-trueResult)<Eps,"Angle between lines had to be "+trueResult+", but was "+angle);
        }

        [TestMethod]
        public void ProjectionTest()
        {
           // AS.Line a = new AS.Line(1,3,4,6);
            AS.Line b = new AS.Line(2,1,6,5);
            AS.Point point = new AS.Point(3,5);
            AS.Point p = AS.Geometry.ParallelProjection(b, point,true);
            Assert.IsTrue(p!=null && Math.Abs(p.X - 4.5) <Eps && Math.Abs(p.Y - 3.5) <Eps,"Resulted projection point had to be (4.5,3.5) but was "+p );
        }

        [TestMethod]
        public void SimpleParallelReflectionTest()
        {
            AS.Line wall = new AS.Line(5,0,5,10);
            AS.Point source = new AS.Point(2,2);
            AS.Point listener = new AS.Point(2,6);
            AS.Point result = AS.Geometry.ReflectionPoint(wall, source, listener);
            Assert.IsTrue(result!=null && Math.Abs(result.X - 5) <Eps && Math.Abs(result.Y - 4)<Eps,"The reflection point had to be (5,4), but was "+result);
        }

        //[TestMethod]
        //public void SimpleSurfaceTest()
        //{
        //    AS.Room room = new AS.Room();
        //    AS.Point a = new AS.Point(0,0);
        //    AS.Point b = new AS.Point(2,5);
        //    AS.Point c = new AS.Point(5,0);
        //    AS.Point d = new AS.Point(2,2);
        //    room.AddSource(a);
        //    room.AddSource(b);
        //    room.AddSource(c);
        //    room.AddSource(d);
        //    List<AS.Point> surf = room.OuterSurface();
        //    bool checkResult = surf.IndexOf(a) != -1 && surf.IndexOf(b) != -1 && surf.IndexOf(c) != -1 &&
        //                        surf.IndexOf(d) == -1;
        //    string text = surf.Aggregate("", (current, point) => current + point.ToString());
        //    Assert.IsTrue(checkResult,"Surface had to be (0,0),(2,5),(5,0) but was "+text);
        //}

        [TestMethod]
        public void SimpleValidRoomTest()
        {
            AS.Room room = new AS.Room();
            AS.Wall.Material m = AS.Wall.Material.Granite;
            room.AddWall(new AS.Wall(2,0,5,0,m));
            room.AddWall(new AS.Wall(5,0,7,2,m));
            room.AddWall(new AS.Wall(7,2,7,5,m));
            room.AddWall(new AS.Wall(7,5,2,7,m));
            room.AddWall(new AS.Wall(2,7,0,3,m));
            room.AddWall(new AS.Wall(0,3,2,0,m));
            room.AddSource(new AS.SoundPoint(4,1));
            room.AddListener(new AS.ListenerPoint(2,4));
            room.AddListener(new AS.ListenerPoint(5,4));
            Assert.IsTrue(room.IsValid(),"Room was invalid!");
        }

        [TestMethod]
        public void SimpleInvalidRoomTest()
        {
            AS.Room room = new AS.Room();
            AS.Wall.Material m = AS.Wall.Material.Granite;
            room.AddWall(new AS.Wall(2, 0, 5, 0, m));
            room.AddWall(new AS.Wall(5, 0, 7, 2, m));
            room.AddWall(new AS.Wall(7, 2, 7, 5, m));
            room.AddWall(new AS.Wall(7, 5, 2, 7, m));
            room.AddWall(new AS.Wall(2, 7, 0, 3, m));
            room.AddWall(new AS.Wall(0, 3, 2, 0, m));
            room.AddSource(new AS.SoundPoint(4, 1));
            room.AddListener(new AS.ListenerPoint(2, 4));
            room.AddListener(new AS.ListenerPoint(5, 7));
            Assert.IsTrue(!room.IsValid(), "Room was valid!");
        }

        [TestMethod]
        public void SimpleInvalidRoomTest2()
        {
            AS.Room room = new AS.Room();
            AS.Wall.Material m = AS.Wall.Material.Granite;
            room.AddWall(new AS.Wall(2, 0, 5, 0, m));
            //room.AddWall(new AS.Wall(5, 0, 7, 2, m));
            room.AddWall(new AS.Wall(7, 2, 7, 5, m));
            room.AddWall(new AS.Wall(7, 5, 2, 7, m));
            room.AddWall(new AS.Wall(2, 7, 0, 3, m));
            room.AddWall(new AS.Wall(0, 3, 2, 0, m));
            room.AddSource(new AS.SoundPoint(4, 1));
            room.AddListener(new AS.ListenerPoint(2, 4));
            room.AddListener(new AS.ListenerPoint(5, 4));
            Assert.IsTrue(!room.IsValid(), "Room was valid!");
        }

        [TestMethod]
        public void DifferentAngles()
        {
            AS.Line[] array = new AS.Line[]
                {
                    new AS.Line(-2,0,0,0),
                    new AS.Line(-2,2,0,0),
                    new AS.Line(0,3,0,0),
                    new AS.Line(2,2,0,0),
                    new AS.Line(0,0,-2,0),
                    new AS.Line(2,-2,0,0),
                    new AS.Line(0,-3,0,0),
                    new AS.Line(-2,-2,0,0), 
                };

            for (int i = 0; i < array.Length; i++)
            {
                //Console.WriteLine("Angle "+i+"*Pi/4  "+AS.Geometry.Angle(array[0],array[i] ,true));
                double result = AS.Geometry.Angle(array[4], array[i], true);
                Assert.IsTrue(AS.Geometry.EqualDouble(result, i*Math.PI/4),"Angle was supposed to be "+i+"*Pi/4, but was "+result);
            }
        }

        [TestMethod]
        public void FirstImageTest()
        {
            AS.Wall.Material mat = AS.Wall.Material.Brick;
            AS.Room room = new AS.Room();
            AS.Point NW = new AS.Point(0,10);
            AS.Point NE = new AS.Point(5,10);
            AS.Point SW = new AS.Point(0,0);
            AS.Point SE = new AS.Point(5,0);
            AS.Wall wall = new AS.Wall(NW,NE,mat);
            room.AddWall(wall);
            room.AddWall(new AS.Wall(NE, SE, mat));
            room.AddWall(new AS.Wall(SE, SW, mat));
            room.AddWall(new AS.Wall(SW, NW, mat));
            AS.SoundPoint source = new AS.SoundPoint(2,4);
            AS.Point result = new AS.Point(2,16);
            AS.RoomImage image = new AS.RoomImage(room,wall,source);
            Assert.IsTrue(result == image.Source,"Source was supposed to be "+result+" but was "+image.Source);
           // Assert.IsTrue(image.Reflectors.Count == 1 && image.Reflectors.Contains(wall), "Reflectors are supposed to contain only top wall, but had "+image.Reflectors.Count);

        }

        [TestMethod]
        public void SecondImageTest()
        {
            AS.Wall.Material mat = AS.Wall.Material.Brick;
            AS.Room room = new AS.Room();
            AS.Point NW = new AS.Point(0, 10);
            AS.Point NE = new AS.Point(5, 10);
            AS.Point SW = new AS.Point(0, 0);
            AS.Point SE = new AS.Point(5, 0);
            AS.Wall wall = new AS.Wall(NW, NE, mat);
            room.AddWall(wall);
            room.AddWall(new AS.Wall(NE, SE, mat));
            room.AddWall(new AS.Wall(SE, SW, mat));
            room.AddWall(new AS.Wall(SW, NW, mat));
            AS.SoundPoint source = new AS.SoundPoint(2, 4);
            AS.Point result = new AS.Point(8, 16);
            AS.RoomImage firstImage = new AS.RoomImage(room,wall,source);
            AS.RoomImage secondImage = new AS.RoomImage(firstImage,new AS.Wall(5,10,5,20,mat));
            Assert.IsTrue(result == secondImage.Source, "Source was supposed to be " + result + " but was " + secondImage.Source);
        }

        [TestMethod]
        public void NonRectangularImageTest()
        {
            AS.Wall.Material mat = AS.Wall.Material.Brick;
            AS.Room room = new AS.Room();
            AS.Wall baseWall = new AS.Wall(0,0,9,0,mat);
            AS.Wall leftWall = new AS.Wall(0,0,0,9,mat);
            AS.Wall rightWall = new AS.Wall(9,0,0,9,mat);
            AS.SoundPoint source = new AS.SoundPoint(1,4);
            room.AddWall(baseWall);
            room.AddWall(leftWall);
            room.AddWall(rightWall);
            room.AddSource(source);
            AS.Point result = new AS.Point(5,8);
            AS.RoomImage image = new AS.RoomImage(room,rightWall,source);
            Assert.IsTrue(result == image.Source, "Source was supposed to be " + result + " but was " + image.Source);

        }

        [TestMethod]
        public void TotalDistanceTest()
        {
            AS.Wall.Material mat = AS.Wall.Material.Brick;
            AS.Room room = new AS.Room();
            room.AddWall(new AS.Wall(0, 0, 5, 0, mat));
            room.AddWall(new AS.Wall(0, 0, 0, 4, mat));
            room.AddWall(new AS.Wall(0, 4, 5, 4, mat));
            AS.Wall wall = new AS.Wall(5,4,5,0,mat);
            room.AddWall(wall);
            AS.SoundPoint source =new AS.SoundPoint(4,3);
            room.AddSource(source);
            AS.ListenerPoint listener = new AS.ListenerPoint(2,2);
            room.AddListener(listener);
            AS.RoomImage firstImage = new AS.RoomImage(room,wall,source);
            AS.RoomImage secondImage = new AS.RoomImage(firstImage,new AS.Wall(5,0,10,0,mat));
            AS.RoomImage thirdImage = new AS.RoomImage(secondImage,new AS.Wall(10,0,10,-4,mat));
            double x = thirdImage.GetTotalDistance(room, listener);
        }


    }
}
