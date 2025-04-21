using NetCord.Services.ApplicationCommands;
using NetCord.Rest;
using Microsoft.Extensions.Logging;
using NetCord;
using yuudachi.Radio;

namespace yuudachi.Commands;

[SlashCommand("radio", "r/a/dio tools")]
public class RadioCommandsModule : ApplicationCommandModule<ApplicationCommandContext>
{
    private RadioClient Radio { get; }
    public ILogger<RadioCommandsModule> Logger { get; }

    public RadioCommandsModule(ILogger<RadioCommandsModule> logger)
    {
        Radio = new RadioClient();
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
