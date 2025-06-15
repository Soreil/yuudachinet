
using Microsoft.Extensions.Logging;

using NetCord.Gateway;
using NetCord.Services.ApplicationCommands;

using System.Runtime.InteropServices;

namespace yuudachi;

public partial class BirdCommandsModule : ApplicationCommandModule<ApplicationCommandContext>
{
    static readonly List<string> birds = [
"🦃",
"🐔",
"🐓",
"🐣",
"🐤",
"🐥",
"🐦",
"🐧",
"🕊️",
"🦅",
"🦆",
"🦢",
"🦉",
"🦤",
"🪶" ,
"🦩",
"🦚",
"🦜",
"🪽" ,
"🐦‍",
"🪿",
"🐦‍🔥"];
    private static int noGuildIndex = 0; // Static index for cycling through the static bird list

    private static readonly Dictionary<Guild, int> guildBirdListIndex = [];


    private static readonly Dictionary<Guild, List<string>> guildBirds = [];

    public BirdCommandsModule(ILogger<BirdCommandsModule> logger)
    {
        Logger = logger;
        Random.Shared.Shuffle(CollectionsMarshal.AsSpan(birds));

    }

    [SlashCommand("birb", "Gets you a cool random bird!")]
    public async Task<string> Post()
    {
        string bird;

        if (Context.Guild is not null)
        {
            if (guildBirds.TryGetValue(Context.Guild, out var birdOptions))
            {
                var index = guildBirdListIndex[Context.Guild];
                bird = birdOptions[index];
                guildBirdListIndex[Context.Guild] = (index + 1) % birdOptions.Count; // Cycle through the list
            }
            else
            {
                var emojis = await Context.Guild.GetEmojisAsync();

                var birdEmotes = emojis.Where(x => x.Name.Contains("birb", StringComparison.InvariantCultureIgnoreCase) || x.Name.Contains("bird", StringComparison.InvariantCultureIgnoreCase))
                    .Select(x => x.RequireColons is bool b && b ? $"<:{x.Name}>" : x.Name)
                    .ToList();

                birdEmotes.AddRange(birds); // Add the static bird emojis to the list
                Random.Shared.Shuffle(CollectionsMarshal.AsSpan(birdEmotes));
                guildBirds[Context.Guild] = birdEmotes;
                guildBirdListIndex[Context.Guild] = 1; // Reset the index for the new list

                bird = birdEmotes.FirstOrDefault() ?? "No birds found!";
            }
        }
        else
        {
            // If not in a guild, use the static list of birds
            bird = birds[noGuildIndex];
            noGuildIndex = (noGuildIndex + 1) % birds.Count; // Cycle through the static list
        }


        return bird;
    }

    public ILogger<BirdCommandsModule> Logger { get; }
}
