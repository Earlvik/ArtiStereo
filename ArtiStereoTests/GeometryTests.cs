using System;
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
            double angle = AS.Geometry.Angle(a, b);
            Assert.IsTrue(Math.Abs(angle - Math.PI/2) <Eps,"Angle between lines had to be Pi/2, but was "+ angle);
        }

        [TestMethod]
        public void AngleTest2()
        {
            AS.Line a = new AS.Line(0,0,6,0);
            AS.Line b = new AS.Line(1,1,3,2);
            double trueResult = Math.Acos(2/Math.Sqrt(5));
            double angle = AS.Geometry.Angle(a, b);
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
            room.AddListener(new AS.SoundPoint(2,4));
            room.AddListener(new AS.SoundPoint(5,4));
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
            room.AddListener(new AS.SoundPoint(2, 4));
            room.AddListener(new AS.SoundPoint(5, 7));
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
            room.AddListener(new AS.SoundPoint(2, 4));
            room.AddListener(new AS.SoundPoint(5, 4));
            Assert.IsTrue(!room.IsValid(), "Room was valid!");
        }


    }
}
