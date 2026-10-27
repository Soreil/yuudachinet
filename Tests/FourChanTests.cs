using Microsoft.Extensions.Logging.Abstractions;

using System.Net;

using yuudachi;
using yuudachi.Chan;
using yuudachi.Commands;

namespace Tests;

public class FourChanTests
{
    [Test]
    public async Task GetBoards()
    {
        var logger = NullLogger<FourChanClient>.Instance;

        var chan = new FourChanClient(logger);
        var boards = await chan.GetBoards();

        Assert.That(boards, Has.Count.GreaterThan(0));
    }

    [Test]
    public async Task GetPagesOnCatalogBoard()
    {
        var logger = NullLogger<FourChanClient>.Instance;

        var chan = new FourChanClient(logger);
        var pages = await chan.GetCatalog("po");
        Assert.That(pages, Is.Not.Null);
        Assert.That(pages, Has.Count.GreaterThan(0));
        var page = pages.FirstOrDefault();
        Assert.That(page, Is.Not.Null);
    }

    [Test]
    public async Task GetPagesOnBoard()
    {
        var logger = NullLogger<FourChanClient>.Instance;

        var chan = new FourChanClient(logger);
        var pages = await chan.GetThreadsOnPage("po");
        Assert.That(pages, Is.Not.Null);
        Assert.That(pages, Has.Count.GreaterThan(0));
        var page = pages.FirstOrDefault();
        Assert.That(page, Is.Not.Null);
    }

    [Test]
    public async Task GetBoardsIndexPage()
    {
        var logger = NullLogger<FourChanClient>.Instance;

        var chan = new FourChanClient(logger);
        var index = await chan.GetBoardIndexPage("po", 1);
        Assert.That(index, Is.Not.Null);
        Assert.That(index.Threads, Has.Count.GreaterThan(0));
        var page = index.Threads.FirstOrDefault();
        Assert.That(page, Is.Not.Null);
    }

    [Test]
    public async Task GetThread()
    {
        var logger = NullLogger<FourChanClient>.Instance;

        var chan = new FourChanClient(logger);
        var threadsOnPage = await chan.GetThreadsOnPage("pol");
        Assert.That(threadsOnPage, Has.Count.GreaterThan(0));
        var threadDescriptor = threadsOnPage.First().ThreadDescriptors.First();

        Assert.That(threadDescriptor, Is.Not.Null);

        var thread = await chan.TryGetThread("pol", threadDescriptor.Number);

        Assert.That(thread, Is.Not.Null);
    }

    [Test]
    [TestCase(@"Spring is here.<br><br>Useful links:<br>- https://www.google.com/maps/d/u/0/v<wbr>iewer?mid=1m0KiLcbTwbcg_WyN_eH4aye2<wbr>fTo<br>- https://kigguide.com/ (biased)<br>- https://kigudb.info/ (under construction)<br>Nsfw:<br>- Art: https://www.pixiv.net/search.php?s_<wbr>mode=s_tag&word=%E7%9D%80%E3%81%90%<wbr>E3%82%8B%E3%81%BF<br>- Videos: https://www.findtubes.com/search/ki<wbr>gurumi<br>- Stick this in japanese sites: 着ぐるみ<br>Discords:<br>- K.I.G: https://discord.gg/kigurumi<br><br>Image Source: https://twitter.com/rain07935516/st<wbr>atus/1909953569274630531<br><br>Previous Thread: <a href=""/jp/thread/49130638#p49130638"" class=""quotelink"">>>49130638</a>")]
    [TestCase(@"?: &gt;&gt;49288936 Cornucopia of Resources: google.comDJT is a thread for learners of the japanese language to discuss the learning process and post about their recent (on topic) experiences.Guide: do things in the language to get better at those thingsnote any guide posted itt likely contains donation links affiliated to the poster")]
    [TestCase(@"A friend recommended that I get a cast iron pan. She told me they are very useful for cooking. My goal was that I wanted to start cooking more home cooked meals for myself instead of making dumb stuff like sandwiches all the time I guess. What are the advantages of cast iron and how do you use them? In what ways have you yourself used them and were they particularly useful or is it more of a whatever? Like what is cast iron doing that other pans don&#039;t etc?")]
    public void DecodeMessageText(string text)
    {
        text = FourChanClient.CleanUpText(text);

        Assert.That(text, Does.Not.Contain(@"<br>"));
        Assert.That(text, Does.Not.Contain(@"<br/>"));
        Assert.That(text, Does.Not.Contain(@"<wbr>"));
        Assert.That(text, Does.Not.Contain(@"<wbr/>"));
        Assert.That(text, Does.Not.Contain(@"&gt;"));
        Assert.That(text, Does.Not.Contain(@"&#039;"));

    }

