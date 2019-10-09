using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GuitarTabber.Properties;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace GuitarTabber
{
	public partial class MainForm : Form
	{
		AudioInterpreter interpreter;

		readonly short[] pcmBuffer;
		int amountBufferFilled;

		//private Tab tab;
		WaveOutEvent metronomeTickSound;
		AudioFileReader tickSound;

		Graphics audioDataGfx;
		Graphics fftGfx;

		public MainForm()
		{
			InitializeComponent();

			interpreter = new AudioInterpreter();
			pcmBuffer = new short[AudioInterpreter.BUFFER_LENGTH_BYTES / 2];

			// initialize other stuff
			metronomeTickSound = new WaveOutEvent();
			tickSound = new AudioFileReader(Resources.MetronomeTickFile);
			metronomeTickSound.Init(tickSound);
			tmrMetronome.Interval = (int)(60000 / udBpm.Value);
			fftGfx = picFFT.CreateGraphics();
			audioDataGfx = picAudioData.CreateGraphics();
		}

		private void TmrReadAudio_Tick(object sender, EventArgs e)
		{
			tmrReadAudio.Stop();

			short[] tickPCM = interpreter.TickData();
			//(int beginNote, int endNote) = interpreter.StartEndNote(tickPCM);
			//if (amountBufferFilled == pcmBuffer.Length)
			//{
			//	Array.Copy(tickPCM, pcmBuffer, tickPCM.Length);
			//}
			//else
			//{
			//	for (int i = beginNote; i < endNote; i++)
			//	{
			//		pcmBuffer[i + amountBufferFilled] = tickPCM[i];
			//	}
			//}

			

			double[] fft = AudioInterpreter.GetFFT(tickPCM); // 0.18 millisec

			int diff = FFTInterpreter.FindPeakDifference(fft);
			label6.Text = diff.ToString();

			DrawDiagrams(tickPCM, fft); // 83 milliseconds



			//if (fft.Max() > 10000.0)
			//{
			//	//int[] dominantFreqs = FFTInterpreter.DominantFrequencies(fft); // 0.01 millisec
			//}

			tmrReadAudio.Start();

		}

		private void DrawDiagrams(short[] pcm, double[] fft)
		{
			audioDataGfx.Clear(picAudioData.BackColor);
			fftGfx.Clear(picAudioData.BackColor);

			Pen blackPen = new Pen(Color.Black);
			Pen redPen = new Pen(Color.Red);

			// draw pcm data to audio data picturebox
			/*double valToXPixCoeff = picAudioData.Width / (double)pcm.Length;
			double valToYPixCoeff = (picAudioData.Height / 2.0) / short.MaxValue;
			for (int i = 0; i < pcm.Length; i += 2)
			{
				int x = (int)(i * valToXPixCoeff) + 1;
				int height = (int)(pcm[i] * valToYPixCoeff * 3);
				if (height != 0)
				{
					audioDataGfx.DrawLine(redPen, x, picAudioData.Height / 2, x, picAudioData.Height / 2 - height);
				}
			}

			// draws audio data grid
			audioDataGfx.DrawLine(blackPen, 0, 0, 0, picAudioData.Height);
			audioDataGfx.DrawLine(blackPen, 0, picAudioData.Height / 2, picAudioData.Width, picAudioData.Height / 2);*/

			// draw fft data to fft picturebox
			double valToXPixCoeffFFT = picFFT.Width / (double)fft.Length;
			double valToYPixCoeffFFT = (picFFT.Height / 2.0) * (tbFFTScale.Value + 1) / 100000.0;
			for (int i = 0; i < fft.Length; i++)
			{
				int x = (int)(i * valToXPixCoeffFFT) + 1;
				int height = (int)(fft[i] * valToYPixCoeffFFT);
				fftGfx.DrawLine(redPen, x, picFFT.Height, x, picFFT.Height - height);
			}

			fftGfx.DrawLine(blackPen, 0, 0, 0, picFFT.Height);
			fftGfx.DrawLine(blackPen, 0, picFFT.Height - 1, picFFT.Width, picFFT.Height - 1);

			blackPen.Dispose();
			redPen.Dispose();
		}

		private void tmrTabTime_Tick(object sender, EventArgs e)
		{
			tmrTabTime.Enabled = false;

			

			/*interpreter.Tick(out double volume, out double frequency);

			if (volume != 0)
			{
				lblVolume.Text = volume.ToString();
				lblVolume.Refresh();
			}

			if(frequency != 0)
			{
				lblFrequency.Text = frequency.ToString();
				lblFrequency.Refresh();
			}*/

			tmrTabTime.Enabled = true;
		}

		private void tmrMetronome_Tick(object sender, EventArgs e)
		{
			if (chkMetronomeOn.Checked)
			{
				tickSound.Position = 0;
				metronomeTickSound.Play();
			}
		}

		private void udBpm_ValueChanged(object sender, EventArgs e)
		{
			tmrMetronome.Interval = (int)(60000 / udBpm.Value);
			//Tab t = new Tab(new string[] { "E", "A", "D", "G", "B", "e" }, 100, 4);
		}

		private void btnStartRecording_Click(object sender, EventArgs e)
		{
			string[] tunings = new string[6]
			{
				txtString1Tuning.Text.Trim(),
				txtString2Tuning.Text.Trim(),
				txtString3Tuning.Text.Trim(),
				txtString4Tuning.Text.Trim(),
				txtString5Tuning.Text.Trim(),
				txtString6Tuning.Text.Trim()
			};

			foreach (string str in tunings)
			{
				if (str.Length < 1)
				{
					MessageBox.Show("Enter notes for all strings");
					return;
				}

				if (str.Length > 2)
				{
					MessageBox.Show("At least one string note's text is too long");
					return;
				}

				if (str[0] < 65 || str[0] > 103 || (str[0] > 71 && str[0] < 97))
				{
					MessageBox.Show("Enter valid notes for all string tunings");
					return;
				}

				if (str.Length > 1)
				{
					if (str[1] != 's' && str[1] != 'f')
					{
						MessageBox.Show("Enter 's' after note for sharp or 'f' after note for flat");
						return;
					}

					if (str[0] == 'B' || str[0] == 'E' || str[0] == 'b' || str[0] == 'e')
					{
						MessageBox.Show("B or E do not have flat or sharp values");
						return;
					}
				}
			}

			foreach (char c in txtBeatsPerMeasure.Text.ToCharArray())
			{
				if (!char.IsDigit(c))
				{
					MessageBox.Show("Beats per measure must be a whole number");
					return;
				}
			}

			//tab = new Tab(tunings, (int)udBpm.Value, int.Parse(txtBeatsPerMeasure.Text));
			tmrTabTime.Enabled = true;
			tmrTabTime.Start();
		}

		private void btnStopRecording_Click(object sender, EventArgs e)
		{
			//tab = null;
			tmrTabTime.Stop();
			tmrTabTime.Enabled = false;
		}	
	}
}
