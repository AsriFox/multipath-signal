namespace MultipathSignal;
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
}