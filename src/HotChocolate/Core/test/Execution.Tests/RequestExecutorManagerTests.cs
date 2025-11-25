using HotChocolate.Execution.Configuration;
using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HotChocolate.Execution;

public class RequestExecutorManagerTests
{
    [Fact]
    public async Task GetExecutorAsync_Throws_If_Schema_Does_Not_Exist()
    {
        // arrange
        var manager =
            new ServiceCollection()
                .AddGraphQL("some-name")
                .AddQueryType(d => d.Field("foo").Resolve(""))
                .Services
                .BuildServiceProvider()
                .GetRequiredService<RequestExecutorManager>();

        // act
        var act = async () => await manager.GetExecutorAsync("unknown-name");

        // assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(act);
        Assert.Equal($"The requested schema 'unknown-name' does not exist.", exception.Message);
    }

    [Fact]
    public async Task Executor_Should_Only_Be_Switched_Once_It_Is_Warmed_Up()
    {
        // arrange
        var warmupResetEvent = new ManualResetEventSlim(true);
        var executorEvictedResetEvent = new ManualResetEventSlim(false);
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var manager = new ServiceCollection()
            .AddGraphQL()
            .AddWarmupTask((_, _) =>
            {
                warmupResetEvent.Wait(cts.Token);

                return Task.CompletedTask;
            })
            .AddQueryType(d => d.Field("foo").Resolve(""))
            .Services.BuildServiceProvider()
            .GetRequiredService<RequestExecutorManager>();

        manager.Subscribe(new RequestExecutorEventObserver(@event =>
        {
            if (@event.Type == RequestExecutorEventType.Evicted)
            {
                executorEvictedResetEvent.Set();
            }
        }));

        // act
        // assert
        var initialExecutor = await manager.GetExecutorAsync();
        warmupResetEvent.Reset();

        manager.EvictExecutor();

        var executorAfterEviction = await manager.GetExecutorAsync();

        Assert.Same(initialExecutor, executorAfterEviction);

        warmupResetEvent.Set();
        executorEvictedResetEvent.Wait(cts.Token);
        var executorAfterWarmup = await manager.GetExecutorAsync();

        Assert.NotSame(initialExecutor, executorAfterWarmup);

        cts.Dispose();
    }

    [Fact]
    public async Task WarmupTasks_Are_Applied_Correct_Number_Of_Times()
    {
        // arrange
        var warmups = 0;
        var executorEvictedResetEvent = new ManualResetEventSlim(false);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var manager = new ServiceCollection()
            .AddGraphQL()
            .AddWarmupTask((_, _) =>
            {
                warmups++;
                return Task.CompletedTask;
            })
            .AddQueryType(d => d.Field("foo").Resolve(""))
            .Services.BuildServiceProvider()
            .GetRequiredService<RequestExecutorManager>();

        manager.Subscribe(new RequestExecutorEventObserver(@event =>
        {
            if (@event.Type == RequestExecutorEventType.Evicted)
            {
                executorEvictedResetEvent.Set();
            }
        }));

        // act
        // assert
        var initialExecutor = await manager.GetExecutorAsync(cancellationToken: cts.Token);

        Assert.Equal(1, warmups);

        manager.EvictExecutor();
        executorEvictedResetEvent.Wait(cts.Token);

        var executorAfterEviction = await manager.GetExecutorAsync(cancellationToken: cts.Token);

        Assert.NotSame(initialExecutor, executorAfterEviction);
        Assert.Equal(2, warmups);
    }

    [Fact]
    public async Task Calling_GetExecutorAsync_Multiple_Times_Only_Creates_One_Executor()
    {
        // arrange
        var manager = new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(d => d.Field("foo").Resolve(""))
            .Services.BuildServiceProvider()
            .GetRequiredService<RequestExecutorManager>();

        // act
        var executor1Task = Task.Run(async () => await manager.GetExecutorAsync());
        var executor2Task = Task.Run(async () => await manager.GetExecutorAsync());

        var executor1 = await executor1Task;
        var executor2 = await executor2Task;

        // assert
        Assert.Same(executor1, executor2);
    }

    [Fact]
    public async Task Executor_Resolution_Should_Be_Parallel()
    {
        // arrange
        var schema1CreationResetEvent = new ManualResetEventSlim(false);
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var services = new ServiceCollection();
        services
            .AddGraphQL("schema1")
            .AddQueryType(d =>
            {
                schema1CreationResetEvent.Wait(cts.Token);
                d.Field("foo").Resolve("");
            });
        services
            .AddGraphQL("schema2")
            .AddQueryType(d => d.Field("foo").Resolve(""));
        var provider = services.BuildServiceProvider();
        var manager = provider.GetRequiredService<RequestExecutorManager>();

        // act
        var executor1Task = Task.Run(async () => await manager.GetExecutorAsync("schema1"), cts.Token);
        var executor2Task = Task.Run(async () => await manager.GetExecutorAsync("schema2"), cts.Token);

        // assert
        await executor2Task;

        schema1CreationResetEvent.Set();

        await executor1Task;

        cts.Dispose();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Ensure_Executor_Is_Created_During_Startup(bool lazyInitialization)
    {
        // arrange
        var typeModule = new TriggerableTypeModule();
        var executorCreated = false;
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var createdResetEvent = new ManualResetEventSlim(false);

        var services = new ServiceCollection();
        services
            .AddGraphQLServer()
            .ModifyOptions(o => o.LazyInitialization = lazyInitialization)
            .AddTypeModule(_ => typeModule)
            .AddQueryType(d => d.Field("foo").Resolve(""));
        var provider = services.BuildServiceProvider();
        var executorManager = provider.GetRequiredService<RequestExecutorManager>();
        var warmupService = provider.GetRequiredService<IHostedService>();

        executorManager.Subscribe(new RequestExecutorEventObserver(@event =>
        {
            if (@event.Type == RequestExecutorEventType.Created)
            {
                executorCreated = true;
                createdResetEvent.Set();
            }
        }));

        // act
        await warmupService.StartAsync(cts.Token);

        // assert
        if (lazyInitialization)
        {
            Assert.False(executorCreated);
        }
        else
        {
            createdResetEvent.Wait(cts.Token);
            Assert.True(executorCreated);
        }
    }

    [Fact]
    public async Task WarmupTask_Should_Be_Able_To_Access_Schema_And_Regular_Services()
    {
        // arrange
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var services = new ServiceCollection();
        services.AddSingleton<SomeService>();
        services
            .AddGraphQLServer()
            .AddWarmupTask<CustomWarmupTask>()
            .AddApplicationService<SomeService>()
            .AddQueryType(d => d.Field("foo").Resolve(""));
        var provider = services.BuildServiceProvider();
        var manager = provider.GetRequiredService<RequestExecutorManager>();

        // act
        var executor = await manager.GetExecutorAsync(cancellationToken: cts.Token);

        // assert
        Assert.NotNull(executor);

        cts.Dispose();
    }

#pragma warning disable CS9113 // Parameter is unread.
    private sealed class CustomWarmupTask(IDocumentCache documentCache, SomeService service) : IRequestExecutorWarmupTask
#pragma warning restore CS9113 // Parameter is unread.
    {
        public bool ApplyOnlyOnStartup => false;

        public Task WarmupAsync(IRequestExecutor executor, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private class SomeService;

    private sealed class TriggerableTypeModule : TypeModule
    {
        public void TriggerChange() => OnTypesChanged();
    }
}
