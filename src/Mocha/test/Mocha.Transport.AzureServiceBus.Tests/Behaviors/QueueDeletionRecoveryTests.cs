using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mocha.Transport.AzureServiceBus.Tests.Helpers;

namespace Mocha.Transport.AzureServiceBus.Tests.Behaviors;

/// <summary>
/// Characterizes the Azure Service Bus SDK's own recovery contract when a queue is deleted while
/// its receive connection stays open. The endpoint no longer runs a repository-owned recovery
/// loop (see <see cref="ProcessorErrorHandlingTests"/>); this class establishes what the SDK does
/// on its own so the boundary of "SDK-owned" recovery is backed by evidence, not assumption.
/// </summary>
[Collection("AzureServiceBus")]
public sealed class QueueDeletionRecoveryTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(60);
    private readonly AzureServiceBusFixture _fixture;

    public QueueDeletionRecoveryTests(AzureServiceBusFixture fixture)
    {
        _fixture = fixture;
    }

    [Theory(Skip = "ASB emulator cannot start in this environment (MSSQL companion container exits with code 1); "
        + "the ServiceBusFailureReason observed by the SDK for a mid-connection queue deletion is unconfirmed, see hc5-2xa")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Processor_Should_ReportMessagingEntityNotFound_When_QueueDeletedWhileConnected(
        bool autoProvision)
    {
        // arrange - the queue is created up front regardless of AutoProvision, so the endpoint
        // has a real queue to connect to before it is deleted out from under the connection
        await using var ctx = _fixture.CreateTestContext();
        var queueName = ctx.QueueName("deleted-q");
        var adminClient = new ServiceBusAdministrationClient(ctx.AdminConnectionString);
        await adminClient.CreateQueueAsync(queueName, Xunit.TestContext.Current.CancellationToken);
        var loggerProvider = CapturingLoggerProvider.For<AzureServiceBusReceiveEndpoint>();
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddProvider(loggerProvider));
        await using var bus = await services
            .AddMessageBus()
            .AddConsumer<NoOpConsumer>()
            .AddAzureServiceBus(t =>
            {
                t.ConnectionString(ctx.ConnectionString);
                t.AdministrationConnectionString(ctx.AdminConnectionString);
                t.AutoProvision(autoProvision);
                t.Endpoint("del-ep").Consumer<NoOpConsumer>().Queue(queueName);
            })
            .BuildTestBusAsync();

        // act - delete the queue while the receive connection stays open
        await adminClient.DeleteQueueAsync(queueName, Xunit.TestContext.Current.CancellationToken);

        // assert - expected but unverified: the SDK is presumed to detect the missing entity on
        // the live connection and report it through ProcessErrorAsync with
        // ServiceBusFailureReason.MessagingEntityNotFound, with AutoProvision having no bearing on
        // it since it only governs provisioning at startup, not receive-side recovery. This has
        // not been empirically confirmed; the test is skipped until the emulator can run it.
        var reported = await loggerProvider.WaitForEntryAsync(
            e => e.Exception is ServiceBusException { Reason: ServiceBusFailureReason.MessagingEntityNotFound },
            s_timeout);
        Assert.True(reported, "the processor never reported MessagingEntityNotFound for the deleted queue");
    }

    private sealed class NoOpConsumer : IConsumer<OrderCreated>
    {
        public ValueTask ConsumeAsync(IConsumeContext<OrderCreated> context) => default;
    }
}
