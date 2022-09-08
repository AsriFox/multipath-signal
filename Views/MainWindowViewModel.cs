namespace MultipathSignal.Views;

using System;
using System.Linq;
using ReactiveUI;

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
			new PlotViewModel { 
				Title = "Very",
				Points = new[] { new OxyPlot.DataPoint(0, 0), new OxyPlot.DataPoint(0.5, 1) }
			},
			new PlotViewModel {
				Title = "Interesting",
				Points = new[] { new OxyPlot.DataPoint(0, 1), new OxyPlot.DataPoint(1, 0) }
			}
		}
	};

	public void GenerateSignal()
	{
		Core.SignalGenerator.Samplerate = 10000;
		var gen = new Core.SignalModulator {
			Generator = new Core.SignalGenerator {
				Frequency = 1000
			},
			BitRate = 100
		};
		var rand = new Random();
		var signal = gen.Modulate(
			Enumerable.Range(0, 64)
				.Select(_ => rand.NextDouble() > 0.5));

		Plots[0].Points = signal.Select((v, i) => new OxyPlot.DataPoint(i, v)).ToList();
	}
}