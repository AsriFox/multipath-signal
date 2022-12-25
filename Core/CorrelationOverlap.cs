using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics; 
using System.Threading.Tasks;
using static FftSharp.Transform;

namespace MultipathSignal.Core
{
    internal class CorrelationOverlap
    {
        public static IList<Complex> Calculate(IList<Complex> bigarr, IList<Complex> smolar)
        {
            int smolsize = 1;
            while (smolsize < smolar.Count) smolsize <<= 1;
            var smolar2 = new FftSharp.Complex[smolsize];
            for (int i = 0; i < smolar.Count; i++)
                smolar2[i] = new(smolar[i].Real, smolar[i].Imaginary);
            FFT(smolar2);
            // Complex conjugate:
            for (int i = 0; i < smolsize; i++)
                smolar2[i] = new(smolar2[i].Real, -smolar2[i].Imaginary);

            int q = smolar.Count;
            var correl = new Complex[bigarr.Count];
            for (int j = 0; j < bigarr.Count; j += q) {
                var bigarr2 = new FftSharp.Complex[smolsize];
                for (int i = 0; i < q; i++) 
                    bigarr2[i] = new(
                        bigarr[(j + i) % bigarr.Count].Real, 
                        bigarr[(j + i) % bigarr.Count].Imaginary
                    );

                FFT(bigarr2);
                for (int i = 0; i < smolsize; i++)
                    bigarr2[i] *= smolar2[i];

                IFFT(bigarr2);
                for (int i = 0; i < smolsize; i++) {
                    int k = j + i;
                    if (i >= q) k -= smolsize;
                    if (k < 0) k += bigarr.Count;
                    if (k >= bigarr.Count) k -= bigarr.Count;
                    correl[k] += new Complex(bigarr2[i].Real, bigarr2[i].Imaginary);
                }
            }
            return correl;
        }
    }
}