using Microsoft.Extensions.Logging;

using NetCord;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;

namespace yuudachi;

public class ButtonPresses : ComponentInteractionModule<ComponentInteractionContext>
{
    public ButtonPresses(ILogger<ButtonPresses> logger)
    {
        Logger = logger;
    }

    public ILogger<ButtonPresses> Logger { get; }

    [ComponentInteraction("buttondf")]
    public static string HandleButtonPress()
    {
        return "test";
    }
}
