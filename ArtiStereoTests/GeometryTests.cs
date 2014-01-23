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
        private const double Eps = 0.0001;
        [TestMethod]
        public void TestMethod1()
        {
            AS.Line a = new AS.Line(1,1,3,5);
            AS.Line b= new AS.Line(1,5,3,1);
            AS.Point p = a.GetIntersection(b);
            Assert.IsTrue(p!=null && Math.Abs(p.X)-2<Eps && Math.Abs(p.Y)-3<Eps,"The intersection point had to be (2,3) but was "+p);
        }
    }
}
