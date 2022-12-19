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

        public event Action<double, double, IList<Complex>[]>? PlotDataReady;
		protected internal void RaisePlotDataReady(double delay, double deviation, params IList<Complex>[] plots) => PlotDataReady?.Invoke(delay, deviation, plots);

		public Task<double> ProcessSingle(double delay, double dopplerShift, double snrClean, double snrNoisy, bool useFft)
		{
            StatusChanged?.Invoke("Processing one signal...");
            return this.FindPredictionDeviation(delay, dopplerShift, snrClean, snrNoisy, useFft, true);
        }

		public async Task<double> ProcessMultiple(double delay, double dopplerShift, double snrClean, double snrNoisy, bool useFft, int testsRepeatCount)
		{
            StatusChanged?.Invoke("Processing signals...");
            var stopw = new Stopwatch();
            stopw.Start();
            var tasks = Enumerable.Range(0, testsRepeatCount)
                                  .Select(_ => this.FindPredictionDeviation(delay, dopplerShift, snrClean, snrNoisy, useFft))
                                  .ToList();
            tasks.Add(this.FindPredictionDeviation(delay, dopplerShift, snrClean, snrNoisy, useFft, true));

            var results = await Task.WhenAll(tasks);
            double predictedDelay = results.Average();

            stopw.Stop();
            StatusChanged?.Invoke($"{testsRepeatCount} tasks were completed in {stopw.Elapsed.TotalSeconds:F2} s. ");
            return predictedDelay;
        }

        public async Task<double> ProcessStatistic(double delayBase, double dopplerShift, double snrClean, double snrNoisy, bool useFft, int testsRepeatCount)
        {
            StatusChanged?.Invoke($"Processing signals with Doppler shift = {dopplerShift:F2} Hz");
            var actualDelays = new List<double>();
            var tasks2 = new List<Task<double>>();
            for (int i = 0; i < testsRepeatCount; i++) {
                double delay = delayBase * 2.0 * Utils.RNG.NextDouble();
                actualDelays.Add(delay);
                tasks2.Add(this.FindPredictionDeviation(delay, dopplerShift, snrClean, snrNoisy, useFft));
            }
            actualDelays.Add(delayBase * 2.0 * Utils.RNG.NextDouble());
            tasks2.Add(this.FindPredictionDeviation(actualDelays.Last(), dopplerShift, snrClean, snrNoisy, useFft, true));

            var results2 = await Task.WhenAll(tasks2);
            return results2.Average();
        }
        
        public async Task<double> FindPredictionDeviation(
            double receiveDelay, 
            double dopplerShift, 
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

            gen.MainFrequency += dopplerShift;
            var dirtySignal = await gen.ModulateAsync(baseMod);
            dirtySignal = Utils.ApplyNoise(
                dirtySignal.Skip(
                    (int)(SignalGenerator.Samplerate * (bitDelay * gen.BitLength - receiveDelay))
                ).ToArray(),
                Math.Pow(10.0, 0.1 * snrNoisy)
            );

			if (output)
                this.RaiseStatusChanged("Signal generation is complete. Calculating correlation...");

            var correl = useFft
                ? await CorrelationOverlap.CalculateAsync(dirtySignal, cleanSignal)
                // ? await CorrelationFft.CalculateAsync(dirtySignal, cleanSignal)
                : await Correlation.CalculateAsync(dirtySignal, cleanSignal);
            int corrPeakPos = 0;
            double corrPeak = 0.0;
            for (int i = 0; i < correl.Count; i++) {
                if (correl[i].Magnitude > corrPeak) {
                    corrPeakPos = i;
                    corrPeak = correl[i].Magnitude;
                }
            }

            // Calculate the standard deviation:
            double deviation = correl.Sum(v => (v.Magnitude - corrPeak) * (v.Magnitude - corrPeak)) / correl.Count;
            // Calculate the criterion:
            deviation = corrPeak / Math.Sqrt(deviation);

			if (output)
                this.RaisePlotDataReady(
                    corrPeakPos / SignalGenerator.Samplerate,
                    deviation,
                    cleanSignal,
                    dirtySignal,
                    correl
                );

            return deviation;
		}
    }
}
