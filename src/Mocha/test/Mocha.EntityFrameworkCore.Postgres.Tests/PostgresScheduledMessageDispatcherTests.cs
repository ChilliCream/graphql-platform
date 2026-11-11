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
