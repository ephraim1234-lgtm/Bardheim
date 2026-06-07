namespace Bardheim.Network;

public readonly struct LyreNetworkPosition
{
    public static readonly LyreNetworkPosition Zero = new(0.0f, 0.0f, 0.0f);

    public LyreNetworkPosition(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public float X { get; }

    public float Y { get; }

    public float Z { get; }
}
