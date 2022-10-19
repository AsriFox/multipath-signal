using HarfBuzzSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultipathSignal.Core
{
	internal class ShiftRegister
	{
		//public int Size { get; private set; }

		//private int structure;
		//public int Structure {
		//	get => structure;
		//	set => structure = value & ((1 << Size) - 1);
		//}
		//public IEnumerable<char> StructureStr {
		//	get {
		//		var s = new char[Size];
		//		for (int i = 0; i < Size; i++)
		//			s[i] = (structure & (1 << i)) > 0 ? '1' : '0';
		//		return s;
		//	}
		//	set {
		//		structure = 0;
		//		int j = 1 << (Size - 1);
		//		foreach (char k in value.Take(Size)) {
		//			if (k == '1')
		//				structure += j;
		//			else if (k != '0')
		//				throw new FormatException($"Expected '0' or '1', got '{k}'");
		//			j >>= 1;
		//		}
		//	}
		//}

		//private int state;
		//public int State {
		//	get => state;
		//	set => state = value & ((1 << Size) - 1);
		//}
		//public IEnumerable<char> StateStr {
		//	get {
		//		var s = new char[Size];
		//		for (int i = 0; i < Size; i++)
		//			s[i] = (state & (1 << i)) > 0 ? '1' : '0';
		//		return s;
		//	}
		//	set {
		//		state = 0;
		//		int j = 1 << (Size - 1);
		//		foreach (char k in value.Take(Size)) {
		//			if (k == '1')
		//				state += j;
		//			else if (k != '0')
		//				throw new FormatException($"Expected '0' or '1', got '{k}'");
		//			j >>= 1;
		//		}
		//	}
		//}

		//public ShiftRegister(IConvertible size) 
		//{ 
		//	Size = Convert.ToInt32(size); 
		//}

		//public ShiftRegister(int structure)
		//{
		//	int j = 1;
		//	Size = 1;
		//	while (j < structure) {
		//		j <<= 1;
		//		Size++;
		//	}
		//	this.structure = structure;
		//}

		//public ShiftRegister(IEnumerable<char> structure) 
		//{
		//	Size = structure.Count();
		//	StateStr = structure; 
		//}

		//public char Output => (state & 1) > 0 ? '1' : '0';

		//public char FeedbackLoop()
		//{
		//	bool feed = false;
		//	int cross = structure & state;
		//	for (int i = 1; i < (1 << Size); i <<= 1)
		//		if ((cross & i) > 0)
		//			feed = !feed;
		//	// feed ^= (cross & i) > 0;
		//	state >>= 1;
		//	if (feed)
		//		state += 1 << (Size - 1);
		//	return Output;
		//}

		//private IEnumerable<char> GetSequence(int istate)
		//{
		//	List<char> seq = new();
		//	do seq.Add(FeedbackLoop());
		//	while (state != istate);
		//	return seq;
		//}

		//public IEnumerable<char> GetSequence(int? initState = null) 
		//	=> GetSequence(initState ?? state);

		//public IEnumerable<char> GetSequence(IList<char>? initState = null)
		//{
		//	if (initState is not null)
		//		StateStr = initState;
		//	return GetSequence(state);
		//}

		//public void Reset() => state = 0;

		public int Size => structure.Length;

		private string structure;
		public string Structure { 
			get => structure;
			set {
				foreach (char c in value)
					if (c != '0' && c != '1')
						throw new FormatException($"Expected '0' or '1', got '{c}'");
				structure = value[..Size];
			}
		}

		private string state;
		public string State { 
			get => state;
			set {
				foreach (char c in value)
					if (c != '0' && c != '1')
						throw new FormatException($"Expected '0' or '1', got '{c}'");
				state = value[..Size];
			}
		}

		public ShiftRegister(int size)
		{
			this.structure = new string('0', size);
			this.state = new string('0', size);
		}

		public ShiftRegister(IEnumerable<char> structure)
		{
			this.structure = new string(structure.ToArray());
			this.state = new string('0', Size);
		}

		public char Output => state[Size - 1];

		public char FeedbackLoop()
		{
			int feed = 0;
			for (int i = 0; i < Size; i++)
				feed += (structure[i] & state[i]) - '0';

			state = ((feed & 1) > 0 ? "1" : "0") + state[1..];
			return Output;
		}

		public IEnumerable<char> GetSequence(IEnumerable<char> initState)
		{
			State = new string(initState.ToArray());
			string istate = state;
			List<char> seq = new();
			do seq.Add(FeedbackLoop());
			while (state != istate);
			return seq;
		}

		public void Reset() => state = new string('0', Size);
	}

	internal class GoldSequenceGenerator
	{
		public static IList<int> CrossCorrelation(string left, string right)
		{
			List<int> ccor = new();
			for (int m = 0; m < left.Length && m < right.Length; m++) {
				int cc = 0;
				for (int j = 0; j < left.Length && j < right.Length; j++)
					cc += (left[j] & right[(j + m) % right.Length]) - '0';
				ccor.Add(cc);
			}
			return ccor;
		}
	}
}
