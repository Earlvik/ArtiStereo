
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Documents;

namespace Earlvik.ArtiStereo
{
    public enum Filter
    {
        High,
        Low
    };
    public class Sound
    {
        private int mChannels;
        private int mDescretionRate;
        private int mBitsPerSample;
        private double[][] mSound;
        private int mMaxValue = 32767;
        private const int max16Bit = 32767;
        public const int LEFT_CHANNEL = 0;
        public const int RIGHT_CHANNEL = 1;

        public int Channels { get { return mChannels; } set { mChannels = value; } }
        public int DiscretionRate { get { return mDescretionRate; } set { mDescretionRate = value; } }
        public int BitsPerSample { get { return mBitsPerSample; } set { mBitsPerSample = value; } }

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
            mSound = new double[channels][];
            for (int i = 0; i < channels; i++)
            {
                mSound[i] = new double[0];
            }
        }

        public Sound(Sound other)
        {
            mChannels = other.mChannels;
            mDescretionRate = other.mDescretionRate;
            mBitsPerSample = other.mBitsPerSample;
            mSound = new double[mChannels][];
            for (int i = 0; i < mChannels; i++)
            {
                mSound[i]=new double[other.mSound[i].Length];
                Array.Copy(other.mSound[i],mSound[i],other.mSound[i].Length);
            }
        }

        public static Sound SimpleWave(int freq)
        {
            Sound result = new Sound(1, 44100, 16);
            result.mSound = new double[1][];
            result.mSound[0] = new double[result.mDescretionRate*10];
            for (int i = 0; i < result.mSound[0].Length; i++)
            {
                double x = freq*i/(result.mDescretionRate*Math.PI);
                result.mSound[0][i] = Math.Sin(x);
               
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
            sound.mChannels = bytes[23]*256 + bytes[22];
            sound.mDescretionRate = bytes[27]*16777216 + bytes[26]*65536 + bytes[25]*256 + bytes[24];
            sound.mBitsPerSample = BytesToInt(bytes[34], bytes[35]);
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
            sound.mSound = new double[sound.mChannels][];
            for (int i = 0; i < sound.mChannels; i++ )
            {
                sound.mSound[i] = new double[samples];
            }
            int j = 0;
            sound.mMaxValue = (int) (Math.Pow(256, sound.mBitsPerSample/8)/2);
            while (pos < bytes.Length)
            {
                for (int k = 0; k < sound.mChannels; k++)
                {
                    if (sound.mBitsPerSample == 16)
                    {
                        sound.mSound[k][j] = (double)BytesToInt(bytes[pos], bytes[pos + 1])/sound.mMaxValue;
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
                        sound.mSound[k][j] = (double)curSound/sound.mMaxValue;
                    }

                    pos += sound.mChannels*(sound.mBitsPerSample/8);
                }
                j++;
            }
            
            return sound;

        }

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

        public void Add(Sound sound,int channelFrom,int channelTo, int offset)
        {

            if(channelTo>mChannels || channelTo<0) throw new ArgumentException("Channel number invalid");
            if (channelFrom > sound.mChannels || channelFrom<0) throw new ArgumentException("Channel number invalid");
            if(mDescretionRate != sound.mDescretionRate) throw new ArgumentException("different discretion rates");
            
            double[] temp = new double[mSound[channelTo].Length];
            Sound fromSound;
            if (this == sound && channelFrom == channelTo)
            {
                fromSound = new Sound();
                fromSound.mSound = new double[channelFrom+1][];
                fromSound.mSound[channelFrom] = new double[temp.Length];
                Array.Copy(sound.mSound[channelFrom], fromSound.mSound[channelFrom], temp.Length);
            }
            else
            {
                fromSound = sound;
            }
            Array.Copy(mSound[channelTo],temp,temp.Length);
            mSound[channelTo] = new double[Math.Max(temp.Length,offset+fromSound.mSound[channelFrom].Length)];
            for (int i = 0; i < offset; i++)
            {
                if (i >= temp.Length)
                {
                    while (i < offset)
                    {
                        mSound[channelTo][i] = 0;
                        i++;
                    }
                    break;
                }
                mSound[channelTo][i] = temp[i];
            }
            for (int i = offset; i < mSound[channelTo].Length; i++)
            {
                double fromOld = (i < temp.Length) ? temp[i] : 0;
                double fromNew = (i-offset < fromSound.mSound[channelFrom].Length) ? fromSound.mSound[channelFrom][i-offset] : 0;
                mSound[channelTo][i] = fromOld + fromNew;
                //if (_sound[channelTo][i] > 1) _sound[channelTo][i] = 1;
            }
        }

        public void SetVolume(double percent, int channel)
        {
            if(channel >mChannels || channel < 0) throw new ArgumentException();
            for (int i = 0; i < mSound[channel].Length; i++)
            {
                mSound[channel][i] = (mSound[channel][i]*percent);
            }
        }

        public void SetVolume(int channel, double lowPercent, double medPercent, double highPercent)
        {
                      
        }

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
                mSound[channel][i] = result;
            }
        }

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
                mSound[channel][i] = result;
            }
        }

        private double PercentToDeciBell(double percent)
        {
            return 20*Math.Log10(percent);
        }

        private double DeciBellToPercent(double decibell)
        {
            return Math.Pow(10, decibell/20);
        }

        public Sound CopyWithVolume( double percent, int channel)
        {
            Sound result = new Sound(this);
            result.SetVolume(percent,channel);
            return result;
        }

        static int BytesToInt(byte firstByte, byte secondByte)
        {
            int value =  (secondByte << 8) | firstByte;
            if (value > max16Bit+1)
            {
                value -= 2*max16Bit+1;
            }
           return value;
        }

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

        public int MillesecondsToSamples(int milliseconds)
        {
            return milliseconds*mDescretionRate/1000;
        }

        public void AdjustVolume()
        {
            double max = 0;
            foreach (double[] channel in mSound)
            {
                foreach (double i in channel)
                {
                    if (Math.Abs(i) > max) max = i;
                }
            }
            double volume = (mMaxValue*0.75)/(max);
            for (int i = 0; i < mChannels; i++)
            {
                SetVolume(volume,i);
            }
        }
        
    }
}
