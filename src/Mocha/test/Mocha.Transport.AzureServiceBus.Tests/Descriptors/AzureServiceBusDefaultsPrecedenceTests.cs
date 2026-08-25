using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.AzureServiceBus.Tests.Helpers;

namespace Mocha.Transport.AzureServiceBus.Tests.Descriptors;

/// <summary>
/// Proves that bus-level queue, topic, and endpoint defaults fill in unset values on auto-provisioned
/// resources without overriding a value the resource set explicitly.
/// </summary>
public class AzureServiceBusDefaultsPrecedenceTests
{
    private const string DummyConnectionString =
        "Endpoint=sb://localhost/;SharedAccessKeyName=test;SharedAccessKey=test";

    [Fact]
    public void ConfigureDefaults_Should_FillUnsetQueueValues_Without_OverridingExplicitOnes()
    {
        // arrange
        var (_, _, topology) = CreateTopology(t =>
        {
            t.ConfigureDefaults(d =>
            {
                d.Queue.LockDuration = TimeSpan.FromSeconds(60);
                d.Queue.MaxDeliveryCount = 5;
            });
            t.DeclareQueue("inherits");
            t.DeclareQueue("overrides").LockDuration(TimeSpan.FromSeconds(15)).MaxDeliveryCount(2);
        });

        // act
        var inherits = topology.Queues.Single(q => q.Name == "inherits");
        var overrides = topology.Queues.Single(q => q.Name == "overrides");

        // assert
        Assert.Equal(TimeSpan.FromSeconds(60), inherits.LockDuration);
        Assert.Equal(5, inherits.MaxDeliveryCount);
        Assert.Equal(TimeSpan.FromSeconds(15), overrides.LockDuration);
        Assert.Equal(2, overrides.MaxDeliveryCount);
    }

    [Fact]
    public void ConfigureDefaults_Should_FillUnsetTopicValues_Without_OverridingExplicitOnes()
    {
        // arrange
        var (_, _, topology) = CreateTopology(t =>
        {
            t.ConfigureDefaults(d =>
            {
                d.Topic.MaxSizeInMegabytes = 4096;
                d.Topic.SupportOrdering = true;
            });
            t.DeclareTopic("inherits");
            t.DeclareTopic("overrides").MaxSizeInMegabytes(1024).SupportOrdering(false);
        });

        // act
        var inherits = topology.Topics.Single(t => t.Name == "inherits");
        var overrides = topology.Topics.Single(t => t.Name == "overrides");

        // assert
        Assert.Equal(4096, inherits.MaxSizeInMegabytes);
        Assert.True(inherits.SupportOrdering);
        Assert.Equal(1024, overrides.MaxSizeInMegabytes);
        Assert.False(overrides.SupportOrdering);
    }

    [Fact]
    public void ConfigureDefaults_Should_FillUnsetEndpointValues_Without_OverridingExplicitOnes()
    {
        // arrange
        var runtime = CreateRuntime(t =>
        {
            t.BindExplicitly();
            t.ConfigureDefaults(d =>
            {
                d.Endpoint.PrefetchCount = 42;
                d.Endpoint.MaxConcurrency = 7;
            });
            t.Queue("inherits");
            t.Queue("overrides").PrefetchCount(3).MaxConcurrency(1);
        });
        var transport = runtime.Transports.OfType<AzureServiceBusMessagingTransport>().Single();

        // act
        var inherits = transport.ReceiveEndpoints.OfType<AzureServiceBusReceiveEndpoint>()
            .Single(e => e.Name == "inherits");
        var overrides = transport.ReceiveEndpoints.OfType<AzureServiceBusReceiveEndpoint>()
            .Single(e => e.Name == "overrides");

        // assert
        Assert.Equal(42, inherits.Configuration.PrefetchCount);
        Assert.Equal(7, inherits.Configuration.MaxConcurrency);
        Assert.Equal(3, overrides.Configuration.PrefetchCount);
        Assert.Equal(1, overrides.Configuration.MaxConcurrency);
    }

    private static (
        MessagingRuntime Runtime,
        AzureServiceBusMessagingTransport Transport,
        AzureServiceBusMessagingTopology Topology) CreateTopology(
        Action<IAzureServiceBusMessagingTransportDescriptor> configure)
    {
        var runtime = CreateRuntime(configure);
        var transport = runtime.Transports.OfType<AzureServiceBusMessagingTransport>().Single();
        return (runtime, transport, (AzureServiceBusMessagingTopology)transport.Topology);
    }

    private static MessagingRuntime CreateRuntime(Action<IAzureServiceBusMessagingTransportDescriptor> configure)
    {
        var services = new ServiceCollection();
        return services
            .AddMessageBus()
            .AddAzureServiceBus(t =>
            {
                t.ConnectionString(DummyConnectionString);
                configure(t);
            })
            .BuildRuntime();
    }
}
