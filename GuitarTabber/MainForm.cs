using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Accord.Math;
using Accord.Math.Transforms;
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
		Graphics fftGfx6;
		Graphics fftGfx7;
		Graphics fftGfx8;
		Graphics fftGfx9;

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
			fftGfx6 = picFFT6.CreateGraphics();
			fftGfx7 = picFFT7.CreateGraphics();
			fftGfx8 = picFFT8.CreateGraphics();
			fftGfx9 = picFFT9.CreateGraphics();
			audioDataGfx = picAudioData.CreateGraphics();

			Init();
		}

		private void TmrReadAudio_Tick(object sender, EventArgs e)
		{
			DateTime t = DateTime.Now;
			tmrReadAudio.Stop();

			if (!GatherInput())
			{
				tmrReadAudio.Start();
				return;
			}

			if (ambientGathered)
			{
				btnBeginAnalyzing.BackColor = Color.Red;

				short[] pcm = AudioBuffer.pcm;

				/////
				/*double[] windowed = FFTInterpreter.WindowedPCM(pcm);
				const int LEN = AudioInput.BUFFER_LENGTH_16 - 10;
				double[] real = new double[LEN];
				double[] imag = new double[LEN];
				for (int i = 0; i < LEN; i++)
				{
					real[i] = windowed[i];
				}
				FourierTransform2.FFT(real, imag, FourierTransform.Direction.Forward);

				const int HIGH = (int)(AudioBuffer.FFT_HIGHEST_FREQ / AudioBuffer.FREQ_RESOLUTION);
				double[] windowedFFT = new double[HIGH];
				for (int i = 7; i < windowedFFT.Length; i++)
				{
					windowedFFT[i] = Math.Sqrt((real[i] * real[i]) + (imag[i] * imag[i]));
				}*/
				////


				double[][] ffts = new double[10][];
				for (int i = 0; i < ffts.Length; i++)
				{
					ffts[i] = Buffers[i].FFT;
				}

				

				if (pcm.Max() > 350)
				{
					DrawDiagrams(pcm, ffts);
					List<double> dominantFreqs = FFTInterpreter.NoteFreqs(Buffers);
					string s = "";
					foreach (double f in dominantFreqs)
					{
						s += f.ToString("F3") + "\n";
					}
					lblFreqs.Text = s;

				}
			}

			double dt = (DateTime.Now - t).TotalMilliseconds;
			//Console.WriteLine(dt.ToString());



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
			fftGfx6.Clear(picAudioData.BackColor);
			fftGfx7.Clear(picAudioData.BackColor);
			fftGfx8.Clear(picAudioData.BackColor);
			fftGfx9.Clear(picAudioData.BackColor);

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

			valToXPixCoeffFFT = picFFT1.Width / (double)ffts[5].Length;
			for (int i = 0; i < ffts[5].Length; i++)
			{
				int x = (int)(i * valToXPixCoeffFFT) + 1;
				int height = (int)(ffts[5][i] * valToYPixCoeffFFT);
				fftGfx6.DrawLine(redPen, x, picFFT1.Height, x, picFFT1.Height - height);
			}

			fftGfx6.DrawLine(blackPen, 0, 0, 0, picFFT1.Height);
			fftGfx6.DrawLine(blackPen, 0, picFFT1.Height - 1, picFFT1.Width, picFFT1.Height - 1);
			fftGfx6.DrawLine(blackPen, picFFT1.Width / 16, 0, picFFT1.Width / 16, picFFT1.Height / 2);

			valToXPixCoeffFFT = picFFT1.Width / (double)ffts[6].Length;
			for (int i = 0; i < ffts[6].Length; i++)
			{
				int x = (int)(i * valToXPixCoeffFFT) + 1;
				int height = (int)(ffts[6][i] * valToYPixCoeffFFT);
				fftGfx7.DrawLine(redPen, x, picFFT1.Height, x, picFFT1.Height - height);
			}

			fftGfx7.DrawLine(blackPen, 0, 0, 0, picFFT1.Height);
			fftGfx7.DrawLine(blackPen, 0, picFFT1.Height - 1, picFFT1.Width, picFFT1.Height - 1);
			fftGfx7.DrawLine(blackPen, picFFT1.Width / 16, 0, picFFT1.Width / 16, picFFT1.Height / 2);

			valToXPixCoeffFFT = picFFT1.Width / (double)ffts[7].Length;
			for (int i = 0; i < ffts[7].Length; i++)
			{
				int x = (int)(i * valToXPixCoeffFFT) + 1;
				int height = (int)(ffts[7][i] * valToYPixCoeffFFT);
				fftGfx8.DrawLine(redPen, x, picFFT1.Height, x, picFFT1.Height - height);
			}

			fftGfx8.DrawLine(blackPen, 0, 0, 0, picFFT1.Height);
			fftGfx8.DrawLine(blackPen, 0, picFFT1.Height - 1, picFFT1.Width, picFFT1.Height - 1);
			fftGfx8.DrawLine(blackPen, picFFT1.Width / 16, 0, picFFT1.Width / 16, picFFT1.Height / 2);

			valToXPixCoeffFFT = picFFT1.Width / (double)ffts[8].Length;
			for (int i = 0; i < ffts[8].Length; i++)
			{
				int x = (int)(i * valToXPixCoeffFFT) + 1;
				int height = (int)(ffts[8][i] * valToYPixCoeffFFT);
				fftGfx9.DrawLine(redPen, x, picFFT1.Height, x, picFFT1.Height - height);
			}

			fftGfx9.DrawLine(blackPen, 0, 0, 0, picFFT1.Height);
			fftGfx9.DrawLine(blackPen, 0, picFFT1.Height - 1, picFFT1.Width, picFFT1.Height - 1);
			fftGfx9.DrawLine(blackPen, picFFT1.Width / 16, 0, picFFT1.Width / 16, picFFT1.Height / 2);

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
