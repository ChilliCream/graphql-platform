using System.Collections.Immutable;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using Mocha.Middlewares;

namespace Mocha;

/// <summary>
/// Concrete implementation of <see cref="IMessagingRuntime"/> that holds the fully configured state
/// of the messaging bus, including transports, consumers, routers, and message type registrations.
/// </summary>
/// <remarks>
/// Created once during host startup and shared across all bus operations for the lifetime of the host.
/// Starting the runtime starts all registered transports and their receive endpoints in sequence.
/// Stopping the runtime stops all started transports, and disposing it disposes the consumers.
/// Starting, stopping and disposing run one at a time.
/// </remarks>
/// <param name="services">The root service provider for the messaging host.</param>
/// <param name="options">Read-only messaging configuration options.</param>
/// <param name="naming">Naming conventions used to derive queue, exchange, and endpoint names.</param>
/// <param name="conventions">Registry of conventions applied during configuration and routing.</param>
/// <param name="consumers">The set of all registered consumer definitions.</param>
/// <param name="transports">The ordered list of configured transports (e.g., RabbitMQ, in-memory).</param>
/// <param name="messages">Registry that maps CLR types to <see cref="MessageType"/> metadata.</param>
/// <param name="host">Information about the current host instance (machine name, process ID, etc.).</param>
/// <param name="router">Router that resolves outbound message types to dispatch endpoints.</param>
/// <param name="endpointRouter">Router that resolves or creates dispatch endpoints by URI address.</param>
/// <param name="features">Feature collection shared across the runtime scope.</param>
public sealed class MessagingRuntime(
    IServiceProvider services,
    IReadOnlyMessagingOptions options,
    IBusNamingConventions naming,
    IConventionRegistry conventions,
    ImmutableHashSet<Consumer> consumers,
    ImmutableArray<MessagingTransport> transports,
    IMessageTypeRegistry messages,
    IHostInfo host,
    IMessageRouter router,
    IEndpointRouter endpointRouter,
    IFeatureCollection features) : IMessagingRuntime, IAsyncDisposable
{
    /// <inheritdoc />
    public IServiceProvider Services => services;

    /// <inheritdoc />
    public IBusNamingConventions Naming => naming;

    /// <inheritdoc />
    public IMessageTypeRegistry Messages => messages;

    /// <inheritdoc />
    public IMessageRouter Router => router;

    /// <inheritdoc />
    public IEndpointRouter Endpoints => endpointRouter;

    /// <inheritdoc />
    public IHostInfo Host => host;

    /// <inheritdoc />
    public IConventionRegistry Conventions => conventions;

    /// <inheritdoc />
    public ImmutableHashSet<Consumer> Consumers => consumers;

    /// <inheritdoc />
    public ImmutableArray<MessagingTransport> Transports => transports;

    /// <inheritdoc />
    public IFeatureCollection Features => features;

    /// <inheritdoc />
    public IReadOnlyMessagingOptions Options => options;
    private MessageBusChangeTokenSource? _changeTokenSource;
    private MessageBusChangeTokenSource ChangeTokenSource => _changeTokenSource ??= new MessageBusChangeTokenSource(this);

    /// <inheritdoc />
    public MessageBusDescription Description => ChangeTokenSource.Description;

    /// <inheritdoc />
    public IChangeToken GetChangeToken() => ChangeTokenSource.GetChangeToken();

    /// <inheritdoc />
    public DispatchEndpoint GetSendEndpoint(MessageType messageType)
    {
        return router.GetEndpoint(this, messageType, OutboundRouteKind.Send);
    }

    /// <inheritdoc />
    public DispatchEndpoint GetPublishEndpoint(MessageType messageType)
    {
        return router.GetEndpoint(this, messageType, OutboundRouteKind.Publish);
    }

    /// <inheritdoc />
    public DispatchEndpoint GetDispatchEndpoint(Uri address)
    {
        return endpointRouter.GetOrCreate(this, address);
    }

    /// <inheritdoc />
    public MessageType GetMessageType(Type type)
    {
        return messages.GetOrAdd(this, type);
    }

    /// <inheritdoc />
    public MessageType? GetMessageType(string? identity)
    {
        return identity is not null ? messages.GetMessageType(identity) : null;
    }

    /// <inheritdoc />
    public MessagingTransport? GetTransport(Uri address)
    {
        return transports.FirstOrDefault(t => t.Schema == address.Scheme);
    }

    /// <summary>
    /// Indicates whether all transports have been started and the runtime is accepting operations.
    /// </summary>
    public bool IsStarted { get; private set; }

    private static readonly CancellationToken s_abort = new(canceled: true);
    private readonly SemaphoreSlim _lifecycle = new(1, 1);
    private readonly ILogger _logger =
        services.GetService<ILogger<MessagingRuntime>>() ?? NullLogger<MessagingRuntime>.Instance;
    private bool _stopped;
    private bool _disposed;

    /// <summary>
    /// Starts all registered transports and their receive endpoints, stopping them if startup fails.
    /// A stopped runtime cannot be started again.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the startup sequence.</param>
    /// <exception cref="ObjectDisposedException">Thrown if the runtime has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the runtime has been stopped.</exception>
    public async ValueTask StartAsync(CancellationToken cancellationToken)
    {
        await _lifecycle.WaitAsync(cancellationToken);

        try
        {
            if (_disposed)
            {
                throw ThrowHelper.RuntimeDisposed();
            }

            if (_stopped)
            {
                throw ThrowHelper.RuntimeStopped();
            }

            if (IsStarted)
            {
                return;
            }

            try
            {
                foreach (var transport in transports)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (!transport.IsStarted)
                    {
                        await transport.StartAsync(this, cancellationToken);
                    }
                }

                cancellationToken.ThrowIfCancellationRequested();
            }
            catch
            {
                await TryStopCoreAsync();
                throw;
            }

            IsStarted = true;
        }
        finally
        {
            _lifecycle.Release();
        }
    }

    /// <summary>
    /// Stops all started transports and waits for their in-flight messages. Reply endpoints stop after
    /// the receive endpoints of every transport, so handlers can still receive replies while they finish.
    /// </summary>
    /// <param name="cancellationToken">
    /// A token that cancels the in-flight messages, typically the host's shutdown token.
    /// </param>
    public async ValueTask StopAsync(CancellationToken cancellationToken)
    {
        await _lifecycle.WaitAsync(CancellationToken.None);

        try
        {
            await StopCoreAsync(cancellationToken);
        }
        finally
        {
            _lifecycle.Release();
        }
    }

    private async Task StopCoreAsync(CancellationToken cancellationToken)
    {
        _stopped = true;
        IsStarted = false;

        var stopping = new List<MessagingTransport>(transports.Length);
        foreach (var transport in transports)
        {
            if (transport.IsStarted)
            {
                stopping.Add(transport);
            }
        }

        var stopTasks = new List<Task>(stopping.Count * 2);
        foreach (var transport in stopping)
        {
            stopTasks.Add(transport.StopReceiveEndpointsAsync(this, cancellationToken));
        }

        await Task.WhenAll(stopTasks).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

        foreach (var transport in stopping)
        {
            stopTasks.Add(transport.CompleteStopAsync(this, cancellationToken));
        }

        await TaskHelper.WhenAllAsync(stopTasks);
    }

    private async Task TryStopCoreAsync()
    {
        try
        {
            await StopCoreAsync(s_abort);
        }
        catch (Exception ex)
        {
            _logger.RuntimeStopFailed(ex);
        }
    }

    /// <summary>
    /// Disposes the consumers. Failures are logged, and calling this method more than once has no
    /// further effect.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await _lifecycle.WaitAsync(CancellationToken.None);

        try
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            foreach (var consumer in consumers)
            {
                try
                {
                    await consumer.DisposeAsync();
                }
                catch (Exception ex)
                {
                    _logger.ConsumerDisposeFailed(ex, consumer.Name);
                }
            }

            _changeTokenSource?.Dispose();
        }
        finally
        {
            _lifecycle.Release();
        }
    }
}

internal static partial class Logs
{
    [LoggerMessage(LogLevel.Error, "Error stopping the messaging transports.")]
    public static partial void RuntimeStopFailed(this ILogger logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "Error disposing consumer {ConsumerName}.")]
    public static partial void ConsumerDisposeFailed(this ILogger logger, Exception exception, string consumerName);
}
