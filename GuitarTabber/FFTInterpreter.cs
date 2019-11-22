using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GuitarTabber
{
	static class FFTInterpreter
	{
		public static List<double> DominantFreqs(AudioBuffer[] buffers)
		{
			List<double> firstHarmonics = new List<double>();

			for (int i = 0; i < buffers[0].FFT.Length; i++)
			{
				AudioBuffer buf = null;
				foreach (AudioBuffer b in buffers)
				{
					if (IsPeak(b, i) && !IsHarmonic(b, firstHarmonics, i))
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
				bool peak = false;
				// first harmonic frequencies according to each buffer frequency resolution
				List<double> peakFreqsInBuffers = new List<double>();
				foreach (AudioBuffer other in buffers)
				{
					if (other == buf)
					{
						continue;
					}
					// initially assume this buffer fft does not have a valid peak
					peak = false;

					// find location of this frequency in other buffers with varying frequency resolutions
					double coeff = buf.FrequencyResolution / other.FrequencyResolution;
					int lowBound = (int)((i - 1) * coeff);
					int upBound = (int)((i + 1) * coeff);
					for (int check = lowBound; check <= upBound; check++)
					{
						// check that peak exists in other buffers
						if (IsPeak(other, check) && !IsHarmonic(other, firstHarmonics, check))
						{
							peak = true;
							peakFreqsInBuffers.Add(check * other.FrequencyResolution);
							break;
						}
					}

					// doesn't check other buffer ffts if one was found to be invalid
					if (!peak)
					{
						break;
					}
				}

				if (peak)
				{
					firstHarmonics.Add(peakFreqsInBuffers.Average());
				}
			}

			return firstHarmonics;
		}

		// finds whether or not a given index is a relative peak
		static bool IsPeak(AudioBuffer buf, int index)
		{
			double[] fft = buf.FFT;
			if (index < 0 || index >= fft.Length || fft[index] <= buf.FFTAmbientNoiseLevels[index] * 2.5)
			{
				return false;
			}

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
		static bool IsHarmonic(AudioBuffer buf, List<double> firstHarmonics, int index)
		{
			double thisFreq = index * buf.FrequencyResolution;
			// allows some tolerance for the possibile loss of precision due to coarse fft frequency resolution
			double upperBound = (index + 1) * buf.FrequencyResolution;
			double lowerBound = (index - 1) * buf.FrequencyResolution;

			foreach (double f in firstHarmonics)
			{
				double possibleHarmonic = f * Math.Round(thisFreq / f);
				if (possibleHarmonic < 2)
				{
					return false;
				}
				if (possibleHarmonic >= lowerBound && possibleHarmonic <= upperBound)
				{
					return true;
				}
			}

			return false;
		}

		static List<int> FindHarmonics(AudioBuffer buf, int index)
		{
			double[] fft = buf.FFT;

			List<int> harmonics = new List<int>();
			harmonics.Add(index);

			for (int harmonicNum = 2; harmonicNum * index < fft.Length; harmonicNum++)
			{
				int upperBound = harmonicNum * (index + 1);
				int lowerBound = harmonicNum * (index - 1);

				for (int searchIndex = lowerBound; searchIndex <= upperBound; searchIndex++)
				{
					if (IsPeak(buf, searchIndex))
					{
						harmonics.Add(searchIndex);
						break;
					}
				}
			}

			return harmonics;
		}
	}
}
