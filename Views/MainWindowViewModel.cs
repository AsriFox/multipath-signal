using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using MultipathSignal.Core;
using OxyPlot;
using ReactiveUI;

namespace MultipathSignal.Views
{
	internal class MainWindowViewModel : ReactiveObject
	{
		// public PlotStorageViewModel PlotStorage { get; private set; } = new();

		public System.Collections.ObjectModel.ObservableCollection<PlotViewModel> Plots { get; }

		public MainWindowViewModel()
		{
			Plots = new() {
				new PlotViewModel { Title = "Clean signal: I", MinimumY = -1, MaximumY = 1 },
				new PlotViewModel { Title = "Clean signal: Q", MinimumY = -1, MaximumY = 1 },
				new PlotViewModel { Title = "Dirty signal: I", MinimumY = -1, MaximumY = 1 },
				new PlotViewModel { Title = "Dirty signal: Q", MinimumY = -1, MaximumY = 1 },
				new PlotViewModel { Title = "Correlation", MinimumY = 0.0 },
				new PlotViewModel { Title = "Statistics", MinimumY = 0.0 },
			};
			Plots[0].CreateSeries(null, OxyColors.DarkBlue);
			Plots[1].CreateSeries(null, OxyColors.DarkBlue);
			Plots[2].CreateSeries(null, OxyColors.OrangeRed);
			Plots[2].BackdropSeries = new OxyPlot.Series.AreaSeries {
				Fill = OxyColors.LightSalmon,
			};
			Plots[3].CreateSeries(null, OxyColors.OrangeRed);
			Plots[3].BackdropSeries = new OxyPlot.Series.AreaSeries {
				Fill = OxyColors.LightSalmon,
			};
			Plots[4].CreateSeries(null, OxyColors.Cyan);
			Plots[4].CreateSeries(null, OxyColors.Blue);
			Plots[5].CreateSeries(null, OxyColors.Black);
			Plots.CollectionChanged += (_, _) => this.RaisePropertyChanged(nameof(Plots));
		}

		/// <summary>
		/// Click event handler for "Launch simulation" button
		/// </summary>
		public async void Process()
		{
			EditMode = false;
			SignalGenerator.Samplerate = Samplerate;
			Statistics stat = new() {
				MainFrequency = MainFrequency,
				ModulationSpeed = ModulationSpeed,
				ModulationType = ModulationType,
				ModulationDepth = ModulationDepth,
				BitSeqLength = BitSeqLength
			};
			stat.StatusChanged += OnStatusChanged;
			stat.PlotDataReady += OnPlotDataReady;

			foreach (var p in Plots) p.Clear();

			try {
				double stab;
				switch (SimulationMode) {
					case 0:     // Single test
						stab = await stat.ProcessSingle(ReceiveDelay, DopplerShift, SNRClean, SNRNoisy, useFft);
						Status += $"Stability: {stab:F4}";
						break;

					case 1:     // Multiple tests
						stab = await stat.ProcessMultiple(ReceiveDelay, DopplerShift, SNRClean, SNRNoisy, useFft, TestsRepeatCount);
                        Status += $"Average stability: {stab:F4}";
                        break;

					case 2:     // Gather statistics
						Plots[5].AddDataPoint(new IEnumerable<DataPoint>[] { new List<DataPoint>() });
						double dmag = DopplerShift;
						double dmagMax = DopplerShiftMax + 0.5 * DopplerShiftStep;
						while (dmag < dmagMax) {
							if (Utils.Cancellation.IsCancellationRequested) break;
							stab = await stat.ProcessStatistic(ReceiveDelay, dmag, SNRClean, SNRNoisy, useFft, TestsRepeatCount);
							Plots[5].AppendTo(0, new DataPoint(dmag, stab));
							DopplerMagnitudeShown = dmag;
							dmag += DopplerShiftStep;
						}
						break;
				}
			}
			catch (TaskCanceledException) {
				Status = "Operation was cancelled.";
				Utils.Cancellation = new();
			}
			EditMode = true;
		}

