using System.Collections.Concurrent;
using System.Text.Json;
using GreenDonut.Data;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types.Pagination;

public class ConnectionFlagsCachingTests
{
    private const string Document =
        """
        query Items($withTotalCount: Boolean!) {
          items(first: 2) {
            totalCount @include(if: $withTotalCount)
            nodes
          }
        }
        """;

    [Fact]
    public async Task ConnectionFlags_Should_HaveTotalCount_When_LaterRequestSelectsIt()
    {
        // arrange
        var recorder = new FlagRecorder();
        var executor = await CreateExecutorAsync(recorder);

        // act
        await ExecuteAsync(executor, withTotalCount: false);
        var withTotalCount = await ExecuteAsync(executor, withTotalCount: true);

        // assert
        Assert.Collection(
            recorder.Flags,
            flags => Assert.Equal(ConnectionFlags.Nodes, flags),
            flags => Assert.Equal(ConnectionFlags.Nodes | ConnectionFlags.TotalCount, flags));

        Assert.Equal(2, GetTotalCount(withTotalCount));
    }

    [Fact]
    public async Task ConnectionFlags_Should_NotHaveTotalCount_When_LaterRequestOmitsIt()
    {
        // arrange
        var recorder = new FlagRecorder();
        var executor = await CreateExecutorAsync(recorder);

        // act
        var withTotalCount = await ExecuteAsync(executor, withTotalCount: true);
        var withoutTotalCount = await ExecuteAsync(executor, withTotalCount: false);

        // assert
        Assert.Collection(
            recorder.Flags,
            flags => Assert.Equal(ConnectionFlags.Nodes | ConnectionFlags.TotalCount, flags),
            flags => Assert.Equal(ConnectionFlags.Nodes, flags));

        Assert.Equal(2, GetTotalCount(withTotalCount));
        Assert.Null(GetTotalCount(withoutTotalCount));
    }

    private static async Task<IRequestExecutor> CreateExecutorAsync(FlagRecorder recorder)
        => await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(d =>
            {
                d.Name("Query");
                d.Field("items")
                    .Type<NonNullType<StringPageConnectionType>>()
                    .AddPagingArguments()
                    .Resolve(context =>
                    {
                        var flags = ConnectionFlagsHelper.GetConnectionFlags(context);
                        recorder.Flags.Enqueue(flags);

                        return new PageConnection<string>(
                            Page<string>.Create(
                                ["a", "b"],
                                hasNextPage: false,
                                hasPreviousPage: false,
                                item => item,
                                totalCount: flags.HasFlag(ConnectionFlags.TotalCount) ? 2 : null));
                    })
                    .Extend().OnBeforeCreate((_, configuration) => configuration.SetConnectionFlags());
            })
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

    private static async Task<OperationResult> ExecuteAsync(
        IRequestExecutor executor,
        bool withTotalCount)
    {
        var request = OperationRequestBuilder.New()
            .SetDocument(Document)
            .SetVariableValues(new Dictionary<string, object?> { ["withTotalCount"] = withTotalCount })
            .Build();

        var result = await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);
        var operationResult = Assert.IsType<OperationResult>(result);

        Assert.True(operationResult.Errors is null or { Count: 0 }, operationResult.ToJson());

        return operationResult;
    }

    private static int? GetTotalCount(OperationResult result)
    {
        using var document = JsonDocument.Parse(result.ToJson());
        var items = document.RootElement.GetProperty("data").GetProperty("items");
        return items.TryGetProperty("totalCount", out var totalCount) ? totalCount.GetInt32() : null;
    }

    public sealed class FlagRecorder
    {
        public ConcurrentQueue<ConnectionFlags> Flags { get; } = new();
    }

    public sealed class StringPageConnectionType : ObjectType<PageConnection<string>>
    {
        protected override void Configure(IObjectTypeDescriptor<PageConnection<string>> descriptor)
            => descriptor.Name("StringConnection");
    }
}
