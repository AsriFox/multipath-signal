using Avalonia.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MultipathSignal.Core
{ 
	internal class SignalModulator
	{
		public static double Samplerate = 100.0;

		public static double BitRate { get; set; } = 20.0;

		public static double BitLength => Samplerate / BitRate;

		public static IList<bool> Construct(IList<bool> modul, string[] goldSeq)
		{
			List<bool> result = new();
			for (int k = 0; k + 1 < modul.Count; k += 2) {
				int bb = modul[k] ? (modul[k + 1] ? 3 : 2) : (modul[k + 1] ? 1 : 0);
				result.AddRange(goldSeq[bb].Select(c => c == '1'));
			}
			if (modul.Count % 2 > 0)
				result.AddRange(goldSeq[modul[^1] ? 2 : 0].Select(c => c == '1'));
			return result;
		}

		public static (IList<double>, IList<double>) Modulate(IList<bool> modul)
		{
			List<double> vi = new(), vq = new();
			void modulate(bool bi, bool bq) {
				double ai = bi ? 1.0 : -1.0, aq = bq ? 1.0 : -1.0;
				for (int i = 0; i < BitLength; i++) {
					vi.Add(ai);
					vq.Add(aq);
				}
			}

			for (int j = 0; j + 1 < modul.Count; j += 2)
				modulate(modul[j], modul[j + 1]);
			if (modul.Count % 2 > 0)
				modulate(modul[^1], false);
			return (vi, vq);
		}

		/// <summary>
		/// Modulate a bit sequence asynchronously.
		/// Uses the default cancellation token.
		/// </summary>
		public static Task<(IList<double>, IList<double>)> ModulateGoldAsync(IList<bool> modul, string[] goldSeq) =>
			Task.Factory.StartNew(
				args => {
					if (args is not Tuple<IList<bool>, string[]> @aargs) 
						return (Array.Empty<double>(), Array.Empty<double>());
					return Modulate(Construct(aargs.Item1, aargs.Item2));
				}, 
				Tuple.Create(modul, goldSeq),
				Utils.Cancellation.Token, 
				TaskCreationOptions.None, 
				TaskScheduler.Default);
	}
}