using System;
using System.Collections.Generic;

namespace Bardheim.Network;

public sealed class LyreRemoteNoteIntakeGate
{
    private const double RateWindowSeconds = 1.0;
    private readonly Dictionary<long, SenderState> _statesBySender = new();
    private readonly int _maxEventsPerSecond;

    public LyreRemoteNoteIntakeGate(int maxEventsPerSecond)
    {
        if (maxEventsPerSecond < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxEventsPerSecond), "Remote rate limit must allow at least one event per second.");
        }

        _maxEventsPerSecond = maxEventsPerSecond;
    }

    public bool TryAccept(
        long senderPeerId,
        LyreNetworkNoteEvent noteEvent,
        double nowSeconds,
        out LyreNetworkNoteRejectReason reason)
    {
        var state = GetSenderState(senderPeerId);
        if (state.SeenEventIds.Contains(noteEvent.EventId))
        {
            reason = LyreNetworkNoteRejectReason.Duplicate;
            return false;
        }

        PruneOldEvents(state, nowSeconds);
        if (state.RecentEventTimes.Count >= _maxEventsPerSecond)
        {
            reason = LyreNetworkNoteRejectReason.RateLimited;
            return false;
        }

        state.SeenEventIds.Add(noteEvent.EventId);
        state.RecentEventTimes.Enqueue(nowSeconds);
        reason = LyreNetworkNoteRejectReason.None;
        return true;
    }

    private SenderState GetSenderState(long senderPeerId)
    {
        if (_statesBySender.TryGetValue(senderPeerId, out var state))
        {
            return state;
        }

        state = new SenderState();
        _statesBySender.Add(senderPeerId, state);
        return state;
    }

    private static void PruneOldEvents(SenderState state, double nowSeconds)
    {
        while (state.RecentEventTimes.Count > 0 &&
               state.RecentEventTimes.Peek() <= nowSeconds - RateWindowSeconds)
        {
            state.RecentEventTimes.Dequeue();
        }
    }

    private sealed class SenderState
    {
        public HashSet<long> SeenEventIds { get; } = new();

        public Queue<double> RecentEventTimes { get; } = new();
    }
}
