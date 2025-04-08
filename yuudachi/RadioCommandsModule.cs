using NetCord.Services.ApplicationCommands;
using NetCord.Rest;
using Microsoft.Extensions.Logging;

namespace yuudachi;

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
    public async Task NowPlaying()
    {
        try
        {
            var embed = await Radio.GetCurrentAsEmbed() ?? throw new Exception("No current state");

            var msg = new InteractionMessageProperties() { Embeds = [embed] };

            var callback = InteractionCallback.Message(msg);
            await RespondAsync(callback);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Error getting current state");
            var msg = InteractionCallback.Message(new InteractionMessageProperties() { Content = $"Error: {e.Message}" });
            await RespondAsync(msg);
        }
    }
}
