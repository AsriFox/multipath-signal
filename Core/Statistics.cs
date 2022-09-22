using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultipathSignal.Core
{
	internal static class Statistics
	{
		// public static IList<double> Correlation(IList<double> left, IList<double> right)
		public static async Task<IList<double>> CorrelationAsync(IList<double> bigarr, IList<double> smolar)
		{
			if (bigarr.Count < smolar.Count) 
				throw new ArgumentException("Place the bigger array in the first argument, please");

			return await Task.Factory.StartNew(() => {
				int n = bigarr.Count;
				var result = new double[n - smolar.Count];
				Parallel.For(0, result.Length, k => {
					double v = 0;
					for (int i = 0; i < smolar.Count; i++)
						v += bigarr[i + k] * smolar[i];
					result[k] = v / smolar.Count;
				});
				return result;
			});
		}
	}
}
