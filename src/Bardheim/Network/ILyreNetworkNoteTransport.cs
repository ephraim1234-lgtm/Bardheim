namespace Bardheim.Network;

public interface ILyreNetworkNoteTransport
{
    void Send(byte[] payload);
}
