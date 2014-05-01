
using System;
using System.Diagnostics;
using System.IO;

namespace Earlvik.ArtiStereo
{
    public enum Filter
    {
        High,
        Low
    };
    public class Sound
    {
       // public static int NUM = 0;
        public const int LEFT_CHANNEL = 0;
        public const int RIGHT_CHANNEL = 1;
        //Empirical constants for filtering
        private const double highBandwidth = 0.3;
        private const int highCenterFrequency = 18000;
        private const double lowBandwidth = 1.0;
        private const int lowCenterFrequency = 150;
        private const double mediumBandwidth = 1.0;
        private const int mediumCenterFrequency = 1000;
        //Sound parameters
        private int mBitsPerSample;
        private int mChannels;
        private int mDescretionRate;
        private int mMaxValue = 32767;
        private const int max16Bit = 32767;
        private float[][] mSound;

        public Sound()
        {
         
            mChannels = 0;
            mDescretionRate = 0;
            mSound = null;
        }

        public Sound(int channels, int discretionRate, int bitsPerSample)
        {
          
            mChannels = channels;
            mDescretionRate = discretionRate;
            mBitsPerSample = bitsPerSample;
            mSound = new float[channels][];
            for (int i = 0; i < channels; i++)
            {
                mSound[i] = new float[0];
            }
        }

        public Sound(Sound other)
        {
          
            mChannels = other.mChannels;
            mDescretionRate = other.mDescretionRate;
            mBitsPerSample = other.mBitsPerSample;
            mSound = new float[mChannels][];
            for (int i = 0; i < mChannels; i++)
            {
                mSound[i]=new float[other.mSound[i].Length];
                Array.Copy(other.mSound[i],mSound[i],other.mSound[i].Length);
            }
        }

       
        public int BitsPerSample { get { return mBitsPerSample; } set { mBitsPerSample = value; } }
        public int Channels { get { return mChannels; } set { mChannels = value; } }
        public int DiscretionRate { get { return mDescretionRate; } set { mDescretionRate = value; } }
        /// <summary>
        /// Get wave sound data from file
        /// </summary>
        /// <param name="filename">Path to file</param>
        /// <returns></returns>
        public static Sound GetSoundFromWav(string filename)
        {
            FileInfo file = new FileInfo(filename);
            if(!file.Exists || file.Extension.Substring(1)!="wav") throw new ArgumentException("The given file is not an existent wav file");
            Sound sound = new Sound();
            byte[] bytes = File.ReadAllBytes(filename);
            //if(bytes[22]>2 || bytes[23]>0) throw new ArgumentException("the given file is not a mono file");
            sound.mChannels = bytes[23]*256 + bytes[22];
            sound.mDescretionRate = bytes[27]*16777216 + bytes[26]*65536 + bytes[25]*256 + bytes[24];
            sound.mBitsPerSample = BytesToInt(bytes[34], bytes[35]);
            if (sound.mBitsPerSample == 0) sound.mBitsPerSample = 16;
            int pos = 12;   

            while (!(bytes[pos] == 100 && bytes[pos + 1] == 97 && bytes[pos + 2] == 116 && bytes[pos + 3] == 97))
            {
                pos += 4;
                int chunkSize = bytes[pos] + bytes[pos + 1] * 256 + bytes[pos + 2] * 65536 + bytes[pos + 3] * 16777216;
                pos += 4 + chunkSize;
            }
            pos += 8;
            int samples = (bytes.Length - pos) / (sound.mBitsPerSample/8);
            if (sound.mChannels == 2) samples =(samples%2 == 0)?samples/2:samples/2+1;
            sound.mSound = new float[sound.mChannels][];
            for (int i = 0; i < sound.mChannels; i++ )
            {
                sound.mSound[i] = new float[samples];
            }
            int j = 0;
            sound.mMaxValue = (int) (Math.Pow(256, sound.mBitsPerSample/8)/2);
            while (pos+1 < bytes.Length)
            {
                
                for (int k = 0; k < sound.mChannels; k++)
                {
                    if (pos + 1 >= bytes.Length) break;
                    if (sound.mBitsPerSample == 16)
                    {
                        sound.mSound[k][j] = (float)BytesToInt(bytes[pos], bytes[pos + 1])/sound.mMaxValue;
                    }
                    else
                    {
                        int curSound = 0;
                        int bytesNum = sound.mBitsPerSample/8;
                        for (int i = 0; i < bytesNum; i++)
                        {
                            curSound += bytes[pos + i]*(int)Math.Pow(256, bytesNum - i - 1);
                        }
                        if (curSound > sound.mMaxValue + 1)
                        {
                            curSound -= 2*sound.mMaxValue + 1;
                        }
                        sound.mSound[k][j] = (float)curSound/sound.mMaxValue;
                    }

                    pos += (sound.mBitsPerSample/8);
                }
                j++;
            }
            
            return sound;

        }
        /// <summary>
        /// Create simple sine soundwave
        /// </summary>
        /// <param name="freq">Frequency of wave</param>
        /// <returns></returns>
        public static Sound SimpleWave(int freq)
        {
            Sound result = new Sound(1, 44100, 16);
            result.mSound = new float[1][];
            result.mSound[0] = new float[result.mDescretionRate*10];
            for (int i = 0; i < result.mSound[0].Length; i++)
            {
                double x = freq*i/(result.mDescretionRate*Math.PI);
                result.mSound[0][i] = (float) Math.Sin(x);
               
            }
            return result;
        }

