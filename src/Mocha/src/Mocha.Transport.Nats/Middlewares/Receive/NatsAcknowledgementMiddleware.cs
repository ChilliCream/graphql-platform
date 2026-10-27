using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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

            // A completed handler is acknowledged even when shutdown aborts the pipeline.
            await message.AckAsync(cancellationToken: CancellationToken.None);
        }
        catch
        {
            try
            {
                // A failed handler's message is released even when shutdown aborts the pipeline.
                await message.NakAsync(cancellationToken: CancellationToken.None);
            }
            catch (Exception exception)
            {
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
                // Nothing awaits this task, so the failure is logged and the handler keeps running.
                logger.AckProgressFailed(exception);
            }
        }
    }
}
