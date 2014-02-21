using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AS = Earlvik.ArtiStereo;

namespace ArtiStereoTests
{
    [TestClass]
    public class SoundTests
    {
        [TestMethod]
        public void ReadWriteTest()
        {
         //   Exception ex = null;
//              try
           // {
                AS.Sound sound = AS.Sound.GetSoundFromWav(@"D:\sound.wav");
                Assert.AreEqual(1,sound.Channels,"The number of channels was expected to be 1, but was: "+sound.Channels);
                sound.CreateWav(@"D:\created.wav");
         //   }
          //  catch (Exception e)
          //  {
           //     ex = e;
           //     throw;
           // }
          //  Assert.IsNull(ex,"Exception was thrown: "+ex.Message);
        }

        [TestMethod]
        public void AddTest()
        {
            AS.Sound sound = AS.Sound.GetSoundFromWav(@"D:\sound.wav");
            AS.Sound second = AS.Sound.GetSoundFromWav(@"D:\Whistling.wav");
            sound.Add(second,0,0,500);
            sound.CreateWav(@"D:\new.wav");
        }

        [TestMethod]
        public void SetVolumeTest()
        {
            AS.Sound sound = AS.Sound.GetSoundFromWav(@"D:\sound.wav");
            sound.SetVolume(95,0);
            sound.CreateWav(@"D:\volume.wav");
        }

       [TestMethod]
       public void UserSoundTest()
       {
           AS.Sound sound = AS.Sound.SimpleWave(5000);
           sound.CreateWav(@"D:\shit.wav");
       }

        [TestMethod]
        public void ByteConversionTest()
        {
            for (byte first = byte.MinValue; first <= byte.MaxValue; first++)
            {
                for (byte second = byte.MinValue; second <= byte.MaxValue; second++)
                {
                    int temp = AS.Sound.BytesToInt(first, second);
                    byte a, b;
                    AS.Sound.IntToBytes(temp,out a, out b);
                    Assert.IsTrue(a == first && b == second,"Bytes "+first+" "+second+" => "+temp+" => "+a+" "+b);
                } 
            }
        }
    }
}
