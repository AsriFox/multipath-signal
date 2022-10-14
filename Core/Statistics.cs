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

        public event Action<IList<double>, IList<double>, IList<double>>? PlotDataReady;
		protected internal void RaisePlotDataReady(IList<double> data1, IList<double> data2, IList<double> data3) => PlotDataReady?.Invoke(data1, data2, data3);

		public Task<double> ProcessSingle(double delay, double snrClean, double snrNoisy)
		{
            StatusChanged?.Invoke("Processing one signal...");
            return this.FindPredictedDelay(delay, snrClean, snrNoisy, true);
        }

		public async Task<double> ProcessMultiple(double delay, double snrClean, double snrNoisy, int testsRepeatCount)
		{
            StatusChanged?.Invoke("Processing signals...");
            var stopw = new Stopwatch();
            stopw.Start();
            var tasks = Enumerable.Range(0, testsRepeatCount)
                                  .Select(_ => this.FindPredictedDelay(delay, snrClean, snrNoisy))
                                  .ToList();
            tasks.Add(this.FindPredictedDelay(delay, snrClean, snrNoisy, true));

            var results = await Task.WhenAll(tasks);
            double predictedDelay = results.Sum() / results.Length;

            stopw.Stop();
            StatusChanged?.Invoke($"{testsRepeatCount} tasks were completed in {stopw.Elapsed.TotalSeconds:F2} s. Ready.");
            return predictedDelay;
        }

        public async Task<double> ProcessStatistic(double delayBase, double snrClean, double snrNoisy, int testsRepeatCount)
        {
            StatusChanged?.Invoke($"Processing signals with SNR = {snrNoisy:F2} dB");
            var actualDelays = new List<double>();
            var tasks2 = new List<Task<double>>();
            for (int i = 0; i < testsRepeatCount; i++) {
                double delay = delayBase * 2.0 * Utils.RNG.NextDouble();
                actualDelays.Add(delay);
                tasks2.Add(this.FindPredictedDelay(delay, snrClean, snrNoisy));
            }
            actualDelays.Add(delayBase * 2.0 * Utils.RNG.NextDouble());
            tasks2.Add(this.FindPredictedDelay(actualDelays.Last(), snrClean, snrNoisy, true));

            var results2 = await Task.WhenAll(tasks2);
            double threshold = 0.5 / ModulationSpeed;
            int gotitCount = 0;
            for (int i = 0; i < results2.Length; i++)
                if (Math.Abs(results2[i] - actualDelays[i]) < threshold)
                    gotitCount++;
            return (double)gotitCount / results2.Length;
        }
    }
}