		/// <summary>
		/// Click event handler for "Stop simulation" button
		/// </summary>
		public void StopProcess() => Utils.Cancellation.Cancel();

		public void OnStatusChanged(string status) => Status = status;

		public void OnPlotDataReady(double delay, double deviation, IList<Complex>[] plots)
		{
			Status = "Data was generated successfully. Plotting...";
			SignalGenerator g = new() {
				Frequency = MainFrequency,
				Phase = 0.0,
			};
			double max = plots[0].Max(c => c.Magnitude);
			var cleanSignalI = new double[plots[0].Count];
			var cleanSignalQ = new double[plots[0].Count];
			for (int i = 0; i < plots[0].Count; i++) {
				var c = plots[0][i] / g.GetNextSample();
				cleanSignalI[i] = c.Real;
				cleanSignalQ[i] = c.Imaginary;
			}
			Plots[0].AddDataPoint(cleanSignalI.Plotify());
			Plots[1].AddDataPoint(cleanSignalQ.Plotify());
			Plots[0].MaximumY = max;
			Plots[0].MinimumY = -max;
			Plots[1].MaximumY = max;
			Plots[1].MinimumY = -max;

			g.Phase = 0.0;
			max = plots[1].Max(c => c.Magnitude);
			var dirtySignalI = new double[plots[1].Count];
			var dirtySignalQ = new double[plots[1].Count];
			for (int i = 0; i < plots[1].Count; i++) {
				var c = plots[1][i] / g.GetNextSample();
				dirtySignalI[i] = c.Real;
				dirtySignalQ[i] = c.Imaginary;
			}
			Plots[2].AddDataPoint(dirtySignalI.Plotify());
			Plots[3].AddDataPoint(dirtySignalQ.Plotify());
			Plots[2].MaximumY = max;
			Plots[2].MinimumY = -max;
			Plots[3].MaximumY = max;
			Plots[3].MinimumY = -max;

			double signalDuration = BitSeqLength / ModulationSpeed;
			OxyPlot.Series.AreaSeries bs1 = new() {
				Color = OxyColors.SteelBlue
			};
			bs1.Points.Add(new(delay, -2 * max));
			bs1.Points.Add(new(delay, 2 * max));
			bs1.Points.Add(new(delay + signalDuration, 2 * max));
			bs1.Points.Add(new(delay + signalDuration, -2 * max));
			bs1.ConstantY2 = -2 * max;
			Plots[2].BackdropSeries = bs1;

			OxyPlot.Series.AreaSeries bs2 = new() {
				Color = OxyColors.SteelBlue
			};
			bs2.Points.Add(new(delay, -2 * max));
			bs2.Points.Add(new(delay, 2 * max));
			bs2.Points.Add(new(delay + signalDuration, 2 * max));
			bs2.Points.Add(new(delay + signalDuration, -2 * max));
			bs2.ConstantY2 = -2 * max;
			Plots[3].BackdropSeries = bs2;

			var correl = plots[2].Select(c => c.Magnitude).ToArray();
			double ceil = correl.Max();
			// double threshold = 0.5 * ceil / deviation;
			Plots[4].AddDataPoint(
				correl.Plotify(),
				new DataPoint[] {
					new(delay, 0.0),
					new(delay, ceil),
				}
			);
			Status = $"Procedure was completed. Predicted delay: {delay:F4}s; ";
		}

		public async void CalculateFunc3D()
		{
			SignalGenerator.Samplerate = Samplerate;
			var gen = new SignalModulator() {
				MainFrequency = MainFrequency,
				BitRate = ModulationSpeed,
				Method = ModulationType,
				Depth = ModulationDepth
			};

			int M = 100;
			var samplesTime = Enumerable.Range(0, M)
				.Select(i => BitSeqLength * 2 * gen.BitLength * i / (M - 1))
				.ToArray();

			int N = (int)(DopplerShiftMax / DopplerShiftStep);
			var samplesDoppler = Enumerable.Range(0, N)
				.Select(j => DopplerShiftMax * j / (N - 1))
				.ToArray();

			await Task.Factory.StartNew(
				() => OpenGlPage.CreateAmbiguityFuncPage(
					gen,			BitSeqLength, 
					ReceiveDelay,	DopplerShift,
					samplesTime, 	samplesDoppler,
					SNRClean,		SNRNoisy
				)
			);
			// await Task.Factory.StartNew(() => OpenGlPage.CreateTeapot());
		}

