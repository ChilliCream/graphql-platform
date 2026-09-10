using System.Diagnostics.CodeAnalysis;
using Azure;
using Azure.Core;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.AzureServiceBus.Tests.Helpers;

namespace Mocha.Transport.AzureServiceBus.Tests.Topology;

/// <summary>
/// Covers <see cref="AzureServiceBusMessagingTransport.OnBeforeStartAsync"/> recursive provisioning
/// of queues that reference other queues through <c>ForwardTo</c> and
/// <c>ForwardDeadLetteredMessagesTo</c>: forwarding targets must be provisioned before the queue that
/// forwards to them regardless of declaration order, and a forwarding cycle must be rejected.
/// </summary>
public class AzureServiceBusForwardingProvisioningTests
{
    [Fact]
    public async Task BuildTestBusAsync_Should_ProvisionForwardingTargetBeforeSource()
    {
        // arrange - "orders" forwards to "archive", but "archive" is declared after "orders"
        var admin = new QueueOrderTrackingAdministrationClient();
        var services = new ServiceCollection();
        services.AddSingleton<ServiceBusClient>(new FakeServiceBusClient(_ => null));
        services.AddSingleton<ServiceBusAdministrationClient>(admin);
        var builder = services
            .AddMessageBus()
            .AddAzureServiceBus(t =>
            {
                t.DeclareQueue("orders").ForwardTo("archive");
                t.DeclareQueue("archive");
            });

        // act
        await using var bus = await builder.BuildTestBusAsync();

        // assert - the forwarding target was provisioned before the queue that forwards to it
        AssertProvisionedBefore(admin.CreatedQueueNames, "archive", "orders");
    }

    [Fact]
    public async Task BuildTestBusAsync_Should_ProvisionDeadLetterForwardingTargetBeforeSource()
    {
        // arrange - "orders" dead-letter-forwards to "orders-dlq-archive", declared after "orders"
        var admin = new QueueOrderTrackingAdministrationClient();
        var services = new ServiceCollection();
        services.AddSingleton<ServiceBusClient>(new FakeServiceBusClient(_ => null));
        services.AddSingleton<ServiceBusAdministrationClient>(admin);
        var builder = services
            .AddMessageBus()
            .AddAzureServiceBus(t =>
            {
                t.DeclareQueue("orders").ForwardDeadLetteredMessagesTo("orders-dlq-archive");
                t.DeclareQueue("orders-dlq-archive");
            });

        // act
        await using var bus = await builder.BuildTestBusAsync();

        // assert - the dead-letter forwarding target was provisioned before the source queue
        AssertProvisionedBefore(admin.CreatedQueueNames, "orders-dlq-archive", "orders");
    }

    [Fact]
    public async Task BuildTestBusAsync_Should_Throw_When_ForwardingCycleConfigured()
    {
        // arrange - "a" forwards to "b" and "b" forwards back to "a"
        var admin = new QueueOrderTrackingAdministrationClient();
        var services = new ServiceCollection();
        services.AddSingleton<ServiceBusClient>(new FakeServiceBusClient(_ => null));
        services.AddSingleton<ServiceBusAdministrationClient>(admin);
        var builder = services
            .AddMessageBus()
            .AddAzureServiceBus(t =>
            {
                t.DeclareQueue("a").ForwardTo("b");
                t.DeclareQueue("b").ForwardTo("a");
            });

        // act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => builder.BuildTestBusAsync());

        // assert
        Assert.Equal(
            "Azure Service Bus queue forwarding contains a cycle involving 'a'.",
            exception.Message);
    }

    /// <summary>
    /// Asserts that <paramref name="target"/> was provisioned before <paramref name="source"/>. The
    /// message bus also provisions its own reply queue during startup, so the created-queue list is
    /// checked for relative order rather than an exact match.
    /// </summary>
    private static void AssertProvisionedBefore(List<string> createdQueueNames, string target, string source)
    {
        var targetIndex = createdQueueNames.IndexOf(target);
        var sourceIndex = createdQueueNames.IndexOf(source);
        Assert.True(
            targetIndex >= 0 && sourceIndex >= 0 && targetIndex < sourceIndex,
            $"Expected '{target}' to be provisioned before '{source}'. "
            + $"Actual order: [{string.Join(", ", createdQueueNames)}]");
    }

    /// <summary>
    /// A <see cref="ServiceBusAdministrationClient"/> test double that records the order in which
    /// queues are created without contacting a live Azure Service Bus namespace.
    /// </summary>
    private sealed class QueueOrderTrackingAdministrationClient : ServiceBusAdministrationClient
    {
        public List<string> CreatedQueueNames { get; } = [];

        public override Task<Response<QueueProperties>> CreateQueueAsync(
            CreateQueueOptions options,
            CancellationToken cancellationToken = default)
        {
            CreatedQueueNames.Add(options.Name);

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
}
