﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Earlvik.ArtiStereo
{
    public class Sound
    {
        private int _channels;
        private int _descretionRate;
        private int[][] _sound;
        private const int MaxValue = 32767;
        public const int LEFT_CHANNEL = 0;
        public const int RIGHT_CHANNEL = 1;

        public int Channels { get { return _channels; } }

        public Sound()
        {
        }

        public Sound(int channels, int discretionRate)
        {
            _channels = channels;
            _descretionRate = discretionRate;
        }

        public static Sound SimpleWave(int freq)
        {
            Sound result = new Sound(1, 44100);
            result._sound = new int[1][];
            result._sound[0] = new int[result._descretionRate*10];
            for (int i = 0; i < result._sound[0].Length; i++)
            {
                double x = freq*i/(result._descretionRate*Math.PI);
                result._sound[0][i] =(int)(MaxValue* Math.Sin(x));
            }
            return result;
        }

        public static Sound GetSoundFromWav(string filename)
        {
            FileInfo file = new FileInfo(filename);
            if(!file.Exists || file.Extension.Substring(1)!="wav") throw new ArgumentException("The given file is not an existent wav file");
            Sound sound = new Sound();
            byte[] bytes = File.ReadAllBytes(filename);
            //if(bytes[22]>2 || bytes[23]>0) throw new ArgumentException("the given file is not a mono file");
            sound._channels = bytes[23]*256 + bytes[22];
            sound._descretionRate = bytes[27]*16777216 + bytes[26]*65536 + bytes[25]*256 + bytes[24];
            int pos = 12;   

            while (!(bytes[pos] == 100 && bytes[pos + 1] == 97 && bytes[pos + 2] == 116 && bytes[pos + 3] == 97))
            {
                pos += 4;
                int chunkSize = bytes[pos] + bytes[pos + 1] * 256 + bytes[pos + 2] * 65536 + bytes[pos + 3] * 16777216;
                pos += 4 + chunkSize;
            }
            pos += 8;
            int samples = (bytes.Length - pos) / 2;
            if (sound._channels == 2) samples /= 2;
            sound._sound = new int[sound._channels][];
            for (int i = 0; i < sound._channels; i++ )
            {
                sound._sound[i] = new int[samples];
            }
            int j = 0;
            while (pos < bytes.Length)
            {
                for (int k = 0; k < sound._channels; k++)
                {
                    sound._sound[k][j] = BytesToInt(bytes[pos], bytes[pos + 1]);
                    pos += 2;
                }
                j++;
            }
            return sound;

        }

        public void CreateWav(string filename)
        {
            //FileStream file = new FileStream(filename, FileMode.OpenOrCreate);
            byte[] data = new byte[44+_channels*_sound[0].Length*2];
            //RIFF word
            data[0] = 0x52;
            data[1] = 0x49;
            data[2] = 0x46;
            data[3] = 0x46;
            //ChunkSize
            int chunkSize = data.Length - 4;
            data[4] = (byte) (chunkSize%256);
            data[5] = (byte) ((chunkSize%65536 - (data[4]))/256);
            data[6] = (byte) ((chunkSize%16777216 - data[4] - data[5]*256)/65536);
            data[7] = (byte) ((chunkSize - data[4] - data[5]*256 - data[6]*65536)/16777216);
            //WAVE word
            data[8] = 0x57;
            data[9] = 0x41;
            data[10] = 0x56;
            data[11] = 0x45;
            //fmt word
            data[12] = 0x66;
            data[13] = 0x6d;
            data[14] = 0x74;
            data[15] = 0x20;
            //subChunk1Size
            data[16] = 0x10;
            data[17] = data[18] = data[19] = 0;
            //audioFormat
            data[20] = 1;
            data[21] = 0;
            //numChannels
            data[22] = (byte) (_channels%256);
            data[23] = (byte) (_channels/256);
            //sampleRate
            data[24] = (byte)(_descretionRate % 256);
            data[25] = (byte)((_descretionRate % 65536 - (data[24])) / 256);
            data[26] = (byte)((_descretionRate % 16777216 - data[24] - data[25] * 256) / 65536);
            data[27] = (byte)((_descretionRate - data[24] - data[25] * 256 - data[26] * 65536) / 16777216);
            //byteRate
            int byteRate = _descretionRate*_channels*2;
            data[28] = (byte)(byteRate % 256);
            data[29] = (byte)((byteRate % 65536 - (data[28])) / 256);
            data[30] = (byte)((byteRate % 16777216 - data[28] - data[29] * 256) / 65536);
            data[31] = (byte)((byteRate - data[28] - data[29] * 256 - data[30] * 65536) / 16777216);
            //blockAlign
            int blockAlign = 2*_channels;
            data[32] = (byte)(blockAlign%256);
            data[33] = (byte) (blockAlign/256);
            //bitsPerSample
            data[34] = 0x10;
            data[35] = 0;
            //DATA word
            data[36] = 0x64;
            data[37] = 0x61;
            data[38] = 0x74;
            data[39] = 0x61;
            //subChunk2Size
            int subChunk2Size = data.Length - 40;
            data[40] = (byte)(subChunk2Size % 256);
            data[41] = (byte)((subChunk2Size % 65536 - (data[40])) / 256);
            data[42] = (byte)((subChunk2Size % 16777216 - data[40] - data[41] * 256) / 65536);
            data[43] = (byte)((subChunk2Size - data[40] - data[41] * 256 - data[42] * 65536) / 16777216);
            for (int i = 44; i < data.Length-2*_channels+1;)
            {
                int pos = (i - 44)/(2*_channels);
                for (int j = 0; j < _channels; j++)
                {
                   IntToBytes(_sound[j][pos],out data[i],out data[i+1]);
                   i += 2;
                }
            }
            File.WriteAllBytes(filename,data);
        }

        public void Add(Sound sound,int channelFrom,int channelTo, int offset)
        {
            if(channelTo>_channels || channelTo<0) throw new ArgumentException("Channel number invalid");
            if (channelFrom > sound._channels || channelFrom<0) throw new ArgumentException("Channel number invalid");
            if(_descretionRate != sound._descretionRate) throw new ArgumentException("different discretion rates");
            
            int[] temp = new int[_sound[channelTo].Length];
            Sound fromSound;
            if (this == sound && channelFrom == channelTo)
            {
                fromSound = new Sound();
                fromSound._sound = new int[channelFrom+1][];
                fromSound._sound[channelFrom] = new int[temp.Length];
                Array.Copy(sound._sound[channelFrom], fromSound._sound[channelFrom], temp.Length);
            }
            else
            {
                fromSound = sound;
            }
            Array.Copy(_sound[channelTo],temp,temp.Length);
            _sound[channelTo] = new int[Math.Max(temp.Length,offset+fromSound._sound[channelFrom].Length)];
            for (int i = 0; i < offset; i++)
            {
                if (i > temp.Length)
                {
                    while (i < offset)
                    {
                        _sound[channelTo][i] = 0;
                    }
                    break;
                }
                _sound[channelTo][i] = temp[i];
            }
            for (int i = offset; i < _sound[channelTo].Length; i++)
            {
                int fromOld = (i < temp.Length) ? temp[i] : 0;
                int fromNew = (i < fromSound._sound[channelFrom].Length) ? fromSound._sound[channelFrom][i] : 0;
                _sound[channelTo][i] = fromOld + fromNew;
                //if (_sound[channelTo][i] > 1) _sound[channelTo][i] = 1;
            }
        }

        public void SetVolume(int percent, int channel)
        {
            if(channel >_channels || channel < 0) throw new ArgumentException();
            for (int i = 0; i < _sound[channel].Length; i++)
            {
                _sound[channel][i] = (_sound[channel][i]*percent)/100;
            }
        }

       public static int BytesToInt(byte firstByte, byte secondByte)
        {
            int value =  (secondByte << 8) | firstByte;
            if (value > MaxValue+1)
            {
                value -= 2*MaxValue+1;
            }
           return value;
        }

       public static void IntToBytes(int value, out byte firstByte, out byte secondByte)
        {
           // if (value > MaxValue) value = MaxValue;
           // if (value < -MaxValue) value = -MaxValue;
           if (value < 0)
           {
               value += 2*MaxValue + 1;
           }
            firstByte = (byte)(value%256.0);
            secondByte = (byte) (value/256.0);
        }

        public int MillesecondsToSamples(int milliseconds)
        {
            return milliseconds*_descretionRate/1000;
        }

        
    }
}
