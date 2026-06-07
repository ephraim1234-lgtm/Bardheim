namespace Bardheim.PlayMode;

public sealed class LyrePlayMode
{
    public bool IsActive { get; private set; }

    public bool Toggle(bool canPlay)
    {
        if (!canPlay)
        {
            return DisableIfActive();
        }

        IsActive = !IsActive;
        return true;
    }

    public bool UpdateEligibility(bool canPlay)
    {
        return canPlay ? false : DisableIfActive();
    }

    public bool DisableIfActive()
    {
        if (!IsActive)
        {
            return false;
        }

        IsActive = false;
        return true;
    }
}
