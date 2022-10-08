using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Avalonia.Input.TextInput;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using ReactiveUI;

namespace MultipathSignal.Views
{ 
	public class PlotViewModel : ReactiveObject
	{
		public readonly static OxyColor[] SeriesColors = new[] { OxyColors.Blue, OxyColors.Red, OxyColors.Cyan, OxyColors.DarkGreen };

		private string title = "Empty";
		public string Title {
			get => title;
			set => this.RaiseAndSetIfChanged(ref title, value);
		}

		public ObservableCollection<LineSeries> Series { get; } = new();

		//get => Series[];
		//set => this.RaiseAndSetIfChanged(ref points, 
		//	value as ObservableCollection<DataPoint> 
		//	?? new ObservableCollection<DataPoint>(value));
		

		private double minimumY = double.NaN;
		public double MinimumY {
			get => minimumY;
			set => this.RaiseAndSetIfChanged(ref minimumY, value);
		}

		private double maximumY = double.NaN;
		public double MaximumY {
			get => maximumY;
			set => this.RaiseAndSetIfChanged(ref maximumY, value);
		}

		public PlotViewModel()
		{
			this.PropertyChanged += OnPropertyChanged;
			Series.CollectionChanged += OnCollectionChanged;
		}

		public PlotViewModel(int seriesCount) : base() 
		{
			for (int i = 0; i < seriesCount; i++)
				this.CreateSeries();
		}

		public PlotModel Model { get; private set; } = new();

		public void CreateSeries(IEnumerable<DataPoint>? points = null, OxyColor? color = null)
		{
			var s = new LineSeries { LineStyle = LineStyle.Solid };
			try { 
				s.Color = color ?? SeriesColors[Series.Count];
			}
			catch (IndexOutOfRangeException) {
				s.Color = OxyColors.Black;
			}
			if (points is not null)
				s.Points.AddRange(points);
			Series.Add(s);
		}

		public IList<DataPoint> PointsOf(int i) => Series[i].Points;

		public void ReplacePointsOf(int i, IEnumerable<DataPoint> points)
		{
			var s = new LineSeries {
				LineStyle = Series[i].LineStyle,
				Color = Series[i].Color
			};
			s.Points.AddRange(points);
			Series[i] = s;
		}

		private void OnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			switch (e.PropertyName) {
				case nameof(MinimumY):
				case nameof(MaximumY):
					break;
				default:
					return;
			}
			Model = new PlotModel();
			Model.Axes.Add(
				new LinearAxis { 
					Position = AxisPosition.Left, 
					Minimum = minimumY, 
					Maximum = maximumY 
				}
			);
			foreach (var s in Series)
				Model.Series.Add(s);
			this.RaisePropertyChanged(nameof(Model));
		}

		private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			switch (e.Action) 
			{
				case NotifyCollectionChangedAction.Add:
					if (e.NewItems?[0] is Series @sa)
						Model.Series.Insert(e.NewStartingIndex, @sa);
					break;

				case NotifyCollectionChangedAction.Remove:
					Model.Series.RemoveAt(e.OldStartingIndex);
					break;

				case NotifyCollectionChangedAction.Replace:
					if (e.NewItems?[0] is Series @sr)
						Model.Series[e.NewStartingIndex] = @sr;
					break;

				case NotifyCollectionChangedAction.Move:
					if (e.NewItems?[0] is Series @sm) {
						Model.Series.RemoveAt(e.OldStartingIndex);
						Model.Series.Insert(e.NewStartingIndex, @sm);
					}
					break;

				case NotifyCollectionChangedAction.Reset:
					Model.Series.Clear();
					break;
			}
		
			Model.InvalidatePlot(true);
		}
	}
}