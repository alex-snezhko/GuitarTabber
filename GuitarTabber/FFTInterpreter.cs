using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GuitarTabber
{
	static class FFTInterpreter
	{
		// array of ambient noise levels in environment at each given fft frequency
		static double[] ambientNoiseLevels;

		public static void FindAmbientLevel(List<double[]> fftList)
		{
			ambientNoiseLevels = new double[fftList[0].Length];
			for (int i = 0; i < ambientNoiseLevels.Length; i++)
			{
				double max = 0;
				for (int listIndex = 0; listIndex < fftList.Count; listIndex++)
				{
					max = Math.Max(max, fftList[listIndex][i]);
				}
				ambientNoiseLevels[i] = max;
			}
		}

		public static List<int> DominantFreqs(double[] fft)
		{
			// finds all prominent frequencies in fft signal (harmonics ignored)
			List<int> peakIndexes = new List<int>();

			for (int i = 0; i < fft.Length; i++)
			{
				if (fft[i] <= ambientNoiseLevels[i] * 2.5)
				{
					continue;
				}

				// add a frequency to the list of peaks if it is higher than surrounding frequencies
				if (!IsHarmonic(i) && IsPeak(i))
				{
					peakIndexes.Add(i);
				}
			}

			// finds whether or not a given index is a harmonic of a note already found
			bool IsHarmonic(int index)
			{
				foreach (int peak in peakIndexes)
				{
					double actualQuotient = (double)index / peak;
					double possibleHarmonic = Math.Round(actualQuotient);

					// tries to see if this is still a harmonic with an allowed tolerance (that may have been caused from
					//   lack of precision in fft index corresponding to its correct frequency e.g. a frequency that should be
					//   at precisely index 10.66 being placed in index 11); +/- 1 index should be the most error that this could cause
					double upperBound = possibleHarmonic * (peak + 1.0 / peak);
					double lowerBound = possibleHarmonic * (peak - 1.0 / peak);

					if (actualQuotient >= lowerBound && actualQuotient <= upperBound)
					{
						return true;
					}
				}

				return false;
			}

			// finds whether or not a given index is a relative peak
			bool IsPeak(int index)
			{
				const int COMP_RADIUS = 4;

				for (int comp = index - COMP_RADIUS; comp <= index + COMP_RADIUS; comp++)
				{
					if (comp == index || comp < 0 || comp >= fft.Length)
					{
						continue;
					}

					if (fft[index] < fft[comp])
					{
						return false;
					}
				}

				return true;
			}

			return peakIndexes;
		}

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
