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

		public int BitSeqLength = 2;

		public async Task<double> ProcessMultiple(double snr, int testsRepeatCount) {
			var stopw = new System.Diagnostics.Stopwatch();
			stopw.Start();
			var tasks = new Task<double>[testsRepeatCount + 1];
			for (int i = 0; i < testsRepeatCount; i++)
				tasks[i + 1] = this.EncodeDecode(snr);
			tasks[0] = this.EncodeDecode(snr, true);

			var results = await Task.WhenAll(tasks);
			double berAvg = results.Sum() / results.Length;

            StatusChanged?.Invoke($"{testsRepeatCount} tasks were completed in {stopw.Elapsed.TotalSeconds:F2} s. Average BER: {berAvg}");
            return berAvg;
		}

		public async Task<double> EncodeDecode(double snr, bool output = false)
		{
			var message = Utils.RandomBitSeq(BitSeqLength).ToArray();
			var signal = await SignalModulator.ModulateGoldAsync(message, GoldSeq);
			
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

			var correl = filters.Select(
				f => CorrelationOverlap.Calculate(signal, f)
			).ToArray();

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

			int errc = 0;
			for (int i = 0; i < message.Length; i++)
				if (message[i] != result[i])
					errc++;
			double ber = (double)errc / message.Length;

			if (output)
				StatusChanged?.Invoke($"Simulation completed. BER: {ber}");
			return ber;
		}
	}
}
