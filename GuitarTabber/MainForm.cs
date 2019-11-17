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
using static GuitarTabber.AudioInput;

namespace GuitarTabber
{
	public partial class MainForm : Form
	{
		//private Tab tab;
		WaveOutEvent metronomeTickSound;
		AudioFileReader tickSound;

		Graphics audioDataGfx;

		Graphics fftGfx1;
		Graphics fftGfx2;
		Graphics fftGfx3;
		Graphics fftGfx4;
		Graphics fftGfx5;

		public MainForm()
		{
			InitializeComponent();

			// initialize other stuff
			metronomeTickSound = new WaveOutEvent();
			tickSound = new AudioFileReader(Resources.MetronomeTickFile);
			metronomeTickSound.Init(tickSound);
			tmrMetronome.Interval = (int)(60000 / udBpm.Value);
			fftGfx1 = picFFT1.CreateGraphics();
			fftGfx2 = picFFT2.CreateGraphics();
			fftGfx3 = picFFT3.CreateGraphics();
			fftGfx4 = picFFT4.CreateGraphics();
			fftGfx5 = picFFT5.CreateGraphics();
			audioDataGfx = picAudioData.CreateGraphics();

			Init();
		}

		private void TmrReadAudio_Tick(object sender, EventArgs e)
		{
			tmrReadAudio.Stop();

			if (!GatherInput())
			{
				tmrReadAudio.Start();
				return;
			}

			if (ambientGathered)
			{
				btnBeginAnalyzing.BackColor = Color.Red;

				double[][] ffts = new double[5][];
				for (int i = 0; i < 5; i++)
				{
					ffts[i] = Buffers[i].GetFFT();
				}

				if (AudioBuffer.pcm.Max() > 350)
				{
					DrawDiagrams(AudioBuffer.pcm, ffts);

					List<int>[] dominantFreqs = FFTInterpreter.DominantFreqs(Buffers);
					double[] actualFreqs = new double[5];
					for (int i = 0; i < 5; i++)
					{
						actualFreqs[i] = dominantFreqs[i][0] * Buffers[i].FrequencyResolution;
					}
					double avg = actualFreqs.Average();

				}
			}


			

			
			//label6.Text = dominantFreqs[0].ToString();

			



			//if (fft.Max() > 10000.0)
			//{
			//	//int[] dominantFreqs = FFTInterpreter.DominantFrequencies(fft); // 0.01 millisec
			//}

			tmrReadAudio.Start();

		}

		private void DrawDiagrams(short[] pcm, double[][] ffts)
		{
			audioDataGfx.Clear(picAudioData.BackColor);
			fftGfx1.Clear(picAudioData.BackColor);
			fftGfx2.Clear(picAudioData.BackColor);
			fftGfx3.Clear(picAudioData.BackColor);
			fftGfx4.Clear(picAudioData.BackColor);
			fftGfx5.Clear(picAudioData.BackColor);

			Pen blackPen = new Pen(Color.Black);
			Pen redPen = new Pen(Color.Red);

			// draw pcm data to audio data picturebox
			/*double valToXPixCoeff = picAudioData.Width / (double)pcm.Length;
			double valToYPixCoeff = (picAudioData.Height / 2.0) * (tbFFTScale.Value + 1) / short.MaxValue;
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
			double valToXPixCoeffFFT = picFFT1.Width / (double)ffts[0].Length;
			double valToYPixCoeffFFT = (picFFT1.Height / 2.0) * (tbFFTScale.Value + 1) / 100000.0;
			for (int i = 0; i < ffts[0].Length; i++)
			{
				int x = (int)(i * valToXPixCoeffFFT) + 1;
				int height = (int)(ffts[0][i] * valToYPixCoeffFFT);
				fftGfx1.DrawLine(redPen, x, picFFT1.Height, x, picFFT1.Height - height);
			}

			fftGfx1.DrawLine(blackPen, 0, 0, 0, picFFT1.Height);
			fftGfx1.DrawLine(blackPen, 0, picFFT1.Height - 1, picFFT1.Width, picFFT1.Height - 1);
			fftGfx1.DrawLine(blackPen, picFFT1.Width / 16, 0, picFFT1.Width / 16, picFFT1.Height / 2);

			valToXPixCoeffFFT = picFFT1.Width / (double)ffts[1].Length;
			for (int i = 0; i < ffts[1].Length; i++)
			{
				int x = (int)(i * valToXPixCoeffFFT) + 1;
				int height = (int)(ffts[1][i] * valToYPixCoeffFFT);
				fftGfx2.DrawLine(redPen, x, picFFT1.Height, x, picFFT1.Height - height);
			}

			fftGfx2.DrawLine(blackPen, 0, 0, 0, picFFT1.Height);
			fftGfx2.DrawLine(blackPen, 0, picFFT1.Height - 1, picFFT1.Width, picFFT1.Height - 1);
			fftGfx2.DrawLine(blackPen, picFFT1.Width / 16, 0, picFFT1.Width / 16, picFFT1.Height / 2);

			valToXPixCoeffFFT = picFFT1.Width / (double)ffts[2].Length;
			for (int i = 0; i < ffts[2].Length; i++)
			{
				int x = (int)(i * valToXPixCoeffFFT) + 1;
				int height = (int)(ffts[2][i] * valToYPixCoeffFFT);
				fftGfx3.DrawLine(redPen, x, picFFT1.Height, x, picFFT1.Height - height);
			}

			fftGfx3.DrawLine(blackPen, 0, 0, 0, picFFT1.Height);
			fftGfx3.DrawLine(blackPen, 0, picFFT1.Height - 1, picFFT1.Width, picFFT1.Height - 1);
			fftGfx3.DrawLine(blackPen, picFFT1.Width / 16, 0, picFFT1.Width / 16, picFFT1.Height / 2);

			valToXPixCoeffFFT = picFFT1.Width / (double)ffts[3].Length;
			for (int i = 0; i < ffts[3].Length; i++)
			{
				int x = (int)(i * valToXPixCoeffFFT) + 1;
				int height = (int)(ffts[3][i] * valToYPixCoeffFFT);
				fftGfx4.DrawLine(redPen, x, picFFT1.Height, x, picFFT1.Height - height);
			}

			fftGfx4.DrawLine(blackPen, 0, 0, 0, picFFT1.Height);
			fftGfx4.DrawLine(blackPen, 0, picFFT1.Height - 1, picFFT1.Width, picFFT1.Height - 1);
			fftGfx4.DrawLine(blackPen, picFFT1.Width / 16, 0, picFFT1.Width / 16, picFFT1.Height / 2);

			valToXPixCoeffFFT = picFFT1.Width / (double)ffts[4].Length;
			for (int i = 0; i < ffts[4].Length; i++)
			{
				int x = (int)(i * valToXPixCoeffFFT) + 1;
				int height = (int)(ffts[4][i] * valToYPixCoeffFFT);
				fftGfx5.DrawLine(redPen, x, picFFT1.Height, x, picFFT1.Height - height);
			}

			fftGfx5.DrawLine(blackPen, 0, 0, 0, picFFT1.Height);
			fftGfx5.DrawLine(blackPen, 0, picFFT1.Height - 1, picFFT1.Width, picFFT1.Height - 1);
			fftGfx5.DrawLine(blackPen, picFFT1.Width / 16, 0, picFFT1.Width / 16, picFFT1.Height / 2);

			blackPen.Dispose();
			redPen.Dispose();
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


		}

		private void btnStopRecording_Click(object sender, EventArgs e)
		{
			//tab = null;
			//tmrTabTime.Stop();
			//tmrTabTime.Enabled = false;
		}	
	}
}
