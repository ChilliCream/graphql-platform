using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mocha.Features;
using Mocha.Threading;
using Mocha.Transport.NATS.Features;
using NATS.Client.JetStream;

namespace Mocha.Transport.NATS;

/// <summary>
/// NATS JetStream receive endpoint that consumes messages from a specific subject using
/// a durable pull consumer.
/// </summary>
/// <remarks>
/// Message processing uses a single consume loop that feeds messages into a
/// <see cref="Channel{T}"/>, which is then consumed by a <see cref="ChannelProcessor{T}"/>
/// with N concurrent workers (where N = <see cref="ReceiveEndpointConfiguration.MaxConcurrency"/>).
/// The <c>MaxPrefetch</c> setting controls only the JetStream consumer's <c>MaxAckPending</c>
/// broker-side limit.
/// </remarks>
/// <param name="transport">The owning NATS transport instance.</param>
public sealed class NatsReceiveEndpoint(NatsMessagingTransport transport)
    : ReceiveEndpoint<NatsReceiveEndpointConfiguration>(transport)
{
    private int _maxConcurrency = Environment.ProcessorCount;
    private ChannelProcessor<INatsJSMsg<ReadOnlyMemory<byte>>>? _processor;
    private ContinuousTask? _consumeTask;
    private Channel<INatsJSMsg<ReadOnlyMemory<byte>>>? _channel;

    /// <summary>
    /// Gets the JetStream stream that this endpoint consumes from.
    /// </summary>
    public NatsStream Stream { get; private set; } = null!;

    /// <summary>
    /// Gets the JetStream durable consumer for this endpoint.
    /// </summary>
    public NatsConsumer Consumer { get; private set; } = null!;

    protected override void OnInitialize(
        IMessagingConfigurationContext context,
        NatsReceiveEndpointConfiguration configuration)
    {
        if (configuration.SubjectName is null)
        {
            throw new InvalidOperationException("Subject name is required");
        }

        if (configuration.ConsumerName is null)
        {
            throw new InvalidOperationException("Consumer name is required");
        }

        _maxConcurrency = configuration.MaxConcurrency;
    }

    protected override void OnComplete(
        IMessagingConfigurationContext context,
        NatsReceiveEndpointConfiguration configuration)
    {
        if (configuration.SubjectName is null)
        {
            throw new InvalidOperationException("Subject name is required");
        }

        var topology = (NatsMessagingTopology)Transport.Topology;

        Stream = topology.GetStreamForSubject(configuration.SubjectName)
            ?? throw new InvalidOperationException(
                $"No stream found for subject '{configuration.SubjectName}'");

        Consumer = topology.Consumers.FirstOrDefault(c => c.Name == configuration.ConsumerName)
            ?? throw new InvalidOperationException(
                $"Consumer '{configuration.ConsumerName}' not found");

        Source = Stream;
    }

    protected override async ValueTask OnStartAsync(
        IMessagingRuntimeContext context,
        CancellationToken cancellationToken)
    {
        if (Transport is not NatsMessagingTransport natsTransport)
        {
            throw new InvalidOperationException("Transport is not a NatsMessagingTransport");
        }

        var js = natsTransport.JetStream;
        var autoProvision = ((NatsMessagingTopology)natsTransport.Topology).AutoProvision;
        var logger = context.Services.GetRequiredService<ILogger<NatsReceiveEndpoint>>();

        if (Stream.AutoProvision ?? autoProvision)
        {
            await Stream.ProvisionAsync(js, cancellationToken);
        }

        if (Consumer.AutoProvision ?? autoProvision)
        {
            await Consumer.ProvisionAsync(js, Stream.Name, cancellationToken);
        }

        _channel = Channel.CreateBounded<INatsJSMsg<ReadOnlyMemory<byte>>>(
            new BoundedChannelOptions(_maxConcurrency * 2)
            {
                SingleWriter = true,
                FullMode = BoundedChannelFullMode.Wait
            });

        _consumeTask = new ContinuousTask(ct => RunConsumeLoopAsync(js, logger, ct));

        _processor = new ChannelProcessor<INatsJSMsg<ReadOnlyMemory<byte>>>(
            _channel.Reader.ReadAllAsync,
            (msg, ct) => ProcessMessageAsync(msg, logger, ct),
            _maxConcurrency);
    }

    private async Task RunConsumeLoopAsync(
        INatsJSContext js,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var writer = _channel!.Writer;

        while (!cancellationToken.IsCancellationRequested)
        {
            INatsJSConsumer jsConsumer;
            try
            {
                jsConsumer = await js.GetConsumerAsync(
                    Stream.Name, Consumer.Name, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "Failed to get consumer {Consumer} on stream {Stream}, retrying...",
                    Consumer.Name, Stream.Name);
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
                continue;
            }

            try
            {
                await foreach (var msg in jsConsumer.ConsumeAsync<ReadOnlyMemory<byte>>(
                    cancellationToken: cancellationToken))
                {
                    await writer.WriteAsync(msg, cancellationToken);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            logger.LogWarning(
                "ConsumeAsync for consumer {Consumer} on stream {Stream} ended unexpectedly, restarting...",
                Consumer.Name, Stream.Name);
        }
    }

    private async Task ProcessMessageAsync(
        INatsJSMsg<ReadOnlyMemory<byte>> msg,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        try
        {
            await ExecuteAsync(
                static (receiveContext, state) =>
                {
                    var feature = receiveContext.Features.GetOrSet<NatsReceiveFeature>();
                    feature.Message = state;
                },
                msg,
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Error processing message on stream {Stream}", Stream.Name);
        }
    }

    protected override async ValueTask OnStopAsync(
        IMessagingRuntimeContext context,
        CancellationToken cancellationToken)
    {
        if (_processor is not null)
        {
            await _processor.DisposeAsync();
            _processor = null;
        }

        if (_consumeTask is not null)
        {
            await _consumeTask.DisposeAsync();
            _consumeTask = null;
        }

        _channel?.Writer.TryComplete();
        _channel = null;

        if (Configuration.IsTemporary)
        {
            try
            {
                var js = ((NatsMessagingTransport)Transport).JetStream;
                await js.DeleteConsumerAsync(Stream.Name, Consumer.Name, cancellationToken);
                await js.DeleteStreamAsync(Stream.Name, cancellationToken);
            }
            catch (NatsJSApiException)
            {
                // Best-effort cleanup; stream/consumer may already be gone.
            }
        }
    }
}
