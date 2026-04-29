using System.Collections.Concurrent;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Middlewares;
using Mocha.Transport.AzureServiceBus.Features;
using Mocha.Transport.AzureServiceBus.Tests.Helpers;

namespace Mocha.Transport.AzureServiceBus.Tests.Behaviors;

[Collection("AzureServiceBus")]
public class SessionReceiveTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(60);
    private readonly AzureServiceBusFixture _fixture;

    public SessionReceiveTests(AzureServiceBusFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Consumer_Should_ReceiveMessages_When_QueueRequiresSession()
    {
        // arrange
        var capture = new SessionMessageCapture();
        var ctx = _fixture.CreateTestContext();
        var queueName = ctx.QueueName("session-recv");
        await using var bus = await new ServiceCollection()
            .AddSingleton(capture)
            .AddMessageBus()
            .AddConsumer<SessionCapturingConsumer>()
            .AddMessage<SessionOrder>(d => d.UseAzureServiceBusSessionId<SessionOrder>(m => m.SessionId))
            .AddAzureServiceBus(t =>
            {
                t.ConnectionString(ctx.ConnectionString);
                t.DeclareQueue(queueName).WithRequiresSession();
                t.Endpoint("session-recv-ep").Consumer<SessionCapturingConsumer>().Queue(queueName);
            })
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        for (var i = 0; i < 3; i++)
        {
            await messageBus.PublishAsync(
                new SessionOrder { SessionId = "S-1", OrderId = $"ORD-{i}" },
                CancellationToken.None);
        }

        // assert
        Assert.True(
            await capture.WaitAsync(s_timeout, expectedCount: 3),
            "Expected 3 session-bound messages to be delivered to the consumer");
        Assert.Equal(3, capture.Records.Count);
        Assert.All(capture.Records, r => Assert.Equal("S-1", r.SessionId));
    }

    [Fact]
    public async Task Consumer_Should_PreserveOrder_When_MessagesShareSessionId()
    {
        // arrange
        var capture = new SessionMessageCapture();
        var ctx = _fixture.CreateTestContext();
        var queueName = ctx.QueueName("session-order");
        await using var bus = await new ServiceCollection()
            .AddSingleton(capture)
            .AddMessageBus()
            .AddConsumer<SessionCapturingConsumer>()
            .AddMessage<SessionOrder>(d => d.UseAzureServiceBusSessionId<SessionOrder>(m => m.SessionId))
            .AddAzureServiceBus(t =>
            {
                t.ConnectionString(ctx.ConnectionString);
                t.DeclareQueue(queueName).WithRequiresSession();
                t.Endpoint("session-order-ep").Consumer<SessionCapturingConsumer>().Queue(queueName);
            })
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        var sentIds = Enumerable.Range(0, 5).Select(i => $"ORD-{i}").ToArray();
        foreach (var orderId in sentIds)
        {
            await messageBus.PublishAsync(
                new SessionOrder { SessionId = "S-2", OrderId = orderId },
                CancellationToken.None);
        }

        // assert
        Assert.True(
            await capture.WaitAsync(s_timeout, expectedCount: sentIds.Length),
            "Expected all session-bound messages to be delivered");
        var receivedIds = capture.Records.Select(r => r.OrderId).ToArray();
        Assert.Equal(sentIds, receivedIds);
    }

    [Fact]
    public async Task AcknowledgementMiddleware_Should_NotThrow_When_HandlerOnSessionQueueAlreadyDeadLettered()
    {
        // arrange - mirrors the non-session test: the handler dead-letters via the SDK; the
        // outer Complete in the ack middleware then catches the resulting MessageLockLost /
        // SessionLockLost and treats it as already-settled. Two observable signals prove the
        // path: (1) message lands in the DLQ; (2) processorErrors does not contain a lock-lost
        // exception that escaped to ProcessErrorAsync.
        var processorErrors = new ConcurrentBag<Exception>();
        var ctx = _fixture.CreateTestContext();
        var queueName = ctx.QueueName("session-idemp");
        await using var bus = await new ServiceCollection()
            .AddSingleton(processorErrors)
            .AddMessageBus()
            .AddConsumer<SessionDeadLetteringConsumer>()
            .AddMessage<SessionOrder>(d => d.UseAzureServiceBusSessionId<SessionOrder>(m => m.SessionId))
            .AddAzureServiceBus(t =>
            {
                t.ConnectionString(ctx.ConnectionString);
                t.DeclareQueue(queueName).WithRequiresSession();
                t.Endpoint("session-idemp-ep").Consumer<SessionDeadLetteringConsumer>().Queue(queueName);
            })
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await messageBus.PublishAsync(
            new SessionOrder { SessionId = "S-DL", OrderId = "DL-1" },
            CancellationToken.None);

        // assert
        var dlqMessage = await ReceiveFromDeadLetterAsync(ctx.ConnectionString, queueName, s_timeout);
        Assert.NotNull(dlqMessage);
        Assert.Equal("InvalidPayload", dlqMessage!.DeadLetterReason);

        Assert.DoesNotContain(
            processorErrors,
            e =>
                e is ServiceBusException sbe
                && (
                    sbe.Reason == ServiceBusFailureReason.MessageLockLost
                    || sbe.Reason == ServiceBusFailureReason.SessionLockLost));
    }

    [Fact]
    public async Task Handler_Should_AccessSessionState_When_OnSessionEndpoint()
    {
        // arrange - the first message stores session state; the second reads it back.
        var capture = new SessionStateCapture();
        var ctx = _fixture.CreateTestContext();
        var queueName = ctx.QueueName("session-state");
        await using var bus = await new ServiceCollection()
            .AddSingleton(capture)
            .AddMessageBus()
            .AddConsumer<SessionStateConsumer>()
            .AddMessage<SessionOrder>(d => d.UseAzureServiceBusSessionId<SessionOrder>(m => m.SessionId))
            .AddAzureServiceBus(t =>
            {
                t.ConnectionString(ctx.ConnectionString);
                t.DeclareQueue(queueName).WithRequiresSession();
                t.Endpoint("session-state-ep").Consumer<SessionStateConsumer>().Queue(queueName);
            })
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await messageBus.PublishAsync(
            new SessionOrder { SessionId = "S-STATE", OrderId = "first" },
            CancellationToken.None);
        await messageBus.PublishAsync(
            new SessionOrder { SessionId = "S-STATE", OrderId = "second" },
            CancellationToken.None);

        // assert - both invocations ran and the second observed the state written by the first.
        Assert.True(
            await capture.WaitAsync(s_timeout, expectedCount: 2),
            "Expected both session-bound messages to be delivered to the consumer");
        Assert.Null(capture.HandlerException);
        Assert.Equal("paid", capture.SecondReadValue);
    }

    [Fact]
    public async Task EndpointStartup_Should_Throw_When_SessionKnobsSetOnNonSessionQueue()
    {
        // arrange - the queue does NOT have WithRequiresSession; the endpoint sets a session-only knob.
        var ctx = _fixture.CreateTestContext();
        var queueName = ctx.QueueName("session-misconf");
        var act = () =>
            new ServiceCollection()
                .AddMessageBus()
                .AddConsumer<SessionCapturingConsumer>()
                .AddAzureServiceBus(t =>
                {
                    t.ConnectionString(ctx.ConnectionString);
                    t.DeclareQueue(queueName);
                    t.Endpoint("session-misconf-ep")
                        .Consumer<SessionCapturingConsumer>()
                        .Queue(queueName)
                        .WithMaxConcurrentCallsPerSession(2);
                })
                .BuildTestBusAsync();

        // act + assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await using var bus = await act();
        });
        Assert.Contains(nameof(IAzureServiceBusReceiveEndpointDescriptor.WithMaxConcurrentCallsPerSession), ex.Message);
        Assert.Contains(queueName, ex.Message);
    }

    [Fact]
    public async Task Handler_Should_AccessSessionId_When_OnSessionEndpoint()
    {
        // arrange
        var capture = new SessionMessageCapture();
        var ctx = _fixture.CreateTestContext();
        var queueName = ctx.QueueName("session-id");
        await using var bus = await new ServiceCollection()
            .AddSingleton(capture)
            .AddMessageBus()
            .AddConsumer<SessionCapturingConsumer>()
            .AddMessage<SessionOrder>(d => d.UseAzureServiceBusSessionId<SessionOrder>(m => m.SessionId))
            .AddAzureServiceBus(t =>
            {
                t.ConnectionString(ctx.ConnectionString);
                t.DeclareQueue(queueName).WithRequiresSession();
                t.Endpoint("session-id-ep").Consumer<SessionCapturingConsumer>().Queue(queueName);
            })
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await messageBus.PublishAsync(
            new SessionOrder { SessionId = "S-ID-1", OrderId = "id-test" },
            CancellationToken.None);

        // assert
        Assert.True(await capture.WaitAsync(s_timeout, expectedCount: 1));
        var record = Assert.Single(capture.Records);
        Assert.Equal("S-ID-1", record.SessionId);
    }

    [Fact]
    public async Task GetAzureServiceBusEventArgs_Should_Throw_When_OnSessionEndpoint()
    {
        // arrange - a consumer that calls GetAzureServiceBusEventArgs() on a session endpoint
        // should fail because the non-session args are unavailable.
        var capture = new ExceptionCapture();
        var ctx = _fixture.CreateTestContext();
        var queueName = ctx.QueueName("session-throws");
        await using var bus = await new ServiceCollection()
            .AddSingleton(capture)
            .AddMessageBus()
            .AddConsumer<NonSessionEventArgsCallingConsumer>()
            .AddMessage<SessionOrder>(d => d.UseAzureServiceBusSessionId<SessionOrder>(m => m.SessionId))
            .AddAzureServiceBus(t =>
            {
                t.ConnectionString(ctx.ConnectionString);
                t.DeclareQueue(queueName).WithRequiresSession().WithMaxDeliveryCount(1);
                t.Endpoint("session-throws-ep").Consumer<NonSessionEventArgsCallingConsumer>().Queue(queueName);
            })
            .BuildTestBusAsync();

        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await messageBus.PublishAsync(
            new SessionOrder { SessionId = "S-THROW", OrderId = "throw-test" },
            CancellationToken.None);

        // assert
        Assert.True(await capture.WaitAsync(s_timeout));
        var ex = Assert.IsType<InvalidOperationException>(capture.LastException);
        Assert.Contains("session-bound", ex.Message);
        Assert.Contains(nameof(AzureServiceBusContextExtensions.GetAzureServiceBusSessionEventArgs), ex.Message);
    }

    private static async Task<ServiceBusReceivedMessage?> ReceiveFromDeadLetterAsync(
        string connectionString,
        string queueName,
        TimeSpan timeout)
    {
        await using var client = new ServiceBusClient(connectionString);
        await using var receiver = client.CreateReceiver(
            queueName,
            new ServiceBusReceiverOptions { SubQueue = SubQueue.DeadLetter });

        return await receiver.ReceiveMessageAsync(timeout);
    }

    public sealed class SessionOrder
    {
        public required string SessionId { get; init; }
        public required string OrderId { get; init; }
    }

    public sealed record SessionDeliveryRecord(string SessionId, string OrderId);

    public sealed class SessionMessageCapture
    {
        private readonly SemaphoreSlim _semaphore = new(0);
        public ConcurrentQueue<SessionDeliveryRecord> Records { get; } = new();

        public void Record(string sessionId, string orderId)
        {
            Records.Enqueue(new SessionDeliveryRecord(sessionId, orderId));
            _semaphore.Release();
        }

        public async Task<bool> WaitAsync(TimeSpan timeout, int expectedCount)
        {
            for (var i = 0; i < expectedCount; i++)
            {
                if (!await _semaphore.WaitAsync(timeout))
                {
                    return false;
                }
            }

            return true;
        }
    }

    public sealed class SessionStateCapture
    {
        private readonly SemaphoreSlim _semaphore = new(0);
        public string? SecondReadValue { get; set; }
        public Exception? HandlerException { get; set; }
        public int Calls { get; private set; }

        public void Signal()
        {
            Calls++;
            _semaphore.Release();
        }

        public async Task<bool> WaitAsync(TimeSpan timeout, int expectedCount)
        {
            for (var i = 0; i < expectedCount; i++)
            {
                if (!await _semaphore.WaitAsync(timeout))
                {
                    return false;
                }
            }

            return true;
        }
    }

    public sealed class ExceptionCapture
    {
        private readonly SemaphoreSlim _semaphore = new(0);
        public Exception? LastException { get; private set; }

        public void Record(Exception ex)
        {
            LastException = ex;
            _semaphore.Release();
        }

        public Task<bool> WaitAsync(TimeSpan timeout) => _semaphore.WaitAsync(timeout);
    }

    public sealed class SessionCapturingConsumer(SessionMessageCapture capture) : IConsumer<SessionOrder>
    {
        public ValueTask ConsumeAsync(IConsumeContext<SessionOrder> context)
        {
            var args = context.GetAzureServiceBusSessionEventArgs();
            capture.Record(args.SessionId, context.Message.OrderId);
            return default;
        }
    }

    public sealed class SessionDeadLetteringConsumer : IConsumer<SessionOrder>
    {
        public async ValueTask ConsumeAsync(IConsumeContext<SessionOrder> context)
        {
            var args = context.GetAzureServiceBusSessionEventArgs();
            await args.DeadLetterMessageAsync(
                args.Message,
                deadLetterReason: "InvalidPayload",
                deadLetterErrorDescription: $"Missing customer id for {context.Message.OrderId}");
        }
    }

    public sealed class SessionStateConsumer(SessionStateCapture capture) : IConsumer<SessionOrder>
    {
        public async ValueTask ConsumeAsync(IConsumeContext<SessionOrder> context)
        {
            var args = context.GetAzureServiceBusSessionEventArgs();

            try
            {
                if (capture.Calls == 0)
                {
                    await args.SetSessionStateAsync(BinaryData.FromString("paid"), context.CancellationToken);
                }
                else
                {
                    var data = await args.GetSessionStateAsync(context.CancellationToken);
                    capture.SecondReadValue = data?.ToString();
                }
            }
            catch (Exception ex)
            {
                capture.HandlerException = ex;
                throw;
            }
            finally
            {
                capture.Signal();
            }
        }
    }

    public sealed class NonSessionEventArgsCallingConsumer(ExceptionCapture capture) : IConsumer<SessionOrder>
    {
        public ValueTask ConsumeAsync(IConsumeContext<SessionOrder> context)
        {
            try
            {
                _ = context.GetAzureServiceBusEventArgs();
            }
            catch (Exception ex)
            {
                capture.Record(ex);
                throw;
            }

            return default;
        }
    }
}

