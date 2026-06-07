namespace Bardheim.Songs;

public sealed class MidiNoteEvent
{
    public MidiNoteEvent(int midiNote, int channel, double startSeconds, double durationSeconds, int velocity)
    {
        MidiNote = midiNote;
        Channel = channel;
        StartSeconds = startSeconds;
        DurationSeconds = durationSeconds;
        Velocity = velocity;
    }

    public int MidiNote { get; }

    public int Channel { get; }

    public double StartSeconds { get; }

    public double DurationSeconds { get; }

    public int Velocity { get; }
}
