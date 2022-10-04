using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace MultipathSignal.Views
{
	public class NumberBox : TemplatedControl
	{
		public NumberBox() {
			Header = "null";
			Format = "{0}";
			Minimum = double.NegativeInfinity;
			Maximum = double.PositiveInfinity;
		}

		public readonly static StyledProperty<string> HeaderProperty = AvaloniaProperty.Register<NumberBox, string>(nameof(Header));
		public string Header {
			get => GetValue(HeaderProperty);
			set => SetValue(HeaderProperty, value);
		}

		public readonly static StyledProperty<string> FormatProperty = AvaloniaProperty.Register<NumberBox, string>(nameof(Format));
		public string Format {
			get => GetValue(FormatProperty);
			set => SetValue(FormatProperty, value);
		}

		public readonly static StyledProperty<double> ValueProperty = AvaloniaProperty.Register<NumberBox, double>(nameof(Value));
		public double Value {
			get => GetValue(ValueProperty);
			set => SetValue(ValueProperty, value);
		}

		public readonly static StyledProperty<double> MinimumProperty = AvaloniaProperty.Register<NumberBox, double>(nameof(Minimum));
		public double Minimum {
			get => GetValue(MinimumProperty);
			set => SetValue(MinimumProperty, value);
		}

		public readonly static StyledProperty<double> MaximumProperty = AvaloniaProperty.Register<NumberBox, double>(nameof(Maximum));
		public double Maximum {
			get => GetValue(MaximumProperty);
			set => SetValue(MaximumProperty, value);
		}

		public readonly static StyledProperty<double> StepProperty = AvaloniaProperty.Register<NumberBox, double>(nameof(Step));
		public double Step {
			get => GetValue(StepProperty);
			set => SetValue(StepProperty, value);
		}

		public new void Focus() =>
			(VisualChildren[0]      // DockPanel
			.VisualChildren[^1]     // NumericUpDown
			.VisualChildren[0]      // ButtonSpinner
			.VisualChildren[0]      // Border
			.VisualChildren[0]      // Grid
			.VisualChildren[0]      // ContentPresenter
			.VisualChildren[0]      // TextBox
			as Control)?.Focus();
	}
}
