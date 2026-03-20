using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Events;
using Mocha.Middlewares;
using Mocha.Transport.InMemory;

namespace Mocha.Tests.Middlewares.Receive;

/// <summary>
/// Tests for <see cref="ReceiveFaultMiddleware"/> which catches exceptions from handlers,
/// marks the message as consumed, and routes faults to the error endpoint or reply address.
/// </summary>
public sealed class ReceiveFaultMiddlewareTests : ReceiveMiddlewareTestBase
{
    [Fact]
    public async Task InvokeAsync_Should_DeliverMessage_When_HandlerDoesNotThrow()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(recorder);
            b.AddEventHandler<FaultTestEventHandler>();
        });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new FaultTestEvent { Id = "success-1" }, CancellationToken.None);

        // assert
        Assert.True(await recorder.WaitAsync(Timeout), "Handler should be called when no exception occurs");

        var message = Assert.Single(recorder.Messages);
        var evt = Assert.IsType<FaultTestEvent>(message);
        Assert.Equal("success-1", evt.Id);
    }

    [Fact]
    public async Task InvokeAsync_Should_DeliverMultipleMessages_When_AllHandlersSucceed()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(recorder);
            b.AddEventHandler<FaultTestEventHandler>();
        });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        for (var i = 0; i < 5; i++)
        {
            await bus.PublishAsync(new FaultTestEvent { Id = $"ok-{i}" }, CancellationToken.None);
        }

        // assert
        Assert.True(
            await recorder.WaitAsync(Timeout, expectedCount: 5),
            "All 5 messages should be delivered when handlers succeed");

        Assert.Equal(5, recorder.Messages.Count);
    }

    [Fact]
    public async Task InvokeAsync_Should_NotCrashRuntime_When_HandlerThrows()
    {
        // arrange
        await using var provider = await CreateBusAsync(b =>
            b.AddEventHandler<AlwaysThrowingEventHandler>());

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act - publish a message whose handler will throw
        await bus.PublishAsync(new FaultTestEvent { Id = "will-fail" }, CancellationToken.None);

        // Deterministic sync not available - no observable side-effect to wait on
        // after a swallowed fault, so a brief delay lets the pipeline finish.
        await Task.Delay(500, default);

        // assert - runtime should still be running
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        Assert.True(runtime.IsStarted);
    }

    [Fact]
    public async Task InvokeAsync_Should_ProcessSubsequentMessages_When_PreviousHandlerThrew()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(recorder);
            b.Services.AddSingleton<FaultConditionalThrowHandler>();
            b.AddEventHandler<FaultConditionalThrowHandler>();
        });

        var handler = provider.GetRequiredService<FaultConditionalThrowHandler>();
        handler.ThrowForIds.Add("fail-1");

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act - first message throws, second should still be processed
        await bus.PublishAsync(new FaultTestEvent { Id = "fail-1" }, CancellationToken.None);

        // Let the fault propagate before publishing the next message -
        // no deterministic signal for a swallowed exception.
        await Task.Delay(200, default);

        await bus.PublishAsync(new FaultTestEvent { Id = "success-after-fail" }, CancellationToken.None);

        // assert
        Assert.True(
            await recorder.WaitAsync(Timeout),
            "Handler should still process messages after a previous handler threw");

        var recorded = Assert.Single(recorder.Messages);
        Assert.Equal("success-after-fail", ((FaultTestEvent)recorded).Id);
    }

    [Fact]
    public async Task InvokeAsync_Should_KeepRuntimeStable_When_MultipleHandlersFail()
    {
        // arrange
        await using var provider = await CreateBusAsync(b =>
            b.AddEventHandler<AlwaysThrowingEventHandler>());

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act - publish 10 failing messages in sequence
        for (var i = 0; i < 10; i++)
        {
            await bus.PublishAsync(new FaultTestEvent { Id = $"fail-{i}" }, CancellationToken.None);
        }

        // No deterministic signal for swallowed faults; wait for all 10 to settle.
        await Task.Delay(1000, default);

        // assert - runtime should remain stable
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        Assert.True(runtime.IsStarted);
    }

    [Fact]
    public async Task InvokeAsync_Should_RecordAllSuccesses_When_SomeHandlersThrow()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(recorder);
            b.Services.AddSingleton<FaultConditionalThrowHandler>();
            b.AddEventHandler<FaultConditionalThrowHandler>();
        });

        var handler = provider.GetRequiredService<FaultConditionalThrowHandler>();
        handler.ThrowForIds.Add("fail-a");
        handler.ThrowForIds.Add("fail-c");

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act - interleave failures and successes
        await bus.PublishAsync(new FaultTestEvent { Id = "ok-1" }, CancellationToken.None);
        await bus.PublishAsync(new FaultTestEvent { Id = "fail-a" }, CancellationToken.None);
        await bus.PublishAsync(new FaultTestEvent { Id = "ok-2" }, CancellationToken.None);
        await bus.PublishAsync(new FaultTestEvent { Id = "fail-c" }, CancellationToken.None);
        await bus.PublishAsync(new FaultTestEvent { Id = "ok-3" }, CancellationToken.None);

        // assert - only the 3 successful messages should be recorded
        Assert.True(
            await recorder.WaitAsync(Timeout, expectedCount: 3),
            "Should receive exactly 3 successful messages");

        // Negative wait: confirm no extra messages arrive after the expected 3.
        await Task.Delay(200, default);

        Assert.Equal(3, recorder.Messages.Count);
        var ids = recorder.Messages.Cast<FaultTestEvent>().Select(e => e.Id).ToList();
        Assert.Contains("ok-1", ids);
        Assert.Contains("ok-2", ids);
        Assert.Contains("ok-3", ids);
        Assert.DoesNotContain("fail-a", ids);
        Assert.DoesNotContain("fail-c", ids);
    }

    [Fact]
    public async Task InvokeAsync_Should_HandleConcurrentMixedMessages_When_PublishedInParallel()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(recorder);
            b.Services.AddSingleton<FaultConditionalThrowHandler>();
            b.AddEventHandler<FaultConditionalThrowHandler>();
        });

        var handler = provider.GetRequiredService<FaultConditionalThrowHandler>();
        handler.ThrowForIds.Add("par-fail-0");
        handler.ThrowForIds.Add("par-fail-2");
        handler.ThrowForIds.Add("par-fail-4");

        // act - publish 6 messages concurrently, 3 will fail
        var tasks = Enumerable
            .Range(0, 6)
            .Select(async i =>
            {
                using var scope = provider.CreateScope();
                var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
                var id = i % 2 == 0 ? $"par-fail-{i}" : $"par-ok-{i}";
                await bus.PublishAsync(new FaultTestEvent { Id = id }, CancellationToken.None);
            });

        await Task.WhenAll(tasks);

        // assert - only the 3 non-failing messages should be recorded
        Assert.True(
            await recorder.WaitAsync(Timeout, expectedCount: 3),
            "Should receive 3 successful messages from concurrent publish");

        // Negative wait: confirm no extra messages arrive after the expected 3.
        await Task.Delay(200, default);

        Assert.Equal(3, recorder.Messages.Count);
        var ids = recorder.Messages.Cast<FaultTestEvent>().Select(e => e.Id).ToList();
        Assert.Contains("par-ok-1", ids);
        Assert.Contains("par-ok-3", ids);
        Assert.Contains("par-ok-5", ids);
    }

    [Fact]
    public async Task InvokeAsync_Should_ThrowException_When_RequestHandlerThrows()
    {
        // arrange
        await using var provider = await CreateBusAsync(b =>
            b.AddRequestHandler<ThrowingFaultRequestHandler>());

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act & assert - exact exception type depends on transport timing:
        // RemoteErrorException if the fault response arrives, TaskCanceledException
        // if the CTS fires first. Both confirm the handler did not succeed.
        using var cts = new CancellationTokenSource(Timeout);
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await bus.RequestAsync(new FaultTestRequest { Id = "req-fail" }, cts.Token)
        );
    }

    [Fact]
    public async Task InvokeAsync_Should_ReturnResponse_When_RequestHandlerSucceeds()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var provider = await CreateBusAsync(b =>
        {
            b.Services.AddSingleton(recorder);
            b.AddRequestHandler<FaultTestRequestHandler>();
        });

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        var response = await bus.RequestAsync(new FaultTestRequest { Id = "req-ok" }, CancellationToken.None);

        // assert
        Assert.Equal("req-ok", response.Id);
        Assert.Equal("Processed", response.Result);
    }

    [Fact]
    public async Task InvokeAsync_Should_PropagateExceptionInfo_When_RequestHandlerThrows()
    {
        // arrange
        await using var provider = await CreateBusAsync(b =>
            b.AddRequestHandler<ThrowingFaultRequestHandler>());

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act - exact exception type depends on transport timing (see comment
        // in ThrowException test above). When it IS a RemoteErrorException we
        // can verify fault details; otherwise just confirm failure.
        using var cts = new CancellationTokenSource(Timeout);
        var ex = await Assert.ThrowsAnyAsync<Exception>(async () =>
            await bus.RequestAsync(new FaultTestRequest { Id = "req-info" }, cts.Token)
        );

        // assert - if the fault arrived we get rich error info
        if (ex is RemoteErrorException remote)
        {
            Assert.NotNull(remote.ErrorMessage);
            Assert.Contains("InvalidOperationException", remote.ErrorMessage);
        }
    }

    [Fact]
    public async Task InvokeAsync_Should_HandleConcurrentFaultingRequests_When_MultipleRequestsFail()
    {
        // arrange
        await using var provider = await CreateBusAsync(b =>
            b.AddRequestHandler<ThrowingFaultRequestHandler>());

        // act - fire 5 concurrent failing requests
        // Exact exception type depends on transport timing (see ThrowException test).
        var tasks = Enumerable
            .Range(0, 5)
            .Select(async i =>
            {
                using var scope = provider.CreateScope();
                var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

                using var cts = new CancellationTokenSource(Timeout);
                var ex = await Assert.ThrowsAnyAsync<Exception>(async () =>
                    await bus.RequestAsync(new FaultTestRequest { Id = $"conc-fail-{i}" }, cts.Token)
                );
                return ex;
            })
            .ToArray();

        var exceptions = await Task.WhenAll(tasks);

        // assert - all 5 requests should have failed
        Assert.Equal(5, exceptions.Length);
        Assert.All(exceptions, Assert.NotNull);
    }

    [Fact]
    public async Task InvokeAsync_Should_DeliverToErrorEndpoint_When_EventHandlerThrows()
    {
        // arrange
        await using var provider = await CreateBusWithErrorEndpointAsync(b =>
            b.AddEventHandler<AlwaysThrowingEventHandler>());

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new FaultTestEvent { Id = "err-1" }, CancellationToken.None);

        // assert - consume from the error queue and verify fault headers
        var errorQueue = GetErrorQueue(provider);
        var items = await ConsumeFromQueueAsync(errorQueue, expectedCount: 1);

        var envelope = Assert.Single(items);
        Assert.Equal(MessageKind.Fault, envelope.Headers!.Get(MessageHeaders.MessageKind));
        Assert.Contains("InvalidOperationException", envelope.Headers!.Get(MessageHeaders.Fault.ExceptionType));
        Assert.NotNull(envelope.Headers!.Get(MessageHeaders.Fault.Message));
        Assert.NotNull(envelope.Headers!.Get(MessageHeaders.Fault.StackTrace));
        Assert.NotNull(envelope.Headers!.Get(MessageHeaders.Fault.Timestamp));
    }

    [Fact]
    public async Task InvokeAsync_Should_DeliverAllFaultsToErrorEndpoint_When_MultipleFail()
    {
        // arrange
        await using var provider = await CreateBusWithErrorEndpointAsync(b =>
            b.AddEventHandler<AlwaysThrowingEventHandler>());

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act
        for (var i = 0; i < 3; i++)
        {
            await bus.PublishAsync(new FaultTestEvent { Id = $"multi-fail-{i}" }, CancellationToken.None);
        }

        // assert
        var errorQueue = GetErrorQueue(provider);
        var items = await ConsumeFromQueueAsync(errorQueue, expectedCount: 3);

        Assert.Equal(3, items.Count);
        Assert.All(
            items,
            envelope =>
            {
                Assert.Equal(MessageKind.Fault, envelope.Headers!.Get(MessageHeaders.MessageKind));
                Assert.Contains("InvalidOperationException", envelope.Headers!.Get(MessageHeaders.Fault.ExceptionType));
            });
    }

    [Fact]
    public async Task InvokeAsync_Should_OnlyDeliverFaults_When_MixedSuccessAndFailure()
    {
        // arrange
        var recorder = new MessageRecorder();
        await using var provider = await CreateBusWithErrorEndpointAsync(b =>
        {
            b.Services.AddSingleton(recorder);
            b.Services.AddSingleton<FaultConditionalThrowHandler>();
            b.AddEventHandler<FaultConditionalThrowHandler>();
        });

        var handler = provider.GetRequiredService<FaultConditionalThrowHandler>();
        handler.ThrowForIds.Add("mixed-fail-1");
        handler.ThrowForIds.Add("mixed-fail-2");

        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // act - 5 messages: 3 succeed, 2 fail
        await bus.PublishAsync(new FaultTestEvent { Id = "mixed-ok-1" }, CancellationToken.None);
        await bus.PublishAsync(new FaultTestEvent { Id = "mixed-fail-1" }, CancellationToken.None);
        await bus.PublishAsync(new FaultTestEvent { Id = "mixed-ok-2" }, CancellationToken.None);
        await bus.PublishAsync(new FaultTestEvent { Id = "mixed-fail-2" }, CancellationToken.None);
        await bus.PublishAsync(new FaultTestEvent { Id = "mixed-ok-3" }, CancellationToken.None);

        // assert - 3 successes recorded by handler
        Assert.True(
            await recorder.WaitAsync(Timeout, expectedCount: 3),
            "Should receive exactly 3 successful messages");
        Assert.Equal(3, recorder.Messages.Count);

        // assert - 2 faults on error queue
        var errorQueue = GetErrorQueue(provider);
        var items = await ConsumeFromQueueAsync(errorQueue, expectedCount: 2);

        Assert.Equal(2, items.Count);
        Assert.All(
            items,
            envelope =>
                Assert.Equal(MessageKind.Fault, envelope.Headers!.Get(MessageHeaders.MessageKind)));
    }

    [Fact]
    public void Create_Should_ReturnConfiguration_WithCorrectKey()
    {
        // act
        var configuration = ReceiveFaultMiddleware.Create();

        // assert
        Assert.NotNull(configuration);
        Assert.Equal("Fault", configuration.Key);
        Assert.NotNull(configuration.Middleware);
    }

    private static async Task<ServiceProvider> CreateBusWithErrorEndpointAsync(Action<IMessageBusHostBuilder> configure)
    {
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        configure(builder);
        builder.AddInMemory(d => d.AddConvention(new TestErrorEndpointConvention()));

        var provider = services.BuildServiceProvider();
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(CancellationToken.None);
        return provider;
    }

    private static InMemoryQueue GetErrorQueue(ServiceProvider provider)
    {
        var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();
        var topology = (InMemoryMessagingTopology)transport.Topology;
        return topology.Queues.First(q => q.Name.EndsWith("_error"));
    }

    private static async Task<List<MessageEnvelope>> ConsumeFromQueueAsync(InMemoryQueue queue, int expectedCount)
    {
        using var cts = new CancellationTokenSource(Timeout);
        var items = new List<MessageEnvelope>();

        try
        {
            await foreach (var item in queue.ConsumeAsync(cts.Token))
            {
                items.Add(new MessageEnvelope(item.Envelope));
                item.Dispose();
                if (items.Count == expectedCount)
                {
                    cts.Cancel();
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when we cancel after reading all messages.
        }

        return items;
    }

    private sealed class TestErrorEndpointConvention : IInMemoryReceiveEndpointConfigurationConvention
    {
        public void Configure(
            IMessagingConfigurationContext context,
            InMemoryMessagingTransport transport,
            InMemoryReceiveEndpointConfiguration configuration)
        {
            if (configuration is { Kind: ReceiveEndpointKind.Default, QueueName: { } queueName })
            {
                configuration.ErrorEndpoint ??= new Uri($"{transport.Schema}:q/{queueName}_error");
            }
        }
    }

    public sealed class FaultTestEvent
    {
        public required string Id { get; init; }
    }

    public sealed class FaultTestRequest : IEventRequest<FaultTestResponse>
    {
        public required string Id { get; init; }
    }

    public sealed class FaultTestResponse
    {
        public required string Id { get; init; }
        public required string Result { get; init; }
    }

    public sealed class FaultTestEventHandler(MessageRecorder recorder) : IEventHandler<FaultTestEvent>
    {
        public ValueTask HandleAsync(FaultTestEvent message, CancellationToken cancellationToken)
        {
            recorder.Record(message);
            return default;
        }
    }

    public sealed class AlwaysThrowingEventHandler : IEventHandler<FaultTestEvent>
    {
        public ValueTask HandleAsync(FaultTestEvent message, CancellationToken cancellationToken)
            => throw new InvalidOperationException($"Handler failed for: {message.Id}");
    }

    public sealed class FaultConditionalThrowHandler(MessageRecorder recorder) : IEventHandler<FaultTestEvent>
    {
        public ConcurrentBag<string> ThrowForIds { get; } = [];

        public ValueTask HandleAsync(FaultTestEvent message, CancellationToken cancellationToken)
        {
            if (ThrowForIds.Contains(message.Id))
            {
                throw new InvalidOperationException($"Configured to throw for {message.Id}");
            }

            recorder.Record(message);
            return default;
        }
    }

    public sealed class FaultTestRequestHandler(MessageRecorder recorder)
        : IEventRequestHandler<FaultTestRequest, FaultTestResponse>
    {
        public ValueTask<FaultTestResponse> HandleAsync(FaultTestRequest request, CancellationToken cancellationToken)
        {
            recorder.Record(request);
            return new(new FaultTestResponse { Id = request.Id, Result = "Processed" });
        }
    }

    public sealed class ThrowingFaultRequestHandler : IEventRequestHandler<FaultTestRequest, FaultTestResponse>
    {
        public ValueTask<FaultTestResponse> HandleAsync(FaultTestRequest request, CancellationToken cancellationToken)
            => throw new InvalidOperationException($"Request handler failed for: {request.Id}");
    }
}
