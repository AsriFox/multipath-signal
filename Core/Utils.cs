using System;
using System.Collections.Generic;
using System.Linq;

namespace MultipathSignal.Core
{
	internal static class Utils
	{
		public readonly static Random RNG = new();
		
		public static IEnumerable<bool> RandomBitSeq(int length) {
			var result = new bool[length];
			for (int i = 0; i < length; i++)
				result[i] = RNG.NextDouble() > 0.5;
			return result;
		}

		public const int MaxPointsCount = 80000;

		public static IEnumerable<double> ValuesCutout(IList<double> values, int start, int end) 
		{
			throw new NotImplementedException();

			if (values.Count <= MaxPointsCount) 
				return values.Skip(start).Take(end);

			double k = (double)values.Count / MaxPointsCount;
			double lerpMid(int i) {
				double t = i * k;
				int i0 = (int)Math.Floor(t);
				t =- i0;
				return values[i0] * t + values[i0 + 1] * (1.0 - t);
			}

			var result = new double[MaxPointsCount];
			for (int i = 0; i < MaxPointsCount; i++)
				result[start + i] = lerpMid(start + i);
			return result;
		}

		//public static IEnumerable<OxyPlot.DataPoint> ValuesToPoints(IEnumerable<double> values) =>
		//	values.Select((v, i) => new OxyPlot.DataPoint(i, v));
	}
}
