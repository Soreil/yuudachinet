namespace yuudachi;

public class GroqSettingsOptions
{
    public const string GroqSettings = "GroqSettings";
    public string DefaultModelName { get; set; } = string.Empty;
    public string DefaultToolModelName { get; set; } = string.Empty;
}