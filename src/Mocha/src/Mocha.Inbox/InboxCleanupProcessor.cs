using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mocha.Inbox;

/// <summary>
/// Periodically removes old inbox entries that have exceeded the configured retention period.
/// Deletes in batches to avoid long-running locks on the inbox table.
/// </summary>
internal sealed class InboxCleanupProcessor(
    IOptions<InboxOptions> options,
    TimeProvider timeProvider,
    IServiceProvider provider,
    ILogger<InboxCleanupProcessor> logger)
{
    private readonly TimeProvider _timeProvider = timeProvider;

    /// <summary>
    /// Runs the cleanup loop, removing expired inbox entries at the configured interval.
    /// </summary>
    /// <param name="cancellationToken">A token that signals when the processor should stop.</param>
    public async Task ProcessAsync(CancellationToken cancellationToken)
    {
        var inboxOptions = options.Value;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(inboxOptions.CleanupInterval, cancellationToken);
                await using var scope = provider.CreateAsyncScope();
                using var activity = OpenTelemetry.Source.StartActivity(
                    "Inbox Cleanup",
                    ActivityKind.Internal,
                    // No parent context since this runs in the background independently of message processing
                    // activities
                    parentContext: new ActivityContext());

                var inbox = scope.ServiceProvider.GetRequiredService<IMessageInbox>();

                var startTime = Stopwatch.GetTimestamp();
                var totalDeleted = 0;

                int deleted;
                do
                {
                    deleted = await inbox.CleanupAsync(inboxOptions.RetentionPeriod, cancellationToken);

                    totalDeleted += deleted;
                } while (deleted > 0 && !cancellationToken.IsCancellationRequested);

                var elapsed = Stopwatch.GetElapsedTime(startTime);

                activity?.SetTag("inbox.cleanup.deleted_count", totalDeleted);
                activity?.SetTag("inbox.cleanup.duration_ms", elapsed.TotalMilliseconds);

                if (totalDeleted > 0)
                {
                    logger.InboxCleanupCompleted(totalDeleted, elapsed);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.InboxCleanupFailed(ex);
            }
        }
    }
}

internal static partial class InboxLogs
{
    [LoggerMessage(LogLevel.Information, "Inbox cleanup completed: deleted {Count} messages in {Duration}.")]
    public static partial void InboxCleanupCompleted(this ILogger logger, int count, TimeSpan duration);

    [LoggerMessage(LogLevel.Error, "An unexpected error occurred during inbox cleanup.")]
    public static partial void InboxCleanupFailed(this ILogger logger, Exception exception);

    [LoggerMessage(LogLevel.Information, "Inbox cleanup worker starting.")]
    public static partial void InboxWorkerStarting(this ILogger logger);

    [LoggerMessage(LogLevel.Information, "Inbox cleanup worker stopping.")]
    public static partial void InboxWorkerStopping(this ILogger logger);
}
