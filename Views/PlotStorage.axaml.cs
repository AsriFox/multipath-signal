using Avalonia;
using Avalonia.Controls;

namespace MultipathSignal.Views
{
    public partial class PlotStorage : UserControl
    {
        public readonly static StyledProperty<bool> ShowCorrelationProperty = AvaloniaProperty.Register<PlotStorage, bool>(nameof(ShowCorrelation));
        public bool ShowCorrelation {
            get => GetValue(ShowCorrelationProperty);
            set => SetValue(ShowCorrelationProperty, value);
        }

        public PlotStorage()
        {
            InitializeComponent();
        }
    }
}
