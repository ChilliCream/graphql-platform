using System.Diagnostics.CodeAnalysis;
using Azure;
using Azure.Core;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;

namespace Mocha.Transport.AzureServiceBus.Tests.Helpers;

/// <summary>
/// A <see cref="ServiceBusAdministrationClient"/> test double that records queue provisioning
/// calls without contacting a live Azure Service Bus namespace.
/// </summary>
internal sealed class FakeServiceBusAdministrationClient : ServiceBusAdministrationClient
{
    public int CreateQueueCallCount { get; private set; }

    public int CreateTopicCallCount { get; private set; }

    public int CreateSubscriptionCallCount { get; private set; }

    /// <summary>
    /// When set, <see cref="CreateSubscriptionAsync(CreateSubscriptionOptions, CancellationToken)"/> throws
    /// the returned exception instead of succeeding, simulating a concurrent provisioning race.
    /// </summary>
    public Func<CreateSubscriptionOptions, ServiceBusException?>? CreateSubscriptionFailure { get; set; }

    /// <summary>
    /// The ForwardTo value returned by <see cref="GetSubscriptionAsync(string, string, CancellationToken)"/>
    /// for the pre-existing subscription, formatted the way the broker returns it (an absolute URI).
    /// </summary>
    public string? ExistingSubscriptionForwardTo { get; set; }

    public override Task<Response<QueueProperties>> CreateQueueAsync(
        CreateQueueOptions options,
        CancellationToken cancellationToken = default)
    {
        CreateQueueCallCount++;

        return Task.FromResult(
            Response.FromValue(
                ServiceBusModelFactory.QueueProperties(
                    options.Name,
                    lockDuration: TimeSpan.FromSeconds(30),
                    defaultMessageTimeToLive: TimeSpan.FromDays(14),
                    autoDeleteOnIdle: TimeSpan.MaxValue,
                    duplicateDetectionHistoryTimeWindow: TimeSpan.FromMinutes(1),
                    maxDeliveryCount: 10,
                    userMetadata: string.Empty),
                new FakeResponse()));
    }

    public override Task<Response<TopicProperties>> CreateTopicAsync(
        CreateTopicOptions options,
        CancellationToken cancellationToken = default)
    {
        CreateTopicCallCount++;

        return Task.FromResult(
            Response.FromValue(
                ServiceBusModelFactory.TopicProperties(
                    options.Name,
                    defaultMessageTimeToLive: TimeSpan.FromDays(14),
                    autoDeleteOnIdle: TimeSpan.MaxValue,
                    duplicateDetectionHistoryTimeWindow: TimeSpan.FromMinutes(1)),
                new FakeResponse()));
    }

    public override Task<Response<SubscriptionProperties>> CreateSubscriptionAsync(
        CreateSubscriptionOptions options,
        CancellationToken cancellationToken = default)
    {
        CreateSubscriptionCallCount++;

        if (CreateSubscriptionFailure?.Invoke(options) is { } exception)
        {
            throw exception;
        }

        return Task.FromResult(
            Response.FromValue(
                ServiceBusModelFactory.SubscriptionProperties(
                    options.TopicName,
                    options.SubscriptionName,
                    lockDuration: TimeSpan.FromSeconds(30),
                    defaultMessageTimeToLive: TimeSpan.FromDays(14),
                    autoDeleteOnIdle: TimeSpan.MaxValue,
                    maxDeliveryCount: 10,
                    forwardTo: options.ForwardTo,
                    userMetadata: string.Empty),
                new FakeResponse()));
    }

    public override Task<Response<SubscriptionProperties>> GetSubscriptionAsync(
        string topicName,
        string subscriptionName,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            Response.FromValue(
                ServiceBusModelFactory.SubscriptionProperties(
                    topicName,
                    subscriptionName,
                    lockDuration: TimeSpan.FromSeconds(30),
                    defaultMessageTimeToLive: TimeSpan.FromDays(14),
                    autoDeleteOnIdle: TimeSpan.MaxValue,
                    maxDeliveryCount: 10,
                    forwardTo: ExistingSubscriptionForwardTo,
                    userMetadata: string.Empty),
                new FakeResponse()));
    }

    private sealed class FakeResponse : Response
    {
        public override int Status => 200;

        public override string ReasonPhrase => "OK";

        public override Stream? ContentStream { get; set; }

        public override string ClientRequestId { get; set; } = string.Empty;

        public override void Dispose()
        {
        }

        protected override bool ContainsHeader(string name) => false;

        protected override IEnumerable<HttpHeader> EnumerateHeaders() => [];

        protected override bool TryGetHeader(string name, [NotNullWhen(true)] out string? value)
        {
            value = null;
            return false;
        }

        protected override bool TryGetHeaderValues(string name, [NotNullWhen(true)] out IEnumerable<string>? values)
        {
            values = null;
            return false;
        }
    }
}
