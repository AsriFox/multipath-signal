using Avalonia.X11;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultipathSignal.Core
{
	internal class Statistics
	{
		public double MainFrequency { get; set; } = 1000.0;
		public double ModulationSpeed { get; set; } = 100.0;
		public SignalModulator.Modulation ModulationType { get; set; } = SignalModulator.Modulation.OOK;
		public double ModulationDepth { get; set; } = 0.8;
		public int BitSeqLength { get; set; } = 64;

        public event Action<string>? StatusChanged;
		protected internal void RaiseStatusChanged(string status) => StatusChanged?.Invoke(status);

        public event Action<double, IList<double>[]>? PlotDataReady;
		protected internal void RaisePlotDataReady(double delay, params IList<double>[] plots) => PlotDataReady?.Invoke(delay, plots);

		public Task<double> ProcessSingle(double delay, double snrClean, double snrNoisy, bool useFft)
		{
            StatusChanged?.Invoke("Processing one signal...");
            return this.FindPredictedDelay(delay, snrClean, snrNoisy, useFft, true);
        }

		public async Task<double> ProcessMultiple(double delay, double snrClean, double snrNoisy, bool useFft, int testsRepeatCount)
		{
            StatusChanged?.Invoke("Processing signals...");
            var stopw = new Stopwatch();
            stopw.Start();
            var tasks = Enumerable.Range(0, testsRepeatCount)
                                  .Select(_ => this.FindPredictedDelay(delay, snrClean, snrNoisy, useFft))
                                  .ToList();
            tasks.Add(this.FindPredictedDelay(delay, snrClean, snrNoisy, useFft, true));

            var results = await Task.WhenAll(tasks);
            double predictedDelay = results.Sum() / results.Length;

            stopw.Stop();
            StatusChanged?.Invoke($"{testsRepeatCount} tasks were completed in {stopw.Elapsed.TotalSeconds:F2} s. ");
            return predictedDelay;
        }

        public async Task<double> ProcessStatistic(double delayBase, double snrClean, double snrNoisy, bool useFft, int testsRepeatCount)
        {
            StatusChanged?.Invoke($"Processing signals with SNR = {snrNoisy:F2} dB");
            var actualDelays = new List<double>();
            var tasks2 = new List<Task<double>>();
            for (int i = 0; i < testsRepeatCount; i++) {
                double delay = delayBase * 2.0 * Utils.RNG.NextDouble();
                actualDelays.Add(delay);
                tasks2.Add(this.FindPredictedDelay(delay, snrClean, snrNoisy, useFft));
            }
            actualDelays.Add(delayBase * 2.0 * Utils.RNG.NextDouble());
            tasks2.Add(this.FindPredictedDelay(actualDelays.Last(), snrClean, snrNoisy, useFft, true));

            var results2 = await Task.WhenAll(tasks2);
            double threshold = 0.5 / ModulationSpeed;
            int gotitCount = 0;
            for (int i = 0; i < results2.Length; i++)
                if (Math.Abs(results2[i] - actualDelays[i]) < threshold)
                    gotitCount++;
            return (double)gotitCount / results2.Length;
        }
        
        public async Task<double> FindPredictedDelay(
            double receiveDelay, 
			double snrClean, 
			double snrNoisy, 
            bool useFft = true,
			bool output = false)
		{
			SignalModulator gen = new() {
				MainFrequency = this.MainFrequency,
				BitRate = this.ModulationSpeed,
				Method = this.ModulationType,
				Depth = this.ModulationDepth
            };

			int bitDelay = (int) Math.Ceiling(receiveDelay * gen.BitRate);
			var signal = await gen.ModulateAsync(
				Utils.RandomBitSeq(bitDelay + 2 * this.BitSeqLength));

			int initDelay = (int)(bitDelay * SignalGenerator.Samplerate / this.ModulationSpeed);
			var clearSignal = Utils.ApplyNoise(
							  signal.Skip(initDelay)
									.Take((int)(this.BitSeqLength * SignalGenerator.Samplerate / this.ModulationSpeed))
									.ToList(),
							  Math.Pow(10.0, 0.1 * snrClean));

			signal = Utils.ApplyNoise(
						 signal.Skip(initDelay - (int)(receiveDelay * SignalGenerator.Samplerate))
							   .ToList(),
						 Math.Pow(10.0, 0.1 * snrNoisy));

			if (output)
                this.RaiseStatusChanged("Signal generation is complete. Calculating correlation...");

            var correl = useFft
                ? await CorrelationFft.CalculateAsync(signal, clearSignal)
                // ? await CorrelationOverlap.CalculateAsync(signal, clearSignal)
                : await Correlation.CalculateAsync(signal, clearSignal);

            // double correlMax = correl.Max();
            // for (int i = 0; i < correl.Count; i++)
            //     correl[i] /= correlMax;

            int maxPos = 0;
			double maxVal = 0.0;
			for (int i = 0; i < correl.Count; i++)
				if (Math.Abs(correl[i]) > maxVal) {
					maxVal = Math.Abs(correl[i]);
					maxPos = i;
				}
			double prediction = maxPos / SignalGenerator.Samplerate;
            // if (useFft) prediction -= this.BitSeqLength / this.ModulationSpeed;

			if (output)
				this.RaisePlotDataReady(receiveDelay, clearSignal, signal, correl);

			return prediction;
		}
    }
}
