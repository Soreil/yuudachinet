
namespace yuudachi.Groq;

public class Conversation
{
    public string Model { get; set; }
    public double Temperature { get; set; }

    private List<Message> Messages { get; set; } = [];

    public Conversation(string model, double startingTemperature = 0.6)
    {
        Model = model;
        Temperature = startingTemperature;
    }

    public GroqRequest ToGroqRequest(string reasoningFormat = "hidden")
    {
        return new GroqRequest(Messages, Model, Temperature, reasoningFormat);
    }

    public void AddResponse(Message message)
    {
        if (message.Role is not "assistant" and not "system")
            throw new ArgumentException("Response message must be of role 'assistant'");
        Messages.Add(message);
    }

    public void AddMessage(string message)
    {
        Messages.Add(Message.NewUserMessage(message));
    }

    /// <summary>
    /// Clears current conversation and sets the starting prompt
    /// </summary>
    /// <param name="message">Required to be a system prompt</param>
    internal void SetSystemMessage(Message message)
    {
        if (message.Role != "system")
            throw new ArgumentException("System message must be of role 'system'");
        Messages = [message];
    }
}