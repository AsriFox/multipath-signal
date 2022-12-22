namespace MultipathSignal.Views
{
	using System.IO;
	using System;
	using System.Diagnostics;
	using System.Linq;
	using System.Numerics;
	using System.Runtime.InteropServices;

	public partial class MainWindow : Avalonia.Controls.Window
	{
		/// <summary>
		/// Access the view model, or create a new one if it was missing.
		/// </summary>
		private MainWindowViewModel ViewModel {
			get {
				if (DataContext is not MainWindowViewModel @mwvm)
					DataContext = @mwvm = new MainWindowViewModel();
				return @mwvm;
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
		
		private void CalculateFunc3D(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
		{
			float[] coords;
			ushort[] indices;

			string name = typeof(OpenGlPage).Assembly
				.GetManifestResourceNames()
				.First(x => x.Contains("teapot.bin"));
			using (BinaryReader sr = new(
				typeof(OpenGlPage).Assembly
					.GetManifestResourceStream(name) 
					?? throw new NullReferenceException()
			)) {
			    var buf = new byte[sr.ReadInt32()];
			    sr.Read(buf, 0, buf.Length);
			    coords = new float[buf.Length / 4];
			    Buffer.BlockCopy(buf, 0, coords, 0, buf.Length);

			    buf = new byte[sr.ReadInt32()];
			    sr.Read(buf, 0, buf.Length);
			    indices = new ushort[buf.Length / 2];
			    Buffer.BlockCopy(buf, 0, indices, 0, buf.Length);
			}

			var points = new Vector3[coords.Length / 3];
			for (var primitive = 0; primitive < coords.Length / 3; primitive++) {
				var srci = primitive * 3;
				points[primitive] = new Vector3(
					coords[srci], 
					coords[srci + 1], 
					coords[srci + 2]
				);
			}
			var resultWindow = new OpenGlPage(points, indices);
			// var resultWindow = OpenGlPage.FromValues(results);
			resultWindow.ShowDialog(this);
		}
	}
}