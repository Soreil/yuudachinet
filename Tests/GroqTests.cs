using yuudachi.Groq;

namespace Tests;

public class GroqTests
{
    private const string Model = "deepseek-r1-distill-llama-70b";
    private static string Key => Environment.GetEnvironmentVariable("GROQ_API_KEY") ?? "ERROR";

    [SetUp]
    public void Setup()
    {
        if (Key == "ERROR")
        {
            Assert.Fail("GROQ_API_KEY environment variable not set");
        }
    }

    [Test]
    public async Task TestGroqGetModelInfo()
    {
        var client = new GroqClient(Key);
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var model = await client.TryGetModel(Model, cts.Token);
        Assert.That(model, Is.Not.Null);
        Assert.That(model.Id, Is.EqualTo(Model));
    }
    [Test]
    public async Task TestGroqGetModels()
    {
        var client = new GroqClient(Key);
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var models = await client.GetAllModels(cts.Token);
        Assert.That(models, Is.Not.Null);
        Assert.That(models.Data, Has.Count.Not.Zero);
    }

    [Test]
    public async Task TestGroqConversation()
    {
        var client = new GroqClient(Key);

        var convo = GroqClient.StartConversation(Model, "You are a helpful assistant. Answer the following question: What is the capital of France?");

        convo.AddMessage("The capital of France is Paris.");

        var res = await client.ConversationResult(convo);
        Assert.That(res, Is.Not.Null);
        Assert.That(res.Choices, Is.Not.Null);
        Assert.That(res.Choices, Is.Not.Empty);

        Assert.That(res.Choices[0].Message, Is.Not.Null);
        Assert.That(res.Choices[0].Message.Content, Has.Length.GreaterThan(0));
    }

    [Test]
    public async Task TestGroqConversationMultipleMessages()
    {
        var client = new GroqClient(Key);

        var convo = GroqClient.StartConversation(Model);
        convo.AddMessage("My favourite colour is blue");
        var promptResult = await client.ConversationResult(convo);
        var chosen = promptResult.Choices.First();
        convo.AddResponse(chosen.Message);

        convo.AddMessage("What is my favourite colour?");
        var questionResult = await client.ConversationResult(convo);
        var chosenquestionResult = questionResult.Choices.First();
        convo.AddResponse(chosenquestionResult.Message);

        Assert.That(questionResult, Is.Not.Null);
        Assert.That(questionResult.Choices, Is.Not.Null);
        Assert.That(questionResult.Choices, Is.Not.Empty);

        Assert.That(questionResult.Choices[0].Message, Is.Not.Null);
        Assert.That(questionResult.Choices[0].Message.Content, Contains.Substring("blue"));
    }

    [Test]
    public async Task TestGroqConversationMoreThanTwoMessages()
    {
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        var client = new GroqClient(Key);

        var convo = GroqClient.StartConversation(Model);
        convo.AddMessage("My favourite colour is blue");
        var firstprompt = await client.ConversationResult(convo, cts.Token);
        var chosen = firstprompt.Choices.First();
        convo.AddResponse(chosen.Message);

        convo.AddMessage("What is my favourite colour?");
        var FirstQuestion = await client.ConversationResult(convo, cts.Token);
        var chosen2 = FirstQuestion.Choices.First();
        convo.AddResponse(chosen2.Message);

        convo.AddMessage("banananananananna?");
        var SecondQuestion = await client.ConversationResult(convo, cts.Token);
        var chosen3 = SecondQuestion.Choices.First();
        convo.AddResponse(chosen3.Message);


        Assert.That(SecondQuestion, Is.Not.Null);
        Assert.That(SecondQuestion.Choices, Is.Not.Null);
        Assert.That(SecondQuestion.Choices, Is.Not.Empty);
        Assert.That(SecondQuestion.Choices[0].Message, Is.Not.Null);
        Assert.That(SecondQuestion.Choices[0].Message.Content, Has.Length.GreaterThan(0));
    }
}
