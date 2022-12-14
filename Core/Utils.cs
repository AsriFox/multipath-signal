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

        public static IList<double> ApplyNoise(IList<double> signal, double snr)
        {
            if (signal.Count <= 0)
                return Array.Empty<double>();

			Random rng = new(RNG.Next());
			double sample() {
				double s = 0.0;
				const int l = 20;
				for (int i = 0; i < l; i++)
					s += 2.0 * rng.NextDouble() - 1.0;
				return s / l;
			}

            //MathNet.Numerics.Distributions.Normal rand = new(new MersenneTwister(true));
            var noise = new double[signal.Count];
            double energyNoise = 0.0, energySignal = 0.0;
            for (int i = 0; i < signal.Count; i++)
            {
                noise[i] = sample();
                energyNoise += noise[i] * noise[i];
                energySignal += signal[i] * signal[i];
            }

            double noiseMod = Math.Sqrt(energySignal / energyNoise / snr);
			double ampAvg = 0.0;
            for (int i = 0; i < signal.Count; i++) {
                noise[i] = signal[i] + noise[i] * noiseMod;
				ampAvg += Math.Abs(noise[i]);
			}
			ampAvg /= noise.Length;
			for (int i = 0; i < noise.Length; i++)
				noise[i] /= ampAvg;
            return noise;
        }

        public const int MaxPointsCount = 80000;

		public static IEnumerable<OxyPlot.DataPoint> Plotify(this IList<double> values, double startX = 0.0) 
		{
			if (values.Count <= MaxPointsCount)
				return values.Select((v, i) => new OxyPlot.DataPoint(startX + i / SignalGenerator.Samplerate, v));

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
