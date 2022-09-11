using System;
using System.Collections.Generic;

namespace MultipathSignal.Core
{
	internal class NoiseGenerator
	{
		private NoiseGenerator() { }
		private static NoiseGenerator? instance;
		public static NoiseGenerator Instance => instance ??= new NoiseGenerator();

		private readonly Random rand = new();

		private const int gAccumCount = 12;
		private double GaussianSample() {
			double v = 0.0;
			for (int i = 0; i < gAccumCount; i++)
				v += rand.NextDouble();
			return (2.0 * v / gAccumCount) - 1.0;
		}

		public IList<double> ApplyNoise(IList<double> signal, double snr = 10.0)
		{
			if (signal.Count <= 0) 
				return Array.Empty<double>();
			var noise = new double[signal.Count];
			double energyNoise = 0.0, energySignal = 0.0;
			for (int i = 0; i < signal.Count; i++) {
				noise[i] = GaussianSample();
				energyNoise += noise[i] * noise[i];
				energySignal += signal[i] * signal[i];
			}
			double noiseMod = Math.Sqrt(energySignal / energyNoise / snr);
			for (int i = 0; i < signal.Count; i++)
				noise[i] = signal[i] + noise[i] * noiseMod;
			return noise;
		}
		public static IList<double> Apply(IList<double> signal, double snr = 10.0) => Instance.ApplyNoise(signal, snr);
	}
}
