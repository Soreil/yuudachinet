namespace Tests;

using yuudachi.Chan;

public class Tests
{
    [Test]
    public async Task GetBoards()
    {
        var chan = new FourChan();
        var boards = await chan.GetBoards();

        Assert.That(boards, Has.Count.GreaterThan(0));
    }
}
