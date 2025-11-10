using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public static class MusicFFTView
{
    public static float[] SplitFreqMap(int sampleRate, float[] freqMap, int bands)
    {
        int singleSidedLength = freqMap.Length / 2;
        float[] singleSided = new float[singleSidedLength];
        for (int i = 1; i <= singleSidedLength; i++)
        {
            singleSided[i - 1] = (float)freqMap[i];
        }

        // 生成对数分布的频段边界
        int[] bandBounds = new int[bands + 1];
        int maxFreq = sampleRate / 2;

        double minLog = Math.Log10(20);
        double maxLog = Math.Log10(maxFreq);
        double step = (maxLog - minLog) / bands;

        for (int i = 0; i <= bands; i++)
        {
            double freq = Math.Pow(10, minLog + i * step);
            bandBounds[i] = (int)freq;
        }
        bandBounds[0] = 0;
        bandBounds[bands] = maxFreq;

        float freqResolution = sampleRate / 2f / singleSidedLength;
        float[] bandValues = new float[bands];

        for (int band = 0; band < bands; band++)
        {
            int startFreq = bandBounds[band];
            int endFreq = bandBounds[band + 1];

            int startBin = (int)(startFreq / freqResolution);
            int endBin = (int)(endFreq / freqResolution);
            endBin = Math.Min(endBin, singleSidedLength - 1);

            if (startBin <= endBin)
            {
                float sum = 0;
                for (int bin = startBin; bin <= endBin; bin++)
                {
                    sum += singleSided[bin];
                }
                bandValues[band] = sum / (endBin - startBin + 1);
            }
        }

        return bandValues;
    }
}
