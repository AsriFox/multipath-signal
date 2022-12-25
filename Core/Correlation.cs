using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using static FftSharp.Transform;

namespace MultipathSignal.Core
{
    internal class Correlation
    {
        public static IList<Complex> Calculate(IList<Complex> bigarr, IList<Complex> smolar)
		{
			if (bigarr.Count < smolar.Count)
				throw new ArgumentException("Place the bigger array in the first argument, please");
			var result = new Complex[bigarr.Count];
			Parallel.For(0, result.Length, k => {
				Complex v = 0;
				for (int i = 0; i < smolar.Count; i++)
					v += bigarr[(i + k) % bigarr.Count] * Complex.Conjugate(smolar[i]);
				result[k] = v / smolar.Count;
			});
			return result;
		}
    }
}