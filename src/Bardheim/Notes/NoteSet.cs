namespace Bardheim.Notes;

public sealed class NoteSet
{
    public NoteSet(string name, NoteMap map)
    {
        Name = name;
        Map = map;
    }

    public string Name { get; }

    public NoteMap Map { get; }
}
