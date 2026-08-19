using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mocha.Transport.AzureServiceBus.Tests.Helpers;

namespace Mocha.Transport.AzureServiceBus.Tests;

/// <summary>
/// Covers how <see cref="AzureServiceBusReceiveEndpoint"/> classifies and logs processor errors
/// reported by the Azure Service Bus SDK, and confirms the endpoint no longer runs a
/// repository-owned recovery loop. Processor retry and connection recovery are the SDK's
/// responsibility; the endpoint only classifies and logs the error.
/// </summary>
public sealed class ProcessorErrorHandlingTests
{
    [Theory]
    [InlineData(ServiceBusFailureReason.ServiceCommunicationProblem)]
    [InlineData(ServiceBusFailureReason.ServiceBusy)]
    [InlineData(ServiceBusFailureReason.ServiceTimeout)]
    [InlineData(ServiceBusFailureReason.MessageLockLost)]
    [InlineData(ServiceBusFailureReason.SessionLockLost)]
    public async Task OnProcessorError_Should_LogWarningWithEntityPath_When_ReasonIsTransient(
        ServiceBusFailureReason reason)
    {
        // arrange
        var provider = CapturingLoggerProvider.For<AzureServiceBusReceiveEndpoint>();
        var exception = new ServiceBusException("transient failure", reason);
        var (client, bus) = await CreateStartedBusAsync(provider);
        await using var busScope = bus;
        var processor = client.CreatedProcessors.Single(p => p.QueueName == "orders").Processor;

        // act
        await processor.RaiseProcessErrorAsync(CreateArgs(client, exception));

        // assert
        var entry = Assert.Single(provider.Entries, e => e.Exception is not null);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Same(exception, entry.Exception);
        Assert.Equal("orders", GetValue(entry.State, "EntityPath"));
    }

    [Fact]
    public async Task OnProcessorError_Should_LogWarningWithEntityPath_When_ExceptionIsOperationCanceled()
    {
        // arrange - cancellation is treated as a transient/recoverable condition
        var provider = CapturingLoggerProvider.For<AzureServiceBusReceiveEndpoint>();
        var exception = new OperationCanceledException("cancelled");
        var (client, bus) = await CreateStartedBusAsync(provider);
        await using var busScope = bus;
        var processor = client.CreatedProcessors.Single(p => p.QueueName == "orders").Processor;

        // act
        await processor.RaiseProcessErrorAsync(CreateArgs(client, exception));

        // assert
        var entry = Assert.Single(provider.Entries, e => e.Exception is not null);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Same(exception, entry.Exception);
        Assert.Equal("orders", GetValue(entry.State, "EntityPath"));
    }

    [Fact]
    public async Task OnProcessorError_Should_LogErrorWithEntityPath_When_ServiceBusReasonIsNotTransient()
    {
        // arrange
        var provider = CapturingLoggerProvider.For<AzureServiceBusReceiveEndpoint>();
        var exception = new ServiceBusException("general failure", ServiceBusFailureReason.GeneralError);
        var (client, bus) = await CreateStartedBusAsync(provider);
        await using var busScope = bus;
        var processor = client.CreatedProcessors.Single(p => p.QueueName == "orders").Processor;

        // act
        await processor.RaiseProcessErrorAsync(CreateArgs(client, exception));

        // assert
        var entry = Assert.Single(provider.Entries, e => e.Exception is not null);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Same(exception, entry.Exception);
        Assert.Equal("orders", GetValue(entry.State, "EntityPath"));
    }

    [Fact]
    public async Task OnProcessorError_Should_LogErrorWithEntityPath_When_ExceptionIsNotServiceBusException()
    {
        // arrange
        var provider = CapturingLoggerProvider.For<AzureServiceBusReceiveEndpoint>();
        var exception = new InvalidOperationException("unexpected failure");
        var (client, bus) = await CreateStartedBusAsync(provider);
        await using var busScope = bus;
        var processor = client.CreatedProcessors.Single(p => p.QueueName == "orders").Processor;

        // act
        await processor.RaiseProcessErrorAsync(CreateArgs(client, exception));

        // assert
        var entry = Assert.Single(provider.Entries, e => e.Exception is not null);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Same(exception, entry.Exception);
        Assert.Equal("orders", GetValue(entry.State, "EntityPath"));
    }

    [Fact]
    public async Task OnProcessorError_Should_NotRestartProcessor_When_MessagingEntityNotFoundReported()
    {
        // arrange - MessagingEntityNotFound previously triggered a repository-owned recovery loop
        // that stopped and restarted the processor (removed in favor of the SDK's own recovery);
        // the endpoint must only log the error now, regardless of how many times it repeats.
        var provider = CapturingLoggerProvider.For<AzureServiceBusReceiveEndpoint>();
        var exception = new ServiceBusException("queue deleted", ServiceBusFailureReason.MessagingEntityNotFound);
        var (client, bus) = await CreateStartedBusAsync(provider);
        await using var busScope = bus;
        var created = client.CreatedProcessors.Single(p => p.QueueName == "orders");

        // act
        await created.Processor.RaiseProcessErrorAsync(CreateArgs(client, exception));
        await created.Processor.RaiseProcessErrorAsync(CreateArgs(client, exception));

        // assert - no additional processor was created for the queue, and the existing one was
        // never stopped or restarted in response to the error
        Assert.Equal(1, client.CreatedProcessors.Count(p => p.QueueName == "orders"));
        Assert.Equal(0, created.Processor.StopProcessingCallCount);
        Assert.Equal(1, created.Processor.StartProcessingCallCount);
    }

    private static ProcessErrorEventArgs CreateArgs(FakeServiceBusClient client, Exception exception) =>
        new(exception, ServiceBusErrorSource.Receive, client.FullyQualifiedNamespace, "orders", CancellationToken.None);

    private static async Task<(FakeServiceBusClient Client, TestBus Bus)> CreateStartedBusAsync(
        CapturingLoggerProvider loggerProvider)
    {
        var client = new FakeServiceBusClient(_ => null);
        var services = new ServiceCollection();
        services.AddSingleton<ServiceBusClient>(client);
        services.AddSingleton<ServiceBusAdministrationClient>(new FakeServiceBusAdministrationClient());
        services.AddLogging(b => b.AddProvider(loggerProvider));
        var builder = services
            .AddMessageBus()
            .AddConsumer<NoOpConsumer>()
            .AddAzureServiceBus(t =>
            {
                t.AutoProvision(false);
                t.Endpoint("orders-ep").Consumer<NoOpConsumer>().Queue("orders");
            });
        var bus = await builder.BuildTestBusAsync();
        return (client, bus);
    }

    private static object? GetValue(IReadOnlyList<KeyValuePair<string, object?>> state, string key) =>
        state.Single(pair => pair.Key == key).Value;

    private sealed class NoOpConsumer : IConsumer<OrderCreated>
    {
        public ValueTask ConsumeAsync(IConsumeContext<OrderCreated> context) => default;
    }
}