    [Test]
    public async Task ParseThreadUrl()
    {
        var logger = NullLogger<FourChanClient>.Instance;

        using var handler = new ThreadResponseHandler();
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://a.4cdn.org/") };
        var client = new FourChanClient(logger, httpClient);

        var sample = @"This is going to be a horrible thread https://boards.4chan.org/a/thread/281091707";
        var urls = await EmbedGeneratorHandler.TryGet4ChanURL(sample, client);
        Assert.That(urls, Is.Not.Null);
        Assert.That(urls, Has.Count.EqualTo(1));
        Assert.That(urls[0].Item2, Is.EqualTo("a"));
        Assert.That(urls[0].Item1.Posts[0].Number, Is.EqualTo(281091707));

        var sampleWithoutURL = @"Okay that's it
I'm going to make yuudachi generate actual embeds for 4chan I think
Fucking dogshit app
Why not include the OP picture??
";

        urls = await EmbedGeneratorHandler.TryGet4ChanURL(sampleWithoutURL, client);
        Assert.That(urls, Is.Null);
        Assert.That(handler.Requests, Has.Count.EqualTo(1));
        var sampleWithMultipleUrls = @"https://boards.4chan.org/a/thread/281091707 https://boards.4chan.org/jp/thread/49779689";
        urls = await EmbedGeneratorHandler.TryGet4ChanURL(sampleWithMultipleUrls, client);
        Assert.That(urls, Is.Not.Null);
        Assert.That(urls, Has.Count.EqualTo(2));
        Assert.That(urls.Select(url => url.Item2), Is.EqualTo(new[] { "a", "jp" }));
        Assert.That(urls.Select(url => url.Item1.Posts[0].Number), Is.EqualTo([281091707, 49779689]));
        Assert.That(handler.Requests, Is.EqualTo(
        [
            "/a/thread/281091707.json",
            "/a/thread/281091707.json",
            "/jp/thread/49779689.json"
        ]));
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task ParseUnavailableThreadUrl(bool includeAvailableThread)
    {
        using var handler = new ThreadResponseHandler();
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://a.4cdn.org/") };
        var client = new FourChanClient(NullLogger<FourChanClient>.Instance, httpClient);
        var sample = "https://boards.4chan.org/a/thread/1";
        if (includeAvailableThread)
        {
            sample += " https://boards.4chan.org/jp/thread/49779689";
        }

        var urls = await EmbedGeneratorHandler.TryGet4ChanURL(sample, client);

        if (includeAvailableThread)
        {
            Assert.That(urls, Is.Not.Null);
            Assert.That(urls, Has.Count.EqualTo(1));
            Assert.That(urls[0].Item2, Is.EqualTo("jp"));
            Assert.That(urls[0].Item1.Posts[0].Number, Is.EqualTo(49779689));
        }
        else
        {
            Assert.That(urls, Is.Null);
        }
        Assert.That(handler.Requests, Has.Count.EqualTo(includeAvailableThread ? 2 : 1));
    }

    private sealed class ThreadResponseHandler : HttpMessageHandler
    {
        public List<string> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = (request.RequestUri ?? throw new InvalidOperationException("Missing request URI.")).AbsolutePath;
            Requests.Add(path);
            var response = path switch
            {
                "/a/thread/281091707.json" => new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"posts\":[{\"no\":281091707}]}")
                },
                "/jp/thread/49779689.json" => new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"posts\":[{\"no\":49779689}]}")
                },
                "/a/thread/1.json" => new HttpResponseMessage(HttpStatusCode.NotFound),
                _ => throw new InvalidOperationException($"Unexpected request: {request.RequestUri}")
            };
            return Task.FromResult(response);
        }
    }
}
