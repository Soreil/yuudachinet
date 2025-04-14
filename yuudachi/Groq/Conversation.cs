namespace yuudachi.Groq;

public class Conversation
{
    public string Model { get; set; }
    public double Temperature { get; set; }

    public List<Message> Messages { get; set; } = [];

    public Conversation(string model, double startingTemperature = 0.6)
    {
        Model = model;
        Temperature = startingTemperature;
    }
}