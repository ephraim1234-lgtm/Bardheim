namespace Bardheim.Network;

public enum LyreNetworkNoteRejectReason
{
    None,
    SelfOriginated,
    UnknownNote,
    InvalidPayload,
    Duplicate,
    RateLimited
}
