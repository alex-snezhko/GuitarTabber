using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GuitarTabber
{
	static class FFTInterpreter
	{
		// considers notes that are less than a quarter note away as the same note
		const double QTR_NOTE_INCREASE = 1.01454533493;

		public static double[] WindowedPCM(short[] pcm)
		{
			double[] coefficients =
			{
				0.27105140069342,
				-0.43329793923448,
				0.21812299954311,
				-0.06592544638803,
				0.01081174209837,
				-0.00077658482522,
				0.00001388721735
			};

			// seven term blackman harris window
			double[] ret = new double[pcm.Length];
			pcm.CopyTo(ret, 0);

			for (int i = 0; i < pcm.Length; i++)
			{
				double result = 0.0;
				for (int j = 0; j < 7; j++)
				{
					result += coefficients[j] * Math.Cos(2 * Math.PI * j * pcm[i] / pcm.Length);
				}
				ret[i] = result;
			}
			return ret;
		}

		public static List<double> NoteFreqs(AudioBuffer[] buffers)
		{
			////
			/*foreach (AudioBuffer b in buffers)
			{
				Console.WriteLine(b.FFT.Max());
			}////*/

			List<double> noteFreqs = new List<double>();

			int maxIndex = buffers[0].FFT.Length;
			for (int i = 0; i < maxIndex; i++)
			{
				AudioBuffer buf = null;
				foreach (AudioBuffer b in buffers)
				{
					if (IsPeakInBuffer(b, i))
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
				bool peakInAll = true;
				// first harmonic frequencies according to each buffer frequency resolution
				List<double> peakFreqsInBuffers = new List<double>();
				peakFreqsInBuffers.Add(buf.Offset + i * AudioBuffer.FREQ_RESOLUTION);
				foreach (AudioBuffer other in buffers)
				{
					if (other == buf)
					{
						continue;
					}
					// initially assume this buffer fft does not have a valid peak
					bool isPeak = false;

					int lowBound = other.Offset < buf.Offset ? i : i - 1;
					for (int searchIndex = lowBound; searchIndex <= lowBound + 1; searchIndex++)
					{
						// check that peak exists in other buffers
						if (IsPeakInBuffer(other, searchIndex))
						{
							isPeak = true;
							peakFreqsInBuffers.Add(other.Offset + searchIndex * AudioBuffer.FREQ_RESOLUTION);
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
					// TODO improve runtime
					// initially assume that this frequency has the most harmonics
					double freq = peakFreqsInBuffers.Average();
					double ___Origfreq = freq;///// debug
					int harmonicsRequired = 4;
					bool hasHarmonics = HasNHarmonics(buffers, harmonicsRequired++, ref freq);

					foreach (double f in noteFreqs)
					{
						// this would only apply if the fundamental harmonic was not found earlier due to close proximity to another peak
						double possibleFirstHarmonic = freq / Math.Round(freq / f);

						double ___Origquot = possibleFirstHarmonic;//////
						bool moreInQuotient = HasNHarmonics(buffers, harmonicsRequired++, ref possibleFirstHarmonic);
						// +1 to account for such situations as original frequency only having 1 harmonic (itsself), where quotient harmonic will
						//     be guaranteed to have itsself as well as the original frequency as harmonics
						if (moreInQuotient)
						{
							___Origfreq = ___Origquot;
							hasHarmonics = true;
							freq = possibleFirstHarmonic;
						}
					}

					if (hasHarmonics)
					{
						if (!AlreadyFound(noteFreqs, freq))
						{
							//Console.WriteLine("Original: " + ___Origfreq + "; " + harmonicsRequired + " harmonics");
							noteFreqs.Add(freq);
						}
					}
				}
			}
			
			return noteFreqs;
		}

		// finds whether or not a given index is a relative peak
		static bool IsPeakInBuffer(AudioBuffer buf, int index)
		{
			double[] fft = buf.FFT;
			if (index >= fft.Length || fft[index] <= buf.FFTAmbientNoiseLevels[index] * 1.5)
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
		/*static bool IsHarmonicInBuffer(AudioBuffer buf, List<double> firstHarmonics, int index)
		{
			double thisFreq = index * buf.FrequencyResolution;
			// allows some tolerance for the possibile loss of precision due to coarse fft frequency resolution
			double upperBound = (index + 1) * buf.FrequencyResolution; // TODO fix bounds/maybe add lower bound

			foreach (double f in firstHarmonics)
			{
				int possibleHarmonicNum = (int)Math.Round(thisFreq / f);

				double possibleHarmonicFreq = f * possibleHarmonicNum;
				if (possibleHarmonicFreq >= thisFreq && possibleHarmonicFreq <= upperBound)
				{
					return true;
				}
			}

			return false;
		}*/

		static bool AlreadyFound(List<double> noteFreqs, double frequency)
		{
			double lowBound = frequency / QTR_NOTE_INCREASE;
			double highBound = frequency * QTR_NOTE_INCREASE;
			return noteFreqs.Any(f => f >= lowBound && f <= highBound);
		}

		/*static int NumHarmonics(AudioBuffer[] buffers, ref double frequency)
		{
			// accurateFreq tries to represent more accurate actual frequency at t
			double accurateFreq = frequency;
			// start at 1: fundamental harmonic of given frequency
			int numHarmonics = 1;

			for (int harmonicNum = 2; harmonicNum <= 8 && harmonicNum * accurateFreq < AudioBuffer.FFT_HIGHEST_FREQ; harmonicNum++)
			{
				bool inAll = true;

				// tries to more accurately depict what the fundamental harmonic frequency is
				List<double> fundHarmFreqs = new List<double>();
				foreach (AudioBuffer buf in buffers)
				{
					if (HasNthHarmonic(buf, harmonicNum, accurateFreq, out double f))
					{
						fundHarmFreqs.Add(f / harmonicNum);
					}
					else
					{
						inAll = false;
						break;
					}
				}
				
				if (inAll)
				{
					int numData = numHarmonics++ * buffers.Length;
					double totalAverage = (fundHarmFreqs.Sum() + accurateFreq * numData) / (numHarmonics * buffers.Length);
					accurateFreq = totalAverage;
				}
			}

			frequency = accurateFreq;
			return numHarmonics;
		}*/

		// adjusts frequency to try to make it more precise based on its harmonics
		static bool HasNHarmonics(AudioBuffer[] buffers, int n, ref double frequency)
		{
			// accurateFreq tries to represent more accurate actual frequency at t
			double accurateFreq = frequency;
			// start at 1: fundamental harmonic of given frequency
			int numHarmonics = 1;

			for (int harmonicNum = 2; harmonicNum <= n + 1 && harmonicNum * accurateFreq < AudioBuffer.FFT_HIGHEST_FREQ; harmonicNum++)
			{
				bool inAll = true;

				// tries to more accurately depict what the fundamental harmonic frequency is
				List<double> fundHarmFreqs = new List<double>();
				foreach (AudioBuffer buf in buffers)
				{
					if (PeakAroundTarget(buf, harmonicNum * accurateFreq, out double f))
					{
						fundHarmFreqs.Add(f / harmonicNum);
					}
					else
					{
						inAll = false;
						break;
					}
				}

				if (inAll)
				{
					int numData = numHarmonics++ * buffers.Length;
					double totalAverage = (fundHarmFreqs.Sum() + accurateFreq * numData) / (numHarmonics * buffers.Length);
					accurateFreq = totalAverage;

					if (numHarmonics == n)
					{
						frequency = accurateFreq;
						return true;
					}
				}
			}

			return false;
		}

		static bool PeakAroundTarget(AudioBuffer buf, double targetFreq, out double accurateFreq)
		{
			double lowBound = targetFreq / QTR_NOTE_INCREASE;
			double upBound = targetFreq * QTR_NOTE_INCREASE;

			int lowBoundIndex = (int)(lowBound / AudioBuffer.FREQ_RESOLUTION);
			int upBoundIndex = (int)(upBound / AudioBuffer.FREQ_RESOLUTION);
			// finds the index in search that is closest to target frequency value
			(double minDist, int index) = (double.MaxValue, 0);
			for (int searchIndex = lowBoundIndex; searchIndex <= upBoundIndex; searchIndex++)
			{
				double thisDist = Math.Abs(searchIndex * AudioBuffer.FREQ_RESOLUTION - targetFreq);
				if (thisDist >= minDist)
				{
					break;
				}
				else if (IsPeakInBuffer(buf, searchIndex))
				{
					minDist = thisDist;
					index = searchIndex;
				}
			}

			// peak not found
			if (minDist == double.MaxValue)
			{
				accurateFreq = 0.0;
				return false;
			}

			accurateFreq = index * AudioBuffer.FREQ_RESOLUTION;
			return true;
		}
	}
}
