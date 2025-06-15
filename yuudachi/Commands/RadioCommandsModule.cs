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

    [SubSlashCommand("queue", "Queue of upcoming songs")]
    public async Task<InteractionMessageProperties> Queue()
    {
        try
        {
            var current = await Radio.GetUpcomingAsEmbed() ?? throw new Exception("No current state");
            var msg = new InteractionMessageProperties()
            {
                Embeds = [current],
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

    [SubSlashCommand("np", "Now Playing")]
    public async Task<InteractionMessageProperties> NowPlaying()
    {
        try
        {
            var embed = await Radio.GetCurrentAsEmbed() ?? throw new Exception("No current state");

            var msg = new InteractionMessageProperties()
            {
                Embeds = [embed],
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
