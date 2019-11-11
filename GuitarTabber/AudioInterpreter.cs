using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NAudio.Wave;
using Accord.Math;
using Accord.Math.Transforms;
using System.Numerics;

namespace GuitarTabber
{
	class AudioInterpreter
	{
		WaveIn waveIn;
		BufferedWaveProvider bwp;

		public const int SAMPLING_FREQUENCY = 11025; // in Hz
		const int BUFFER_LENGTH_BYTES = 4096;
		public const int BUFFER_LENGTH_16 = BUFFER_LENGTH_BYTES / 2; // buffer length in 16-bit units

		public const double INDEX_TO_HZ = (double)SAMPLING_FREQUENCY / BUFFER_LENGTH_16; // each index + 5.38 Hz

		public AudioInterpreter()
		{
			if (WaveIn.DeviceCount < 1)
			{
				throw new Exception("No audio device connected");
			}

			waveIn = new WaveIn
			{
				DeviceNumber = 0,
				WaveFormat = new WaveFormat(SAMPLING_FREQUENCY, 16, 1)
			};
			
			bwp = new BufferedWaveProvider(waveIn.WaveFormat)
			{
				DiscardOnBufferOverflow = true,
				BufferLength = BUFFER_LENGTH_BYTES
			};

			waveIn.DataAvailable += (s, args) => bwp.AddSamples(args.Buffer, 0, args.BytesRecorded);

			waveIn.StartRecording();
		}

		public short[] TickData()
		{
			if (bwp.BufferedBytes != BUFFER_LENGTH_BYTES)
			{
				int i = 1;
			}
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

			return data16Bit;
		}

		public static double[] GetFFT(short[] pcm)
		{
			double[] real = new double[pcm.Length];
			double[] imag = new double[pcm.Length];
			for (int i = 0; i < pcm.Length; i++)
			{
				real[i] = pcm[i];
			}
			FourierTransform2.FFT(real, imag, FourierTransform.Direction.Forward);

			// open low e string: 82.4 Hz, F24 high e: 1318.5 Hz
			const double LOWEST_FREQ = 0;
			const double HIGHEST_FREQ = 330 * 16;			

			// indexes of lower and upper bound freqs in FFT
			const int START = (int)(LOWEST_FREQ / INDEX_TO_HZ);
			const int END = (int)(HIGHEST_FREQ / INDEX_TO_HZ);
			double[] freqs = new double[END - START];
			for (int i = 10; i < freqs.Length; i++)
			{
				freqs[i] = Math.Sqrt((real[START + i] * real[START + i]) + (imag[START + i] * imag[START + i]));
			}

			return freqs;
		}

		// finds when a note begins to be played from the pcm sample
		public (int, int) StartEndNote(short[] pcm)
		{
			// minimum level for note to be detected
			const int THRESHOLD = 150;
			// minimum 'quiet' duration after a note required for the note to be considered completed
			const int QUIET_DURATION = 1000;
			//List<(int, int)> notes = new List<(int, int)>();

			for (int i = 0; i < pcm.Length; i++)
			{
				// beginning of note found
				if (pcm[i] > THRESHOLD)
				{
					int beginIndex = i;

					// try to find end of note
					int startBelowThreshold = beginIndex;	
					for (int j = beginIndex + 1; j < pcm.Length; j++)
					{
						// end of note found
						if (j == pcm.Length - 1)
						{
							return (beginIndex, pcm.Length);
						}
						if (j - startBelowThreshold >= QUIET_DURATION)
						{
							//notes.Add((beginIndex, startBelowThreshold));
							//i = startBelowThreshold;
							//break;
							return (beginIndex, startBelowThreshold);
						}

						// mark beginning of when audio falls below note threshold
						if (pcm[j] < THRESHOLD && pcm[j - 1] > THRESHOLD)
						{
							startBelowThreshold = j;
						}
					}
				}
			}

			return (0, 0);
		}


		/*public void Tick(out double volume, out double frequency)
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
			return 96.33 * (averageValue / 32767.0);
		}

		// finds dominant frequency with Fast Fourier Transform
		private double GetFrequency(short[] data)
		{
			double[] freqs = GetFFT(data);

			return freqs.Max();
		}*/

		
	}
}
