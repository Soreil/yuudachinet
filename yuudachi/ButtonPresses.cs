using Microsoft.Extensions.Logging;

using NetCord;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;

using yuudachi.Commands;

namespace yuudachi;

public class ButtonPresses : ComponentInteractionModule<ComponentInteractionContext>
{
    public ButtonPresses(ILogger<ButtonPresses> logger, YoutubeResponses youtubeResponses)
    {
        Logger = logger;
        YoutubeResponses = youtubeResponses;
    }

    public ILogger<ButtonPresses> Logger { get; }
    public YoutubeResponses YoutubeResponses { get; }

    [ComponentInteraction("youtubeNext")]
    public async Task HandleYoutubeNext()
    {
        if (Context.Interaction is MessageComponentInteraction m)
        {
            var thing = Context.Interaction;
            if (YoutubeResponses.TryGetContext(m.Message.Id, out var ctx))
            {
                var vid = ctx.browser.GetNextVideo();
                var msg = InteractionCallback.ModifyMessage(x => x.WithContent(vid));
                await RespondAsync(msg);
            }
        }
    }

    [ComponentInteraction("youtubePrevious")]
    public async Task HandleYoutubePrevious()
    {
        if (Context.Interaction is MessageComponentInteraction m)
        {
            var thing = Context.Interaction;
            if (YoutubeResponses.TryGetContext(m.Message.Id, out var ctx))
            {
                var vid = ctx.browser.GetPreviousVideo();
                var msg = InteractionCallback.ModifyMessage(x => x.WithContent(vid));
                await RespondAsync(msg);
            }
        }
    }
}
