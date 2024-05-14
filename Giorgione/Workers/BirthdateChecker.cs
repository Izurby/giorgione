// Copyright (c) Davide Pierotti <d.pierotti@live.it>. Licensed under the GPLv3 Licence.
// See the LICENCE file in the repository root for full licence text.

using Discord;
using Discord.WebSocket;

using Giorgione.Config;
using Giorgione.Database;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Quartz;

namespace Giorgione;

internal class BirthdateChecker(
    DiscordSocketClient client,
    BotConfig config,
    ILogger<BirthdateChecker> logger,
    IDbContextFactory<UsersDbContext> dbFactory) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        logger.LogDebug("Checking for birthdays...");

        await using var db = await dbFactory.CreateDbContextAsync();

        var results = await db.Users
            .Where(user => user.Birthday.HasValue &&
                           user.Birthday.Value.Day == DateTime.UtcNow.Day &&
                           user.Birthday.Value.Month == DateTime.UtcNow.Month)
            .ToListAsync();

        logger.LogDebug("Found {Count} users", results.Count);

        var channel = (ITextChannel)client.GetGuild(config.GuildId).GetChannel(712271135021989938);

        await channel.SendMessageAsync($"Auguri a {string.Join(' ',results.Select(user => $"<@{user.Id}>"))}!");
    }
}
