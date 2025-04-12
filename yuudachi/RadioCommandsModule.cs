using NetCord.Services.ApplicationCommands;
using NetCord.Rest;
using Microsoft.Extensions.Logging;
using NetCord;
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

[SlashCommand("radio", "r/a/dio tools")]
public class RadioCommandsModule : ApplicationCommandModule<ApplicationCommandContext>
{
    private Radio Radio { get; }
    public ILogger<RadioCommandsModule> Logger { get; }

    public RadioCommandsModule(ILogger<RadioCommandsModule> logger)
    {
        Radio = new Radio();
        Logger = logger;
    }



    [SubSlashCommand("np", "Now Playing")]
    public async Task<InteractionMessageProperties> NowPlaying()
    {
        try
        {
            var embed = await Radio.GetCurrentAsEmbed() ?? throw new Exception("No current state");

            var msg = new InteractionMessageProperties()
            {
                Embeds = [embed],
                Components = [new ActionRowProperties(
                    [
                    new ButtonProperties("buttondf",
                    new EmojiProperties("⬅️"),
                    ButtonStyle.Primary)

                    ])
                ]
            };

            return msg;
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Error getting current state");
            return new InteractionMessageProperties()
            {
                Content = $"Error: {e.Message}"
            };
        }
    }
}
