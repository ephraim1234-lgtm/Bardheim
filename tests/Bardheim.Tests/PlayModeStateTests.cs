using Bardheim.PlayMode;
using Xunit;

namespace Bardheim.Tests;

public sealed class PlayModeStateTests
{
    [Fact]
    public void ToggleDoesNotEnterPlayModeWhenLyreIsNotEquipped()
    {
        var playMode = new LyrePlayMode();

        var changed = playMode.Toggle(canPlay: false);

        Assert.False(changed);
        Assert.False(playMode.IsActive);
    }

    [Fact]
    public void ToggleEntersAndExitsPlayModeWhenLyreIsEquipped()
    {
        var playMode = new LyrePlayMode();

        Assert.True(playMode.Toggle(canPlay: true));
        Assert.True(playMode.IsActive);

        Assert.True(playMode.Toggle(canPlay: true));
        Assert.False(playMode.IsActive);
    }

    [Fact]
    public void InvalidEligibilityDisablesActivePlayMode()
    {
        var playMode = new LyrePlayMode();
        playMode.Toggle(canPlay: true);

        var changed = playMode.UpdateEligibility(canPlay: false);

        Assert.True(changed);
        Assert.False(playMode.IsActive);
    }
}
