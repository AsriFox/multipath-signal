namespace MultipathSignal.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using static FftSharp.Transform;

public static class CorrelationOverlap
{
    /// <summary>
    /// Calculate correlation with Overlap-Add method.
    /// </summary>
    public static IList<Complex> Calculate(IList<Complex> bigarr, IList<Complex> smolar)
    {
        int smolsize = 1;
        while (smolsize < smolar.Count) smolsize <<= 1;
        var smolar2 = new FftSharp.Complex[smolsize];
        for (int i = 0; i < smolar.Count; i++)
            smolar2[i] = new(smolar[i].Real, smolar[i].Imaginary);
        FFT(smolar2);
        // Complex conjugate:
        for (int i = 0; i < smolsize; i++)
            smolar2[i] = new(smolar2[i].Real, -smolar2[i].Imaginary);

        int q = smolar.Count;
        var correl = new Complex[bigarr.Count + smolar.Count];
        for (int j = 0; j < bigarr.Count + smolar.Count; j += q) {
            var bigarr2 = new FftSharp.Complex[smolsize];
            for (int i = Math.Max(smolar.Count - j, 0); 
                i < q && j + i - smolar.Count < bigarr.Count;
                i++) 
                bigarr2[i] = new(
                    bigarr[j + i - smolar.Count].Real, 
                    bigarr[j + i - smolar.Count].Imaginary
                );

            FFT(bigarr2);
            for (int i = 0; i < smolsize; i++)
                bigarr2[i] *= smolar2[i];

            IFFT(bigarr2);
            for (int i = 0; i < smolsize; i++) {
                int k = j + i;
                if (i >= q) k -= smolsize;
                if (k < 0) break;
                if (k >= correl.Length) break;
                correl[k] += new Complex(bigarr2[i].Real, bigarr2[i].Imaginary);
            }
        }
        return correl;
    }

    public static Task<IList<Complex>> CalculateAsync(IList<Complex> bigarr, IList<Complex> smolar) =>
        Task.Factory.StartNew(
            args => {
                if (args is not Tuple<IList<Complex>, IList<Complex>> arrs)
                    throw new ArgumentException($"Expected a pair of arrays, got {args?.GetType()}", nameof(args));
                return Calculate(arrs.Item1, arrs.Item2);
            },
            Tuple.Create(bigarr, smolar),
            Utils.Cancellation.Token,
            TaskCreationOptions.None,
            TaskScheduler.Default);
}