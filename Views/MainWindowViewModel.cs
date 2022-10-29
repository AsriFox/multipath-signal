using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
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
		public System.Collections.ObjectModel.ObservableCollection<PlotViewModel> Plots { get; private set; }

		public MainWindowViewModel()
		{
			Plots = new() {
				new PlotViewModel(2) { Title = "Modulated signal" },
				new PlotViewModel(4) { Title = "Impulse responses of filters" },
				new PlotViewModel(4) { Title = "Filtered responses to signal" },
				new PlotViewModel(1) { Title = "Statistics", MinimumY = 0.0 },
			};

			Plots.CollectionChanged += (_, _) => this.RaisePropertyChanged(nameof(Plots));
		}

		public async void Process()
		{
			EditMode = false;

			GoldSeq = await Task.Factory.StartNew(arg => {
				if (arg is not double[] shifts)
					throw new InvalidCastException();

				var seqs = GoldSequenceGenerator.Generate("00101", "01111");
				return shifts.Select(j => seqs[(int)j]).ToArray();
			}, 
			GoldSeqShift);
			this.RaisePropertyChanged(nameof(GoldSeq));

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

		public void OnStatusChanged(string status) => Status = status;

		#region Modulation parameters

		public double ModulationSpeed { get; set; } = 100.0;

		public int BitSeqLength { get; set; } = 64;

		public double Samplerate { get; set; } = 10000.0;

		#endregion

		#region Simulation parameters

		public int SimulationMode { get; set; } = 1;

		public double SNRClean { get; set; } = 10.0;

		public double SNRNoisy { get; set; } = -10.0;

		public double SNRNoisyMax { get; set; } = 10.0;

		public double SNRNoisyStep { get; set; } = 1.0;

		public int TestsRepeatCount { get; set; } = 1000;

        #endregion

        #region Gold sequences

        public string[] GoldSeq { get; private set; } = new string[4];

		public double[] GoldSeqShift { get; } = new double[] { 0, 10, 20, 30 };

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