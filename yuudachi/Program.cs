using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using NetCord.Gateway;
using NetCord.Hosting.Gateway;
using NetCord.Hosting.Services;
using NetCord.Hosting.Services.ApplicationCommands;
using NetCord.Hosting.Services.ComponentInteractions;

using yuudachi;
using yuudachi.Chan;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddTransient<FourChanClient>()
    .AddTransient<FourChanBoardPicker>()
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

host.AddModules(typeof(Program).Assembly);

await host.RunAsync();
