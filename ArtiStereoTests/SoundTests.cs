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
        public void PrimaryReflectionsComplexTest()
        {
            AS.Wall.Material mat = AS.Wall.Material.Brick;
            AS.Room room = new AS.Room();
            room.AddWall(new AS.Wall(0, 0, 17, 0, mat));
            room.AddWall(new AS.Wall(0, 0, 2, 10, mat));
            room.AddWall(new AS.Wall(17, 0, 14, 10, mat));
            room.AddWall(new AS.Wall(2, 10, 14, 10, mat));
            AS.SoundPoint source = new AS.SoundPoint(12, 5);
            source.Sound = AS.Sound.GetSoundFromWav(@"D:\sound.wav");
            room.AddSource(source);
            room.AddListener(new AS.SoundPoint(3, 7));
            room.AddListener(new AS.SoundPoint(7, 7));

            //room.AddWall(new AS.Wall(0,0,9,0,mat));
            //room.AddWall(new AS.Wall(0,0,0,10,mat));
            //room.AddWall(new AS.Wall(0,10,4,14,mat));
            //room.AddWall(new AS.Wall(4,14,9,10,mat));
            //room.AddWall(new AS.Wall(9,10,9,0,mat));
            //AS.SoundPoint source = new AS.SoundPoint(7,11);
            //source.Sound = AS.Sound.GetSoundFromWav(@"D:\sound.wav");
            //source.Sound.SetVolume(0.7,0);
            //room.AddSource(source);
            //room.AddListener(new AS.SoundPoint(2,3));
            //room.AddListener(new AS.SoundPoint(2.3,3));

            room.CalculateSound();
            AS.Sound sound = new AS.Sound(2,source.Sound.DiscretionRate,source.Sound.BitsPerSample);
            sound.Add(room.Listeners[1].Sound,0,0,0);
            sound.Add(room.Listeners[0].Sound,0,1,0);
            sound.AdjustVolume();
            sound.SetVolume(0.6,0);
            sound.SetVolume(0.6,1);
            sound.CreateWav(@"D:\result1.wav");
        }
    }
}