        /// <summary>
        /// Adding one sound to another
        /// </summary>
        /// <param name="sound">Sound to add</param>
        /// <param name="channelFrom">Channel of sound to use for sum</param>
        /// <param name="channelTo">Target channel of sound</param>
        /// <param name="offset">Time offset in milliseconds</param>
        /// <param name="volume">Optional parameter to add with certain volume level</param>
        public void Add(Sound sound,int channelFrom,int channelTo, int offset, double volume = 1)
        {

            if(channelTo>mChannels || channelTo<0) throw new ArgumentException("Channel number invalid");
            if (channelFrom > sound.mChannels || channelFrom<0) throw new ArgumentException("Channel number invalid");
            if(mDescretionRate != sound.mDescretionRate) throw new ArgumentException("different discretion rates");
            
            
            float[] fromSound = sound.mSound[channelFrom];
            float[] temp = new float[Math.Max(mSound[channelTo].Length, offset + fromSound.Length)];
            for (int i = 0; i < mSound[channelTo].Length; i++)
            {
                temp[i] = mSound[channelTo][i];
            }
            mSound[channelTo] = null;
            for (int i = offset; i < temp.Length; i++)
            {
                temp[i] += i - offset < fromSound.Length ? (float)volume*fromSound[i - offset] : 0;
               
            }
            mSound[channelTo] = temp;
        }

        public void Copy(Sound sound)
        {
            mChannels = sound.mChannels;
            mDescretionRate = sound.mDescretionRate;
            mBitsPerSample = sound.mBitsPerSample;
            if (!SameSized(sound))
            {
                mSound = new float[mChannels][];
                for (int i = 0; i < mChannels; i++)
                {
                    mSound[i] = new float[sound.mSound[i].Length];
                    Array.Copy(sound.mSound[i], mSound[i], sound.mSound[i].Length);
                }
            }
            else
            {
                for (int i = 0; i < mChannels; i++)
                {
                    Array.Copy(sound.mSound[i], mSound[i], sound.mSound[i].Length);
                }
            }
        }

