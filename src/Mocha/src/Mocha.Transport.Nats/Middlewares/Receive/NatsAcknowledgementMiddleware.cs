using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mocha.Features;
using Mocha.Middlewares;
using Mocha.Transport.Nats.Features;
using NATS.Client.JetStream;

namespace Mocha.Transport.Nats.Middlewares;

/// <summary>
/// Settles each JetStream message according to how the receive pipeline finished.
/// </summary>
internal sealed class NatsAcknowledgementMiddleware
{
    private static readonly NatsAcknowledgementMiddleware s_instance = new();

    /// <summary>
    /// Runs the pipeline and acknowledges or negatively acknowledges the message.
    /// </summary>
    /// <param name="context">The receive context.</param>
    /// <param name="next">The next middleware.</param>
    public async ValueTask InvokeAsync(IReceiveContext context, ReceiveDelegate next)
    {
        var feature = context.Features.GetOrSet<NatsReceiveFeature>();

        if (feature.Message is not { } message)
        {
            // Core NATS delivery, as used by reply endpoints: nothing to settle.
            await next(context);
            return;
        }

        var cancellationToken = context.CancellationToken;

        using var progress = AckProgressReporter.Start(
            message,
            feature.AckProgressInterval,
            context.Services,
            cancellationToken);

        try
        {
            await next(context);

            await message.AckAsync(cancellationToken: cancellationToken);
        }
        catch
        {
            try
            {
                // Settled without the pipeline token: when a handler fails because the host is
                // shutting down, the message should still be released for redelivery straight away
                // rather than waiting out AckWait.
                await message.NakAsync(cancellationToken: CancellationToken.None);
            }
            catch (Exception exception)
            {
                // Settling is secondary to why the pipeline failed, and the message is redelivered
                // once its acknowledgement deadline expires either way.
                context.Services
                    .GetRequiredService<ILogger<NatsAcknowledgementMiddleware>>()
                    .SettlementFailed(exception);
            }

            throw;
        }
    }

    /// <summary>
    /// Creates the middleware configuration.
    /// </summary>
    /// <returns>The configuration.</returns>
    public static ReceiveMiddlewareConfiguration Create()
        => new(static (_, next) => ctx => s_instance.InvokeAsync(ctx, next), "NatsAcknowledgement");

    private sealed class AckProgressReporter : IDisposable
    {
        private readonly CancellationTokenSource _cancellation;

        private AckProgressReporter(CancellationTokenSource cancellation)
        {
            _cancellation = cancellation;
        }

        public static AckProgressReporter? Start(
            INatsJSMsg<ReadOnlyMemory<byte>> message,
            TimeSpan? interval,
            IServiceProvider services,
            CancellationToken cancellationToken)
        {
            if (interval is not { } period || period <= TimeSpan.Zero)
            {
                return null;
            }

            // Resolved only once progress reporting is actually in use, to keep it off the path every
            // other message takes.
            var logger = services.GetRequiredService<ILogger<NatsAcknowledgementMiddleware>>();

            var cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            _ = ReportAsync(message, period, logger, cancellation.Token);

            return new AckProgressReporter(cancellation);
        }

        public void Dispose()
        {
            _cancellation.Cancel();
            _cancellation.Dispose();
        }

        private static async Task ReportAsync(
            INatsJSMsg<ReadOnlyMemory<byte>> message,
            TimeSpan period,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(period, cancellationToken);

                    await message.AckProgressAsync(cancellationToken: cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected: the handler finished, so there is no deadline left to extend.
            }
            catch (Exception exception)
            {
                // Logged rather than rethrown: nobody awaits this task, so an escaping exception
                // would surface only as an unobserved one. The handler keeps running, and its message
                // is redelivered once the deadline expires.
                logger.AckProgressFailed(exception);
            }
        }
    }
}
