
using yuudachi.Chan;

namespace Tests;
public class Tests
{
    [Test]
    public async Task GetBoards()
    {
        var chan = new FourChanClient();
        var boards = await chan.GetBoards();

        Assert.That(boards, Has.Count.GreaterThan(0));
    }

    [Test]

    public async Task GetPagesOnCatalogBoard()
    {
        var chan = new FourChanClient();
        var pages = await chan.GetCatalog("po");
        Assert.That(pages, Is.Not.Null);
        Assert.That(pages, Has.Count.GreaterThan(0));
        var page = pages.FirstOrDefault();
        Assert.That(page, Is.Not.Null);
    }

    [Test]

    public async Task GetPagesOnBoard()
    {
        var chan = new FourChanClient();
        var pages = await chan.GetThreadsOnPage("po");
        Assert.That(pages, Is.Not.Null);
        Assert.That(pages, Has.Count.GreaterThan(0));
        var page = pages.FirstOrDefault();
        Assert.That(page, Is.Not.Null);
    }

    [Test]
    public async Task GetBoardsIndexPage()
    {
        var chan = new FourChanClient();
        var index = await chan.GetBoardIndexPage("po", 1);
        Assert.That(index, Is.Not.Null);
        Assert.That(index.Threads, Has.Count.GreaterThan(0));
        var page = index.Threads.FirstOrDefault();
        Assert.That(page, Is.Not.Null);
    }

    [Test]
    public async Task GetThread()
    {
        var threadId = 503089951;

        var chan = new FourChanClient();
        var threads = await chan.GetThreadsOnPage("pol");
        Assert.That(threads, Has.Count.GreaterThan(0));
        var item = threads.Single(x => x.Threads.SingleOrDefault(x => x.Number == threadId) != null).Threads.Single(x => x.Number == threadId);

        Assert.That(item, Is.Not.Null);

        var thread = await chan.TryGetThread("pol", item.Number);

        Assert.That(thread, Is.Not.Null);
    }
}
