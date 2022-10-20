using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static FftSharp.Transform;

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

        public static IList<double> CalcByFFT(IList<double> bigarr, IList<double> smolar)
        {
            int bigsize = 1;
            while (bigsize < bigarr.Count + smolar.Count) bigsize <<= 1;
            var bigarr2 = new double[bigsize];
            bigarr.CopyTo(bigarr2, 0);
            var smolar2 = new double[bigsize];
            for (int i = 0; i < smolar.Count; i++)
                smolar2[i] = smolar[smolar.Count - 1 - i];

            var bigfft = FFT(bigarr2);
            var smolft = FFT(smolar2);
            var corrft = Enumerable.Zip(bigfft, smolft, (a, b) => a * b).ToArray();
            IFFT(corrft);
            var correl = new double[bigarr.Count - smolar.Count];
            for (int i = 0; i < correl.Length; i++)
                correl[i] = corrft[i + smolar.Count].Magnitude;
            return correl;
        }

        public static Task<IList<double>> CalcByFFTAsync(IList<double> bigarr, IList<double> smolar) =>
            Task.Factory.StartNew(
                args => {
                    if (args is not Tuple<IList<double>, IList<double>> arrs)
                        throw new ArgumentException($"Expected a pair of arrays, got {args?.GetType()}", nameof(args));
                    return CalcByFFT(arrs.Item1, arrs.Item2);
                },
                Tuple.Create(bigarr, smolar),
                Utils.Cancellation.Token,
                TaskCreationOptions.None,
                TaskScheduler.Default);

        public static async Task<double> FindPredictedDelay(
            this Statistics parent,
            double receiveDelay, 
			double snrClean, 
			double snrNoisy, 
            bool useFft = true,
			bool output = false)
		{
			SignalModulator gen = new() {
				MainFrequency = parent.MainFrequency,
				BitRate = parent.ModulationSpeed,
				Method = parent.ModulationType,
				Depth = parent.ModulationDepth
            };

			int bitDelay = (int) Math.Ceiling(receiveDelay * gen.BitRate);
			var signal = await gen.ModulateAsync(
				Utils.RandomBitSeq(
					bitDelay + parent.BitSeqLength + Utils.RNG.Next(bitDelay, parent.BitSeqLength)));

			int initDelay = (int)(bitDelay * SignalGenerator.Samplerate / parent.ModulationSpeed);
			var clearSignal = Utils.ApplyNoise(
							  signal.Skip(initDelay)
									.Take((int)(parent.BitSeqLength * SignalGenerator.Samplerate / parent.ModulationSpeed))
									.ToList(),
							  Math.Pow(10.0, 0.1 * snrClean));

			signal = Utils.ApplyNoise(
						 signal.Skip(initDelay - (int)(receiveDelay * SignalGenerator.Samplerate))
							   .ToList(),
						 Math.Pow(10.0, 0.1 * snrNoisy));

			if (output)
                parent.RaiseStatusChanged("Signal generation is complete. Calculating correlation...");

            var correl = useFft
                ? await CalcByFFTAsync(signal, clearSignal)
                : await CalculateAsync(signal, clearSignal);

            int maxPos = 0;
			double maxVal = 0.0;
			for (int i = 0; i < correl.Count; i++)
				if (Math.Abs(correl[i]) > maxVal) {
					maxVal = Math.Abs(correl[i]);
					maxPos = i;
				}
			double prediction = maxPos / SignalGenerator.Samplerate;
            // if (useFft) prediction -= parent.BitSeqLength / parent.ModulationSpeed;

			if (output)
				parent.RaisePlotDataReady(receiveDelay, clearSignal, signal, correl);

			return prediction;
		}
    }
}
