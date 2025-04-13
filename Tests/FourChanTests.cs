using yuudachi.Chan;

namespace Tests;

public class FourChanTests
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
        var chan = new FourChanClient();
        var threadsOnPage = await chan.GetThreadsOnPage("pol");
        Assert.That(threadsOnPage, Has.Count.GreaterThan(0));
        var threadDescriptor = threadsOnPage.First().ThreadDescriptors.First();

        Assert.That(threadDescriptor, Is.Not.Null);

        var thread = await chan.TryGetThread("pol", threadDescriptor.Number);

        Assert.That(thread, Is.Not.Null);
    }
}
