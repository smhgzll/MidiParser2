using OpenTK.Audio;
using OpenTK.Audio.OpenAL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace MidiParser2
{
    public class OpenALSoundPlayer
    {
        private readonly AudioContext _audioContext;
        private readonly Dictionary<int, int> _activeSources = new(); // Note -> OpenAL source
        private const int SampleRate = 44100;

        public OpenALSoundPlayer()
        {
            _audioContext = new AudioContext();
        }

        public void PlayMidiEvents(List<MidiEvent> midiEvents)
        {
            if (midiEvents == null || midiEvents.Count == 0)
            {
                Console.WriteLine("No MIDI events to play.");
                return;
            }

            // Sort events by timestamp for correct playback order
            midiEvents = midiEvents.OrderBy(e => e.Timestamp).ToList();

            Stopwatch timer = Stopwatch.StartNew();
            int eventIndex = 0;

            while (eventIndex < midiEvents.Count)
            {
                // Convert elapsed milliseconds to seconds
                double currentTime = timer.ElapsedMilliseconds / 1000.0;

                // Process all events up to the current time
                while (eventIndex < midiEvents.Count && midiEvents[eventIndex].Timestamp <= currentTime)
                {
                    var midiEvent = midiEvents[eventIndex];
                    Console.WriteLine(
                        $"Processing Event: {midiEvent.EventType} - Note: {midiEvent.Note}, Timestamp: {midiEvent.Timestamp:F2}, Current Time: {currentTime:F2}");

                    if (midiEvent.EventType == "NoteOn" && midiEvent.Velocity > 0)
                    {
                        // Start the sound for the note
                        double frequency = MidiNoteToFrequency(midiEvent.Note);
                        var soundBuffer = GenerateTone(frequency, 2.0); // Generate a 2-second tone
                        PlaySound(midiEvent.Note, soundBuffer);
                    }
                    else if (midiEvent.EventType == "NoteOff")
                    {
                        // Stop the sound for the note, regardless of its current state
                        StopSound(midiEvent.Note);
                    }

                    eventIndex++;
                }

                // Yield CPU to avoid high usage
                System.Threading.Thread.Yield();
            }

            // Clean up all active notes at the end
            StopAllSounds();
        }

        private void PlaySound(int note, byte[] soundData)
        {
            int buffer, source;

            // Generate a new buffer for each note
            AL.GenBuffers(1, out buffer);
            AL.BufferData(buffer, ALFormat.Mono16, soundData, soundData.Length, SampleRate);
            AL.GenSources(1, out source);
            AL.Source(source, ALSourcei.Buffer, buffer);
            AL.SourcePlay(source);

            // Store the source for the note so we can manage it later
            if (_activeSources.ContainsKey(note))
                StopSound(note); // Stop any currently playing instance of this note

            _activeSources[note] = source;
        }

        private void StopSound(int note)
        {
            if (_activeSources.TryGetValue(note, out int source))
            {
                Console.WriteLine($"Stopping Note: {note}");

                // Stop and delete the source
                AL.SourceStop(source);
                AL.DeleteSource(source);

                // Remove the source from active sources
                _activeSources.Remove(note);
            }
            else
            {
                Console.WriteLine($"Note {note} is not currently playing. Ignoring NoteOff.");
            }
        }

        private void StopAllSounds()
        {
            // Stop and delete all active sources once they are no longer needed
            foreach (var source in _activeSources.Values)
            {
                AL.SourceStop(source);
                AL.DeleteSource(source);
            }

            _activeSources.Clear();
        }

        private double MidiNoteToFrequency(int note)
        {
            return 440.0 * Math.Pow(2.0, (note - 69) / 12.0);
        }

        public byte[] GenerateTone(double frequency, double duration, double attackTime = 0.10,
            double releaseTime = 0.10)
        {
            int length = (int)(duration * SampleRate);
            short[] samples = new short[length];
            double increment = 2 * Math.PI * frequency / SampleRate;

            int attackSamples = (int)(attackTime * SampleRate);
            int releaseSamples = (int)(releaseTime * SampleRate);
            int sustainStart = attackSamples;
            int sustainEnd = length - releaseSamples;

            for (int i = 0; i < length; i++)
            {
                double envelope;

                // Apply attack envelope
                if (i < attackSamples)
                {
                    envelope = (double)i / attackSamples; // Linear ramp up
                }
                // Sustain level (constant)
                else if (i < sustainEnd)
                {
                    envelope = 1.0;
                }
                // Apply release envelope
                else
                {
                    envelope = (double)(length - i) / releaseSamples; // Linear ramp down
                }

                // Generate the sample with the envelope applied
                samples[i] = (short)(Math.Sin(increment * i) * short.MaxValue * envelope);
            }

            // Convert to byte array
            byte[] byteArray = new byte[length * sizeof(short)];
            Buffer.BlockCopy(samples, 0, byteArray, 0, byteArray.Length);
            return byteArray;
        }
    }
}