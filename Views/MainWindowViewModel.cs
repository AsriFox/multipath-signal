using System;
using System.Linq;
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
			Plots.CollectionChanged += (_, _) => this.RaisePropertyChanged(nameof(Plots));
		}

		/// <summary>
		/// The default view model for this MainWindow.
		/// Contains two linear plots for the signals.
		/// </summary>
		public static MainWindowViewModel Default => new() {
			Plots = { 
				new PlotViewModel { Title = "Clean signal" },
				new PlotViewModel { Title = "Noisy signal" },
			}
		};

		public async void GenerateSignal()
		{
			Core.SignalGenerator.Samplerate = Samplerate;
			var gen = new Core.SignalModulator {
				MainFrequency = MainFrequency,
				BitRate = ModulationSpeed,
				Method = ModulationType,
				Depth = ModulationDepth
			};

			Status = "Signals generation has started...";

			int bitDelay = (int) Math.Ceiling(ReceiveDelay * Samplerate / ModulationSpeed);
			var signal = await gen.ModulateAsync(
				Core.Utils.RandomBitSeq(
					bitDelay + BitSeqLength + Core.Utils.RNG.Next(bitDelay, BitSeqLength)));

			Status = "Signals were generated successfully. Plotting...";

			int initDelay = (int)(bitDelay * Samplerate / ModulationSpeed);
			Plots[0].Points = Core.NoiseGenerator.Apply(
				signal.Skip(initDelay)
					  .Take((int)(BitSeqLength * Samplerate / ModulationSpeed))
					  .ToList(),
				Math.Pow(10.0, 0.1 * SNRClean))
					.Select((v, i) => new OxyPlot.DataPoint(i, v));

			Plots[1].Points = Core.NoiseGenerator.Apply(
				signal.Skip(initDelay - (int)(ReceiveDelay * Samplerate))
					  .ToList(),
				Math.Pow(10, 0.1 * SNRNoisy))
					.Select((v, i) => new OxyPlot.DataPoint(i, v));

			Status = "Signal generation is complete. Ready.";
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
	}
}