        bool SameSized(Sound sound)
        {
            if (mSound == null || sound.mSound == null) return false;
            if (mSound.Length != sound.mSound.Length) return false;
            for (int i = 0; i < mSound.Length; i++)
            {
                if (mSound[i].Length != sound.mSound[i].Length) return false;
            }
            return true;
        }
        /// <summary>
        /// Adjusting sound to the given maximum
        /// </summary>
        /// <param name="level">Target sound level in percents</param>
        public void AdjustVolume(double level)
        {
            double max = 0;
            foreach (float[] channel in mSound)
            {
                Debug.Assert(channel != null, "channel != null");
                foreach (float i in channel)
                {
                    if (Math.Abs(i) > max) max = i;
                }
            }
            double volume = level/max;
            for (int i = 0; i < mChannels; i++)
            {
                SetVolume(volume,i);
            }
        }
        /// <summary>
        /// Applying bell-shaped filter to sound
        /// </summary>
        /// <param name="centerF">Central frequency</param>
        /// <param name="bandwidth">Width of filter in octaves</param>
        /// <param name="percentreduction">Amplitude of filtering</param>
        /// <param name="channel">Channel to edit</param>
        public void BellFilter(int centerF, double bandwidth, double percentreduction, int channel)
        {
            double dbGain = PercentToDeciBell(percentreduction);
            double a = Math.Pow(10, dbGain/40);
            double w0 = 2*Math.PI*((double)centerF/DiscretionRate);
            double cos = Math.Cos(w0);
            double sin = Math.Sin(w0);
            double alpha = sin*Math.Sinh(Math.Log(2, Math.E)*bandwidth*w0/sin);
            double b0 = 1 + alpha*a,
                b1 = -2*cos,
                b2 = 1 - alpha*a,
                a0 = 1 + alpha/a,
                a1 = -2*cos,
                a2 = 1 - alpha/a;
            b0 /= a0;
            b1 /= a0;
            b2 /= a0;
            a1 /= a0;
            a2 /= a0;
            
            double sourceMem1=0, sourceMem2=0, resultMem1=0, resultMem2=0;
            for (int i = 0; i < mSound[channel].Length; i++)
            {
                double result = b0*mSound[channel][i] + b1*sourceMem1 + b2*sourceMem2 - a1*resultMem1 - a2*resultMem2;
                sourceMem2 = sourceMem1;
                sourceMem1 = mSound[channel][i];
                resultMem2 = resultMem1;
                resultMem1 = result;
                mSound[channel][i] = (float) result;
            }
        }
        /// <summary>
        /// Combination of sound copying and volume adjusting
        /// </summary>
        /// <param name="percent">Target volume level in percents</param>
        /// <param name="channel">Channel to use in setVolume</param>
        /// <returns></returns>
        public Sound CopyWithVolume( double percent, int channel)
        {
            Sound result = new Sound(this);
            result.SetVolume(percent,channel);
            return result;
        }
        /// <summary>
        /// Creating an output wavesound file
        /// </summary>
        /// <param name="filename">Path to the file</param>
        public void CreateWav(string filename)
        {
            //FileStream file = new FileStream(filename, FileMode.OpenOrCreate);
            byte[] data = new byte[44+mChannels*mSound[0].Length*mBitsPerSample/8];
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
            //audioFormat -- standard
            data[20] = 1;
            data[21] = 0;
            //numChannels
            data[22] = (byte) (mChannels%256);
            data[23] = (byte) (mChannels/256);
            //sampleRate
            data[24] = (byte)(mDescretionRate % 256);
            data[25] = (byte)((mDescretionRate % 65536 - (data[24])) / 256);
            data[26] = (byte)((mDescretionRate % 16777216 - data[24] - data[25] * 256) / 65536);
            data[27] = (byte)((mDescretionRate - data[24] - data[25] * 256 - data[26] * 65536) / 16777216);
            //byteRate
            int byteRate = mDescretionRate*mChannels*(mBitsPerSample/8);
            data[28] = (byte)(byteRate % 256);
            data[29] = (byte)((byteRate % 65536 - (data[28])) / 256);
            data[30] = (byte)((byteRate % 16777216 - data[28] - data[29] * 256) / 65536);
            data[31] = (byte)((byteRate - data[28] - data[29] * 256 - data[30] * 65536) / 16777216);
            //blockAlign
            int blockAlign = (mBitsPerSample/8)*mChannels;
            data[32] = (byte)(blockAlign%256);
            data[33] = (byte) (blockAlign/256);
            //bitsPerSample
            //data[34] = 0x10; 
            //data[35] = 0;
            IntToBytes(mBitsPerSample,out data[34],out data[35]);
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
            int bytesPerSample = mBitsPerSample/8;
            //Actual sound data
            for (int i = 44; i < data.Length-bytesPerSample*mChannels+1;)
            {
                int pos = (i - 44)/(bytesPerSample*mChannels);
                for (int j = 0; j < mChannels; j++)
                {
                    if (pos >= mSound[j].Length)
                    {
                        for (int k = 0; k < bytesPerSample; k++)
                        {
                            data[i + k] = 0;
                        }
                    }
                    else
                    {
                        if (bytesPerSample == 2)
                        {
                            int sample = (int) Math.Round(mSound[j][pos]*mMaxValue);
                            // if (sample > mMaxValue) sample = mMaxValue;
                            // if (sample < -mMaxValue) sample = -mMaxValue;
                            IntToBytes(sample, out data[i], out data[i + 1]);
                        }
                        else
                        {
                            double sample = mSound[j][pos];
                            sample = Math.Round(sample*mMaxValue);
                            if (sample > mMaxValue) sample = mMaxValue;
                            if (sample < -mMaxValue) sample = -mMaxValue;
                            if (sample < 0)
                            {
                                sample += 2 * mMaxValue + 1;
                            }
                            for (int k = 0; k < bytesPerSample; k++)
                            {
                                data[i + k] = (byte) ((sample%Math.Pow(256, bytesPerSample - k)) -
                                                      (sample%Math.Pow(256, bytesPerSample - k-1)));
                            }
                        }

                    }
                    i += bytesPerSample;
                }
            }
            File.WriteAllBytes(filename,data);
        }
        /// <summary>
        /// Time units conversion dependent on discretion rate
        /// </summary>
        /// <param name="milliseconds">Time in milliseconds</param>
        /// <returns></returns>
        public int MillesecondsToSamples(int milliseconds)
        {
            return milliseconds*mDescretionRate/1000;
        }
        /// <summary>
        /// Setting volume on given level
        /// </summary>
        /// <param name="percent">Target volume level in percents</param>
        /// <param name="channel">Channel to set volume on</param>
        public void SetVolume(double percent, int channel)
        {
            if(channel >mChannels || channel < 0) throw new ArgumentException();
            for (int i = 0; i < mSound[channel].Length; i++)
            {
                mSound[channel][i] = (float) (mSound[channel][i]*percent);
            }
        }
        /// <summary>
        /// Adjust volume dependent of frequency. Applying bell-filter, high and low shelf filters
        /// </summary>
        /// <param name="channel">Channel to edit</param>
        /// <param name="lowPercent">Sound decrease level for low frequencies</param>
        /// <param name="medPercent">Sound decrease level for medium frequencies</param>
        /// <param name="highPercent">Sound decrease level for high frequencies</param>
        public void SetVolume(int channel, double lowPercent, double medPercent, double highPercent)
        {
            //Percents in parameters are given as a difference. So they should be substracted from 1
            double lowLevel = 1 - lowPercent;
            double medLevel = 1 - medPercent;
            double highLevel = 1 - highPercent;
            BellFilter(mediumCenterFrequency,mediumBandwidth,medLevel,channel);
            ShelfFilter(lowCenterFrequency,lowBandwidth,lowLevel,channel,Filter.Low);
            ShelfFilter(highCenterFrequency,highBandwidth,highLevel,channel,Filter.High);
        }
        /// <summary>
        /// Applying shelf filter to adjust high or low frequencies
        /// </summary>
        /// <param name="centerF">Central frequency of filter</param>
        /// <param name="bandwidth">Filter width in octaves</param>
        /// <param name="percentreduction">Target volume level</param>
        /// <param name="channel">Channel to edit</param>
        /// <param name="type">Filter.Low or Filter.High</param>
        public void ShelfFilter(int centerF, double bandwidth, double percentreduction, int channel,Filter type)
        {
            double dbGain = PercentToDeciBell(percentreduction);
            
            double a = Math.Pow(10, dbGain / 40);
            double w0 = 2 * Math.PI * ((double)centerF / DiscretionRate);
            double cos = Math.Cos(w0);
            double sin = Math.Sin(w0);
            double Q = 1 / (2 * Math.Sinh(Math.Log(2, Math.E) * bandwidth * w0 / sin));
            double alpha = sin/(2*Q);

            double b0 = a*((a + 1) - (a - 1)*cos + 2*Math.Sqrt(a)*alpha),
                b1 = 2*a*((a - 1) - (a + 1)*cos),
                b2 = a*((a + 1) - (a - 1)*cos - 2*Math.Sqrt(a)*alpha),
                a0 = (a + 1) + (a - 1)*cos + 2*Math.Sqrt(a)*alpha,
                a1 = -2*((a - 1) + (a + 1)*cos),
                a2 = (a + 1) + (a - 1)*cos - 2*Math.Sqrt(a)*alpha;
            b0 /= a0;
            b1 /= a0;
            b2 /= a0;
            a1 /= a0;
            a2 /= a0;
            if (type == Filter.High)
            {
                a1 *= -1;
                b1 *= -1;
            }
            double sourceMem1 = 0, sourceMem2 = 0, resultMem1 = 0, resultMem2 = 0;
            for (int i = 0; i < mSound[channel].Length; i++)
            {
                double result = b0 * mSound[channel][i] + b1 * sourceMem1 + b2 * sourceMem2 - a1 * resultMem1 - a2 * resultMem2;
                sourceMem2 = sourceMem1;
                sourceMem1 = mSound[channel][i];
                resultMem2 = resultMem1;
                resultMem1 = result;
                mSound[channel][i] = (float) result;
            }
        }
        /// <summary>
        /// Conversion of two bytes in littleEndian to one int value
        /// </summary>
        /// <param name="firstByte"></param>
        /// <param name="secondByte"></param>
        /// <returns></returns>
        static int BytesToInt(byte firstByte, byte secondByte)
        {
            int value =  (secondByte << 8) | firstByte;
            if (value > max16Bit+1)
            {
                value -= 2*max16Bit+1;
            }
           return value;
        }
        /// <summary>
        /// Converting int value to two bytes in littleEndian
        /// </summary>
        /// <param name="value"></param>
        /// <param name="firstByte"></param>
        /// <param name="secondByte"></param>
        static void IntToBytes(int value, out byte firstByte, out byte secondByte)
        {
            if (value > max16Bit) value = max16Bit-50;
            if (value < -max16Bit) value = -max16Bit+50;
           if (value < 0)
           {
               value += 2 * max16Bit + 1;
           }
            firstByte = (byte)(value%256.0);
            secondByte = (byte) (value/256.0);
        }
        /// <summary>
        /// Conversion of decibell to percentage
        /// </summary>
        /// <param name="decibell"></param>
        /// <returns></returns>
        private double DeciBellToPercent(double decibell)
        {
            return Math.Pow(10, decibell/20);
        }
        /// <summary>
        /// Conversion of percentage to decibell value
        /// </summary>
        /// <param name="percent"></param>
        /// <returns></returns>
        private double PercentToDeciBell(double percent)
        {
            return 20*Math.Log10(percent);
        }
    }
}
