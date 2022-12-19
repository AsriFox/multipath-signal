using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace MultipathSignal.Core
{ 
	/// <summary>
	/// Signal generator: complex waveform
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

		public Complex GetNextSample() {
			Phase += Math.Tau * Frequency * Delta;
			if (Phase > Math.Tau) 
				Phase -= Math.Tau;
			return Complex.Exp(Complex.ImaginaryOne * Phase);
		}
	}

	internal class SignalModulator
	{
		public enum Modulation { 
			/// <summary>
			/// Amplitude shift keying with 2 states (1 bit)
			/// </summary>
			AM,

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
			/// MSK: binary minimum-shift keying (2 states - 1 bit) 
			/// </summary>
			MSK,
		}

		public Modulation Method { get; set; } = Modulation.AM;

		public SignalGenerator Generator { get; set; } = new();

		public double BitLength { get; set; } = 5;

		public double BitRate {
			get => 1.0 / BitLength;
			set => BitLength = 1.0 / value;
		}

		public double MainFrequency { get; set; } = 1.0;

		public double Depth { get; set; } = 0.8;

		public IList<Complex> Modulate(IEnumerable<bool> modul)
		{
			Generator.Phase = 0.0;
			Generator.Frequency = MainFrequency;

			int bitLength = (int)(BitLength * SignalGenerator.Samplerate);
			List<Complex> signal = new(bitLength);
			switch (Method) {
				case Modulation.AM:
					foreach (bool b in modul) {
						double a = b ? 1.0 : 1.0 - Depth;
						for (uint i = 0; i < bitLength; i++)
							signal.Add(Generator.GetNextSample() * a);
					}
					break;
				
				case Modulation.BPSK:
					foreach (bool b in modul) {
						// if (q) Generator.Phase += Math.PI;
						double a = b ? -1.0 : 1.0;
						for (uint i = 0; i < bitLength; i++)
							signal.Add(Generator.GetNextSample() * a);
						// if (q) Generator.Phase -= Math.PI;
					}
					break;

				case Modulation.BPSK_I:
					foreach (bool b in modul) {
						if (b) Generator.Phase += Math.PI;
						for (uint i = 0; i < bitLength; i++)
							signal.Add(Generator.GetNextSample());
					}
					break;

				case Modulation.MSK:
					double mf = Math.PI * 0.5 / bitLength;
					foreach (Complex a in modul.Quadify()) {
						for (uint i = 0; i < bitLength; i++) {
							Complex s = Generator.GetNextSample();
							double mi = Math.Cos(i * mf);
							double mq = Math.Sin(i * mf);
							signal.Add(new(
								a.Real * s.Real * mi,
								a.Imaginary * s.Imaginary * mq
							));
						}
					}
					break;
			}
			return signal;
		}

		/// <summary>
		/// Modulate a bit sequence asynchronously.
		/// Uses the default cancellation token.
		/// </summary>
		public Task<IList<Complex>> ModulateAsync(IEnumerable<bool> modul) =>
			Task.Factory.StartNew(
				mv => {
					if (mv is not IEnumerable<bool> @modv) 
						return Array.Empty<Complex>();
					return Modulate(modv);
				}, 
				modul,
				Utils.Cancellation.Token, 
				TaskCreationOptions.None, 
				TaskScheduler.Default);
	}
}