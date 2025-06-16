using NetCord.Rest;

using System.Diagnostics.CodeAnalysis;

namespace yuudachi.Commands;

public class YoutubeResponses
{
    public List<YoutubeResponse> Responses { get; set; } = [];

    internal bool TryGetContext(ulong id, [NotNullWhen(true)] out YoutubeResponse? response)
    {
        var resp = Responses.SingleOrDefault(x => x.response.Resource.Message.Id == id);
        if (resp is not null)
        {
            response = resp;
            return true;
        }

        response = null;
        return false;
    }

    public record YoutubeResponse(InteractionCallbackResponse response, YouTubeSearchResultBrowser browser);
}
