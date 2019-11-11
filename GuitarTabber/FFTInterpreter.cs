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
			List<int> firstHarmonics = new List<int>();

			for (int i = 0; i < 1320 / AudioInterpreter.INDEX_TO_HZ; i++)
			{
				if (fft[i] <= ambientNoiseLevels[i] * 2.5)
				{
					continue;
				}

				// add a frequency to the list of peaks if it is higher than surrounding frequencies
				if (!IsHarmonic(i) && IsPeak(i))
				{
					// maximum possible number of harmonics that the length of fft array will allow to be found
					int maxNumHarmonics = fft.Length / i;
					List<int> harmonics = FindHarmonics(i);
					// ignore if very few harmonics were found (for the case where 'harmonics' were found coincidentally in distortion)
					if (harmonics.Count + 1 < maxNumHarmonics / 2)
					{
						continue;
					}

					// try to find actual first harmonic in the case that a second, third, etc harmonic was found
					int actualFirstHarmonic = i;
					int possibleFirstHarmonic;
					for (int divide = 2; (possibleFirstHarmonic = i / divide) < 10; divide++)
					{
						List<int> quotientHarmonics = FindHarmonics(possibleFirstHarmonic);
						if (quotientHarmonics.Count >= harmonics.Count)
						{
							harmonics = quotientHarmonics;
							actualFirstHarmonic = possibleFirstHarmonic;
						}
					}
					firstHarmonics.Add(actualFirstHarmonic);
				}
			}

			return firstHarmonics;

			// finds whether or not a given index is a harmonic of a note already found
			bool IsHarmonic(int index)
			{
				foreach (int e in firstHarmonics)
				{
					int possibleHarmonic = (int)Math.Round((double)index / e);

					// tries to see if this is still a harmonic with an allowed tolerance (that may have been caused from
					//   lack of precision in fft index corresponding to its correct frequency e.g. a frequency that should be
					//   at precisely index 10.66 being placed in index 11); +/- 1 index should be the most error that this could cause
					int upperBound = possibleHarmonic * (e + 1);
					int lowerBound = possibleHarmonic * (e - 1);

					if (index >= lowerBound && index <= upperBound)
					{
						return true;
					}
				}

				return false;
			}

			// finds whether or not a given index is a relative peak
			bool IsPeak(int index)
			{
				if (index < 0 || index >= fft.Length)
				{
					return false;
				}

				const int COMP_RADIUS = 3;

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

			List<int> FindHarmonics(int index)
			{
				List<int> harmonics = new List<int>();

				for (int harmonicNum = 2; harmonicNum * index < fft.Length; harmonicNum++)
				{
					int upperBound = harmonicNum * (index + 1);
					int lowerBound = harmonicNum * (index - 1);

					for (int searchIndex = lowerBound; searchIndex <= upperBound; searchIndex++)
					{
						if (IsPeak(searchIndex))
						{
							harmonics.Add(searchIndex);
							break;
						}
					}
				}

				return harmonics;
			}

			void Recheck()
			{
				foreach (int e in firstHarmonics)
				{
					
				}
			}
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
