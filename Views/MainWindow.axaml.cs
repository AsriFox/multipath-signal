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
					DataContext = @mvvm = new MainWindowViewModel();
				return @mvvm;
			}
		}

		public MainWindow()
		{
			InitializeComponent();
			if (ViewModel is null) throw new System.NullReferenceException();
		}

		private void PointerMoveFocus(object? sender, Avalonia.Input.PointerEventArgs e)
		{
			if (sender is NumberBox numbox) numbox.Focus();
			else (sender as Avalonia.Controls.Control)?.Focus();
		}
	}
}