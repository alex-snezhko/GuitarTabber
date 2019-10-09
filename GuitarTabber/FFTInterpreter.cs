using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GuitarTabber
{
	static class FFTInterpreter
	{
		public static void AnalyzeFFT(short[] fft)
		{

		}

		public static int FindPeakDifference(double[] fft)
		{
			int[] topN = DominantFrequencies(fft);

			return ValidDiff(topN);
		}

		static int[] DominantFrequencies(double[] fft)
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
			highIndexes.CopyTo(0, topN, 0, N);
			Array.Sort(topN);

			return topN;
		}

		static int ValidDiff(int[] topN)
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
		}
	}
}
