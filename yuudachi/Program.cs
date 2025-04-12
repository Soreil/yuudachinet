//ViI8ZVPvuxxVlOUIwisZjzmVLuGw_IOg
using Microsoft.Extensions.Hosting;

using NetCord.Gateway;
using NetCord.Hosting.Gateway;
using NetCord.Hosting.Services;
using NetCord.Hosting.Services.ApplicationCommands;
using NetCord.Hosting.Services.ComponentInteractions;

using System.Reflection;


var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddDiscordGateway(options => options.Intents = GatewayIntents.GuildMessages
                          | GatewayIntents.DirectMessages
                          | GatewayIntents.MessageContent
                          | GatewayIntents.DirectMessageReactions
                          | GatewayIntents.GuildMessageReactions)
    .AddApplicationCommands()
    .AddComponentInteractions()
    .AddGatewayEventHandlers(typeof(Program).Assembly);

var host = builder.Build()
    .UseGatewayEventHandlers();

host.AddModules(typeof(Program).Assembly);

await host.RunAsync();
