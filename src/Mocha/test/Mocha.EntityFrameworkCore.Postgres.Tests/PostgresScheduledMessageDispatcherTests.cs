using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Mocha.EntityFrameworkCore.Postgres.Tests.Helpers;
using Mocha.Middlewares;
using Mocha.Scheduling;
using Mocha.Transport.InMemory;

namespace Mocha.EntityFrameworkCore.Postgres.Tests;

public sealed class PostgresScheduledMessageDispatcherTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan s_stopGracePeriod = TimeSpan.FromMilliseconds(500);
    private static CancellationToken TestToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task ProcessAsync_Should_DeleteMessage_When_TransportAcceptedItBeforeTheStop()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var accepted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var dispatch = new ScriptedDispatch(async attempt =>
        {
            await attempt.SendAsync();
            accepted.TrySetResult();
            await attempt.WaitForCancellationAsync(Task.Delay(s_timeout));
        });
        await using var provider = await CreateProviderAsync(connectionString, dispatch);
        await MessageRows.InsertScheduledAsync(connectionString, CreateEnvelope(provider, "A"), TestToken);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestToken);
        var processing = provider.GetRequiredService<ScheduledMessageDispatcher>().ProcessAsync(cancellation.Token);
        await accepted.Task.WaitAsync(s_timeout, TestToken);

        // act
        await cancellation.CancelAsync();
        await processing.WaitAsync(s_timeout, TestToken);
        var remaining = await MessageRows.CountScheduledAsync(connectionString, TestToken);

        // assert
        Assert.Equal(0, remaining);
        Assert.Equal(["A"], dispatch.Accepted);
    }

    [Fact]
    public async Task StopAsync_Should_WaitUntilMessageIsDeleted_When_TransportAcceptedItBeforeTheStop()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var accepted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var dispatch = new ScriptedDispatch(async attempt =>
        {
            await attempt.SendAsync();
            accepted.TrySetResult();
            await attempt.WaitForCancellationAsync(Task.Delay(s_timeout));
            cancelled.TrySetResult();
            await release.Task;
        });
        await using var provider = await CreateProviderAsync(connectionString, dispatch);
        await MessageRows.InsertScheduledAsync(connectionString, CreateEnvelope(provider, "A"), TestToken);
        var host = await MessagingTestHost.StartAsync(provider, TestToken);
        await accepted.Task.WaitAsync(s_timeout, TestToken);

        // act
        // the dispatch returns only after the stop had the time to return without it
        var stopping = host.DisposeAsync().AsTask();
        await cancelled.Task.WaitAsync(s_timeout, TestToken);
        var stoppedBeforeDispatchReturned =
            await Task.WhenAny(stopping, Task.Delay(s_stopGracePeriod, TestToken)) == stopping;
        release.TrySetResult();
        await stopping.WaitAsync(s_timeout, TestToken);
        var remaining = await MessageRows.CountScheduledAsync(connectionString, TestToken);

        // assert
        Assert.False(stoppedBeforeDispatchReturned);
        Assert.Equal(0, remaining);
        Assert.Equal(["A"], dispatch.Accepted);
    }

    [Fact]
    public async Task ProcessAsync_Should_DispatchAgainAfterOneAndTwoSeconds_When_AttemptsKeepFailing()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var watch = new Stopwatch();
        var attempts = new ConcurrentQueue<TimeSpan>();
        var dispatch = new ScriptedDispatch(attempt =>
        {
            attempts.Enqueue(watch.Elapsed);

            return attempt.Number <= 2
                ? throw new InvalidOperationException("The broker refused the message.")
                : attempt.SendAsync();
        });
        await using var provider = await CreateProviderAsync(connectionString, dispatch);
        await MessageRows.InsertScheduledAsync(connectionString, CreateEnvelope(provider, "A"), TestToken);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestToken);

        // act
        watch.Start();
        var processing = provider.GetRequiredService<ScheduledMessageDispatcher>().ProcessAsync(cancellation.Token);
        var remaining = await MessageRows.WaitForScheduledCountAsync(connectionString, 0, s_timeout, TestToken);
        await cancellation.CancelAsync();
        await processing.WaitAsync(s_timeout, TestToken);

        // assert
        // A message is due one second after its first counted attempt and two seconds after its second one.
        TimeSpan[] times = [.. attempts];
        Assert.Equal(0, remaining);
        Assert.Equal(3, times.Length);
        Assert.InRange(times[1] - times[0], TimeSpan.FromSeconds(0.5), TimeSpan.FromSeconds(2.5));
        Assert.InRange(times[2] - times[1], TimeSpan.FromSeconds(1.5), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task ProcessAsync_Should_NotCountAttempts_When_TransportIsUnavailable()
    {
        // arrange
        const int unavailableAttempts = 3;
        var connectionString = await fixture.CreateDatabaseAsync();
        var watch = new Stopwatch();
        var attempts = new ConcurrentQueue<TimeSpan>();
        var available = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var resume = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var dispatch = new ScriptedDispatch(async attempt =>
        {
            attempts.Enqueue(watch.Elapsed);

            if (attempt.Number <= unavailableAttempts)
            {
                throw new TransportUnavailableException("The broker is unreachable.", null);
            }

            available.TrySetResult();
            await resume.Task.WaitAsync(attempt.CancellationToken);
            await attempt.SendAsync();
        });
        await using var provider = await CreateProviderAsync(connectionString, dispatch);
        var id = await MessageRows.InsertScheduledAsync(connectionString, CreateEnvelope(provider, "A"), TestToken);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestToken);

        // act
        watch.Start();
        var processing = provider.GetRequiredService<ScheduledMessageDispatcher>().ProcessAsync(cancellation.Token);
        await available.Task.WaitAsync(s_timeout, TestToken);
        var timesSentAfterOutage = await MessageRows.GetScheduledTimesSentAsync(connectionString, id, TestToken);
        resume.TrySetResult();
        var remaining = await MessageRows.WaitForScheduledCountAsync(connectionString, 0, s_timeout, TestToken);
        await cancellation.CancelAsync();
        await processing.WaitAsync(s_timeout, TestToken);

        // assert
        // The three pauses double from at least 100 milliseconds, which adds up to at least 0.7 seconds.
        TimeSpan[] times = [.. attempts];
        Assert.Equal(0, timesSentAfterOutage);
        Assert.Equal(0, remaining);
        Assert.Equal(["A"], dispatch.Accepted);
        Assert.InRange(times[unavailableAttempts] - times[0], TimeSpan.FromSeconds(0.5), s_timeout);
    }

    [Fact]
    public async Task ProcessAsync_Should_NotCountAttempt_When_HostStopsDuringDispatch()
    {
        // arrange
        var connectionString = await fixture.CreateDatabaseAsync();
        var dispatching = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var dispatch = new ScriptedDispatch(async attempt =>
        {
            dispatching.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, attempt.CancellationToken);
        });
        await using var provider = await CreateProviderAsync(connectionString, dispatch);
        var id = await MessageRows.InsertScheduledAsync(connectionString, CreateEnvelope(provider, "A"), TestToken);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestToken);
        var processing = provider.GetRequiredService<ScheduledMessageDispatcher>().ProcessAsync(cancellation.Token);
        await dispatching.Task.WaitAsync(s_timeout, TestToken);

        // act
        await cancellation.CancelAsync();
        await processing.WaitAsync(s_timeout, TestToken);
        var timesSent = await MessageRows.GetScheduledTimesSentAsync(connectionString, id, TestToken);

        // assert
        Assert.Equal(0, timesSent);
        Assert.Equal(1, dispatch.AttemptsOf("A"));
    }

    private static MessageEnvelope CreateEnvelope(IServiceProvider provider, string messageId)
    {
        var runtime = provider.GetRequiredService<IMessagingRuntime>();
        var type = runtime.GetMessageType(typeof(ScheduledEvent));
        return new MessageEnvelope
        {
            MessageId = messageId,
            MessageType = type.Identity,
            ContentType = MessageContentType.Json.Value,
            DestinationAddress = runtime.GetPublishEndpoint(type).Address.ToString(),
            Body = JsonSerializer.SerializeToUtf8Bytes(new ScheduledEvent(messageId))
        };
    }

    private static async Task<ServiceProvider> CreateProviderAsync(string connectionString, ScriptedDispatch dispatch)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<TestDbContext>(o => o.UseTestNpgsql(connectionString));
        var builder = services.AddMessageBus();
        builder.AddEntityFramework<TestDbContext>(ef => ef.UsePostgresScheduling());
        builder.AddInMemory(t =>
        {
            t.DeclareQueue("scheduled");
            t.DispatchEndpoint("scheduled").ToQueue("scheduled").Publish<ScheduledEvent>();
        });
        builder.ConfigureMessageBus(b => b.UseDispatch(dispatch.Middleware, after: "Serialization"));

        var provider = services.BuildServiceProvider();
        await ((MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>()).StartAsync(TestToken);
        await using var scope = provider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<TestDbContext>().Database.EnsureCreatedAsync(TestToken);
        return provider;
    }

    public sealed record ScheduledEvent(string Name);
}
