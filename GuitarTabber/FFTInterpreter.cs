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
					if (IsPeak(b, i) && !IsHarmonic(b, i))
					{
						buf = b;
						break;
					}
				}

				if (buf == null)
				{
					continue;
				}

				// check if each fft has a valid peak (is actually a peak and has harmonics)
				bool valid;
				// first harmonic frequencies according to each buffer frequency resolution
				List<double> firstHarmonicsInBuffers = new List<double>();
				foreach (AudioBuffer other in buffers)
				{
					if (other == buf)
					{
						continue;
					}
					// initially assume this buffer fft does not have a valid first harmonic here
					valid = false;

					// find location of this frequency in other buffers with varying frequency resolutions
					double coeff = buf.FrequencyResolution / other.FrequencyResolution;
					int lowBound = (int)((i - 1) * coeff);
					int upBound = (int)((i + 1) * coeff);
					for (int check = lowBound; check <= upBound; check++)
					{
						// check that peak exists in other buffers
						if (IsPeak(other, check))
						{
							// check that peak has reasonable amount of harmonics
							int maximumHarmonics = other.FFT.Length / check;
							int numHarmonics = FindHarmonics(other, check).Count;
							if (numHarmonics / 2 < maximumHarmonics)
							{
								valid = false;
								break;
							}
							firstHarmonicsInBuffers.Add(check * other.FrequencyResolution);
						}
					}

					if (!valid)
					{
						break;
					}

				}

				if (valid)
				{
					firstHarmonics.Add(firstHarmonicsInBuffers.Average());
				}
			}

			return firstHarmonics;

			// finds whether or not a given index is a relative peak
			bool IsPeak(AudioBuffer buf, int index)
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
			bool IsHarmonic(AudioBuffer buf, int index)
			{
				double freqResolution = buf.FrequencyResolution;

				double thisFreq = index * freqResolution;

				// allows some tolerance for the possibile loss of precision due to coarse fft frequency resolution
				double upperBound = (index + 1) * freqResolution;
				double lowerBound = (index - 1) * freqResolution;

				return firstHarmonics.Any(e => e >= lowerBound && e <= upperBound);
			}

			List<int> FindHarmonics(AudioBuffer buf, int index)
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

			/*int num;
			for (num = 0; num < 5; num++)
			{
				double[] fft = ffts[num];

				

				for (int i = 0; i < 1320 / buffers[num].FrequencyResolution; i++)
				{
					// add a frequency to the list of peaks if it is higher than surrounding frequencies
					if (IsPeak(fft, i) && !IsHarmonic(firstHarmonics, i))
					{
						// maximum possible number of harmonics that the length of fft array will allow to be found
						int maxNumHarmonics = fft.Length / i;
						List<int> harmonics = FindHarmonics(fft, i);
						// ignore if very few harmonics were found (for the case where 'harmonics' were found coincidentally in distortion)
						if (harmonics.Count < maxNumHarmonics / 2)
						{
							continue;
						}

						// try to find actual first harmonic in the case that a second, third, etc harmonic was found
						// (this will only apply to the case where this fact was not discovered earlier)
						int actualFirstHarmonic = i;
						int possibleFirstHarmonic;
						for (int divide = 2; (possibleFirstHarmonic = i / divide) >= 80 / buffers[num].FrequencyResolution; divide++) // TODO use prime numbers
						{
							List<int> quotientHarmonics = FindHarmonics(fft, possibleFirstHarmonic);
							if (quotientHarmonics.Count >= harmonics.Count)
							{
								harmonics = quotientHarmonics;
								actualFirstHarmonic = possibleFirstHarmonic;
							}
						}
						firstHarmonics.Add(actualFirstHarmonic);
					}
				}

				freqsList[num] = firstHarmonics;
			}

			return freqsList;*/






		}

		//// finds whether or not a given index is a relative peak
		//static bool IsPeak(double[] fft, double[] ambientNoiseLevels, int index)
		//{
		//	if (index < 0 || index >= fft.Length || fft[index] <= ambientNoiseLevels[index] * 2.5)
		//	{
		//		return false;
		//	}

		//	for (int comp = index - 1; comp <= index + 1; comp++)
		//	{
		//		bool validIndex = comp != index && comp >= 0 && comp < fft.Length;
		//		if (validIndex && fft[index] < fft[comp])
		//		{
		//			return false;
		//		}
		//	}

		//	return true;
		//}

		//// finds whether or not a given index is a harmonic of a note already found
		//static bool IsHarmonic(List<double> firstHarmonics, int index, double freqResolution)
		//{
		//	double thisFreq = index * freqResolution;

		//	// allows some tolerance for the possibile loss of precision due to coarse fft frequency resolution
		//	double upperBound = (index + 1) * freqResolution;
		//	double lowerBound = (index - 1) * freqResolution;

		//	return firstHarmonics.Any(e => e >= lowerBound && e <= upperBound);
		//}

		//static List<int> FindHarmonics(double[] fft, int index)
		//{
		//	List<int> harmonics = new List<int>();
		//	harmonics.Add(index);

		//	for (int harmonicNum = 2; harmonicNum * index < fft.Length; harmonicNum++)
		//	{
		//		int upperBound = harmonicNum * (index + 1);
		//		int lowerBound = harmonicNum * (index - 1);

		//		for (int searchIndex = lowerBound; searchIndex <= upperBound; searchIndex++)
		//		{
		//			if (IsPeak(fft, searchIndex))
		//			{
		//				harmonics.Add(searchIndex);
		//				break;
		//			}
		//		}
		//	}

		//	return harmonics;
		//}

		/*static int[] DominantFrequencies(double[] fft)
		{
			double avgVal = fft.Sum() / fft.Length;

			// make list of relatively high frequency peaks
			List<int> highIndexes = new List<int>();
			// number of surrounding values to compare each element to
			const int COMP_RADIUS = 4;
			for (int i = 0; i < fft.Length; i++)
			{
				if (fft[i] > avgVal)
				{
					bool highest = true;
					for (int comp = i - COMP_RADIUS; comp <= i + COMP_RADIUS; comp++)
					{
						if (comp == i || comp < 0 || comp >= fft.Length)
						{
							continue;
						}

						if (fft[i] < fft[comp])
						{
							highest = false;
							break;
						}
					}

					// add a frequency to the list of peaks if it is higher than surrounding frequencies
					if (highest)
					{
						highIndexes.Add(i);
					}
				}
			}

			double[] highVals = new double[highIndexes.Count];
			for (int i = 0; i < highVals.Length; i++)
			{
				highVals[i] = fft[highIndexes[i]];
			}
			// sorts in descending order
			highIndexes.Sort((x, y) => (int)(fft[y] - fft[x]));

			double[] ohighVals = new double[highIndexes.Count];
			for (int i = 0; i < ohighVals.Length; i++)
			{
				ohighVals[i] = fft[highIndexes[i]];
			}

			// gets top 5 highest dominant frequencies
			const int N = 5;
			int[] topN = new int[N];
			if (highIndexes.Count >= 5)
			{
				highIndexes.CopyTo(0, topN, 0, N);
				Array.Sort(topN);
			}
			

			return topN;
		}*/

		/*static int ValidDiff(int[] topN)
		{
			int smallestDiff = topN[1] - topN[0];
			for (int i = 1; i < topN.Length - 1; i++)
			{
				if (topN[i + 1] - topN[i] < smallestDiff)
				{
					smallestDiff = topN[i + 1] - topN[i];
				}
			}

			for (int i = 0; i < topN.Length - 1; i++)
			{
				double diffsBetween = topN[i + 1] - topN[i] / (double)smallestDiff;
				double integralDiffs = Math.Round(diffsBetween);

				if (Math.Abs(diffsBetween - integralDiffs) / diffsBetween > 0.03)
				{
					return 0;
				}
			}

			return smallestDiff;
		}*/
	}
}
