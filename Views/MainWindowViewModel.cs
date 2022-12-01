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
				new PlotViewModel(2) { Title = "Modulated signal, I-component" },
				new PlotViewModel(2) { Title = "Modulated signal, Q-component" },
				new PlotViewModel(4) { Title = "Filtered responses to signal" },
				new PlotViewModel(1) { Title = "Statistics", MinimumY = 0.0 },
			};

			Plots[0].Series[0].Color = OxyColors.LightSalmon;
			Plots[0].Series[1].Color = OxyColors.OrangeRed;
			Plots[1].Series[0].Color = OxyColors.LightBlue;
			Plots[1].Series[1].Color = OxyColors.DarkBlue;	

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

			if (GoldSeq.Any(s => s is null || s.Length == 0)) 
				await GenerateGoldSequences();

			SignalModulator.Samplerate = Samplerate;
			SignalModulator.BitRate = ModulationSpeed;
			Statistics stat = new();
			for (int q = 0; q < 4; q++) {
				stat.Filters[q] = SignalModulator.Modulate(
					GoldSeq[q]
						.Select(c => c == '1')
						.ToArray()
				);
			}

			try {
				switch (SimulationMode) {
					case 0:     // Single test
						Status = "Processing one signal...";
						
                        var x = await SignalModulator.ModulateGoldAsync(
                                Utils.RandomBitSeq(BitSeqLength).ToArray(),
                                GoldSeq);
						
						IList<double> ix = new double[x.Count];
						IList<double> qx = new double[x.Count];
						for (int i = 0; i < x.Count; i++) {
							ix[i] = x[i].Real;
							qx[i] = x[i].Imaginary;
						}

						var inx = Utils.ApplyNoise(ix, Math.Pow(10.0, 0.1 * SNRNoisy));
						var qnx = Utils.ApplyNoise(qx, Math.Pow(10.0, 0.1 * SNRNoisy));
						
						var nx = new Complex[x.Count];
						for (int i = 0; i < x.Count; i++)
							nx[i] = new(inx[i], qnx[i]);

						var correl = new IList<Complex>[4];
						for (int q = 0; q < 4; q++)
							correl[q] = Statistics.Correlation(nx, stat.Filters[q]);

						var corabs = new IList<double>[4];
						for (int q = 0; q < 4; q++)
							corabs[q] = correl[q].Select(v => v.Magnitude).ToArray();

                        await Dispatcher.UIThread.InvokeAsync(() => {
							OnPlotDataReady(0, inx, ix);
							OnPlotDataReady(1, qnx, qx);
							OnPlotDataReady(2, corabs);
						});
                        this.RaisePropertyChanged(nameof(Plots));
						break;
					case 1:
						break;
					case 2:
						break;
				}
			}
			catch (OperationCanceledException) {
				Status = "Operation was cancelled.";
				Utils.Cancellation = new();
			}

			//SignalGenerator.Samplerate = Samplerate;
			//Statistics stat = new() {
			//	MainFrequency = MainFrequency,
			//	ModulationSpeed = ModulationSpeed,
			//	ModulationType = ModulationType,
			//	ModulationDepth = ModulationDepth
			//};
			//stat.StatusChanged += OnStatusChanged;

			//try { 
			//	switch (SimulationMode) {
			//		case 0:     // Single test
			//			Status = "Processing one signal...";
			//			PredictedDelay = await stat.FindPredictedDelayAsync(ReceiveDelay, SNRClean, SNRNoisy, true);
			//			break;

			//		case 1:     // Multiple tests
			//			Status = "Processing signals...";
			//			var stopw = new Stopwatch();
			//			stopw.Start();
			//			var tasks = Enumerable.Range(0, TestsRepeatCount)
			//								.Select(_ => stat.FindPredictedDelayAsync(ReceiveDelay, SNRClean, SNRNoisy))
			//								.ToList();
			//			tasks.Add(stat.FindPredictedDelayAsync(ReceiveDelay, SNRClean, SNRNoisy, true));

			//			var results = await Task.WhenAll(tasks);
			//			PredictedDelay = results.Sum() / results.Length;

			//			stopw.Stop();
			//			Status = $"{TestsRepeatCount} tasks were completed in {stopw.Elapsed.TotalSeconds:F2} s. Ready.";
			//			break;

			//		case 2:     // Gather statistics
			//			Plots[3].ReplacePointsOf(0, new List<DataPoint>());
			//			double snr = SNRNoisy;
			//			double snrMax = SNRNoisyMax + 0.5 * SNRNoisyStep;
			//			double threshold = 0.5 / ModulationSpeed;
			//			while (snr < snrMax) {
			//				if (Utils.Cancellation.IsCancellationRequested) break;

			//				Status = $"Processing signals with SNR = {snr:F2} dB";
			//				var actualDelays = new List<double>();
			//				var tasks2 = new List<Task<double>>();
			//				for (int i = 0; i < TestsRepeatCount; i++) {
			//					double delay = ReceiveDelay * 2.0 * Utils.RNG.NextDouble();
			//					actualDelays.Add(delay);
			//					tasks2.Add(stat.FindPredictedDelayAsync(delay, SNRClean, snr));
			//				}
			//				actualDelays.Add(ReceiveDelay * 2.0 * Utils.RNG.NextDouble());
			//				tasks2.Add(stat.FindPredictedDelayAsync(actualDelays.Last(), SNRClean, snr, true));

			//				var results2 = await Task.WhenAll(tasks2);
			//				int gotitCount = 0;
			//				for (int i = 0; i < results2.Length; i++)
			//					if (Math.Abs(results2[i] - actualDelays[i]) < threshold)
			//						gotitCount++;

			//				Plots[3].PointsOf(0).Add(new DataPoint(snr, (double)gotitCount / results2.Length));
			//				snr += SNRNoisyStep;
			//			}
			//			break;
			//	}
			//}
			//catch (TaskCanceledException) {
			//	Status = "Operation was cancelled.";
			//	Utils.Cancellation = new();
			//}
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

		public void OnPlotDataReady(int where, params IList<double>[] values)
		{
			Plots[where].ReplacePointsWith(values.Select(v => v.Plotify()).ToArray());
		}

		#region Modulation parameters

		public double ModulationSpeed { get; set; } = 100.0;

		public int BitSeqLength { get; set; } = 64;

		public double Samplerate { get; set; } = 10000.0;

		#endregion

		#region Simulation parameters

		public int SimulationMode { get; set; } = 0;

		public double SNRClean { get; set; } = 10.0;

		public double SNRNoisy { get; set; } = -10.0;

		public double SNRNoisyMax { get; set; } = 10.0;

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
	}
}