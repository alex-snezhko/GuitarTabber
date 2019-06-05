using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NAudio.Wave;
using Accord.Math;
using System.Numerics;

namespace GuitarTabber
{
	class AudioInterpreter
	{
		private WaveIn waveIn;
		private BufferedWaveProvider bwp;

		public AudioInterpreter()
		{
			if(WaveIn.DeviceCount < 1)
			{
				throw new Exception("No audio device connected");
			}

			waveIn = new WaveIn();
			waveIn.DeviceNumber = 0;
			waveIn.WaveFormat = new WaveFormat(44100, 16, 1);
			waveIn.DataAvailable += (s, args) => bwp.AddSamples(args.Buffer, 0, args.BytesRecorded);

			bwp = new BufferedWaveProvider(waveIn.WaveFormat);
			bwp.DiscardOnBufferOverflow = true;
			bwp.BufferLength = (int)Math.Pow(2, 11);

			waveIn.StartRecording();
		}

		public void Tick(out double volume, out double frequency)
		{
			byte[] data8Bit = new byte[bwp.BufferLength];
			bwp.Read(data8Bit, 0, bwp.BufferLength);

			// bit depth is 16-bit, but buffer contains 8-bit data; correct this
			short[] data16Bit = new short[data8Bit.Length / 2];
			for (int i = 0; i < data16Bit.Length; i++)
			{
				byte large = data8Bit[2 * i + 1];
				byte small = data8Bit[2 * i];
				// bit shift to make data 16 bit
				data16Bit[i] = (short)((large << 8) | small);
			}

			volume = GetVolume(data16Bit);
			frequency = GetFrequency(data16Bit);

			bwp.ClearBuffer();
		}

		// returns average volume over tick
		private double GetVolume(short[] data)
		{
			return data.Max();
			/*short sum = 0;
			foreach (short d in data)
			{
				sum += d;
			}

			short averageValue = (short)(sum / data.Length);

			// formula for calculating volume in dB
			return 96.33 * (averageValue / 32767.0);*/
		}

		// finds dominant frequency with Fast Fourier Transform
		private double GetFrequency(short[] data)
		{
			Complex[] dataComplex = new Complex[data.Length];
			FourierTransform.FFT(dataComplex, FourierTransform.Direction.Forward);

			/*
			// counts number of times the data crosses the x-axis
			int zerosCounter = 0;
			for(int i = 0; i < data.Length - 1; i++)
			{
				if(data[i] > 0 && data[i + 1] <= 0
					|| data[i] < 0 && data[i + 1] >= 0)
				{
					zerosCounter++;
				}
			}

			return zerosCounter / 2.0;*/
		}
	}
}
