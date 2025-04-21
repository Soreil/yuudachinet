using NetCord.Services.ApplicationCommands;

namespace yuudachi.Commands;

public class MoonCommmandsModule : ApplicationCommandModule<ApplicationCommandContext>
{
    [SlashCommand("moon", "Gets the current moon phase")]
    public static string GetMoonEmoji() => Moon.Moon.MoonPhase();
}
