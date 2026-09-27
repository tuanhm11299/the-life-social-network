using TheLife.Application.Common.Messaging;
using TheLife.Application.Features.Stories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TheLife.Infrastructure.BackgroundJobs;

/// <summary>Every hour, deletes stories older than 24 hours (the actual logic is in DeleteExpiredStories.cs).</summary>
internal sealed class ExpiredStoryCleanupJob(IServiceScopeFactory scopeFactory, ILogger<ExpiredStoryCleanupJob> logger)
    : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        do
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var handler = scope.ServiceProvider
                    .GetRequiredService<ICommandHandler<DeleteExpiredStoriesCommand, int>>();

                var result = await handler.Handle(new DeleteExpiredStoriesCommand(), stoppingToken);
                if (result.IsSuccess && result.Value > 0)
                    logger.LogInformation("Deleted {Count} expired stories", result.Value);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Expired story cleanup failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
