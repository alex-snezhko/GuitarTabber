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
		//private Tab tab;
		private WaveOutEvent metronomeTickSound;
		private AudioFileReader tickSound;

		private AudioInterpreter interpreter;

		public MainForm()
		{
			InitializeComponent();

			metronomeTickSound = new WaveOutEvent();
			tickSound = new AudioFileReader(Resources.MetronomeTickFile);
			metronomeTickSound.Init(tickSound);
			tmrMetronome.Interval = (int)(60000 / udBpm.Value);

			interpreter = new AudioInterpreter();
		}

		private void tmrTabTime_Tick(object sender, EventArgs e)
		{
			double volume, frequency;
			interpreter.Tick(out volume, out frequency);

			//txtDebug.Text += volume.ToString() + '-';

			if (volume != 0)
			{
				lblVolume.Text = volume.ToString();
				lblVolume.Refresh();
			}

			if(frequency != 0)
			{
				lblFrequency.Text = frequency.ToString();
				lblFrequency.Refresh();
			}

			//tab.totalLength += tmrTabTime.Interval;
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

		private void NewTab(string[] tuning)
		{
			//tab = new Tab(tuning, udBpm.Value, );
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
