namespace yuudachi.Commands;

public class YouTubeSearchResultBrowser
{
    private readonly List<string> videoStrings;
    private int index = 0;

    public YouTubeSearchResultBrowser(List<string> videoStrings)
    {
        this.videoStrings = videoStrings;
    }

    public string GetCurrentVideo()
    {
        if (videoStrings.Count == 0)
            return "No videos available.";
        return videoStrings[index];
    }
    public string GetNextVideo()
    {
        if (videoStrings.Count == 0)
            return "No videos available.";
        index = (index + 1) % videoStrings.Count;
        return GetCurrentVideo();
    }
    public string GetPreviousVideo()
    {
        if (videoStrings.Count == 0)
            return "No videos available.";
        index = (index - 1 + videoStrings.Count) % videoStrings.Count;
        return GetCurrentVideo();
    }
}
