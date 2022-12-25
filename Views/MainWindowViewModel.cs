using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Markup;
using Avalonia.Threading;
using DynamicData;
using MultipathSignal.Core;
using OxyPlot;
using ReactiveUI;

namespace MultipathSignal.Views
{
	internal class MainWindowViewModel : ReactiveObject
	{

		/// <summary>
		/// Collection of view models for PlotView elements.
		/// </summary>
		public ObservableCollection<PlotViewModel> Plots { get; private set; }

		public MainWindowViewModel()
		{
			Plots = new() {
				new PlotViewModel(2) { Title = "Modulated signal" },
				new PlotViewModel(4) { Title = "Filtered responses to signal" },
				new PlotViewModel(1) { Title = "Statistics", MinimumY = 0.0 },
			};
			Plots[0].Series[0].Color = OxyColors.LightBlue;
			Plots[0].Series[1].Color = OxyColors.DarkBlue;	

            Plots.CollectionChanged += (_, _) => this.RaisePropertyChanged(nameof(Plots));

			GoldSeqShift = new() { 0, 10, 20, 30 };
			GoldSeqShift.CollectionChanged += (_, _) => this.RaisePropertyChanged(nameof(GoldSeqShift));

			this.PropertyChanged += OnPropertyChanged;
		}

		public Task GenerateGoldSequences()
		{
            return Task.Factory.StartNew(arg => {
                if (arg is not double[] shifts)
                    throw new InvalidCastException();

                var seqs = GoldSequenceGenerator.Generate("00101", "01111");
                return shifts.Select(j => seqs[(int)j]).ToArray();
            },
            GoldSeqShift.ToArray())
				.ContinueWith(t => {
					GoldSeq = t.Result;
					this.RaisePropertyChanged(nameof(GoldSeq));
				});
        }

		public async void Process()
		{
			EditMode = false;

			foreach (var p in Plots) p.Clear();

			if (GoldSeq.Any(s => s is null || s.Length == 0)) 
				await GenerateGoldSequences();

			SignalModulator.Samplerate = Samplerate;
			SignalModulator.BitRate = ModulationSpeed;
			Statistics stat = new() {
				BitSeqLength = BitSeqLength,
				GoldSeq = GoldSeq
			};
			stat.StatusChanged += OnStatusChanged;
			stat.PlotDataReady += OnPlotDataReady;

			try {
				switch (SimulationMode) {
					case 0:     // Single test
						Status = "Processing one signal...";
						double ber1 = await stat.EncodeDecode(SNRNoisy, UseFFT, true);
						Status += $"Simulation completed. BER: {ber1}";
						break;
					case 1:
						Status = "Processing multiple signals...";
						await stat.ProcessMultiple(SNRNoisy, TestsRepeatCount, UseFFT);
						break;
					case 2:
						Plots[2].AddDataPoint(new IEnumerable<DataPoint>[] { new List<DataPoint>() });
						double snr = SNRNoisy;
						double snrMax = SNRNoisyMax + 0.5 * SNRNoisyStep;
						while (snr < snrMax) {
							if (Utils.Cancellation.IsCancellationRequested) break;
							Status = $"Processing signals with SNR = {snr} db...";
							double ber = await stat.ProcessMultiple(snr, TestsRepeatCount, UseFFT);
							Plots[2].AppendTo(0, new DataPoint(snr, ber));
							SNRShown = snr;
							snr += SNRNoisyStep;
						}
						break;
				}
			}
			catch (OperationCanceledException) {
				Status = "Operation was cancelled.";
				Utils.Cancellation = new();
			}
			EditMode = true;
		}

		public void StopProcess() => Utils.Cancellation.Cancel();

        private void OnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName) {
				case nameof(GoldSeqShift):
					break;
				default:
					return;
			}
			GenerateGoldSequences();
        }

        public void OnStatusChanged(string status) => Status = status;

		public void OnPlotDataReady(params IList<double>[] values)
		{
			Plots[0].AddDataPoint(
				values[4].Plotify(),
				values[5].Plotify()
			);
			Plots[1].AddDataPoint(
				values[0].Plotify(),
				values[1].Plotify(),
				values[2].Plotify(),
				values[3].Plotify()
			);
		}

		#region Modulation parameters

		public double ModulationSpeed { get; set; } = 100.0;

		public int BitSeqLength { get; set; } = 64;

		public double Samplerate { get; set; } = 10000.0;

		#endregion

		#region Simulation parameters

		public int SimulationMode { get; set; } = 0;

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

		public double SNRNoisyStep { get; set; } = 1.0;

		public int TestsRepeatCount { get; set; } = 1000;

        #endregion

        #region Gold sequences

        public string[] GoldSeq { get; private set; } = new string[4];

		public ObservableCollection<double> GoldSeqShift { get; private set; }

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