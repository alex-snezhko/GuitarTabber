using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GuitarTabber
{
	class Tab
	{
		// list of all recorded notes in the tab
		private List<Note> notes;
		// notes on each string of guitar i.e. EADGBe
		public readonly string[] stringTuning;

		private readonly int bpm;
		private readonly int beatsPerMeasure;

		// current length of tab in seconds
		public double totalLength;

		public Tab(string[] stringTuning, int bpm, int beatsPerMeasure)
		{
			notes = new List<Note>();

			this.stringTuning = stringTuning;
			this.bpm = bpm;
			this.beatsPerMeasure = beatsPerMeasure;
		}

		// adds a Note to the tab once the note has ended
		public void AddNote(double rawFrequency, double rawStartTime, double rawDuration)
		{
			notes.Add(new Note(this, rawFrequency, rawStartTime, rawDuration));
		}

		// how many seconds each measure will last
		public double MeasureDuration()
		{
			return beatsPerMeasure / (60.0 * bpm);
		}

		private class Note
		{
			// provides access to enclosing Tab's members
			private readonly Tab enclosingTab;

			// frequency in Hz of note
			public readonly double frequency;
			// duration in seconds
			public readonly double duration;
			// time in the measure at which this note begins playing
			public readonly double startTimeInMeasure;

			public Note(Tab enclosingTab, double rawFrequency, double rawStartTimeInMeasure, double rawDuration)
			{
				if (rawFrequency < 13.75 || rawDuration <= 0)
				{
					throw new ArgumentException();
				}

				this.enclosingTab = enclosingTab;

				frequency = NoteFrequency(rawFrequency);
				startTimeInMeasure = NoteStartTime(rawStartTimeInMeasure);
				duration = NoteDuration(rawDuration);
			}

			private double NoteFrequency(double rawFrequency)
			{
				// ratio at which one half step increases the frequency
				const double halfStepDeltaFreq = 1.059463094;
				// frequency of lowest A note a human can hear (used as initial point for algorithm)
				const double lowestAFreq = 13.75;

				double lowerFreq = lowestAFreq, higherFreq;

				// keeps increasing lower bounds until it is higher than raw frequency
				while (lowerFreq < rawFrequency)
				{			
					lowerFreq *= halfStepDeltaFreq;
				}

				// make sure lower bound is actually lower than raw frequency
				lowerFreq /= halfStepDeltaFreq;
				// set higher frequency bound
				higherFreq = lowerFreq * halfStepDeltaFreq;

				double deltaLower = rawFrequency - lowerFreq;
				double deltaHigher = higherFreq - rawFrequency;

				// returns frequency of note with smallest delta-absolute value to raw frequency
				return deltaLower > deltaHigher ? deltaHigher : deltaLower;
			}
		
			private double NoteStartTime(double rawStartTime)
			{
				return rawStartTime;
			}

			// returns 'proper' duration of note (makes the note the duration of a quarter note, eight note, etc)
			private double NoteDuration(double rawDuration)
			{
				return rawDuration;
				/*double fractionOfMeasure = rawDuration / enclosingTab.MeasureDuration();

				double upperBound = Math.Ceiling(fractionOfMeasure), lowerBound = upperBound;
				while(true)
				{

				}*/
			}
		}		
	}
}
