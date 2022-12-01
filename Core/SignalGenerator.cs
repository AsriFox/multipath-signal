using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MultipathSignal.Core
{ 
	/// <summary>
	/// Signal generator: harmonic wave
	/// </summary>
	internal class SignalGenerator
	{
		public static double Samplerate { get; set; } = 10;

		public static double Delta {
			get => 1.0 / Samplerate;
			set => Samplerate = 1.0 / value;
		}

		public double Phase { get; set; } = 0;

		public double Frequency { get; set; } = 1;

		public double Period {
			get => 1.0 / (Math.Tau * Frequency);
			set => Frequency = Math.Tau / value;
		}

		public double GetNextSample() {
			Phase += Math.Tau * Frequency * Delta;
			if (Phase > Math.Tau) 
				Phase -= Math.Tau;
			// if (Phase < -Math.PI) Phase += Math.Tau;
			return Math.Sin(Phase);
		}
	}

	internal class SignalModulator
	{
		public enum Modulation { 
			/// <summary>
			/// On-Off Keying: amplitude shift keying with 2 states (1 bit)
			/// </summary>
			OOK,

			/// <summary>
			/// BPSK: binary phase shift keying (2 states - 1 bit) 
			/// <br/>
			/// NRZ variation (default)
			/// </summary>
			BPSK,

			/// <summary>
			/// BPSK: binary phase shift keying (2 states - 1 bit) 
			/// <br/>
			/// NRZI variation (flip on "1", no flip on "0")
			/// </summary>
			BPSK_I,

			/// <summary>
			/// BFSK: binary frequency shift keying (2 states - 1 bit) 
			/// <br/>
			/// "Frequency Telegraph"
			/// </summary>
			FT,
		}

		public Modulation Method { get; set; } = Modulation.OOK;

		public SignalGenerator Generator { get; set; } = new();

		public double BitLength { get; set; } = 5;

		public double BitRate {
			get => 1.0 / BitLength;
			set => BitLength = 1.0 / value;
		}

		public double MainFrequency { get; set; } = 1.0;

		public double Depth { get; set; } = 0.8;

		public IList<double> Modulate(IEnumerable<bool> modul)
		{
			Generator.Frequency = MainFrequency;

			void m(double a, ref List<double> s) {
				for (uint i = 0; i < BitLength * SignalGenerator.Samplerate; i++)
					s.Add(Generator.GetNextSample() * a);
			}

			List<double> signal = new();
			switch (Method) {
				case Modulation.OOK:
					foreach (bool q in modul) 
						m(q ? 1.0 : 1.0 - Depth, ref signal);
					break;
				
				case Modulation.BPSK:
					foreach (bool q in modul) {
						if (q) Generator.Phase += Math.PI;
						m(1.0, ref signal);
						if (q) Generator.Phase -= Math.PI;
					}
					break;

				case Modulation.BPSK_I:
					foreach (bool q in modul) {
						if (q) Generator.Phase += Math.PI;
						m(1.0, ref signal);
					}
					break;

				case Modulation.FT:
					foreach (bool q in modul) {
						Generator.Frequency = MainFrequency * (q ? 1.0 + Depth : 1.0 - Depth);
						m(1.0, ref signal);
					}
					break;
			}
			return signal;
		}

		/// <summary>
		/// Modulate a bit sequence asynchronously.
		/// Uses the default cancellation token.
		/// </summary>
		public Task<IList<double>> ModulateAsync(IEnumerable<bool> modul) =>
			Task.Factory.StartNew(
				mv => {
					if (mv is not IEnumerable<bool> @modv) 
						return Array.Empty<double>();
					return Modulate(modv);
				}, 
				modul,
				Utils.Cancellation.Token, 
				TaskCreationOptions.None, 
				TaskScheduler.Default);
	}
}