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
				new PlotViewModel(1) { Title = "Clean signal", MinimumY = -Math.PI, MaximumY = Math.PI },
				new PlotViewModel(2) { Title = "Dirty signal" },
				new PlotViewModel(1) { Title = "Statistics", MinimumY = 0.0, MaximumY = 1.0 },
			};
			Plots[0].Series[0].Color = OxyColors.ForestGreen;
			Plots[1].Series[0].Color = OxyColors.LightSalmon;
			Plots[1].Series[1].Color = OxyColors.DarkBlue;
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

			try {
				double predictedDelay;
				switch (SimulationMode) {
					case 0:     // Single test
						predictedDelay = await stat.ProcessSingle(ReceiveDelay, DopplerMagnitude, SNRClean, SNRNoisy, useFft);
						Status = $"Predicted delay: {predictedDelay:F4}s";
						break;

					case 1:     // Multiple tests
						predictedDelay = await stat.ProcessMultiple(ReceiveDelay, DopplerMagnitude, SNRClean, SNRNoisy, useFft, TestsRepeatCount);
                        Status += $"Average predicted delay: {predictedDelay:F4}s";
                        break;

					case 2:     // Gather statistics
						Plots[0].Clear();
						Plots[1].Clear();
						Plots[2].Clear();
						Plots[2].AddDataPoint(new IEnumerable<DataPoint>[] { new List<DataPoint>() });
						double snr = SNRNoisy;
						double snrMax = SNRNoisyMax + 0.5 * SNRNoisyStep;
						while (snr < snrMax) {
							if (Utils.Cancellation.IsCancellationRequested) break;
							double gotitPercent = await stat.ProcessStatistic(ReceiveDelay, DopplerMagnitude, SNRClean, snr, useFft, TestsRepeatCount);
							Plots[2].AppendTo(0, new DataPoint(snr, gotitPercent));
							SNRShown = snr;
							snr += SNRNoisyStep;
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

		public void OnPlotDataReady(double delay, IList<Complex>[] plots)
		{
			Status = "Data was generated successfully. Plotting...";
			SignalGenerator g = new() {
				Frequency = MainFrequency,
				Phase = 0.0,
			};
			var cleanSignal = new double[plots[0].Count];
			for (int i = 0; i < cleanSignal.Length; i++) {
				var c = plots[0][i] / g.GetNextSample();
				cleanSignal[i] = c.Phase;
			}
			Plots[0].AddDataPoint(cleanSignal.Plotify());

			g.Phase = 0.0;
			var dirtySignal = new double[plots[1].Count];
			for (int i = 0; i < dirtySignal.Length; i++) {
				var c = plots[1][i] / g.GetNextSample();
				dirtySignal[i] = c.Phase;
			}
			Plots[1].AddDataPoint(
				dirtySignal.Plotify(),
				new DataPoint[] {
					new(delay, -Math.PI),
					new(delay, Math.PI)
				}
			);

			// double ceil = plots[2].Max();
			// double threshold = 0.5 / ModulationSpeed;
			// var delayLimits = new double[(int)(2 * threshold * Samplerate)];
			// for (int t = 1; t < delayLimits.Length - 1; t++)
			// 	delayLimits[t] = ceil;

			// Plots[1].AddDataPoint(
			// 	plots[2].Plotify(),
			// 	delayLimits.Plotify(delay - threshold)
			// );
			// Plots[1].MaximumX = plots[1].Count / Samplerate;
			Status = "Procedure was completed. Ready.";
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
		
		private double modulationSpeed = 100;
		public double ModulationSpeed {
			get => modulationSpeed;
			set => this.RaiseAndSetIfChanged(ref modulationSpeed, value);
		}

		private int bitSeqLength = 64;
		public int BitSeqLength {
			get => bitSeqLength;
			set => this.RaiseAndSetIfChanged(ref bitSeqLength, value);
		}

		private double mainFrequency = 1000.0;
		public double MainFrequency {
			get => mainFrequency;
			set => this.RaiseAndSetIfChanged(ref mainFrequency, value);
		}

		private double samplerate = 10000.0;
		public double Samplerate {
			get => samplerate;
			set => this.RaiseAndSetIfChanged(ref samplerate, value);
		}

		#endregion

		#region Simulation parameters

		private int simulationMode = 1;
        public int SimulationMode {
            get => simulationMode;
            set => this.RaiseAndSetIfChanged(ref simulationMode, value);
        }

        private double receiveDelay = 0.08;
        public double ReceiveDelay {
            get => receiveDelay;
            set => this.RaiseAndSetIfChanged(ref receiveDelay, value);
        }

		private double dopplerMag = 0.001;
		public double DopplerMagnitude {
			get => dopplerMag;
			set => this.RaiseAndSetIfChanged(ref dopplerMag, value);
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

        private double snrNoisyMax = 10.0;
        public double SNRNoisyMax {
            get => snrNoisyMax;
            set => this.RaiseAndSetIfChanged(ref snrNoisyMax, value);
        }

        private double snrNoisyStep = 1.0;
        public double SNRNoisyStep {
            get => snrNoisyStep;
            set => this.RaiseAndSetIfChanged(ref snrNoisyStep, value);
        }

        private int testsRepeatCount = 1000;
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

		private double snrShown = 0.0;
		public double SNRShown {
			get => snrShown;
			set {
				this.RaiseAndSetIfChanged(ref snrShown, value);
				int sel = (int)((snrShown - SNRNoisy) / SNRNoisyStep);
				foreach (var plot in Plots)
					plot.SelectDataPoint(sel);
			}
		}
	}
}