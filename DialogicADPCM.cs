// Dialogic ADPCM encoder and decoder using the Dialogic ADPCM algorithm specification which is used by the OKI MSM6650/M6650.
// Encodes PCM samples into Dialogic ADPCM samples.
// Encodes Dialogic ADPCM samples into PCM samples.

using System;
using System.Collections.Generic;

internal class DialogicADPCM
{
    int[] adjFactorTable = { -1, -1, -1, -1, 2, 4, 6, 8, -1, -1, -1, -1, 2, 4, 6, 8 };
    int[] stepSizeTable = { 16, 17, 19, 21, 23, 25, 28, 31, 34, 37, 41, 45, 50, 55, 60, 66, 73, 80, 88, 97, 107, 118, 130, 143, 157, 173, 190, 209, 230, 253, 279, 307, 337, 371, 408, 449, 494, 544, 598, 658, 724, 796, 876, 963, 1060, 1166, 1282, 1411, 1552 };
    int stepSize = 0;
    int stepSizeIndex = 0;
    int predictedSample = 0;

    int lastPCM12sample = 0;

    List<int> adpcmSamples = new List<int>();
    List<int> PCMsamples = new List<int>();
    public int decodeSample(int nibble)
    {
        stepSize = stepSizeTable[stepSizeIndex];

        int B3 = (nibble >> 3) & 1;
        int B2 = (nibble >> 2) & 1;
        int B1 = (nibble >> 1) & 1;
        int B0 = (nibble >> 0) & 1;

        int dif = (stepSize * B2) + ((stepSize >> 1) * B1) + ((stepSize >> 2) * B0) + (stepSize >> 3);
        if (B3 == 1)
        {
            dif = dif * (-1);
        }
        predictedSample = predictedSample + dif;

        if (predictedSample > 2047)
        {
            predictedSample = 2047;
        }
        if (predictedSample < -2048)
        {
            predictedSample = -2048;
        }

        int PCM16sample = predictedSample << 4;

        int adjFactor = adjFactorTable[nibble];
        stepSizeIndex = stepSizeIndex + adjFactor;
        if (stepSizeIndex > 48)
        {
            stepSizeIndex = 48;
        }
        if (stepSizeIndex < 0)
        {
            stepSizeIndex = 0;
        }

        return PCM16sample;
    }
    public int encodeSample(int PCM16sample)
    {
        int PCM12sample = PCM16sample >> 4;
        int dif = PCM12sample - lastPCM12sample;
        lastPCM12sample = PCM12sample;
        stepSize = stepSizeTable[stepSizeIndex];

        int B3 = 0;
        int B2 = 0;
        int B1 = 0;
        int B0 = 0;

        if (dif < 0)
        {
            B3 = 1;
        }
        dif = Math.Abs(dif);
        if (dif >= stepSize)
        {
            B2 = 1;
            dif = dif - stepSize;
        }
        if (dif >= stepSize >> 1)
        {
            B1 = 1;
            dif = dif - (stepSize >> 1);
        }
        if (dif >= stepSize >> 2)
        {
            B0 = 1;
        }
        int ADPCMsample = (B3 << 3) | (B2 << 2) | (B1 << 1) | B0;

        int adjFactor = adjFactorTable[ADPCMsample];
        stepSizeIndex = stepSizeIndex + adjFactor;
        if (stepSizeIndex > 48)
        {
            stepSizeIndex = 48;
        }
        if (stepSizeIndex < 0)
        {
            stepSizeIndex = 0;
        }

        return ADPCMsample;
    }
}