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
        
                AS.Sound sound = AS.Sound.GetSoundFromWav(@"D:\sweep.wav");
               // Assert.AreEqual(1,sound.Channels,"The number of channels was expected to be 1, but was: "+sound.Channels);
                sound.CreateWav(@"D:\sweepC.wav");
         
        }

        [TestMethod]
        public void AddTest()
        {
            AS.Sound second = AS.Sound.GetSoundFromWav(@"D:\Whistling.wav");
            AS.Sound sound = AS.Sound.GetSoundFromWav(@"D:\sound.wav");
            sound.SetVolume(0.5,0);
            second.SetVolume(0.5,0);
            
            int offset = sound.MillesecondsToSamples(2500);
            sound.Add(second,0,0,offset+5);
            sound.CreateWav(@"D:\new1.wav");
        }

        [TestMethod]
        public void SetVolumeTest()
        {
            AS.Sound sound = AS.Sound.GetSoundFromWav(@"D:\sound.wav");
            sound.SetVolume(0.2,0);
            sound.CreateWav(@"D:\volume.wav");
        }

       [TestMethod]
       public void UserSoundTest()
       {
           AS.Sound sound = AS.Sound.SimpleWave(400);
           sound.CreateWav(@"D:\shit.wav");
       }

        [TestMethod]
        public void PrimaryReflectionsComplexTest()
        {
             
            AS.Wall.Material mat = AS.Wall.Material.OakWoodCarpeted;
            AS.Room room = new AS.Room();
            room.FloorMaterial = AS.Wall.Material.Brick;
            room.CeilingMaterial = AS.Wall.Material.OakWood;
            room.CeilingHeight = 2;
            room.AddWall(new AS.Wall(0, 0, 4, 0, mat));
            room.AddWall(new AS.Wall(0, 0, 0, 10, mat));
            room.AddWall(new AS.Wall(4, 0, 4, 10, mat));
            room.AddWall(new AS.Wall(0, 10, 4, 10, mat));
            AS.SoundPoint source = new AS.SoundPoint(2, 1);
            source.Sound = AS.Sound.GetSoundFromWav(@"D:\Whistling.wav");
            room.AddSource(source);
            room.AddListener(new AS.ListenerPoint(1, 8,new AS.Line(0,0,-1,0),AS.ListenerPoint.Cardioid));
            room.AddListener(new AS.ListenerPoint(3, 8, new AS.Line(0,0,1,0),AS.ListenerPoint.Cardioid ));

            room.CalculateSound();
            AS.Sound sound = new AS.Sound(2,source.Sound.DiscretionRate,source.Sound.BitsPerSample);
            sound.Add(room.Listeners[1].Sound,0,0,0);
            sound.Add(room.Listeners[0].Sound,0,1,0);
            sound.AdjustVolume(0.75);
            //sound.SetVolume(0.6,0);
            //sound.SetVolume(0.6,1);
            sound.CreateWav(@"D:\Result.wav");
        }
    
        [TestMethod]
        public void PrimaryReflectionsHugeTest()
        {
            AS.Wall.Material mat = AS.Wall.Material.Brick;
            AS.Room room = new AS.Room();
            room.FloorMaterial = AS.Wall.Material.Brick;
            room.CeilingMaterial = AS.Wall.Material.OakWood;
            room.CeilingHeight = 2;
            room.AddWall(new AS.Wall(0,5,10,0,mat));
            room.AddWall(new AS.Wall(10,0,20,5,mat));
            room.AddWall(new AS.Wall(20,5,20,45,mat));
            room.AddWall(new AS.Wall(20,45,0,45,mat));
            room.AddWall(new AS.Wall(0,45,0,5, mat));
            AS.SoundPoint source = new AS.SoundPoint(10,40);
            source.Sound = AS.Sound.GetSoundFromWav(@"D:\dirac.wav");
            room.AddSource(source);
            room.AddListener(new AS.ListenerPoint(9,4));
            room.AddListener(new AS.ListenerPoint(11,4));


            room.CalculateSound();
            AS.Sound sound = new AS.Sound(2, source.Sound.DiscretionRate, source.Sound.BitsPerSample);
            sound.Add(room.Listeners[1].Sound, 0, 0, 0);
            sound.Add(room.Listeners[0].Sound, 0, 1, 0);
            sound.AdjustVolume(0.75);
            //sound.SetVolume(0.6, 0);
            //sound.SetVolume(0.6, 1);
            sound.CreateWav(@"D:\diracR.wav");
            Console.WriteLine(GC.GetTotalMemory(false)/(1024*1024)+"");
        }

        [TestMethod]
        public void BellFilterTest()
        {
            AS.Sound sound = AS.Sound.GetSoundFromWav(@"D:\sweep.wav");
            sound.BellFilter(1000,1,0.9,0);
            sound.CreateWav(@"D:\bell1.wav");
        }

        [TestMethod]
        public void LowShelfFilterTest()
        {
            AS.Sound sound = AS.Sound.GetSoundFromWav(@"D:\sweep.wav");
            sound.ShelfFilter(150, 1, .5, 0,AS.Filter.Low);
            sound.CreateWav(@"D:\shelfLOW.wav");
        }

        [TestMethod]
        public void HighShelfFileterTest()
        {
            AS.Sound sound = AS.Sound.GetSoundFromWav(@"D:\sweep.wav");
            sound.ShelfFilter(10000,1,.4,0,AS.Filter.High);
            sound.CreateWav(@"D:\shelfHIGH.wav");
        }

        [TestMethod]
        public void ComplexFilterTest()
        {
            AS.Sound sound = AS.Sound.GetSoundFromWav(@"D:\Whistling.wav");
            sound.SetVolume(0,0.1,0.5,0.9);
            sound.CreateWav(@"D:\complex.wav");
        }


        [TestMethod]
        public void ConvolutionTest()
        {
            AS.Sound kernelSound = AS.Sound.GetSoundFromWav(@"D:\diracR.wav");
            AS.Sound dataSound = AS.Sound.GetSoundFromWav(@"D:\blatt.wav");
            dataSound.Convolve(kernelSound,0,0);
            dataSound.Convolve(kernelSound,1,0);
            dataSound.CreateWav(@"D:\blattR");
        }
        [TestMethod]
        public void MultiChanneltest()
        {
            AS.Wall.Material mat = AS.Wall.Material.OakWoodCarpeted;
            AS.Room room = new AS.Room();
            room.FloorMaterial = AS.Wall.Material.Brick;
            room.CeilingMaterial = AS.Wall.Material.OakWood;
            room.CeilingHeight = 2;
            room.AddWall(new AS.Wall(0, 0, 4, 0, mat));
            room.AddWall(new AS.Wall(0, 0, 0, 10, mat));
            room.AddWall(new AS.Wall(4, 0, 4, 10, mat));
            room.AddWall(new AS.Wall(0, 10, 4, 10, mat));
            AS.SoundPoint source = new AS.SoundPoint(2, 1);
            source.Sound = AS.Sound.GetSoundFromWav(@"D:\Whistling.wav");
            room.AddSource(source);
            room.AddListener(new AS.ListenerPoint(1, 8, new AS.Line(0, 0, -1, 0), AS.ListenerPoint.Cardioid));
            room.AddListener(new AS.ListenerPoint(3, 8, new AS.Line(0, 0, 1, 0), AS.ListenerPoint.Cardioid));
            room.AddListener(new AS.ListenerPoint(2, 8, 2));
            room.Listeners[2].Channel = (AS.Sound.Channel) 3;
            room.CalculateSound();
            AS.Sound sound = room.GetSoundFromListeners();
            sound.AdjustVolume(0.75);
            sound.CreateWav(@"D:\Result.wav");
        }
    }
}
