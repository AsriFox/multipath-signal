namespace MultipathSignal;

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using ReactiveUI;

internal class PlotViewModel : ReactiveObject
{
	private string title = "Empty";
	public string Title {
		get => title;
		set => this.RaiseAndSetIfChanged(ref title, value);
	}

	private ObservableCollection<DataPoint> points = new();
	public System.Collections.Generic.IList<DataPoint> Points {
		get => points;
		set => this.RaiseAndSetIfChanged(ref points, 
			value as ObservableCollection<DataPoint> 
			?? new ObservableCollection<DataPoint>(value));
	}

	public PlotViewModel()
	{
		this.PropertyChanged += OnPropertyChanged;
	}

	public PlotModel Model { get; private set; } = new();

	private void OnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
	{
		if (e.PropertyName != nameof(Points)) return;

		points.CollectionChanged += OnCollectionChanged;

		Model = new PlotModel();
		Model.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = 0, Maximum = 1 });

		var s = new LineSeries { LineStyle = LineStyle.Solid, Color = OxyColors.Blue };
		s.Points.AddRange(points);
		Model.Series.Add(s);
	}

	private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		if (Model.Series.Count != 1) throw new System.IndexOutOfRangeException($"Unpredictable series collection: {Model.Series} is not 1 element");

		var s = Model.Series[0] as LineSeries ?? throw new System.InvalidCastException($"Unexpected type: {Model.Series[0]} is not LineSeries");

		switch (e.Action)
		{
			case NotifyCollectionChangedAction.Add:
				if (e.NewItems?[0] is DataPoint @pa)
					s.Points.Insert(e.NewStartingIndex, @pa);
				break;

			case NotifyCollectionChangedAction.Remove:
				s.Points.RemoveAt(e.OldStartingIndex);
				break;

			case NotifyCollectionChangedAction.Replace:
				if (e.NewItems?[0] is DataPoint @pr)
					s.Points[e.NewStartingIndex] = @pr;
				break;

			case NotifyCollectionChangedAction.Move:
				if (e.NewItems?[0] is DataPoint @pm) {
					s.Points.RemoveAt(e.OldStartingIndex);
					s.Points.Insert(e.NewStartingIndex, @pm);
				}
				break;

			case NotifyCollectionChangedAction.Reset:
				s.Points.Clear();
				break;
		}
		
		Model.InvalidatePlot(true);
	}
}
