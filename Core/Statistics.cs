using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace MultipathSignal.Core
{
	internal class Statistics
	{
		
		public event Action<string>? StatusChanged;
		public event Action<int, IList<double>[]>? PlotDataReady;

		public string[] GoldSeq = new string[4];

		public IList<bool> EncodeDecode(
			bool[] message,
			double snr,
			bool output = false)
		{
			var task = SignalModulator.ModulateGoldAsync(message, GoldSeq);
			task.Wait();
			var signal = task.Result;

			var filters = GoldSeq.Select(
				seq => SignalModulator.Modulate(
					seq.Select(c => c == '1').ToArray()
				)
			).ToArray();

			var noisySignal = Utils.ApplyNoise(signal, Math.Pow(10.0, 0.1 * snr));
			if (output) {
				StatusChanged?.Invoke("Signal generation complete. Calculating correlation...");
				PlotDataReady?.Invoke(0,
					new IList<double>[] {
						noisySignal
							.Select(c => c.Phase)
							.ToArray(),
						signal
							.Select(c => c.Phase)
							.ToArray(),
					}
				);
			}
			signal = noisySignal;

			var correl = new IList<Complex>[GoldSeq.Length];
			for (int k = 0; k < GoldSeq.Length; k++)
				correl[k] = CorrelationOverlap.Calculate(signal, filters[k]);

			if (output)
				PlotDataReady?.Invoke(1,
					correl
						.Select(cl => cl
							.Select(c => c.Magnitude)
							.ToArray()
						).ToArray()
				);

			int length = signal.Count;
			int bitLength = (int)SignalModulator.BitLength * 16;
			List<bool> result = new();
			for (int t = bitLength / 2; t < length; t += bitLength) {
				var max = new double[4];
				for (int i = t - bitLength; i < t; i++) {
					for (int k = 0; k < 4; k++) {
						max[k] = Math.Max(
							max[k], 
							correl[k][(i + length) % length].Magnitude
						);
					}
				}
				int m = 0;
				for (int i = 1; i < 4; i++)
					if (max[m] < max[i])
						m = i;
				result.Add(m / 2 > 0);
				result.Add(m % 2 > 0);
			}
			
			if (output) {
				string orig = "", res = "";
				int errc = 0;
				for (int i = 0; i < message.Length; i++) {
					orig += message[i] ? '1' : '0';
					res += result[i] ? '1' : '0';
					if (message[i] != result[i])
						errc++;
				}
				double ber = (double)errc / message.Length;
				StatusChanged?.Invoke(
					$"BER: {ber}, Message: {res}; original: {orig}"
				);
			}

			return result;
		}
		//	if (output)
		//		PlotDataReady?.Invoke(clearSignal, signal, correl);

		public Task<IList<bool>> EncodeDecodeAsync(
			bool[] signal,
			double snr,
			bool output = false) =>
			Task.Factory.StartNew(
				args => {
					if (args is not Tuple<bool[], double, bool> @params)
						throw new ArgumentException("Wrong arguments set", nameof(args));
					return EncodeDecode(@params.Item1, @params.Item2, @params.Item3);
				},
				Tuple.Create(signal, snr, output),
				Utils.Cancellation.Token,
				TaskCreationOptions.None,
				TaskScheduler.Default);
	}
}
