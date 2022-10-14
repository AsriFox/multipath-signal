using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls.Templates;
using Avalonia.Threading;
using MultipathSignal.Core;
using OxyPlot;
using ReactiveUI;

namespace MultipathSignal.Views
{
	internal class MainWindowViewModel : ReactiveObject
	{
		public PlotStorageViewModel PlotStorage { get; private set; } = new();

		/// <summary>
		/// Click event handler for "Launch simulation" button
		/// </summary>
		public async void Process()
		{
			EditMode = false;
			PlotStorage.Clear();
			SignalGenerator.Samplerate = Samplerate;
			Statistics stat = new() {
				MainFrequency = MainFrequency,
				ModulationSpeed = ModulationSpeed,
				ModulationType = ModulationType,
				ModulationDepth = ModulationDepth
			};
			stat.StatusChanged += OnStatusChanged;
			stat.PlotDataReady += OnPlotDataReady;

			try { 
				switch (SimulationMode) {
					case 0:     // Single test
						PredictedDelay = await stat.ProcessSingle(ReceiveDelay, SNRClean, SNRNoisy);
						break;

					case 1:     // Multiple tests
						PredictedDelay = await stat.ProcessMultiple(ReceiveDelay, SNRClean, SNRNoisy, TestsRepeatCount);
						break;

					case 2:     // Gather statistics
						PredictedDelay = double.NaN;
						PlotStorage.Plots[3].Points = new List<DataPoint>();
						double snr = SNRNoisy;
						double snrMax = SNRNoisyMax + 0.5 * SNRNoisyStep;
						while (snr < snrMax) {
							if (Utils.Cancellation.IsCancellationRequested) break;
							double gotitPercent = await stat.ProcessStatistic(ReceiveDelay, SNRClean, snr, TestsRepeatCount);
							(PlotStorage.Plots[3].Points as IList<DataPoint>)?.Add(new DataPoint(snr, gotitPercent));
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

		public void OnPlotDataReady(IList<double> clearSignal, IList<double> signal, IList<double> correl)
		{
			Status = "Data was generated successfully. Plotting...";
			PlotStorage.OnPlotDataReady(clearSignal, signal, correl);
			Status = "Procedure was completed. Ready.";
		}

		#region Modulation parameters

		private SignalModulator.Modulation modulationType = SignalModulator.Modulation.OOK;
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

        private double predictedDelay = double.NaN;
		public double PredictedDelay {
			get => predictedDelay; 
			set => this.RaiseAndSetIfChanged(ref predictedDelay, value); 
		}

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

		private double snrShown = 0.0;
		public double SNRShown {
			get => snrShown;
			set {
				this.RaiseAndSetIfChanged(ref snrShown, value);
				PlotStorage.Select((int)((snrShown - SNRNoisy) / SNRNoisyStep));
			}
		}
	}
}