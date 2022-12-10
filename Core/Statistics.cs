using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace MultipathSignal.Core
{
	internal class Statistics
	{
		public static IList<Complex> Correlation(IList<Complex> bigarr, IList<Complex> smolar)
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

		/// <summary>
		/// Calculate the cross-correlation asynchronously.
		/// Uses the default cancellation token.
		/// </summary>
		public static Task<IList<Complex>> CorrelationAsync(IList<Complex> bigarr, IList<Complex> smolar)=>
			Task.Factory.StartNew(
				args => {
					if (args is not Tuple<IList<Complex>, IList<Complex>> arrs)
						throw new ArgumentException($"Expected a pair of arrays, got {args?.GetType()}", nameof(args));
					return Correlation(arrs.Item1, arrs.Item2);
				},
				Tuple.Create(bigarr, smolar),
				Utils.Cancellation.Token,
				TaskCreationOptions.None,
				TaskScheduler.Default);

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

			signal = Utils.ApplyNoise(signal, Math.Pow(10.0, 0.1 * snr));
			if (output)
				StatusChanged?.Invoke("Signal generation complete. Calculating correlation...");

			var correl = new IList<Complex>[GoldSeq.Length];
			Parallel.For(0, GoldSeq.Length, k => {
				correl[k] = Correlation(signal, filters[k]);
			});

			int length = signal.Count;
			int bitLength = (int)SignalModulator.BitLength * 16;
			List<int> bitSel = new();
			for (int t = bitLength / -2; t < length; t += bitLength) {
				var max = new double[4];
				for (int i = t; i < t + bitLength; i++) {
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
				bitSel.Add(m);
			}
			
			List<bool> result = new();
			foreach (var m in bitSel) {
				result.Add(m / 2 > 0);
				result.Add(m % 2 > 0);
			}
			
			if (output) {
				string orig = new(message.Select(b => b ? '1' : '0').ToArray());
				string res = new(result.Select(b => b ? '1' : '0').ToArray());
				StatusChanged?.Invoke(
					$"Message received: {res}; original: {orig}"
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
