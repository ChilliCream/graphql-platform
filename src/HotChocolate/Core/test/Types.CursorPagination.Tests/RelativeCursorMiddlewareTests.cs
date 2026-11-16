using System.Text.Json;
using GreenDonut.Data;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types.Pagination;

public class RelativeCursorMiddlewareTests
{
    [Theory]
    [InlineData("classicDisabled", "forwardCursors")]
    [InlineData("classicDisabled", "backwardCursors")]
    [InlineData("pageDisabled", "forwardCursors")]
    [InlineData("pageDisabled", "backwardCursors")]
    [InlineData("streamDisabled", "forwardCursors")]
    [InlineData("streamDisabled", "backwardCursors")]
    public async Task Execute_Should_ReturnFieldErrorBeforeResolver_When_RelativeCursorsAreDisabled(
        string fieldName,
        string cursorField)
    {
        // arrange
        var probe = new Probe();
        var executor = await new ServiceCollection()
            .AddSingleton(probe)
            .AddGraphQLServer()
            .ModifyPagingOptions(options => options.EnableRelativeCursors = false)
            .AddQueryType<QueryType>()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            BuildQuery(fieldName, cursorField),
            TestContext.Current.CancellationToken);

        // assert
        var error = Assert.Single(result.ExpectOperationResult().Errors!);
        Assert.Equal(
            $"The field `{fieldName}` does not allow relative cursors. "
                + "Enable the `EnableRelativeCursors` option to select them.",
            error.Message);
        Assert.Equal(0, probe.Calls);
    }

    [Fact]
    public async Task Execute_Should_ReturnRelativeCursors_When_FieldEnablesThem()
    {
        // arrange
        var probe = new Probe();
        var executor = await new ServiceCollection()
            .AddSingleton(probe)
            .AddGraphQLServer()
            .ModifyPagingOptions(options => options.EnableRelativeCursors = false)
            .AddQueryType<QueryType>()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              classicEnabled(first: 1) {
                pageInfo {
                  forwardCursors { page }
                  backwardCursors { page }
                }
              }
              pageEnabled {
                pageInfo {
                  forwardCursors { page }
                  backwardCursors { page }
                }
              }
              streamEnabled {
                pageInfo {
                  forwardCursors { page }
                  backwardCursors { page }
                }
              }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        var operationResult = result.ExpectOperationResult();
        Assert.Empty(operationResult.Errors!);
        Assert.Equal(3, probe.Calls);

        using var document = JsonDocument.Parse(operationResult.ToJson());
        var pageInfo = document.RootElement
            .GetProperty("data")
            .GetProperty("pageEnabled")
            .GetProperty("pageInfo");
        Assert.Equal(3, pageInfo.GetProperty("forwardCursors").GetArrayLength());
        Assert.Equal(1, pageInfo.GetProperty("backwardCursors").GetArrayLength());
    }

    private static string BuildQuery(string fieldName, string cursorField)
    {
        var arguments = fieldName.StartsWith("classic", StringComparison.Ordinal)
            ? "(first: 1)"
            : string.Empty;
        return $"{{ {fieldName}{arguments} {{ pageInfo {{ {cursorField} {{ page }} }} }} }}";
    }

    private static Page<PageNode> CreatePage()
        => Page<PageNode>.Create(
            [new PageNode { Id = "first" }, new PageNode { Id = "second" }],
            hasNextPage: true,
            hasPreviousPage: true,
            createCursor: entry => entry.Node.Id,
            index: 2,
            requestedPageSize: 2,
            totalCount: 10);

    public sealed class Query
    {
        [UseConnection(EnableRelativeCursors = false)]
        public ClassicConnection GetClassicDisabled([Service] Probe probe)
        {
            probe.RecordCall();
            return new ClassicConnection();
        }

        [UseConnection(EnableRelativeCursors = false)]
        public PageConnection<PageNode> GetPageDisabled([Service] Probe probe)
        {
            probe.RecordCall();
            return new PageConnection<PageNode>(Page<PageNode>.Empty);
        }

        [UseConnection(EnableRelativeCursors = false)]
        public StreamPageConnection<StreamNode> GetStreamDisabled([Service] Probe probe)
        {
            probe.RecordCall();
            return new StreamPageConnection<StreamNode>(StreamPage<StreamNode>.Empty);
        }

        [UseConnection(EnableRelativeCursors = true)]
        public ClassicConnection GetClassicEnabled([Service] Probe probe)
        {
            probe.RecordCall();
            return new ClassicConnection();
        }

        [UseConnection(EnableRelativeCursors = true)]
        public PageConnection<PageNode> GetPageEnabled([Service] Probe probe)
        {
            probe.RecordCall();
            return new PageConnection<PageNode>(CreatePage());
        }

        [UseConnection(EnableRelativeCursors = true)]
        public StreamPageConnection<StreamNode> GetStreamEnabled([Service] Probe probe)
        {
            probe.RecordCall();
            return new StreamPageConnection<StreamNode>(StreamPage<StreamNode>.Empty);
        }
    }

    public sealed class QueryType : ObjectType<Query>
    {
        protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
        {
            descriptor
                .Field(t => t.GetClassicDisabled(default!))
                .Type<NonNullType<ClassicConnectionType>>()
                .AddPagingArguments();

            descriptor
                .Field(t => t.GetClassicEnabled(default!))
                .Type<NonNullType<ClassicConnectionType>>()
                .AddPagingArguments();

            descriptor
                .Field(t => t.GetPageDisabled(default!))
                .Type<NonNullType<PageNodeConnectionType>>()
                .AddPagingArguments();

            descriptor
                .Field(t => t.GetPageEnabled(default!))
                .Type<NonNullType<PageNodeConnectionType>>()
                .AddPagingArguments();

            descriptor
                .Field(t => t.GetStreamDisabled(default!))
                .AddPagingArguments();

            descriptor
                .Field(t => t.GetStreamEnabled(default!))
                .AddPagingArguments();
        }
    }

    public sealed class ClassicConnection
    {
        public PageInfo PageInfo => ConnectionPageInfo.Empty;
    }

    public sealed class ClassicConnectionType : ObjectType<ClassicConnection>
    {
        protected override void Configure(IObjectTypeDescriptor<ClassicConnection> descriptor)
        {
            descriptor.Name("ClassicConnection");
            descriptor
                .Field(connection => connection.PageInfo)
                .Type<NonNullType<ObjectType<PageInfo>>>();
        }
    }

    public sealed class PageNodeConnectionType : ObjectType<PageConnection<PageNode>>
    {
        protected override void Configure(IObjectTypeDescriptor<PageConnection<PageNode>> descriptor)
            => descriptor.Name("PageNodeConnection");
    }

    public sealed class PageNode
    {
        public string Id { get; init; } = string.Empty;
    }

    public sealed class StreamNode
    {
        public string Id => "stream";
    }

    public sealed class Probe
    {
        private int _calls;

        public int Calls => Volatile.Read(ref _calls);

        public void RecordCall()
            => Interlocked.Increment(ref _calls);
    }
}
