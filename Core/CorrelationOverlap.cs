namespace MultipathSignal.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static FftSharp.Transform;

public static class CorrelationOverlap
{
    /// <summary>
    /// Calculate correlation with Overlap-Add method.
    /// </summary>
    public static IList<double> Calculate(IList<double> bigarr, IList<double> smolar)
    {
        int smolsize = 1;
        while (smolsize < smolar.Count) smolsize <<= 1;
        var smolar2 = new double[smolsize];
        for (int i = 0; i < smolar.Count; i++)
            smolar2[i] = smolar[smolar.Count - 1 - i];
        var smolft = FFT(smolar2);
        var bigarr2 = new double[smolsize];
        var correl = new double[bigarr.Count - smolar.Count];
        for (int n = 0; n < bigarr.Count; n += smolar.Count) {
            // Parallel.For(
            //     0, 
            //     bigarr.Count - n <= smolar.Count 
            //         ? smolar.Count 
            //         : bigarr.Count - n, 
            //     i => bigarr2[i] = bigarr[n + i]
            // );
            for (int i = 0; i < smolar.Count && n + i < bigarr.Count; i++)
                bigarr2[i] = bigarr[n + i];
            var bigfft = FFT(bigarr2);
            var corrft = Enumerable.Zip(bigfft, smolft, (a, b) => a * b).ToArray();
            IFFT(corrft);
            // Parallel.For(
            //     n >= smolar.Count ? 0 : smolar.Count,
            //     n - smolar.Count < bigarr.Count 
            //         ? smolsize
            //         : bigarr.Count - n,
            //     i => correl[i + n - smolar.Count] = corrft[i].Magnitude
            // );
            for (int i = n >= smolar.Count ? 0 : smolar.Count; 
                i < corrft.Length && n + i < bigarr.Count; 
                i++)
                    correl[n - smolar.Count + i] += corrft[i].Magnitude;
        }
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