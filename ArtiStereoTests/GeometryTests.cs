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
        public void TestMethod1()
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
            Assert.IsTrue(Math.Abs(AS.Line.Distance(a,b) - Math.Sqrt(18.25)) < Eps);
        }

        [TestMethod]
        public void AngleTest()
        {
            AS.Line a = new AS.Line(1,2,2,6);
            AS.Line b = new AS.Line(2,2,6,1);
            double angle = AS.Line.Angle(a, b);
            Assert.IsTrue(Math.Abs(angle - Math.PI/2) <Eps,"Angle between lines had to be Pi/2, but was "+ angle);
        }

        [TestMethod]
        public void ProjectionTest()
        {
           // AS.Line a = new AS.Line(1,3,4,6);
            AS.Line b = new AS.Line(2,1,6,5);
            AS.Point point = new AS.Point(3,5);
            AS.Point p = AS.Line.ParallelProjection(b, point);
            Assert.IsTrue(p!=null && Math.Abs(p.X - 4.5) <Eps && Math.Abs(p.Y - 3.5) <Eps,"Resulted projection point had to be (4.5,3.5) but was "+p );
        }

        [TestMethod]
        public void SimpleParallelReflectionTest()
        {
            AS.Line wall = new AS.Line(5,0,5,10);
            AS.Point source = new AS.Point(2,2);
            AS.Point listener = new AS.Point(2,6);
            AS.Point result = AS.Line.ReflectionPoint(wall, source, listener);
            Assert.IsTrue(result!=null && Math.Abs(result.X - 5) <Eps && Math.Abs(result.Y - 4)<Eps,"The reflection point had to be (5,4), but was "+result);
        }
    }
}
