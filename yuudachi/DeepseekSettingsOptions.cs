namespace yuudachi;

public class DeepseekSettingsOptions
{
    public const string DeepseekSettings = "Deepseek";
    public string ModelName { get; set; } = string.Empty;

    /// <summary>Persistent counters outside release folders. Override with an absolute path writable by the service account.</summary>
    public string UsageFilePath { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.DoNotVerify),
        "yuudachi", "deepseek-usage.json");
}