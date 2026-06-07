namespace Bardheim.Instruments;

public sealed class DrumHitDefinition
{
    public DrumHitDefinition(int slot, string id)
    {
        Slot = slot;
        Id = id;
    }

    public int Slot { get; }

    public string Id { get; }
}
