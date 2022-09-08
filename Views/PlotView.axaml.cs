using Avalonia;
using ReactiveUI;

namespace MultipathSignal.Views
{
	public partial class PlotView : Avalonia.Controls.UserControl
	{
		public PlotView()
		{
			InitializeComponent();

			DataContextChanged += (s, _) => {
				if (s is not StyledElement @se)
					throw new System.InvalidOperationException();

				if (@se.DataContext is PlotViewModel @pvm)
					@pvm.PropertyChanged += OnPropertyChanged;
			};
		}

		private void OnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			ThePlot.InvalidatePlot(true);
		}
	}
}