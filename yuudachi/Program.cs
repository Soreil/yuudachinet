using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using NetCord.Gateway;
using NetCord.Hosting.Gateway;
using NetCord.Hosting.Services;
using NetCord.Hosting.Services.ApplicationCommands;
using NetCord.Hosting.Services.ComponentInteractions;

using yuudachi;
using yuudachi.Chan;
using yuudachi.ChoiceProviders;
using yuudachi.Groq;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<GroqClientKey>(options => options.Key = builder.Configuration["GROQ_API_KEY"]
                  ?? throw new InvalidOperationException("Environment variable GROQ_API_KEY is not set."));

builder.Services.Configure<GroqSettingsOptions>(builder.Configuration.GetSection("GroqSettings"));

builder.Services
    .AddTransient<FourChanClient>()
    .AddTransient<FourChanBoardPicker>()
    .AddTransient<GroqClient>()
    .AddTransient<GroqModelPicker>()
    .AddSingleton<GroqConversationHistory>()
    .AddDiscordGateway(options => options.Intents =
                            GatewayIntents.GuildMessages
                          | GatewayIntents.DirectMessages
                          | GatewayIntents.MessageContent
                          | GatewayIntents.DirectMessageReactions
                          | GatewayIntents.GuildMessageReactions)
    .AddApplicationCommands()
    .AddComponentInteractions()
    .AddGatewayEventHandlers(typeof(Program).Assembly);

var host = builder.Build()
    .UseGatewayEventHandlers();


var client = host.Services.GetRequiredService<FourChanClient>();
FourChanBoardPicker.Chan = client;
var groqClient = host.Services.GetRequiredService<GroqClient>();
GroqModelPicker.Client = groqClient;

host.AddModules(typeof(Program).Assembly);

await host.RunAsync();
