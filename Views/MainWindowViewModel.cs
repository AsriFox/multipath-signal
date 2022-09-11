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

		public void GenerateSignal()
		{
			Core.SignalGenerator.Samplerate = Samplerate;
			var gen = new Core.SignalModulator {
				MainFrequency = MainFrequency,
				BitRate = ModulationSpeed,
				Method = ModulationType,
				Depth = ModulationDepth
			};
			var rand = new Random();
			var signal = gen.Modulate(
				Enumerable.Range(0, BitSeqLength)
					.Select(_ => rand.NextDouble() > 0.5));

			Plots[0].Points = signal.Select((v, i) => new OxyPlot.DataPoint(i, v)).ToList();
		}

		public Core.SignalModulator.Modulation ModulationType { get; set; } = Core.SignalModulator.Modulation.OOK;

		public double ModulationDepth { get; set; } = 0.8;

		public double ModulationSpeed { get; set; } = 100.0;

		public int BitSeqLength { get; set; } = 64;

		public double MainFrequency { get; set; } = 1000.0;

		public double Samplerate { get; set; } = 10000.0;
	}
}