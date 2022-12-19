using Avalonia.X11;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace MultipathSignal.Core
{
	internal class Statistics
	{
		public double MainFrequency { get; set; } = 1000.0;
		public double ModulationSpeed { get; set; } = 100.0;
		public SignalModulator.Modulation ModulationType { get; set; } = SignalModulator.Modulation.AM;
		public double ModulationDepth { get; set; } = 0.8;
		public int BitSeqLength { get; set; } = 64;

        public event Action<string>? StatusChanged;
		protected internal void RaiseStatusChanged(string status) => StatusChanged?.Invoke(status);

        public event Action<double, IList<Complex>[]>? PlotDataReady;
		protected internal void RaisePlotDataReady(double delay, params IList<Complex>[] plots) => PlotDataReady?.Invoke(delay, plots);

		public Task<double> ProcessSingle(double delay, double dopplerMag, double snrClean, double snrNoisy, bool useFft)
		{
            StatusChanged?.Invoke("Processing one signal...");
            return this.FindPredictedDelay(delay, dopplerMag, snrClean, snrNoisy, useFft, true);
        }

		public async Task<double> ProcessMultiple(double delay, double dopplerMag, double snrClean, double snrNoisy, bool useFft, int testsRepeatCount)
		{
            StatusChanged?.Invoke("Processing signals...");
            var stopw = new Stopwatch();
            stopw.Start();
            var tasks = Enumerable.Range(0, testsRepeatCount)
                                  .Select(_ => this.FindPredictedDelay(delay, dopplerMag, snrClean, snrNoisy, useFft))
                                  .ToList();
            tasks.Add(this.FindPredictedDelay(delay, dopplerMag, snrClean, snrNoisy, useFft, true));

            var results = await Task.WhenAll(tasks);
            double predictedDelay = results.Sum() / results.Length;

            stopw.Stop();
            StatusChanged?.Invoke($"{testsRepeatCount} tasks were completed in {stopw.Elapsed.TotalSeconds:F2} s. ");
            return predictedDelay;
        }

        public async Task<double> ProcessStatistic(double delayBase, double dopplerMag, double snrClean, double snrNoisy, bool useFft, int testsRepeatCount)
        {
            StatusChanged?.Invoke($"Processing signals with SNR = {snrNoisy:F2} dB");
            var actualDelays = new List<double>();
            var tasks2 = new List<Task<double>>();
            for (int i = 0; i < testsRepeatCount; i++) {
                double delay = delayBase * 2.0 * Utils.RNG.NextDouble();
                actualDelays.Add(delay);
                tasks2.Add(this.FindPredictedDelay(delay, dopplerMag, snrClean, snrNoisy, useFft));
            }
            actualDelays.Add(delayBase * 2.0 * Utils.RNG.NextDouble());
            tasks2.Add(this.FindPredictedDelay(actualDelays.Last(), dopplerMag, snrClean, snrNoisy, useFft, true));

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
            double dopplerMag, 
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
			var baseMod = Utils.RandomBitSeq(bitDelay + 2 * this.BitSeqLength);
            var cleanSignal = await gen.ModulateAsync(
                baseMod.Skip(bitDelay).Take(this.BitSeqLength)
            );
            cleanSignal = Utils.ApplyNoise(cleanSignal, Math.Pow(10.0, 0.1 * snrClean));

            gen.MainFrequency *= (1.0 + dopplerMag);        
            var dirtySignal = await gen.ModulateAsync(baseMod);
            dirtySignal = Utils.ApplyNoise(
                dirtySignal.Skip(
                    (int)(SignalGenerator.Samplerate * (bitDelay * gen.BitLength - receiveDelay))
                ).ToArray(),
                Math.Pow(10.0, 0.1 * snrNoisy)
            );

			if (output)
                this.RaiseStatusChanged("Signal generation is complete. Calculating correlation...");

            // Do correlation here;

			if (output)
			 	this.RaisePlotDataReady(receiveDelay, cleanSignal, dirtySignal);
            return receiveDelay;
			// return prediction;
		}
    }
}
