using System;
using System.Collections.Generic;
using Bardheim.Instruments;

namespace Bardheim.Network;

public static class LyreNetworkNoteValidator
{
    public static bool TryValidate(
        LyreNetworkNoteEvent noteEvent,
        long senderPeerId,
        long localPeerId,
        ISet<string> validNoteNames,
        out LyreNetworkNoteRejectReason reason)
    {
        var validEventNamesByInstrument = new Dictionary<string, ISet<string>>(StringComparer.Ordinal)
        {
            [InstrumentCatalog.LyreId] = validNoteNames
        };

        return TryValidate(noteEvent, senderPeerId, localPeerId, validEventNamesByInstrument, out reason);
    }

    public static bool TryValidate(
        LyreNetworkNoteEvent noteEvent,
        long senderPeerId,
        long localPeerId,
        IReadOnlyDictionary<string, ISet<string>> validEventNamesByInstrument,
        out LyreNetworkNoteRejectReason reason)
    {
        if (senderPeerId == localPeerId)
        {
            reason = LyreNetworkNoteRejectReason.SelfOriginated;
            return false;
        }

        if (validEventNamesByInstrument is null ||
            !validEventNamesByInstrument.TryGetValue(noteEvent.InstrumentId, out var validEventNames) ||
            validEventNames is null ||
            !validEventNames.Contains(noteEvent.NoteName))
        {
            reason = LyreNetworkNoteRejectReason.UnknownNote;
            return false;
        }

        if (noteEvent.EventId <= 0 ||
            noteEvent.Volume < 0.0f ||
            float.IsNaN(noteEvent.Volume) ||
            float.IsInfinity(noteEvent.Volume) ||
            !Enum.IsDefined(typeof(LyreNetworkNoteSource), noteEvent.Source))
        {
            reason = LyreNetworkNoteRejectReason.InvalidPayload;
            return false;
        }

        reason = LyreNetworkNoteRejectReason.None;
        return true;
    }
}
