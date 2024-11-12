namespace MidiParser2;

public class MidiEvent
{
    public string EventType { get; set; }
    public string Track { get; set; }
    public double Timestamp { get; set; }
    public int Channel { get; set; }
    public int Velocity { get; set; }     // Volume (0–127)
    public int Note { get; set; }         // MIDI note number (0–127)
    public double Duration { get; set; }  // New property
}