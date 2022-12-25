using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics; 
using System.Threading.Tasks;
using static FftSharp.Transform;

namespace MultipathSignal.Core
{
    internal class CorrelationFft
    {
        public static IList<Complex> Calculate(IList<Complex> bigarr, IList<Complex> smolar)
        {
            int bigsize = 1;
            while (bigsize < bigarr.Count + smolar.Count) bigsize <<= 1;
            var bigarr2 = new FftSharp.Complex[bigsize];
            for (int i = 0; i < bigarr.Count; i++)
                bigarr2[i] = new(bigarr[i].Real, bigarr[i].Imaginary);
            var smolar2 = new FftSharp.Complex[bigsize];
            for (int i = 0; i < smolar.Count; i++)
                smolar2[i] = new(smolar[i].Real, smolar[i].Imaginary);

            FFT(bigarr2);
            FFT(smolar2);
            var corrft = Enumerable.Zip(bigarr2, smolar2, (a, b) => a * b).ToArray();
            IFFT(corrft);
            return corrft
                .Skip(smolar.Count)
                .Take(bigarr.Count)
                .Select(c => new Complex(c.Real, c.Imaginary))
                .ToArray();
        }
    }
}