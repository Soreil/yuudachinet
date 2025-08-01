using Microsoft.Extensions.Logging.Abstractions;

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

        var client = new FourChanClient(logger);

        var sample = @"This is going to be a horrible thread https://boards.4chan.org/a/thread/281091707";
        var urls = await EmbedGeneratorHandler.TryGet4ChanURL(sample, client);
        Assert.That(urls, Has.Count.EqualTo(1));

        var sampleWithoutURL = @"Okay that's it
I'm going to make yuudachi generate actual embeds for 4chan I think
Fucking dogshit app
Why not include the OP picture??
";

        urls = await EmbedGeneratorHandler.TryGet4ChanURL(sampleWithoutURL, client);
        Assert.That(urls, Is.Null);
        var sampleWithMultipleUrls = @"https://boards.4chan.org/a/thread/281091707 https://boards.4chan.org/jp/thread/49779689";
        urls = await EmbedGeneratorHandler.TryGet4ChanURL(sampleWithMultipleUrls, client);
        Assert.That(urls, Has.Count.EqualTo(2));
    }
}