		#region Modulation parameters

		private SignalModulator.Modulation modulationType = SignalModulator.Modulation.AM;
        public SignalModulator.Modulation ModulationType {
            get => modulationType;
            set => this.RaiseAndSetIfChanged(ref modulationType, value);
        }

        private double modulationDepth = 0.4;
		public double ModulationDepth {
			get => modulationDepth;
			set => this.RaiseAndSetIfChanged(ref modulationDepth, value);
		}
		
		private double modulationSpeed = 1080;
		public double ModulationSpeed {
			get => modulationSpeed;
			set => this.RaiseAndSetIfChanged(ref modulationSpeed, value);
		}

		private int bitSeqLength = 50;
		public int BitSeqLength {
			get => bitSeqLength;
			set => this.RaiseAndSetIfChanged(ref bitSeqLength, value);
		}

		private double mainFrequency = 10000.0;
		public double MainFrequency {
			get => mainFrequency;
			set => this.RaiseAndSetIfChanged(ref mainFrequency, value);
		}

		private double samplerate = 131072.0;
		public double Samplerate {
			get => samplerate;
			set => this.RaiseAndSetIfChanged(ref samplerate, value);
		}

		#endregion

		#region Simulation parameters

		private int simulationMode = 0;
        public int SimulationMode {
            get => simulationMode;
            set => this.RaiseAndSetIfChanged(ref simulationMode, value);
        }

        private double receiveDelay = 0.02;
        public double ReceiveDelay {
            get => receiveDelay;
            set => this.RaiseAndSetIfChanged(ref receiveDelay, value);
        }

        private double snrClean = 10.0;
        public double SNRClean {
            get => snrClean;
            set => this.RaiseAndSetIfChanged(ref snrClean, value);
        }

        private double snrNoisy = -10.0;
        public double SNRNoisy {
            get => snrNoisy;
            set => this.RaiseAndSetIfChanged(ref snrNoisy, value);
        }

		private double dopplerShift = 100.0;
		public double DopplerShift {
			get => dopplerShift;
			set => this.RaiseAndSetIfChanged(ref dopplerShift, value);
		}

        private double dopplerShiftMax = 1000.0;
        public double DopplerShiftMax {
            get => dopplerShiftMax;
            set => this.RaiseAndSetIfChanged(ref dopplerShiftMax, value);
        }

        private double dopplerShiftStep = 50.0;
        public double DopplerShiftStep {
            get => dopplerShiftStep;
            set => this.RaiseAndSetIfChanged(ref dopplerShiftStep, value);
        }

        private int testsRepeatCount = 200;
        public int TestsRepeatCount {
            get => testsRepeatCount;
            set => this.RaiseAndSetIfChanged(ref testsRepeatCount, value);
        }

		#endregion

		private string status = "Ready.";
		public string Status { 
			get => status; 
			set => this.RaiseAndSetIfChanged(ref status, value);
		}

		private bool editMode = true;
		public bool EditMode { 
			get => editMode;
			set => this.RaiseAndSetIfChanged(ref editMode, value);
		}

		private bool useFft = true;
		public bool UseFFT {
			get => useFft;
			set => this.RaiseAndSetIfChanged(ref useFft, value);
		}

		private double dopplerMagShown = 0.0;
		public double DopplerMagnitudeShown {
			get => dopplerMagShown;
			set {
				this.RaiseAndSetIfChanged(ref dopplerMagShown, value);
				int sel = (int)((dopplerMagShown - DopplerShift) / DopplerShiftStep);
				foreach (var plot in Plots)
					plot.SelectDataPoint(sel);
			}
		}
	}
}