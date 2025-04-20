namespace yuudachi.Groq;

public class GroqClientKey
{
    public string Key { get; set; } = string.Empty;

    public override string ToString()
    {
        return Key;
    }
}