using Mocha.Features;
using Mocha.Middlewares;

namespace Mocha.Sagas.Tests;

/// <summary>
/// Minimal IConsumeContext implementation for unit testing saga state machine behavior.
/// Only the properties actually used by Saga.HandleEvent need real values.
/// </summary>
public sealed class TestConsumeContext : IConsumeContext
{
    public IFeatureCollection Features { get; } = new FeatureCollection();

    // IExecutionContext
    public IMessagingRuntime Runtime { get; set; } = null!;
    public CancellationToken CancellationToken { get; set; }
    public IServiceProvider Services { get; set; } = null!;

    // IMessageContext
    public MessagingTransport Transport { get; set; } = null!;
    public ReceiveEndpoint Endpoint { get; set; } = null!;
    public string? MessageId { get; set; }
    public string? CorrelationId { get; set; }
    public string? ConversationId { get; set; }
    public string? CausationId { get; set; }
    public Uri? SourceAddress { get; set; }
    public Uri? DestinationAddress { get; set; }
    public Uri? ResponseAddress { get; set; }
    public Uri? FaultAddress { get; set; }
    public MessageContentType? ContentType { get; set; }
    public MessageType? MessageType { get; set; }
    public IReadOnlyHeaders Headers => _headers;
    public DateTimeOffset? SentAt { get; set; }
    public DateTimeOffset? DeliverBy { get; set; }
    public int? DeliveryCount { get; set; }
    public ReadOnlyMemory<byte> Body => ReadOnlyMemory<byte>.Empty;
    public MessageEnvelope? Envelope { get; set; }
    public IRemoteHostInfo Host { get; set; } = null!;

    private readonly Headers _headers = new();

    /// <summary>
    /// Provides mutable access to headers for test setup.
    /// </summary>
    public Headers MutableHeaders => _headers;
}
