using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GuitarTabber
{
	static class FFTInterpreter
	{
		//public static List<double> DominantFreqs(AudioBuffer[] buffers)
		//{
		//	List<double> firstHarmonics = new List<double>();

		//	for (int i = 0; i < buffers[0].FFT.Length; i++)
		//	{
		//		AudioBuffer buf = null;
		//		foreach (AudioBuffer b in buffers)
		//		{
		//			if (IsPeak(b, i) && !IsHarmonic(b, firstHarmonics, i))
		//			{
		//				buf = b;
		//				break;
		//			}
		//		}

		//		if (buf == null)
		//		{
		//			continue;
		//		}

		//		// check if each fft has a valid peak
		//		bool peakInAll = false;
		//		// first harmonic frequencies according to each buffer frequency resolution
		//		List<double> peakFreqsInBuffers = new List<double>();
		//		foreach (AudioBuffer other in buffers)
		//		{
		//			if (other == buf)
		//			{
		//				continue;
		//			}
		//			// initially assume this buffer fft does not have a valid peak
		//			peakInAll = false;

		//			// find location of this frequency in other buffers with varying frequency resolutions
		//			double coeff = buf.FrequencyResolution / other.FrequencyResolution;
		//			int lowBound = (int)((i - 1) * coeff);
		//			int upBound = (int)((i + 1) * coeff);
		//			for (int searchIndex = lowBound; searchIndex <= upBound; searchIndex++)
		//			{
		//				// check that peak exists in other buffers
		//				if (IsPeak(other, searchIndex) && !IsHarmonic(other, firstHarmonics, searchIndex))
		//				{
		//					peakInAll = true;
		//					peakFreqsInBuffers.Add(searchIndex * other.FrequencyResolution);
		//					break;
		//				}
		//			}

		//			// doesn't check other buffer ffts if one was found to be invalid
		//			if (!peakInAll)
		//			{
		//				break;
		//			}
		//		}

		//		if (peakInAll)
		//		{
		//			// adds this to the list of *possible* first harmonics (not certain yet)
		//			firstHarmonics.Add(peakFreqsInBuffers.Average());
		//		}
		//	}

		//	// tries to find actual first harmonic from peaks found
		//	foreach (double f in firstHarmonics)
		//	{
		//		List<double> possibleHarmonics = FindHarmonics(buffers, f);
		//		for (int i = 2; i < 5; i++)
		//		{

		//		}
		//	}

		//	return firstHarmonics;
		//}

		public static List<double> DominantFreqs(AudioBuffer[] buffers)
		{
			List<double> firstHarmonics = new List<double>();

			int maxIndex = buffers[0].FFT.Length;
			for (int i = 0; i < maxIndex; i++)
			{
				AudioBuffer buf = null;
				foreach (AudioBuffer b in buffers)
				{
					if (i > b.FFT.Length || b.FFT[i] <= b.FFTAmbientNoiseLevels[i] * 2.5)
					{
						continue;
					}
					if (IsPeakInBuffer(b, i) && !IsHarmonicInBuffer(b, firstHarmonics, i))
					{
						buf = b;
						break;
					}
				}

				if (buf == null)
				{
					continue;
				}

				// check if each fft has a valid peak
				bool peakInAll = false;
				// first harmonic frequencies according to each buffer frequency resolution
				List<double> peakFreqsInBuffers = new List<double>();
				foreach (AudioBuffer other in buffers)
				{
					if (other == buf)
					{
						continue;
					}
					// initially assume this buffer fft does not have a valid peak
					bool isPeak = false;

					// find location of this frequency in other buffers with varying frequency resolutions
					double coeff = buf.FrequencyResolution / other.FrequencyResolution;
					int lowBound = (int)(i * coeff);
					// allow some tolerance for possible roundoff error
					int upBound = (int)((i + 1) * coeff);
					for (int searchIndex = lowBound; searchIndex <= upBound; searchIndex++)
					{
						// check that peak exists in other buffers
						if (IsPeakInBuffer(other, searchIndex) && !IsHarmonicInBuffer(other, firstHarmonics, searchIndex))
						{
							isPeak = true;
							peakFreqsInBuffers.Add(searchIndex * other.FrequencyResolution);
							break;
						}
					}

					// doesn't check other buffer ffts if one was found to be invalid
					if (!isPeak)
					{
						peakInAll = false;
						break;
					}
				}

				// at this point there is a fair amount of confidence that this is not a coincidental peak; verify again now based on presence of harmonics
				if (peakInAll)
				{
					// initially assume that this frequency has the most harmonics
					int mostHarmonics = NumHarmonics(buffers, peakFreqsInBuffers.Average(), out double accurateFreq);

					foreach (double f in firstHarmonics)
					{
						// this would only apply if the fundamental harmonic was not found earlier due to close proximity to another peak
						double possibleFirstHarmonic = accurateFreq / Math.Round(accurateFreq / f);

						int quotientHarmonics = NumHarmonics(buffers, possibleFirstHarmonic, out double quotientFreq);
						// +1 to account for such situations as original frequency only having 1 harmonic (itsself), where quotient harmonic will
						//     be guaranteed to have itsself as well as the original frequency as harmonics
						if (quotientHarmonics > mostHarmonics + 1)
						{
							mostHarmonics = quotientHarmonics;
							accurateFreq = quotientFreq;
						}
					}

					if (mostHarmonics >= 4)
					{
						firstHarmonics.Add(accurateFreq);
					}
				}
			}

			return firstHarmonics;
		}

		// finds whether or not a given index is a relative peak
		static bool IsPeakInBuffer(AudioBuffer buf, int index)
		{
			double[] fft = buf.FFT;
			for (int comp = index - 1; comp <= index + 1; comp++)
			{
				bool validIndex = comp != index && comp >= 0 && comp < fft.Length;
				if (validIndex && fft[index] < fft[comp])
				{
					return false;
				}
			}

			return true;
		}

		// finds whether or not a given index is a harmonic of a note already found
		static bool IsHarmonicInBuffer(AudioBuffer buf, List<double> firstHarmonics, int index)
		{
			double thisFreq = index * buf.FrequencyResolution;
			// allows some tolerance for the possibile loss of precision due to coarse fft frequency resolution
			double upperBound = (index + 1) * buf.FrequencyResolution;

			foreach (double f in firstHarmonics)
			{
				int possibleHarmonicNum = (int)Math.Round(thisFreq / f);
				if (possibleHarmonicNum < 2)
				{
					return false;
				}

				double possibleHarmonicFreq = f * possibleHarmonicNum;
				if (possibleHarmonicFreq >= thisFreq && possibleHarmonicFreq <= upperBound)
				{
					return true;
				}
			}

			return false;
		}

		static int NumHarmonics(AudioBuffer[] buffers, double frequency, out double moreAccurateFreq)
		{
			// accurateFreq tries to represent more accurate actual frequency at t
			double accurateFreq = frequency;
			int numHarmonics = 0;

			for (int harmonicNum = 2; harmonicNum <= 8 && harmonicNum * accurateFreq < AudioBuffer.FFT_HIGHEST_FREQ; harmonicNum++)
			{
				bool inAll = true;

				List<double> freqs = new List<double>();
				foreach (AudioBuffer buf in buffers)
				{
					if (IsNthHarmonic(buf, harmonicNum, out double f))
					{
						freqs.Add(f);
					}
				}
				
				if (inAll)
				{
					int numData = numHarmonics++ * buffers.Length;
					double totalAverage = (freqs.Sum() + accurateFreq * numData) / numHarmonics;
					accurateFreq = totalAverage;
				}
			}

			moreAccurateFreq = accurateFreq;
			return numHarmonics;

			bool IsNthHarmonic(AudioBuffer buf, int n, out double freq)
			{
				double nthHarmonic = n * accurateFreq;
				int nthHarmonicIndex = (int)(nthHarmonic / buf.FrequencyResolution);
				for (int searchIndex = nthHarmonicIndex; searchIndex <= nthHarmonicIndex + 1; searchIndex++)
				{
					if (IsPeakInBuffer(buf, searchIndex))
					{
						freq = searchIndex * buf.FrequencyResolution;
						return true;
					}
				}

				freq = -1.0;
				return false;
			}
		}
	}
}
