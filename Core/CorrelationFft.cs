namespace MultipathSignal.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static FftSharp.Transform;

public static class CorrelationFft 
{
    public static IList<double> Calculate(IList<double> bigarr, IList<double> smolar)
    {
        int bigsize = 1;
        while (bigsize < bigarr.Count + smolar.Count) bigsize <<= 1;
        var bigarr2 = new double[bigsize];
        bigarr.CopyTo(bigarr2, 0);
        var smolar2 = new double[bigsize];
        for (int i = 0; i < smolar.Count; i++)
            smolar2[i] = smolar[i];

        var bigfft = FFT(bigarr2);
        var smolft = FFT(smolar2);
        var corrft = Enumerable.Zip(
            bigfft, 
            smolft, 
            (a, b) => a * new FftSharp.Complex(b.Real, -b.Imaginary)
        ).ToArray();
        IFFT(corrft);
        var correl = new double[bigarr.Count - smolar.Count];
        for (int i = 0; i < correl.Length; i++)
            correl[i] = corrft[i].Magnitude;
        return correl;
    }

    public static Task<IList<double>> CalculateAsync(IList<double> bigarr, IList<double> smolar) =>
        Task.Factory.StartNew(
            args => {
                if (args is not Tuple<IList<double>, IList<double>> arrs)
                    throw new ArgumentException($"Expected a pair of arrays, got {args?.GetType()}", nameof(args));
                return Calculate(arrs.Item1, arrs.Item2);
            },
            Tuple.Create(bigarr, smolar),
            Utils.Cancellation.Token,
            TaskCreationOptions.None,
            TaskScheduler.Default);
}