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

		public static readonly int SAMPLING_FREQUENCY = 11025;

		static AudioBuffer[] buffers;
		public static AudioBuffer[] Buffers { get => buffers; }

		public static bool ambientGathered;

		public static void Init()
		{
			if (WaveIn.DeviceCount < 1)
			{
				throw new Exception("No audio device connected");
			}

			buffers = new AudioBuffer[5];

			double[] freqResolutions = { 4.9, 5.9, 6.9, 7.9, 8.9 };
			for (int i = 0; i < 5; i++)
			{
				buffers[i] = new AudioBuffer((int)(SAMPLING_FREQUENCY / freqResolutions[i]), freqResolutions[i]);
			}

			input = new WaveIn
			{
				DeviceNumber = 0,
				WaveFormat = new WaveFormat(SAMPLING_FREQUENCY, 16, 1)
			};

			input.DataAvailable += (s, args) =>	AudioBuffer.pcmBuffer.AddSamples(args.Buffer, 0, args.BytesRecorded);
			AudioBuffer.pcmBuffer = new BufferedWaveProvider(input.WaveFormat)
			{
				BufferLength = 2 * buffers[0].BufferLength,
				DiscardOnBufferOverflow = true
			};

			input.StartRecording();
		}

		public static bool GatherInput()
		{
			if (!AudioBuffer.ExtractPcm())
			{
				return false;
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
		/*public static (int, int) StartEndNote(short[] pcm)
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
		}*/
	}
}
