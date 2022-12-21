namespace MultipathSignal.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using static FftSharp.Transform;

public static class CorrelationFft 
{
    public static IList<Complex> Calculate(IList<Complex> bigarr, IList<Complex> smolar)
    {
        int bigsize = 1;
        while (bigsize < bigarr.Count + smolar.Count) bigsize <<= 1;
        var bigarr2 = new FftSharp.Complex[bigsize];
        for (int i = 0; i < bigarr.Count; i++)
            bigarr2[i] = new(bigarr[i].Real, bigarr[i].Imaginary);
        var smolar2 = new FftSharp.Complex[bigsize];
        for (int i = 0; i < smolar.Count; i++)
            smolar2[i] = new(smolar[i].Real, smolar[i].Imaginary);

        FFT(bigarr2);
        FFT(smolar2);
        var corrft = Enumerable.Zip(
            bigarr2, 
            smolar2, 
            (a, b) => a * new FftSharp.Complex(b.Real, -b.Imaginary)
        ).ToArray();
        IFFT(corrft);
        var correl = new Complex[bigarr.Count - smolar.Count];
        for (int i = 0; i < correl.Length; i++)
            correl[i] = new(
                corrft[i].Real / smolar.Count,
                corrft[i].Imaginary / smolar.Count
            );
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