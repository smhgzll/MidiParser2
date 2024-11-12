using System.Text;

namespace MidiParser2;

public class MidiParser
{
    public static List<MidiEvent> ParseMidiFile(string filePath)
    {
        var midiEvents = new List<MidiEvent>();
        double previousTimestamp = 0;

        using (var reader = new BinaryReader(File.Open(filePath, FileMode.Open)))
        {
            // Read the header chunk
            string headerChunk = Encoding.ASCII.GetString(reader.ReadBytes(4));
            if (headerChunk != "MThd")
                throw new Exception("Invalid MIDI file: Missing MThd chunk");

            int headerLength = ReadBigEndianInt(reader);
            short formatType = ReadBigEndianShort(reader);
            short trackCount = ReadBigEndianShort(reader);
            short division = ReadBigEndianShort(reader);

            Console.WriteLine($"Header Length: {headerLength} bytes");
            Console.WriteLine($"Format Type: {formatType} (0 = Single Track, 1 = Multiple Tracks, 2 = Multiple Songs)");
            Console.WriteLine($"Track Count: {trackCount}");
            Console.WriteLine($"Division (Ticks per Quarter Note): {division}");
            Console.WriteLine("------------------------");

            double ticksPerQuarter = division & 0x7FFF;
            double microsecondsPerQuarterNote = 500000; // Default tempo: 120 BPM
            double totalDuration = 0;

            // Read each track chunk
            for (int i = 0; i < trackCount; i++)
            {
                string trackChunk = Encoding.ASCII.GetString(reader.ReadBytes(4));
                if (trackChunk != "MTrk")
                    throw new Exception("Invalid MIDI file: Missing MTrk chunk");

                int trackLength = ReadBigEndianInt(reader);
                long trackEnd = reader.BaseStream.Position + trackLength;

                Console.WriteLine($"\nTrack {i + 1}:");
                string trackName = $"Track {i + 1}";
                double trackDuration = 0;
                double currentTimeInSeconds = 0;
                byte runningStatus = 0;

                while (reader.BaseStream.Position < trackEnd)
                {
                    int deltaTime = ReadVariableLengthValue(reader);
                    double deltaSeconds = (deltaTime / ticksPerQuarter) * (microsecondsPerQuarterNote / 1_000_000);
                    currentTimeInSeconds += deltaSeconds;

                    byte eventType = reader.ReadByte();
                    if ((eventType & 0x80) == 0) // Running status
                    {
                        eventType = runningStatus;
                        reader.BaseStream.Seek(-1, SeekOrigin.Current); // Move back one byte
                    }
                    else
                    {
                        runningStatus = eventType;
                    }
                    
                    
                    double eventDuration = currentTimeInSeconds - previousTimestamp; // Calculate duration


                    if (eventType == 0xFF) // Meta Event
                    {
                        byte metaType = reader.ReadByte();
                        int length = ReadVariableLengthValue(reader);
                        byte[] data = reader.ReadBytes(length);

                        if (metaType == 0x03) // Track Name
                        {
                            trackName = Encoding.ASCII.GetString(data);
                            Console.WriteLine($"Track Name: {trackName}");
                        }
                        else if (metaType == 0x51) // Set Tempo
                        {
                            microsecondsPerQuarterNote = (data[0] << 16) | (data[1] << 8) | data[2];
                            Console.WriteLine($"Set Tempo: {microsecondsPerQuarterNote} microseconds per quarter note");
                        }
                        else if (metaType == 0x2F) // End of Track
                        {
                            Console.WriteLine("End of Track");
                            break;
                        }
                    }
                    else if ((eventType & 0xF0) == 0xF0) // System Exclusive
                    {
                        int length = ReadVariableLengthValue(reader);
                        reader.ReadBytes(length);
                        Console.WriteLine($"Sysex Event: Length {length} bytes");
                    }
                    else // MIDI Channel Event
                    {
                        int dataLength = (eventType & 0xE0) == 0xC0 || (eventType & 0xE0) == 0xD0 ? 1 : 2;
                        byte[] eventData = reader.ReadBytes(dataLength);

                        byte channel = (byte)(eventType & 0x0F);

                        if ((eventType & 0xF0) == 0x90) // Note On
                        {
                            if (eventData[1] > 0)
                            {
                                midiEvents.Add(new MidiEvent()
                                {
                                    Channel = channel,
                                    Timestamp = currentTimeInSeconds,
                                    EventType = "NoteOn",
                                    Track = trackName,
                                    Velocity = eventData[1],
                                    Note = eventData[0],
                                    Duration = eventDuration // Set Duration
                                });
                                Console.WriteLine(
                                    $"Note On: Track {trackName}, Channel {channel + 1}, Note {eventData[0]}, Velocity {eventData[1]}");
                            }
                            else
                            {
                                midiEvents.Add(new MidiEvent()
                                {
                                    Channel = channel,
                                    Timestamp = currentTimeInSeconds,
                                    EventType = "NoteOff",
                                    Track = trackName,
                                    Velocity = eventData[1],
                                    Note = eventData[0],
                                    Duration = eventDuration // Set Duration
                                });
                                Console.WriteLine(
                                    $"Note Off (via Note On): Track {trackName}, Channel {channel + 1}, Note {eventData[0]}");
                            }
                        }
                        else if ((eventType & 0xF0) == 0x80) // Note Off
                        {
                            midiEvents.Add(new MidiEvent()
                            {
                                Channel = channel,
                                Timestamp = currentTimeInSeconds,
                                EventType = "NoteOff",
                                Track = trackName,
                                Velocity = eventData[1],
                                Note = eventData[0],
                                Duration = eventDuration // Set Duration
                            });
                            Console.WriteLine(
                                $"Note Off: Track {trackName}, Channel {channel + 1}, Note {eventData[0]}");
                        }
                    }
                    
                    previousTimestamp = currentTimeInSeconds;
                }

                Console.WriteLine($"Track Duration: {currentTimeInSeconds:F2} seconds");
                totalDuration = Math.Max(totalDuration, currentTimeInSeconds); // Keep the longest track duration
            }

            Console.WriteLine($"\nTotal Duration: {totalDuration / 60:F2} minutes");
        }

        return midiEvents;
    }

    private static int ReadBigEndianInt(BinaryReader reader)
    {
        byte[] bytes = reader.ReadBytes(4);
        return (bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3];
    }

    private static short ReadBigEndianShort(BinaryReader reader)
    {
        byte[] bytes = reader.ReadBytes(2);
        return (short)((bytes[0] << 8) | bytes[1]);
    }

    private static int ReadVariableLengthValue(BinaryReader reader)
    {
        int value = 0;
        byte nextByte;
        do
        {
            nextByte = reader.ReadByte();
            value = (value << 7) | (nextByte & 0x7F);
        } while ((nextByte & 0x80) != 0);

        return value;
    }
}