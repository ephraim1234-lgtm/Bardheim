using Bardheim.UI;
using Xunit;

namespace Bardheim.Tests;

public sealed class HudVisibilityStateTests
{
    [Fact]
    public void CtrlF3TogglesHudHiddenState()
    {
        var state = new HudVisibilityState();

        Assert.False(state.IsHidden);

        state.Update(controlHeld: true, f3Pressed: true);

        Assert.True(state.IsHidden);

        state.Update(controlHeld: true, f3Pressed: true);

        Assert.False(state.IsHidden);
    }

    [Fact]
    public void F3WithoutControlDoesNotToggleHudHiddenState()
    {
        var state = new HudVisibilityState();

        state.Update(controlHeld: false, f3Pressed: true);

        Assert.False(state.IsHidden);
    }
}
