// See https://aka.ms/new-console-template for more information

using MidiParser2;

string path = AppDomain.CurrentDomain.BaseDirectory + "/Samples/coldplay-clocks.mid";
if (File.Exists(path))
{
    try
    {
        var events = MidiParser.ParseMidiFile(path);
        var melodyEvents = events
            .Where(e => e.Track == "Track 3")
            .OrderBy(e => e.Timestamp)
            .ToList();

        Console.WriteLine("Playing MIDI events...");
        var player = new OpenALSoundPlayer();
        player.PlayMidiEvents(melodyEvents);
        Console.WriteLine("Playback finished.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
}
else
{
    Console.WriteLine("File not found.");
}