namespace MultipathSignal.Core;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

	public double GetNextSample() => Math.Sin(Phase += Frequency * Delta);
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
		/// NRZI variation (flip on "1", no flip on "0")
		/// </summary>
		BPSK,
		/// <summary>
		/// BFSK: binary frequency shift keying (2 states - 1 bit) 
		/// <br/>
		/// "Frequency Telegraph"
		/// </summary>
		BFSK,
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
		var signal = new List<double>();
		foreach (var q in modul) {
			double v = 1.0;
			switch (Method) {
				case Modulation.OOK:
					if (q) v += Depth; else v -= Depth;
					break;
				case Modulation.BPSK:	// NRZI
					if (q) Generator.Phase += Math.PI;
					break;
				case Modulation.BFSK:
					Generator.Frequency = MainFrequency * (q ? 1.0 + Depth : 1.0 - Depth);
					break;
				default:
					throw new NotImplementedException();
			}
			for (uint i = 0; i < BitLength * SignalGenerator.Samplerate; i++)
				signal.Add(Generator.GetNextSample() * v);
		}
		return signal;
	}
}
