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
	static class AudioInput
	{
		static WaveIn input;

		public const int BUFFER_LENGTH_16 = 2048;

		public const int SAMPLING_RATE = (int)(AudioBuffer.FREQ_RESOLUTION * BUFFER_LENGTH_16);

		static AudioBuffer[] buffers;
		public static AudioBuffer[] Buffers { get => buffers; }

		public static bool ambientGathered;

		public static void Init()
		{
			if (WaveIn.DeviceCount < 1)
			{
				throw new Exception("No audio device connected");
			}

			const int NUM_BUFFERS = 10;
			
			input = new WaveIn
			{
				DeviceNumber = 0,
				WaveFormat = new WaveFormat(SAMPLING_RATE, 16, 1)
			};

			AudioBuffer.PcmBuffer = new BufferedWaveProvider(input.WaveFormat)
			{
				DiscardOnBufferOverflow = true,
				BufferLength = 2 * BUFFER_LENGTH_16
			};
			input.DataAvailable += (s, args) => AudioBuffer.PcmBuffer.AddSamples(args.Buffer, 0, args.BytesRecorded);

			buffers = new AudioBuffer[NUM_BUFFERS];
			for (int i = 0; i < NUM_BUFFERS; i++)
			{
				buffers[i] = new AudioBuffer(i);
			}

			input.StartRecording();
		}

		public static bool GatherInput()
		{
			BufferedWaveProvider bwp = AudioBuffer.PcmBuffer;
			if (bwp.BufferedBytes != bwp.BufferLength)
			{
				return false;
			}
			AudioBuffer.RefreshPCM();
			

			foreach (AudioBuffer buf in buffers)
			{
				buf.CalculateFFT();
			}

			if (!ambientGathered)
			{
				foreach (AudioBuffer buf in buffers)
				{
					if (!buf.AddAmbientLevelSample())
					{
						ambientGathered = true;
						break;
					}
				}
			}
			

			return true;
		}

		// finds when a note begins to be played from the pcm sample
		public static (int, int) StartEndNote(short[] pcm)
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
	}
}
