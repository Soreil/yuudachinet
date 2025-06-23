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
using yuudachi.Groq;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<GroqClientKey>(options => options.Key = builder.Configuration["GROQ_API_KEY"]
                  ?? throw new InvalidOperationException("Environment variable GROQ_API_KEY is not set."));

builder.Services.Configure<GroqSettingsOptions>(builder.Configuration.GetSection("GroqSettings"));

builder.Services.Configure<GroqToolModels>(builder.Configuration.GetSection("GroqToolModels"));

builder.Services.Configure<YoutubeClientKey>(options => options.Key = builder.Configuration["YOUTUBE_API_KEY"]
?? throw new InvalidOperationException("Environment variable YOUTUBE_API_KEY is not set."));

builder.Services
    .AddTransient<FourChanClient>()
    .AddTransient<FourChanBoardPicker>()
    .AddTransient<GroqClient>()
    .AddTransient<GroqModelPicker>()
    .AddTransient<GroqToolModelPicker>()
    .AddSingleton<GroqConversationHistory>()
    .AddSingleton<YoutubeResponses>()
    .AddDiscordGateway(options => options.Intents =
                            GatewayIntents.GuildMessages
                          | GatewayIntents.DirectMessages
                          | GatewayIntents.MessageContent
                          | GatewayIntents.DirectMessageReactions
                          | GatewayIntents.GuildMessageReactions
                          | GatewayIntents.Guilds)
    .AddApplicationCommands()
    .AddComponentInteractions()
    .AddGatewayEventHandlers(typeof(Program).Assembly);

var host = builder.Build()
    .UseGatewayEventHandlers();


var client = host.Services.GetRequiredService<FourChanClient>();
FourChanBoardPicker.Chan = client;
var groqClient = host.Services.GetRequiredService<GroqClient>();
GroqModelPicker.Client = groqClient;
GroqToolModelPicker.Client = groqClient;
GroqToolModelPicker.Options = host.Services.GetService<IOptions<GroqToolModels>>()
    ?? throw new InvalidOperationException("GroqToolModels options are not configured.");

host.AddModules(typeof(Program).Assembly);

await host.RunAsync();
