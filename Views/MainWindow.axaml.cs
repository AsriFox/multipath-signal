namespace MultipathSignal.Views
{
	public partial class MainWindow : Avalonia.Controls.Window
	{
		/// <summary>
		/// Access the view model, or create a new one if it was missing.
		/// </summary>
		private MainWindowViewModel ViewModel {
			get {
				if (DataContext is not MainWindowViewModel @mvvm)
					DataContext = @mvvm = MainWindowViewModel.Default;
				return @mvvm;
			}
		}

		public MainWindow()
		{
			InitializeComponent();
			if (ViewModel is null) throw new System.NullReferenceException();
		}
	}
}