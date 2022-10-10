using System;
using System.Collections.Generic;
using MathNet.Numerics.Random;

namespace MultipathSignal.Core
{
	internal static class NoiseGenerator
	{
		public static IList<double> Apply(IList<double> signal, double snr = 10.0)
		{
			if (signal.Count <= 0) 
				return Array.Empty<double>();

			MathNet.Numerics.Distributions.Normal rand = new(new MersenneTwister(true));
			var noise = new double[signal.Count];
			double energyNoise = 0.0, energySignal = 0.0;
			for (int i = 0; i < signal.Count; i++) {
				noise[i] = rand.Sample();
				energyNoise += noise[i] * noise[i];
				energySignal += signal[i] * signal[i];
			}

			double noiseMod = Math.Sqrt(energySignal / energyNoise / snr);
			for (int i = 0; i < signal.Count; i++)
				noise[i] = signal[i] + noise[i] * noiseMod;
			return noise;
		}
	}
}
