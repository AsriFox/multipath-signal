using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel.Design.Serialization;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using ReactiveUI;

namespace MultipathSignal.Views
{ 
	public class PlotViewModel : ReactiveObject
	{
        #region Properties

        private string title = "Empty";
		public string Title {
			get => title;
			set => this.RaiseAndSetIfChanged(ref title, value);
		}

		private AreaSeries? backdrop;
		public AreaSeries? BackdropSeries {
			get => backdrop;
			set => this.RaiseAndSetIfChanged(ref backdrop, value);
		}

		public ObservableCollection<LineSeries> Series { get; } = new();

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

		private double minimumX = double.NaN;
		public double MinimumX {
			get => minimumX;
			set => this.RaiseAndSetIfChanged(ref minimumX, value);
		}

		private double maximumX = double.NaN;
		public double MaximumX {
			get => maximumX;
			set => this.RaiseAndSetIfChanged(ref maximumX, value);
		}
        #endregion

        public PlotViewModel()
		{
			this.PropertyChanged += OnPropertyChanged;
			Series.CollectionChanged += OnCollectionChanged;
		}

		public PlotViewModel(int seriesCount) : this()
		{
			for (int i = 0; i < seriesCount; i++)
				this.CreateSeries();
        }

        public PlotModel Model { get; private set; } = new();

        #region Series handling

        public void CreateSeries(IEnumerable<DataPoint>? points = null, OxyColor? color = null)
		{
			var s = new LineSeries { 
				LineStyle = LineStyle.Solid,
				Color = color ?? OxyColors.Black,
			};
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

        public void ReplacePointsWith(params IEnumerable<DataPoint>[] points) 
		{
            for (int i = 0; i < points.Length && i < Series.Count; i++)
                ReplacePointsOf(i, points[i]);
        }

		public void AppendTo(int i, DataPoint p)
		{
			Series[i].Points.Add(p);
			Model.InvalidatePlot(true);
			this.RaisePropertyChanged(nameof(Model));
		}

        #endregion

        #region Points history

		private readonly List<IEnumerable<DataPoint>[]> history = new();

        public void AddDataPoint(params IEnumerable<DataPoint>[] points)
		{
			history.Add(points);
			SelectDataPoint(history.Count - 1);
        }

        private int selectedIndex = -1;
		public int SelectedIndex {
			get => selectedIndex;
			set => SelectDataPoint(value);
		}

		public void SelectDataPoint(int t)
		{
			if (t >= history.Count || t < 0) return;
			if (t == selectedIndex) return;
			selectedIndex = t;
			ReplacePointsWith(history[t]);
			this.RaisePropertyChanged(nameof(Model));
		}

		public void Clear()
		{
			history.Clear();
			foreach (var s in Series)
				s.Points.Clear();
			selectedIndex = -1;
		}

        #endregion

        #region Event handlers

        private void OnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			switch (e.PropertyName) {
				case nameof(BackdropSeries):
					if (Model.Series.Count > 0 && Model.Series[0] is AreaSeries)
						Model.Series[0] = BackdropSeries;
					else Model.Series.Insert(0, BackdropSeries);
					return;
				case nameof(MinimumY):
				case nameof(MaximumY):
				case nameof(MinimumX):
				case nameof(MaximumX):
					break;
				default:
					return;
			}

			// Model.Series.Clear();
			// Model = new PlotModel();
			Model.Axes.Clear();
			Model.Axes.Add(
				new LinearAxis { 
					Position = AxisPosition.Left, 
					Minimum = minimumY, 
					Maximum = maximumY 
				}
			);
			Model.Axes.Add(
				new LinearAxis { 
					Position = AxisPosition.Bottom, 
					Minimum = minimumX, 
					Maximum = maximumX 
				}
			);
			// foreach (var s in Series)
			// 	Model.Series.Add(s);
			this.RaisePropertyChanged(nameof(Model));
		}

		private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			int shift = BackdropSeries is null ? 0 : 1;
			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Add:
					if (e.NewItems?[0] is Series @sa)
						Model.Series.Insert(e.NewStartingIndex + shift, @sa);
					break;

				case NotifyCollectionChangedAction.Remove:
					Model.Series.RemoveAt(e.OldStartingIndex + shift);
					break;

				case NotifyCollectionChangedAction.Replace:
					if (e.NewItems?[0] is Series @sr)
						Model.Series[e.NewStartingIndex + shift] = @sr;
					break;

				case NotifyCollectionChangedAction.Move:
					if (e.NewItems?[0] is Series @sm) {
						Model.Series.RemoveAt(e.OldStartingIndex + shift);
						Model.Series.Insert(e.NewStartingIndex + shift, @sm);
					}
					break;

				case NotifyCollectionChangedAction.Reset:
					Model.Series.Clear();
					break;
			}
		
			Model.InvalidatePlot(true);
		}

        #endregion
    }
}