/// <summary>
/// Pure unit tests for the session-receive surface. These do not need the Squadron emulator and
/// run in the default (non-collection) test context.
/// </summary>
public class SessionReceiveUnitTests
{
    [Fact]
    public void GetAzureServiceBusSessionEventArgs_Should_Throw_When_OnNonSessionEndpoint()
    {
        // arrange - a context with no Azure Service Bus feature should fail to surface session args.
        var context = new ReceiveContext();

        // act & assert
        var ex = Assert.Throws<InvalidOperationException>(() => context.GetAzureServiceBusSessionEventArgs());
        Assert.Contains("session-bound", ex.Message);
    }

    [Fact]
    public void ReceiveFeature_Should_NotLeakArgsAcrossDispatches_When_Reused()
    {
        // arrange - exercise the pooled-feature invariant: SetSession then Reset then SetNonSession
        // must leave no trace of the prior session args.
        var feature = new AzureServiceBusReceiveFeature();

        var sessionMessage = ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: BinaryData.FromString("session"),
            messageId: "sess-1",
            sessionId: "S-1");
        var sessionArgs = new ProcessSessionMessageEventArgs(
            sessionMessage,
            (ServiceBusSessionReceiver)null!,
            CancellationToken.None);

        var nonSessionMessage = ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: BinaryData.FromString("non-session"),
            messageId: "non-1");
        var nonSessionArgs = new ProcessMessageEventArgs(
            nonSessionMessage,
            (ServiceBusReceiver)null!,
            CancellationToken.None);

        // act + assert - first dispatch is session
        feature.SetSession(sessionArgs);
        Assert.Same(sessionMessage, feature.Message);
        Assert.Same(sessionArgs, feature.ProcessSessionMessageEventArgs);
        Assert.Null(feature.ProcessMessageEventArgs);

        // reset returns the feature to a clean state
        feature.Reset();
        Assert.Null(feature.ProcessMessageEventArgs);
        Assert.Null(feature.ProcessSessionMessageEventArgs);

        // second dispatch is non-session
        feature.SetNonSession(nonSessionArgs);
        Assert.Same(nonSessionMessage, feature.Message);
        Assert.Same(nonSessionArgs, feature.ProcessMessageEventArgs);
        Assert.Null(feature.ProcessSessionMessageEventArgs);
    }
}
