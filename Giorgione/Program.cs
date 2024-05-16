﻿using Discord;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;

using Giorgione;
using Giorgione.Config;
using Giorgione.Database;
using Giorgione.Workers;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();

builder.Services
    .AddHttpClient()
    .AddSerilog()
    .AddScheduling(builder.Configuration.GetConnectionString("QuartzContext"))
    .AddSingleton(builder.Configuration.GetSection("BotConfig").Get<BotConfig>()
                  ?? throw new InvalidOperationException("Could not read the bot configuration"))
    .AddDbContextFactory<UsersDbContext>(db =>
    {
        db.UseNpgsql(builder.Configuration.GetConnectionString("ApplicationContext")
                     ?? throw new InvalidOperationException("Could not read EF database connection string"));
    })
    .AddSingleton(new DiscordSocketConfig
    {
        GatewayIntents = GatewayIntents.AllUnprivileged |
                         GatewayIntents.GuildMembers |
                         GatewayIntents.GuildPresences |
                         GatewayIntents.MessageContent,
        DefaultRetryMode = RetryMode.AlwaysFail,
        AuditLogCacheSize = 15,
        MessageCacheSize = 50,
    })
    .AddSingleton(new InteractionServiceConfig
    {
        UseCompiledLambda = true
    })
    .AddSingleton<DiscordSocketClient>()
    .AddSingleton<IRestClientProvider>(x => x.GetRequiredService<DiscordSocketClient>()) //TODO: wait for upstream fix
    .AddSingleton<InteractionService>()
    .AddSingleton<InteractionHandler>()
    .AddHostedService<EvalWatchdog>()
    .AddHostedService<GiorgioneBot>();

var host = builder.Build();

await host.StartAsync();
await host.WaitForShutdownAsync();
