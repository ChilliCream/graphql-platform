using HotChocolate.Execution;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution;

public class FusionRequestExecutorManagerUpdateTests : FusionTestBase
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task Update_Should_KeepPreviousExecutorAndApplyLaterConfiguration_When_ExecutorCreationThrows()
    {
        // arrange
        var failCreation = false;
        var listener = new UpdateFailureListener();
        var configProvider = new TestFusionConfigurationProvider(CreateConfiguration("field"));

        var services =
            new ServiceCollection()
                .AddGraphQLGateway()
                .AddConfigurationProvider(_ => configProvider)
                .AddDiagnosticEventListener(_ => listener)
                .ConfigureSchemaServices((_, _) =>
                {
                    if (failCreation)
                    {
                        throw new InvalidOperationException("creation failed");
                    }
                })
                .Services
                .BuildServiceProvider();

        var manager = services.GetRequiredService<FusionRequestExecutorManager>();
        var initialExecutor = await manager.GetExecutorAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        var created = ObserveCreatedExecutors(manager);

        // act
        failCreation = true;
        configProvider.UpdateConfiguration(CreateConfiguration("rejected"));
        var failure = await listener.Failure.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        var executorAfterFailure = await manager.GetExecutorAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        failCreation = false;
        configProvider.UpdateConfiguration(CreateConfiguration("accepted"));
        var executorAfterRecovery = await created.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        var currentExecutor = await manager.GetExecutorAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        Assert.Same(initialExecutor, executorAfterFailure);
        Assert.Equal(ISchemaDefinition.DefaultName, failure.SchemaName);
        Assert.Equal("creation failed", failure.Exception.Message);
        Assert.True(executorAfterRecovery.Schema.QueryType.Fields.ContainsName("accepted"));
        Assert.Same(executorAfterRecovery, currentExecutor);
    }

    [Fact]
    public async Task Update_Should_ApplyRetriedConfiguration_When_IdenticalConfigurationFailedBefore()
    {
        // arrange
        var failWarmup = false;
        var listener = new UpdateFailureListener();
        var configProvider = new TestFusionConfigurationProvider(CreateConfiguration("field"));

        var services =
            new ServiceCollection()
                .AddGraphQLGateway()
                .AddConfigurationProvider(_ => configProvider)
                .AddDiagnosticEventListener(_ => listener)
                .AddWarmupTask((_, _) =>
                {
                    return failWarmup
                        ? throw new InvalidOperationException("warmup failed")
                        : Task.CompletedTask;
                })
                .Services
                .BuildServiceProvider();

        var manager = services.GetRequiredService<FusionRequestExecutorManager>();
        var initialExecutor = await manager.GetExecutorAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        var created = ObserveCreatedExecutors(manager);

        // act
        failWarmup = true;
        configProvider.UpdateConfiguration(CreateConfiguration("retried"));
        var failure = await listener.Failure.WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        failWarmup = false;
        configProvider.UpdateConfiguration(CreateConfiguration("retried"));
        var executorAfterRetry = await created.WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal("warmup failed", failure.Exception.Message);
        Assert.NotSame(initialExecutor, executorAfterRetry);
        Assert.True(executorAfterRetry.Schema.QueryType.Fields.ContainsName("retried"));
    }

    [Fact]
    public async Task Update_Should_DisposeRejectedExecutorAndConfiguration_When_WarmupThrows()
    {
        // arrange
        var failWarmup = false;
        DisposalProbe? rejectedProbe = null;
        var listener = new UpdateFailureListener();
        var configProvider = new TestFusionConfigurationProvider(CreateConfiguration("field"));

        var services =
            new ServiceCollection()
                .AddGraphQLGateway()
                .AddConfigurationProvider(_ => configProvider)
                .AddDiagnosticEventListener(_ => listener)
                .ConfigureSchemaServices((_, s) => s.AddSingleton<DisposalProbe>())
                .AddWarmupTask((executor, _) =>
                {
                    if (failWarmup)
                    {
                        rejectedProbe = executor.Schema.Services.GetRequiredService<DisposalProbe>();
                        throw new InvalidOperationException("warmup failed");
                    }

                    return Task.CompletedTask;
                })
                .Services
                .BuildServiceProvider();

        var manager = services.GetRequiredService<FusionRequestExecutorManager>();
        await manager.GetExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);
        var rejectedConfiguration = CreateConfiguration("rejected");

        // act
        failWarmup = true;
        configProvider.UpdateConfiguration(rejectedConfiguration);
        await listener.Failure.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        await rejectedProbe!.Disposed.WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        // assert
        Assert.Throws<ObjectDisposedException>(() => rejectedConfiguration.Settings.Document.RootElement.ValueKind);
    }

    private static Task<IRequestExecutor> ObserveCreatedExecutors(FusionRequestExecutorManager manager)
    {
        var created = new TaskCompletionSource<IRequestExecutor>(TaskCreationOptions.RunContinuationsAsynchronously);

        manager.Subscribe(new RequestExecutorEventObserver(@event =>
        {
            if (@event.Type == RequestExecutorEventType.Created)
            {
                created.TrySetResult(@event.Executor);
            }
        }));

        return created.Task;
    }

    private static FusionConfiguration CreateConfiguration(string fieldName)
        => CreateFusionConfiguration(
            $$"""
            type Query {
              {{fieldName}}: String!
            }
            """);

    private sealed class UpdateFailureListener : FusionExecutionDiagnosticEventListener
    {
        private readonly TaskCompletionSource<(string SchemaName, Exception Exception)> _failure =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<(string SchemaName, Exception Exception)> Failure => _failure.Task;

        public override void ExecutorUpdateFailed(string schemaName, Exception exception)
            => _failure.TrySetResult((schemaName, exception));
    }

    private sealed class DisposalProbe : IDisposable
    {
        private readonly TaskCompletionSource _disposed = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Disposed => _disposed.Task;

        public void Dispose() => _disposed.TrySetResult();
    }
}
