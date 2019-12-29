using System;
using System.Collections.Generic;
using System.Linq;
using Accord.Math;
using Accord.Math.Transforms;
using NAudio.Wave;
using System.Numerics;

namespace GuitarTabber
{
	class AudioBuffer
	{
		public static BufferedWaveProvider PcmBuffer { get; set; }
		public static short[] pcm;
		public const double FREQ_RESOLUTION = 10.0;

		public double[] FFT { get; private set; }
		public const double FFT_HIGHEST_FREQ = 5280;

		List<double[]> fftAmbientNoiseLevelSamples;
		public double[] FFTAmbientNoiseLevels { get; private set; }

		// offset for pcm so each audio buffer gets different fft
		public double Offset { get; }

		public AudioBuffer(double offset)
		{
			Offset = offset;
			fftAmbientNoiseLevelSamples = new List<double[]>();
		}

		public static void RefreshPCM()
		{
			byte[] data8Bit = new byte[PcmBuffer.BufferLength];
			PcmBuffer.Read(data8Bit, 0, PcmBuffer.BufferLength);

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
		}

		public void CalculateFFT()
		{
			Complex[] complexPcm = new Complex[AudioInput.BUFFER_LENGTH_16];
			for (int i = 0; i < AudioInput.BUFFER_LENGTH_16; i++)
			{
				// https://dsp.stackexchange.com/questions/1991/how-to-shift-the-frequency-spectrum
				Complex exp = new Complex(0.0, -2 * Math.PI * Offset * i / AudioInput.SAMPLING_RATE);
				complexPcm[i] = pcm[i] * Complex.Exp(exp);
			}
			FourierTransform2.FFT(complexPcm, FourierTransform.Direction.Forward);

			const int HIGH = (int)(FFT_HIGHEST_FREQ / FREQ_RESOLUTION);
			double[] freqs = new double[HIGH];
			for (int i = 7; i < freqs.Length; i++)
			{
				freqs[i] = complexPcm[i].Magnitude;
			}

			FFT = freqs;
		}

		public bool AddAmbientLevelSample()
		{
			if (fftAmbientNoiseLevelSamples.Count == 10)
			{
				return false;
			}

			CalculateFFT();
			fftAmbientNoiseLevelSamples.Add(FFT);

			if (fftAmbientNoiseLevelSamples.Count == 10)
			{
				FFTAmbientNoiseLevels = new double[FFT.Length];
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
	}
}
