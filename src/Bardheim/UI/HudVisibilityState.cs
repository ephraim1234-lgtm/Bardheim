namespace Bardheim.UI;

public sealed class HudVisibilityState
{
    public bool IsHidden { get; private set; }

    public void Update(bool controlHeld, bool f3Pressed)
    {
        if (controlHeld && f3Pressed)
        {
            IsHidden = !IsHidden;
        }
    }
}
