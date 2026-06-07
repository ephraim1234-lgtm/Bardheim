namespace Bardheim.Notes;

public sealed class NoteDefinition
{
    public NoteDefinition(int slot, string name, float frequencyHz)
    {
        Slot = slot;
        Name = name;
        FrequencyHz = frequencyHz;
    }

    public int Slot { get; }

    public string Name { get; }

    public float FrequencyHz { get; }
}
