using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Animation.Animators;
using MultipathSignal.Core;
using ReactiveUI;

namespace MultipathSignal.Views
{
	internal class MainWindowViewModel : ReactiveObject
	{
		/// <summary>
		/// Collection of view models for PlotView elements.
		/// </summary>
		public System.Collections.ObjectModel.ObservableCollection<PlotViewModel> Plots { get; private set; } = new();

		public MainWindowViewModel()
		{
			Plots = new() {
				new PlotViewModel { Title = "Clean signal" },
				new PlotViewModel { Title = "Noisy signal" },
				new PlotViewModel { Title = "Correlation" },
				new PlotViewModel { Title = "Statistics" },
			};
			Plots.CollectionChanged += (_, _) => this.RaisePropertyChanged(nameof(Plots));
		}

		public async Task<double> FindPredictedDelay(bool output = false)
		{
			var gen = new SignalModulator {
				MainFrequency = MainFrequency,
				BitRate = ModulationSpeed,
				Method = ModulationType,
				Depth = ModulationDepth
			};

			if (output)
				Status = "Signals generation has started...";

			int bitDelay = (int)Math.Ceiling(ReceiveDelay * Samplerate / ModulationSpeed);
			var signal = await gen.ModulateAsync(
				Utils.RandomBitSeq(
					bitDelay + BitSeqLength + Utils.RNG.Next(bitDelay, BitSeqLength)));
			
			int initDelay = (int)(bitDelay * Samplerate / ModulationSpeed);

			var clearSignal = NoiseGenerator.Apply(
								  signal.Skip(initDelay)
										.Take((int)(BitSeqLength * Samplerate / ModulationSpeed))
										.ToList(),
								  Math.Pow(10.0, 0.1 * SNRClean));

			signal = NoiseGenerator.Apply(
						 signal.Skip(initDelay - (int)(ReceiveDelay * Samplerate))
							   .ToList(),
						 Math.Pow(10, 0.1 * SNRNoisy));

			if (output)
				Status = "Signal generation is complete. Calculating correlation...";

			var correl = await Statistics.CorrelationAsync(signal, clearSignal);

			int maxPos = 0;
			double maxVal = 0.0;
			for (int i = 0; i < correl.Count; i++)
				if (Math.Abs(correl[i]) > maxVal)
				{
					maxVal = Math.Abs(correl[i]);
					maxPos = i;
				}
			var prediction = maxPos / Samplerate;

			if (output) {
				Status = "Data was generated successfully. Plotting...";

				Plots[0].Points = clearSignal
					.Select((v, i) => new OxyPlot.DataPoint(i / Samplerate, v));

				Plots[1].Points = signal
					.Select((v, i) => new OxyPlot.DataPoint(i / Samplerate, v));

				Plots[2].Points = correl
					.Select((v, i) => new OxyPlot.DataPoint(i / Samplerate, v));

				Status = "Procedure was completed. Ready.";
			}

			return prediction;
		}

		public async void ProcessSignal() {
			EditMode = false;
			SignalGenerator.Samplerate = Samplerate;
			PredictedDelay = await FindPredictedDelay(true);
			EditMode = true;
		}

		public async void ProcessRepeat()
		{
			EditMode = false;
			SignalGenerator.Samplerate = Samplerate;
			Status = "Processing signals...";

			var stopw = new System.Diagnostics.Stopwatch();
			stopw.Start();

			var tasks = Enumerable.Range(0, TestsRepeatCount)
								  .Select(_ => FindPredictedDelay())
								  .ToList();
			tasks.Add(FindPredictedDelay(true));

			var results = await Task.WhenAll(tasks);
			PredictedDelay = results.Sum() / results.Length;

			stopw.Stop();
			Status = $"{TestsRepeatCount} tasks were completed in {stopw.Elapsed.TotalSeconds:F2} s. Ready.";
			EditMode = true;
		}

		#region Modulation parameters

		public Core.SignalModulator.Modulation ModulationType { get; set; } = Core.SignalModulator.Modulation.OOK;

		public double ModulationDepth { get; set; } = 0.8;

		public double ModulationSpeed { get; set; } = 100.0;

		public int BitSeqLength { get; set; } = 64;

		public double MainFrequency { get; set; } = 1000.0;

		public double Samplerate { get; set; } = 10000.0;

		#endregion

		#region Simulation parameters

		public double ReceiveDelay { get; set; } = 0.08;

		public double SNRClean { get; set; } = 10.0;

		public double SNRNoisy { get; set; } = -10.0;

		public int TestsRepeatCount { get; set; } = 1000;

		#endregion

		private double predictedDelay = double.NaN;
		public double PredictedDelay {
			get => predictedDelay; 
			set => this.RaiseAndSetIfChanged(ref predictedDelay, value); 
		}

		private string status = "Ready.";
		public string Status { 
			get => status; 
			set => this.RaiseAndSetIfChanged(ref status, value);
		}

		private bool editMode = true;
		public bool EditMode { 
			get => editMode;
			set => this.RaiseAndSetIfChanged(ref editMode, value);
		}
	}
}