using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Language;
using System.Runtime.CompilerServices;

namespace HotChocolate.Fusion.Execution;

public sealed class SourceSchemaRequestDispatcherTests
{
    [Fact]
    public async Task ExecuteAsync_Ungrouped_Request_Remains_Pass_Through()
    {
        // arrange
        var client = new TestSourceSchemaClient();
        var dispatcher = new SourceSchemaRequestDispatcher((_, _, _) => client);
        var request = CreateRequest(nodeId: 1);

        // act
        var response = await dispatcher.ExecuteAsync(null!, request, CancellationToken.None);

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
        var dispatcher = new SourceSchemaRequestDispatcher((_, _, _) => client);

        dispatcher.RegisterGroup(7, [1, 2, 3]);

        // act
        var first = dispatcher.ExecuteAsync(
                null!,
                CreateRequest(nodeId: 1, groupId: 7),
                CancellationToken.None)
            .AsTask();
        var second = dispatcher.ExecuteAsync(
                null!,
                CreateRequest(nodeId: 2, groupId: 7),
                CancellationToken.None)
            .AsTask();

        await Task.Delay(50);
        Assert.False(first.IsCompleted);
        Assert.False(second.IsCompleted);

        dispatcher.SkipNode(3);

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
        var dispatcher = new SourceSchemaRequestDispatcher((_, _, _) => client);

        dispatcher.RegisterGroup(11, [10, 11, 12]);

        // act
        var pending = dispatcher.ExecuteAsync(
                null!,
                CreateRequest(nodeId: 10, groupId: 11),
                CancellationToken.None)
            .AsTask();

        dispatcher.SkipNode(11);
        dispatcher.SkipNode(12);

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
        var dispatcher = new SourceSchemaRequestDispatcher((_, _, _) => client);

        dispatcher.RegisterGroup(13, [1, 2]);

        // act
        var ungrouped = await dispatcher.ExecuteAsync(
            null!,
            CreateRequest(nodeId: 9),
            CancellationToken.None);

        var groupedFirst = dispatcher.ExecuteAsync(
                null!,
                CreateRequest(nodeId: 1, groupId: 13),
                CancellationToken.None)
            .AsTask();
        var groupedSecond = dispatcher.ExecuteAsync(
                null!,
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
    public async Task ExecuteAsync_Grouped_Batch_Correlates_Responses_By_NodeId()
    {
        // arrange
        var client = new TestSourceSchemaClient();
        var responses = new Dictionary<int, SourceSchemaClientResponse>
        {
            [1] = new TestResponse("batch-1"),
            [2] = new TestResponse("batch-2")
        };
        client.OnBatch = _ => responses;

        var dispatcher = new SourceSchemaRequestDispatcher((_, _, _) => client);
        dispatcher.RegisterGroup(17, [1, 2]);

        // act
        var firstTask = dispatcher.ExecuteAsync(
                null!,
                CreateRequest(nodeId: 1, groupId: 17),
                CancellationToken.None)
            .AsTask();
        var secondTask = dispatcher.ExecuteAsync(
                null!,
                CreateRequest(nodeId: 2, groupId: 17),
                CancellationToken.None)
            .AsTask();

        var first = await firstTask.WaitAsync(TimeSpan.FromSeconds(2));
        var second = await secondTask.WaitAsync(TimeSpan.FromSeconds(2));

        // assert
        Assert.Same(responses[1], first);
        Assert.Same(responses[2], second);
    }

    [Fact]
    public async Task Abort_Releases_Grouped_Waiters()
    {
        // arrange
        var client = new TestSourceSchemaClient();
        var dispatcher = new SourceSchemaRequestDispatcher((_, _, _) => client);
        dispatcher.RegisterGroup(19, [1, 2]);

        var pending = dispatcher.ExecuteAsync(
                null!,
                CreateRequest(nodeId: 1, groupId: 19),
                CancellationToken.None)
            .AsTask();

        // act
        dispatcher.Abort(new InvalidOperationException("aborted"));

        // assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await pending.WaitAsync(TimeSpan.FromSeconds(2)));
    }

    [Fact]
    public async Task ExecuteAsync_Subscription_Request_Does_Not_Use_Group_Dispatch()
    {
        // arrange
        var client = new TestSourceSchemaClient();
        var dispatcher = new SourceSchemaRequestDispatcher((_, _, _) => client);
        dispatcher.RegisterGroup(23, [1, 2]);

        var request = CreateRequest(
            nodeId: 1,
            groupId: 23,
            operationType: OperationType.Subscription);

        // act
        var response = await dispatcher.ExecuteAsync(
            null!,
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
        var dispatcher = new SourceSchemaRequestDispatcher((_, _, _) =>
            throw new InvalidOperationException("resolver-failed"));
        dispatcher.RegisterGroup(29, [1, 2]);

        var first = dispatcher.ExecuteAsync(
                null!,
                CreateRequest(nodeId: 1, groupId: 29),
                CancellationToken.None)
            .AsTask();
        var second = dispatcher.ExecuteAsync(
                null!,
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

    private sealed class TestSourceSchemaClient : ISourceSchemaClient
    {
        public int ExecuteCount { get; private set; }

        public int ExecuteBatchCount { get; private set; }

        public Func<SourceSchemaClientRequest, SourceSchemaClientResponse>? OnExecute { get; set; }

        public Func<IReadOnlyList<SourceSchemaClientRequest>, IReadOnlyDictionary<int, SourceSchemaClientResponse>>? OnBatch { get; set; }

        public ValueTask<SourceSchemaClientResponse> ExecuteAsync(
            OperationPlanContext context,
            SourceSchemaClientRequest request,
            CancellationToken cancellationToken)
        {
            ExecuteCount++;

            var response = OnExecute?.Invoke(request) ?? new TestResponse($"single-{request.Node.Id}");
            return new ValueTask<SourceSchemaClientResponse>(response);
        }

        public ValueTask<IReadOnlyDictionary<int, SourceSchemaClientResponse>> ExecuteBatchAsync(
            OperationPlanContext context,
            IReadOnlyList<SourceSchemaClientRequest> requests,
            CancellationToken cancellationToken)
        {
            ExecuteBatchCount++;

            if (OnBatch is not null)
            {
                return new ValueTask<IReadOnlyDictionary<int, SourceSchemaClientResponse>>(OnBatch(requests));
            }

            var responses = new Dictionary<int, SourceSchemaClientResponse>(requests.Count);

            for (var i = 0; i < requests.Count; i++)
            {
                responses[requests[i].Node.Id] = new TestResponse($"batch-{requests[i].Node.Id}");
            }

            return new ValueTask<IReadOnlyDictionary<int, SourceSchemaClientResponse>>(responses);
        }

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
