using Avalonia.Threading;
using ReactiveUI;
using System.Collections.Generic;
using System.Linq;
using MultipathSignal.Core;

namespace MultipathSignal.Views
{
    internal class PlotStorageViewModel : ReactiveObject
    {
        private readonly List<IEnumerable<OxyPlot.DataPoint>[]> history = new();

        /// <summary>
        /// Collection of view models for PlotView elements.
        /// </summary>
        public System.Collections.ObjectModel.ObservableCollection<PlotViewModel> Plots { get; private set; } = new();

        public PlotStorageViewModel()
        {
            Plots = new() {
                new PlotViewModel { Title = "Clean signal" },
                new PlotViewModel { Title = "Noisy signal" },
                new PlotViewModel { Title = "Correlation" },
                new PlotViewModel { Title = "Statistics", MinimumY = 0.0, MaximumY = 1.0 },
            };
            Plots.CollectionChanged += (_, _) => this.RaisePropertyChanged(nameof(Plots));
        }

        public void OnPlotDataReady(params IList<double>[] points)
        {
            history.Add(points.Select(pts => pts.Plotify()).ToArray());
            Dispatcher.UIThread.InvokeAsync(() => {
                for (int i = 0; i < points.Length && i < Plots.Count; i++)
                    Plots[i].Points = points[i].Plotify();
            });
        }

        int selectedIndex = -1;
        public void Select(int index)
        {
            if (index >= history.Count || index < 0) return;
            if (index == selectedIndex) return;
            selectedIndex = index;
            for (int i = 0; i < history[index].Length && i < Plots.Count; i++)
                Plots[i].Points = history[index][i];
        }

        public void Clear()
        {
            history.Clear();
            selectedIndex = -1;
            foreach (var plot in Plots)
                plot.Points = new List<OxyPlot.DataPoint>();
        }
    }
}
