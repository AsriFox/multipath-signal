using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MathNet.Numerics.Random;

namespace MultipathSignal.Core
{
	internal static class Utils
	{
		public readonly static MersenneTwister RNG = new();
		
		public static IEnumerable<bool> RandomBitSeq(int length) {
			var result = new bool[length];
			for (int i = 0; i < length; i++)
				result[i] = RNG.NextBoolean(); // .NextDouble() > 0.5;
			return result;
		}

		public const int MaxPointsCount = 80000;

		public static IEnumerable<OxyPlot.DataPoint> Plotify(this IList<double> values) 
		{
			if (values.Count <= MaxPointsCount)
				return values.Select((v, i) => new OxyPlot.DataPoint(i / SignalGenerator.Samplerate, v));

			int div = 2;
			while (values.Count / div > MaxPointsCount)
				div <<= 1;

			var result = new OxyPlot.DataPoint[values.Count / div];
			for (int i = 0; i < values.Count; i += div)
				result[i / div] = new OxyPlot.DataPoint(i / SignalGenerator.Samplerate, values[i]);
			return result;

			//static double lerp(double a, double b, double t) => a * t + (1.0 - t) * b;

			//var result = new double[MaxPointsCount];
			//for (int i = 0; i < MaxPointsCount; i++) {
			//	double k = start + c * (double)i / MaxPointsCount;
			//	int j = (int)Math.Floor(k);
			//	result[i] = lerp(values[j], values[j + 1], k - j);
			//}
			//return result;
		}

		public static CancellationTokenSource Cancellation = new();
	}
}
