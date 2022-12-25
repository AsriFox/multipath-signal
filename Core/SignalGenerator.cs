using Avalonia.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace MultipathSignal.Core
{ 
	internal class SignalModulator
	{
		public static double Samplerate = 100.0;

		public static double BitRate { get; set; } = 20.0;

		public static double BitLength => Samplerate / BitRate;

		public static IEnumerable<bool> Construct(IEnumerable<bool> modul, string[] goldSeq)
		{
			using (var iterator = modul.GetEnumerator()) {
				while (iterator.MoveNext()) {
					int bb = iterator.Current ? 2 : 0;
					if (iterator.MoveNext() && iterator.Current) bb++;
					foreach (char c in goldSeq[bb])
						yield return c == '1';
					yield return false;	// even bit count
				}
			}
		}

		public static IEnumerable<Complex> Modulate(IEnumerable<bool> modul)
		{
			double am = Math.Sqrt(0.5);
			using (var iterator = modul.GetEnumerator()) {
				while (iterator.MoveNext()) {
					bool i = iterator.Current;
					bool q = iterator.MoveNext() && iterator.Current;
					for (int t = 0; t < BitLength; t++) {
						yield return new Complex(
							i ? am : -am,
							q ? am : -am
						);
					}
				}
			}
		}

		/// <summary>
		/// Modulate a bit sequence asynchronously.
		/// Uses the default cancellation token.
		/// </summary>
		public static Task<IList<Complex>> ModulateGoldAsync(IEnumerable<bool> modul, string[] goldSeq) =>
			Task.Factory.StartNew(
				args => {
					if (args is not Tuple<IEnumerable<bool>, string[]> @aargs) 
						return Array.Empty<Complex>();
					return Modulate(
						Construct(aargs.Item1, aargs.Item2)
					).ToArray() as IList<Complex>;
				}, 
				Tuple.Create(modul, goldSeq),
				Utils.Cancellation.Token, 
				TaskCreationOptions.None, 
				TaskScheduler.Default);
	}
}