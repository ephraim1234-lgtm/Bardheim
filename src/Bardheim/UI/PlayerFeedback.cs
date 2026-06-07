using BepInEx.Logging;

namespace Bardheim.UI;

public sealed class PlayerFeedback
{
    private readonly ManualLogSource _logger;

    public PlayerFeedback(ManualLogSource logger)
    {
        _logger = logger;
    }

    public void Center(string message)
    {
        _logger.LogInfo(message);

        if (MessageHud.instance is not null)
        {
            MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, message);
        }
    }
}
