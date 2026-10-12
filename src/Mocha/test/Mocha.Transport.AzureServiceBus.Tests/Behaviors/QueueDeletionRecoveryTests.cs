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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Processor_Should_ReportMessagingEntityNotFound_When_QueueDeletedWhileConnected(
        bool autoProvision)
    {
        // arrange - the queue is created up front regardless of AutoProvision, so the endpoint
        // has a real queue to connect to before it is deleted out from under the connection
        await using var ctx = _fixture.CreateTestContext();
        var queueName = ctx.QueueName(autoProvision ? "auto-deleted-q" : "deleted-q");
        var adminClient = new ServiceBusAdministrationClient(ctx.AdminConnectionString);
        await adminClient.CreateQueueAsync(queueName, Xunit.TestContext.Current.CancellationToken);
        var recorder = new MessageRecorder();
        var loggerProvider = CapturingLoggerProvider.For<AzureServiceBusReceiveEndpoint>();
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddProvider(loggerProvider));
        await using var bus = await services
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddAzureServiceBus(t =>
            {
                t.ConnectionString(ctx.ConnectionString);
                t.AdministrationConnectionString(ctx.AdminConnectionString);
                t.AutoProvision(autoProvision);
                t.Endpoint("del-ep").Handler<OrderCreatedHandler>().Queue(queueName);
                t.DispatchEndpoint("del-send-ep").ToQueue(queueName).Send<OrderCreated>();
            })
            .BuildTestBusAsync();

        // the processor attaches its receive link in the background after start, so a delivered
        // message is what proves the connection is live before the queue is deleted beneath it
        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        await messageBus.SendAsync(new OrderCreated { OrderId = "DEL-1" }, Xunit.TestContext.Current.CancellationToken);
        Assert.True(
            await recorder.WaitAsync(s_timeout),
            "the endpoint never received the message sent before deletion");

        // act - delete the queue while the receive connection stays open
        await adminClient.DeleteQueueAsync(queueName, Xunit.TestContext.Current.CancellationToken);

        // assert - confirmed against the live emulator: the SDK detects the missing entity on the
        // live connection and reports it through ProcessErrorAsync with
        // ServiceBusFailureReason.MessagingEntityNotFound, with AutoProvision having no bearing on
        // it since it only governs provisioning at startup, not receive-side recovery.
        var reported = await loggerProvider.WaitForEntryAsync(
            e => e.Exception is ServiceBusException { Reason: ServiceBusFailureReason.MessagingEntityNotFound },
            s_timeout);
        Assert.True(reported, "the processor never reported MessagingEntityNotFound for the deleted queue");
    }
}
