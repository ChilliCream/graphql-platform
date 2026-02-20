using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using HotChocolate.Execution;
using HotChocolate.Execution.Errors;
using HotChocolate.Features;
using HotChocolate.Fusion.Diagnostics;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public sealed class SourceSchemaRequestDispatcherTests : FusionTestBase
{
    [Fact]
    public async Task ExecuteAsync_Ungrouped_Request_Remains_Pass_Through()
    {
        // arrange
        var client = new TestSourceSchemaClient();
        await using var context = CreateContext(client);
        var request = CreateRequest(nodeId: 1);

        // act
        var response = await context.SourceSchemaScheduler.ExecuteAsync(
            request,
            CancellationToken.None);

        // assert
        Assert.Equal(1, client.ExecuteCount);
        Assert.Equal(0, client.ExecuteBatchCount);
        Assert.IsType<TestResponse>(response);
    }

    [Fact]
    public async Task ExecuteAsync_Grouped_Waits_Until_All_Submitted_Or_Skipped()
    {
        // arrange
        var client = new TestSourceSchemaClient();
        await using var context = CreateContext(client);

        context.SourceSchemaDispatcher.RegisterGroup(7, [1, 2, 3]);

        // act
        var first = context.SourceSchemaScheduler.ExecuteAsync(
                CreateRequest(nodeId: 1, groupId: 7),
                CancellationToken.None)
            .AsTask();
        var second = context.SourceSchemaScheduler.ExecuteAsync(
                CreateRequest(nodeId: 2, groupId: 7),
                CancellationToken.None)
            .AsTask();

        await Task.Delay(50);
        Assert.False(first.IsCompleted);
        Assert.False(second.IsCompleted);

        context.SourceSchemaDispatcher.SkipNode(3);

        await first.WaitAsync(TimeSpan.FromSeconds(2));
        await second.WaitAsync(TimeSpan.FromSeconds(2));

        // assert
        Assert.Equal(0, client.ExecuteCount);
        Assert.Equal(1, client.ExecuteBatchCount);
    }

    [Fact]
    public async Task ExecuteAsync_Cascaded_Skip_Does_Not_Deadlock()
    {
        // arrange
        var client = new TestSourceSchemaClient();
        await using var context = CreateContext(client);

        context.SourceSchemaDispatcher.RegisterGroup(11, [10, 11, 12]);

        // act
        var pending = context.SourceSchemaScheduler.ExecuteAsync(
                CreateRequest(nodeId: 10, groupId: 11),
                CancellationToken.None)
            .AsTask();

        context.SourceSchemaDispatcher.SkipNode(11);
        context.SourceSchemaDispatcher.SkipNode(12);

        var response = await pending.WaitAsync(TimeSpan.FromSeconds(2));

        // assert
        Assert.Equal(1, client.ExecuteCount);
        Assert.Equal(0, client.ExecuteBatchCount);
        Assert.IsType<TestResponse>(response);
    }

    [Fact]
    public async Task ExecuteAsync_Mixed_Grouped_And_Ungrouped_Dispatches_Both_Paths()
    {
        // arrange
        var client = new TestSourceSchemaClient();
        await using var context = CreateContext(client);

        context.SourceSchemaDispatcher.RegisterGroup(13, [1, 2]);

        // act
        var ungrouped = await context.SourceSchemaScheduler.ExecuteAsync(
            CreateRequest(nodeId: 9),
            CancellationToken.None);

        var groupedFirst = context.SourceSchemaScheduler.ExecuteAsync(
                CreateRequest(nodeId: 1, groupId: 13),
                CancellationToken.None)
            .AsTask();
        var groupedSecond = context.SourceSchemaScheduler.ExecuteAsync(
                CreateRequest(nodeId: 2, groupId: 13),
                CancellationToken.None)
            .AsTask();

        await groupedFirst.WaitAsync(TimeSpan.FromSeconds(2));
        await groupedSecond.WaitAsync(TimeSpan.FromSeconds(2));

        // assert
        Assert.IsType<TestResponse>(ungrouped);
        Assert.Equal(1, client.ExecuteCount);
        Assert.Equal(1, client.ExecuteBatchCount);
    }

    [Fact]
    public async Task ExecuteAsync_Grouped_Batch_Correlates_Responses_Positionally()
    {
        // arrange
        var client = new TestSourceSchemaClient();
        var response1 = new TestResponse("batch-1");
        var response2 = new TestResponse("batch-2");
        client.OnBatch = _ => [response1, response2];

        await using var context = CreateContext(client);
        context.SourceSchemaDispatcher.RegisterGroup(17, [1, 2]);

        // act — node 1 submits first, node 2 second → responses[0] goes to node 1, responses[1] to node 2
        var firstTask = context.SourceSchemaScheduler.ExecuteAsync(
                CreateRequest(nodeId: 1, groupId: 17),
                CancellationToken.None)
            .AsTask();
        var secondTask = context.SourceSchemaScheduler.ExecuteAsync(
                CreateRequest(nodeId: 2, groupId: 17),
                CancellationToken.None)
            .AsTask();

        var first = await firstTask.WaitAsync(TimeSpan.FromSeconds(2));
        var second = await secondTask.WaitAsync(TimeSpan.FromSeconds(2));

        // assert — positional correlation: first submitted gets responses[0], second gets responses[1]
        Assert.Same(response1, first);
        Assert.Same(response2, second);
    }

    [Fact]
    public async Task Abort_Releases_Grouped_Waiters()
    {
        // arrange
        var client = new TestSourceSchemaClient();
        await using var context = CreateContext(client);
        context.SourceSchemaDispatcher.RegisterGroup(19, [1, 2]);

        var pending = context.SourceSchemaScheduler.ExecuteAsync(
                CreateRequest(nodeId: 1, groupId: 19),
                CancellationToken.None)
            .AsTask();

        // act
        context.SourceSchemaDispatcher.Abort(new InvalidOperationException("aborted"));

        // assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await pending.WaitAsync(TimeSpan.FromSeconds(2)));
    }

    [Fact]
    public async Task ExecuteAsync_Subscription_Request_Does_Not_Use_Group_Dispatch()
    {
        // arrange
        var client = new TestSourceSchemaClient();
        await using var context = CreateContext(client);
        context.SourceSchemaDispatcher.RegisterGroup(23, [1, 2]);

        var request = CreateRequest(
            nodeId: 1,
            groupId: 23,
            operationType: OperationType.Subscription);

        // act
        var response = await context.SourceSchemaScheduler.ExecuteAsync(
            request,
            CancellationToken.None);

        // assert
        Assert.Equal(1, client.ExecuteCount);
        Assert.Equal(0, client.ExecuteBatchCount);
        Assert.IsType<TestResponse>(response);
    }

    [Fact]
    public async Task ExecuteAsync_Grouped_When_ClientResolver_Throws_All_Waiters_Are_Faulted()
    {
        // arrange
        await using var context = CreateContext(new FailingSourceSchemaClient());
        context.SourceSchemaDispatcher.RegisterGroup(29, [1, 2]);

        var first = context.SourceSchemaScheduler.ExecuteAsync(
                CreateRequest(nodeId: 1, groupId: 29),
                CancellationToken.None)
            .AsTask();
        var second = context.SourceSchemaScheduler.ExecuteAsync(
                CreateRequest(nodeId: 2, groupId: 29),
                CancellationToken.None)
            .AsTask();

        // act/assert
        var firstError = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await first.WaitAsync(TimeSpan.FromSeconds(2)));
        var secondError = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await second.WaitAsync(TimeSpan.FromSeconds(2)));

        Assert.Equal("resolver-failed", firstError.Message);
        Assert.Equal("resolver-failed", secondError.Message);
    }

    private static OperationPlanContext CreateContext(ISourceSchemaClient client)
    {
        var schemaServices = new ServiceCollection()
            .AddSingleton<INodeIdParser>(new TestNodeIdParser())
            .AddSingleton<IFusionExecutionDiagnosticEvents>(
                NoopFusionExecutionDiagnosticEvents.Instance)
            .AddSingleton<IErrorHandler>(new DefaultErrorHandler([]))
            .BuildServiceProvider();

        var schemaFeatures = new FeatureCollection();
        schemaFeatures.Set(new FusionOptions());
        schemaFeatures.Set(new FusionRequestOptions());

        var doc = ComposeSchemaDocument("type Query { hello: String }");
        var schema = FusionSchemaDefinition.Create(doc, schemaServices, schemaFeatures);

        var plan = PlanOperation(schema, "{ hello }");

        var requestServices = new ServiceCollection()
            .AddSingleton<ISourceSchemaClientScopeFactory>(
                new TestClientScopeFactory(client))
            .BuildServiceProvider();

        var request = OperationRequest.FromId("test-doc-id");

        var requestContext = new PooledRequestContext();
        requestContext.Initialize(
            schema, 0, request, 0, requestServices, CancellationToken.None);
        requestContext.VariableValues = [VariableValueCollection.Empty];

        return new OperationPlanContext(
            requestContext,
            VariableValueCollection.Empty,
            plan,
            new CancellationTokenSource());
    }

    private static SourceSchemaClientRequest CreateRequest(
        int nodeId,
        int? groupId = null,
        OperationType operationType = OperationType.Query)
        => new()
        {
            Node = new TestExecutionNode(nodeId),
            SchemaName = "schema",
            BatchingGroupId = groupId,
            OperationType = operationType,
            OperationSourceText = "query { __typename }",
            Variables = []
        };

    private sealed class TestNodeIdParser : INodeIdParser
    {
        public bool TryParseTypeName(
            string id,
            [NotNullWhen(true)] out string? typeName)
        {
            typeName = null;
            return false;
        }
    }

    private sealed class TestClientScopeFactory(
        ISourceSchemaClient client) : ISourceSchemaClientScopeFactory
    {
        public ISourceSchemaClientScope CreateScope(ISchemaDefinition schemaDefinition)
            => new TestClientScope(client);
    }

    private sealed class TestClientScope(
        ISourceSchemaClient client) : ISourceSchemaClientScope
    {
        public ISourceSchemaClient GetClient(
            string schemaName, OperationType operationType)
            => client;

        public ValueTask DisposeAsync()
            => ValueTask.CompletedTask;
    }

    private sealed class TestSourceSchemaClient : ISourceSchemaClient
    {
        public int ExecuteCount { get; private set; }

        public int ExecuteBatchCount { get; private set; }

        public Func<ImmutableArray<SourceSchemaClientRequest>,
            ImmutableArray<SourceSchemaClientResponse>>? OnBatch { get; set; }

        public ValueTask<SourceSchemaClientResponse> ExecuteAsync(
            OperationPlanContext context,
            SourceSchemaClientRequest request,
            CancellationToken cancellationToken)
        {
            ExecuteCount++;

            var response = new TestResponse($"single-{request.Node.Id}");
            return new ValueTask<SourceSchemaClientResponse>(response);
        }

        public ValueTask<ImmutableArray<SourceSchemaClientResponse>> ExecuteBatchAsync(
            OperationPlanContext context,
            ImmutableArray<SourceSchemaClientRequest> requests,
            CancellationToken cancellationToken)
        {
            ExecuteBatchCount++;

            if (OnBatch is not null)
            {
                return new ValueTask<ImmutableArray<SourceSchemaClientResponse>>(
                    OnBatch(requests));
            }

            var builder = ImmutableArray.CreateBuilder<SourceSchemaClientResponse>(
                requests.Length);

            for (var i = 0; i < requests.Length; i++)
            {
                builder.Add(new TestResponse($"batch-{requests[i].Node.Id}"));
            }

            return new ValueTask<ImmutableArray<SourceSchemaClientResponse>>(
                builder.MoveToImmutable());
        }

        public ValueTask DisposeAsync()
            => ValueTask.CompletedTask;
    }

    private sealed class FailingSourceSchemaClient : ISourceSchemaClient
    {
        public ValueTask<SourceSchemaClientResponse> ExecuteAsync(
            OperationPlanContext context,
            SourceSchemaClientRequest request,
            CancellationToken cancellationToken)
            => throw new InvalidOperationException("resolver-failed");

        public ValueTask<ImmutableArray<SourceSchemaClientResponse>> ExecuteBatchAsync(
            OperationPlanContext context,
            ImmutableArray<SourceSchemaClientRequest> requests,
            CancellationToken cancellationToken)
            => throw new InvalidOperationException("resolver-failed");

        public ValueTask DisposeAsync()
            => ValueTask.CompletedTask;
    }

    private sealed class TestResponse(string id) : SourceSchemaClientResponse
    {
        public string Id { get; } = id;

        public override Uri Uri => new("http://localhost/graphql");

        public override string ContentType => "application/json";

        public override bool IsSuccessful => true;

        public override async IAsyncEnumerable<SourceSchemaResult> ReadAsResultStreamAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield break;
        }

        public override void Dispose()
        {
        }
    }

    private sealed class TestExecutionNode(int id) : ExecutionNode
    {
        public override int Id { get; } = id;

        public override ExecutionNodeType Type => ExecutionNodeType.Operation;

        public override ReadOnlySpan<ExecutionNodeCondition> Conditions => [];

        protected override ValueTask<ExecutionStatus> OnExecuteAsync(
            OperationPlanContext context,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
