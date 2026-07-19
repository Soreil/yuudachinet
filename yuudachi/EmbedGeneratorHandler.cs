using Microsoft.Extensions.Logging;

using NetCord;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;
using NetCord.Rest;

using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

using yuudachi.Chan;
using yuudachi.Chan.DTO;
using yuudachi.Commands;

namespace yuudachi;

public partial class EmbedGeneratorHandler(ILogger<MessageCreateHandler> logger, FourChanClient fourChanClient) : IMessageCreateGatewayHandler
{
    public ILogger<MessageCreateHandler> Logger { get; } = logger;
    public FourChanClient FourChanClient { get; } = fourChanClient;

    public async ValueTask HandleAsync(NetCord.Gateway.Message message)
    {
        if (message.Channel is null) return;

        var threads = await TryGet4ChanURL(message.Content, FourChanClient);
        if (threads is null) return;

        foreach (var (thread,board) in threads)
        {
            var props = FourChanClient.MakeThreadEmbedStructure<MessageProperties>(thread, board);
            await message.Channel.SendMessageAsync(props);
        }

        return;
    }

    public static async Task<List<(ThreadRepliesDTO, string)>?> TryGet4ChanURL(string content, FourChanClient client)
    {
        var matches = ChanUrlRegex().Matches(content);

        List<(ThreadRepliesDTO, string)> dtos = [];

        foreach (var urlText in matches.Cast<Match>())
        {
            if (!urlText.Success)
            {
                return null;
            }

            foreach (Capture match in urlText.Captures.Cast<Capture>())
            {
                if (match is not null && Uri.TryCreate(match.Value, UriKind.Absolute, out var possibleUrl))
                {
                    var board = urlText.Groups[1].Value;
                    var threadNumber = long.Parse(urlText.Groups[2].Value);
                    var threadDTO = await client.TryGetThread(board, threadNumber);

                    if (threadDTO is not null)
                    {
                        dtos.Add((threadDTO, board));
                    }
                    else
                    {
                        // Log or handle the case where the thread could not be retrieved
                        Console.WriteLine($"Could not retrieve thread for URL: {match.Value}");
                    }
                }
            }

        }
        return dtos.Count > 0 ? dtos : null;
    }

    [GeneratedRegex(@"https?:\/\/boards\.4chan\.org\/(\w+)\/thread\/(\d+)", RegexOptions.Multiline)]
    private static partial Regex ChanUrlRegex();
}
