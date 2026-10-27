using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using NetCord.Gateway;
using NetCord.Hosting.Gateway;
using NetCord.Hosting.Services;
using NetCord.Hosting.Services.ApplicationCommands;
using NetCord.Hosting.Services.ComponentInteractions;

using yuudachi;
using yuudachi.Chan;
using yuudachi.ChoiceProviders;
using yuudachi.Commands;
using yuudachi.Deepseek;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddJsonFile("usersettings.json", optional: true);
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<DeepseekSettingsOptions>(optional: true);
}
builder.Configuration.AddEnvironmentVariables().AddCommandLine(args);

builder.Services.AddOptions<DeepseekSettingsOptions>()
    .Bind(builder.Configuration.GetSection(DeepseekSettingsOptions.DeepseekSettings))
    .Validate(options => !string.IsNullOrWhiteSpace(options.ModelName), "Deepseek:ModelName must be configured.")
    .Validate(options => Path.IsPathFullyQualified(options.UsageFilePath), "Deepseek:UsageFilePath must be an absolute path.")
    .ValidateOnStart();

builder.Services.Configure<YoutubeClientKey>(options => options.Key = builder.Configuration["YOUTUBE_API_KEY"]
?? throw new InvalidOperationException("Environment variable YOUTUBE_API_KEY is not set."));

builder.Services.AddOptions<DeepSeekClientKey>()
    .Configure(options => options.Key = builder.Configuration["DEEPSEEK_API_KEY"] ?? string.Empty)
    .Validate(options => !string.IsNullOrWhiteSpace(options.Key), "DEEPSEEK_API_KEY must be configured.")
    .ValidateOnStart();

builder.Services.AddChatClient(services =>
{
    var apiKey = services
        .GetRequiredService<IOptions<DeepSeekClientKey>>()
        .Value.Key;

    var settings = services.GetRequiredService<IOptions<DeepseekSettingsOptions>>().Value;

    return new OpenAI.Chat.ChatClient(settings.ModelName, new System.ClientModel.ApiKeyCredential(apiKey),
        new OpenAI.OpenAIClientOptions { Endpoint = new Uri("https://api.deepseek.com") })
        .AsIChatClient();
})
.UseLogging();

builder.Services
    .AddTransient<FourChanClient>()
    .AddTransient<FourChanBoardPicker>()
    .AddSingleton<YoutubeResponses>()
    .AddTransient<DeepSeekClient>()
    .AddSingleton<ConversationHistory>()
    .AddSingleton<DeepSeekUsageStore>()
    .AddSingleton(services => new DeepSeekSearchClient(new HttpClient
    {
        BaseAddress = new Uri("https://api.deepseek.com"),
        Timeout = TimeSpan.FromSeconds(30),
        MaxResponseContentBufferSize = 1024 * 1024,
        DefaultRequestHeaders =
        {
            Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer",
                services.GetRequiredService<IOptions<DeepSeekClientKey>>().Value.Key)
        }
    }, services.GetRequiredService<DeepSeekUsageStore>()))
    .AddDiscordGateway(options => options.Intents =
                            GatewayIntents.GuildMessages
                          | GatewayIntents.DirectMessages
                          | GatewayIntents.MessageContent
                          | GatewayIntents.DirectMessageReactions
                          | GatewayIntents.GuildMessageReactions
                          | GatewayIntents.Guilds)
    .AddApplicationCommands()
    .AddComponentInteractions()
    .AddGatewayHandlers(typeof(Program).Assembly);

var host = builder.Build();

var client = host.Services.GetRequiredService<FourChanClient>();
FourChanBoardPicker.Chan = client;

host.AddModules(typeof(Program).Assembly);

await host.RunAsync();
