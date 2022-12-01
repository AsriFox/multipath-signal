using System;
using System.Collections.Generic;
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
					v += bigarr[(i + k) % bigarr.Count] * smolar[i];
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
		public event Action<IList<double>, IList<double>, IList<double>>? PlotDataReady;

		public double MainFrequency { get; set; } = 1000.0;
		public double ModulationSpeed { get; set; } = 100.0;
		public double ModulationDepth { get; set; } = 0.8;
		public int BitSeqLength { get; set; } = 64;

		public IList<Complex>[] Filters = new IList<Complex>[4];


		//public double FindPredictedDelay(
		//	double receiveDelay, 
		//	double snrClean, 
		//	double snrNoisy, 
		//	bool output = false)
		//{
		//	int bitDelay = (int) Math.Ceiling(receiveDelay / ModulationSpeed);
		//	var task = gen.ModulateAsync(
		//		Utils.RandomBitSeq(
		//			bitDelay + BitSeqLength + Utils.RNG.Next(bitDelay, BitSeqLength)));
		//	task.Wait();
		//	var signal = task.Result;

		//	int initDelay = (int)(bitDelay * SignalGenerator.Samplerate / ModulationSpeed);
		//	var clearSignal = //NoiseGenerator.Apply(
		//					  signal.Skip(initDelay)
		//							.Take((int)(BitSeqLength * SignalGenerator.Samplerate / ModulationSpeed))
		//							.ToList();
		//	//Math.Pow(10.0, 0.1 * snrClean));

		//	signal = //NoiseGenerator.Apply(
		//				 signal.Skip(initDelay - (int)(receiveDelay * SignalGenerator.Samplerate))
		//					   .ToList();
		//				 //Math.Pow(10.0, 0.1 * snrNoisy));

		//	if (output)
		//		StatusChanged?.Invoke("Signal generation is complete. Calculating correlation...");

		//	task = CorrelationAsync(signal, clearSignal);
		//	task.Wait();
		//	var correl = task.Result;

		//	int maxPos = 0;
		//	double maxVal = 0.0;
		//	for (int i = 0; i < correl.Count; i++)
		//		if (Math.Abs(correl[i]) > maxVal) {
		//			maxVal = Math.Abs(correl[i]);
		//			maxPos = i;
		//		}
		//	var prediction = maxPos / SignalGenerator.Samplerate;

		//	if (output)
		//		PlotDataReady?.Invoke(clearSignal, signal, correl);

		//	return prediction;
		//}

		//public Task<double> FindPredictedDelayAsync(
		//	double receiveDelay,
		//	double snrClean,
		//	double snrNoisy,
		//	bool output = false) =>
		//	Task.Factory.StartNew(
		//		args => {
		//			if (args is not Tuple<double, double, double, bool> @params)
		//				throw new ArgumentException($"Expected three 'double' and a 'bool', got {args?.GetType()}", nameof(args));
		//			return FindPredictedDelay(@params.Item1, @params.Item2, @params.Item3, @params.Item4);
		//		},
		//		Tuple.Create(receiveDelay, snrClean, snrNoisy, output),
		//		Utils.Cancellation.Token,
		//		TaskCreationOptions.None,
		//		TaskScheduler.Default);
	}
}
