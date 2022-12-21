using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace MultipathSignal.Core
{
    internal static class Correlation
    {
        public static IList<Complex> Calculate(IList<Complex> bigarr, IList<Complex> smolar)
        {
            if (bigarr.Count < smolar.Count)
                throw new ArgumentException("Place the bigger array in the first argument, please");
            var result = new Complex[bigarr.Count - smolar.Count];
            Parallel.For(0, result.Length, k => {
                Complex v = 0;
                for (int i = 0; i < smolar.Count; i++)
                    v += bigarr[i + k] * Complex.Conjugate(smolar[i]);
                result[k] = v / smolar.Count;
            });
            return result;
        }

        /// <summary>
        /// Calculate the cross-correlation asynchronously.
        /// Uses the default cancellation token.
        /// </summary>
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
}
