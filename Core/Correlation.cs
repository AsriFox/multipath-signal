using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MultipathSignal.Core
{
    internal static class Correlation
    {
        public static IList<double> Calculate(IList<double> bigarr, IList<double> smolar)
        {
            if (bigarr.Count < smolar.Count)
                throw new ArgumentException("Place the bigger array in the first argument, please");
            int n = bigarr.Count;
            var result = new double[n - smolar.Count];
            Parallel.For(0, result.Length, k => {
                double v = 0;
                for (int i = 0; i < smolar.Count; i++)
                    v += bigarr[i + k] * smolar[i];
                result[k] = Math.Abs(v) / smolar.Count;
            });
            return result;
        }

        /// <summary>
        /// Calculate the cross-correlation asynchronously.
        /// Uses the default cancellation token.
        /// </summary>
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
}
