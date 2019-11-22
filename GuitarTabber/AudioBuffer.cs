using System;
using System.Collections.Generic;
using System.Linq;
using Accord.Math;
using Accord.Math.Transforms;
using NAudio.Wave;

namespace GuitarTabber
{
	class AudioBuffer
	{
		public BufferedWaveProvider pcmBuffer;
		public short[] pcm;
		public double FrequencyResolution { get; }

		public double[] FFT { get; private set; }

		List<double[]> fftAmbientNoiseLevelSamples;
		public double[] FFTAmbientNoiseLevels { get; private set; }

		public AudioBuffer(double freqResolution)
		{
			FrequencyResolution = freqResolution;
			fftAmbientNoiseLevelSamples = new List<double[]>();
		}

		public void RefreshData()
		{
			byte[] data8Bit = new byte[pcmBuffer.BufferLength];
			pcmBuffer.Read(data8Bit, 0, pcmBuffer.BufferLength);

			// bit depth is 16-bit, but buffer contains 8-bit data; correct this
			short[] data16Bit = new short[data8Bit.Length / 2];
			for (int i = 0; i < data16Bit.Length; i++)
			{
				byte large = data8Bit[2 * i + 1];
				byte small = data8Bit[2 * i];
				// bit shift to make data 16 bit
				data16Bit[i] = (short)((large << 8) | small);
			}

			pcm = data16Bit;

			FFT = CalculateFFT();
		}

		public bool AddAmbientLevelSample()
		{
			if (fftAmbientNoiseLevelSamples.Count == 10)
			{
				return false;
			}

			double[] fft = CalculateFFT();
			fftAmbientNoiseLevelSamples.Add(fft);

			if (fftAmbientNoiseLevelSamples.Count == 10)
			{
				FFTAmbientNoiseLevels = new double[fft.Length];
				for (int i = 0; i < FFTAmbientNoiseLevels.Length; i++)
				{
					double max = 0;
					for (int listIndex = 0; listIndex < fftAmbientNoiseLevelSamples.Count; listIndex++)
					{
						max = Math.Max(max, fftAmbientNoiseLevelSamples[listIndex][i]);
					}
					FFTAmbientNoiseLevels[i] = max;
				}
			}
			return true;
		}

		double[] CalculateFFT()
		{
			int len = pcmBuffer.BufferLength / 2;
			double[] real = new double[len];
			double[] imag = new double[len];
			for (int i = 0; i < len; i++)
			{
				real[i] = pcm[i];
			}
			FourierTransform2.FFT(real, imag, FourierTransform.Direction.Forward);

			// open low e string: 82.4 Hz, F24 high e: 1318.5 Hz
			const double LOWEST_FREQ = 0;
			const double HIGHEST_FREQ = 330 * 16;

			// indexes of lower and upper bound freqs in FFT
			int start = (int)(LOWEST_FREQ / FrequencyResolution);
			int end = (int)(HIGHEST_FREQ / FrequencyResolution);
			double[] freqs = new double[end - start];
			for (int i = 10; i < freqs.Length; i++)
			{
				freqs[i] = Math.Sqrt((real[start + i] * real[start + i]) + (imag[start + i] * imag[start + i]));
			}

			return freqs;
		}
	}
}
