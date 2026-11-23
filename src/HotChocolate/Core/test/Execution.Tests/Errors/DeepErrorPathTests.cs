using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution.Errors;

public class DeepErrorPathTests
{
    [Theory]
    [InlineData(20)]
    [InlineData(21)]
    [InlineData(33)]
    [InlineData(64)]
    [InlineData(100)]
    [InlineData(200)]
    public async Task ExecuteAsync_Should_ReportFullPath_When_FieldErrorIsNestedInObjects(int depth)
    {
        // arrange
        SkipWhenDepthIsNotSupported(depth);
        var executor = await CreateExecutorAsync();
        var query = BuildQuery("child", depth);

        // act
        var path = await ExecuteAndReadErrorPathAsync(executor, query);

        // assert
        Assert.Equal(Enumerable.Repeat("child", depth).Append("fail").ToArray(), path);
    }

    [Theory]
    [InlineData(12)]
    [InlineData(13)]
    [InlineData(33)]
    [InlineData(64)]
    [InlineData(100)]
    [InlineData(200)]
    public async Task ExecuteAsync_Should_ReportFullPath_When_FieldErrorIsNestedInLists(int depth)
    {
        // arrange
        SkipWhenDepthIsNotSupported(depth);
        var executor = await CreateExecutorAsync();
        var query = BuildQuery("children", depth);

        // act
        var path = await ExecuteAndReadErrorPathAsync(executor, query);

        // assert
        Assert.Equal(
            Enumerable.Range(0, depth).SelectMany(_ => new[] { "children", "0" }).Append("fail").ToArray(),
            path);
    }

    [Theory]
    [InlineData(21)]
    [InlineData(33)]
    public async Task ExecuteAsync_Should_ReportFullPath_When_DelegateResolverErrorIsNestedInObjects(int depth)
    {
        // arrange
        var executor = await CreateDelegateExecutorAsync();
        var query = BuildQuery("child", depth);

        // act
        var path = await ExecuteAndReadErrorPathAsync(executor, query);

        // assert
        Assert.Equal(Enumerable.Repeat("child", depth).Append("fail").ToArray(), path);
    }

    [Theory]
    [InlineData(13)]
    [InlineData(33)]
    public async Task ExecuteAsync_Should_ReportFullPath_When_DelegateResolverErrorIsNestedInLists(int depth)
    {
        // arrange
        var executor = await CreateDelegateExecutorAsync();
        var query = BuildQuery("children", depth);

        // act
        var path = await ExecuteAndReadErrorPathAsync(executor, query);

        // assert
        Assert.Equal(
            Enumerable.Range(0, depth).SelectMany(_ => new[] { "children", "0" }).Append("fail").ToArray(),
            path);
    }

    private static void SkipWhenDepthIsNotSupported(int depth)
    {
#if NET8_0
        Assert.SkipWhen(depth >= 150, "This nesting depth is not supported on net8.0.");
#endif
    }

    private static string BuildQuery(string field, int depth)
    {
        // The parser accepts 199 nested levels in one document, so a depth of 200 needs fragments.
        const int chunk = 20;
        var fragments = (depth - 1) / chunk + 1;
        var query = new StringBuilder("{ ...F0 }");

        for (var f = 0; f < fragments; f++)
        {
            var levels = Math.Min(chunk, depth - f * chunk);
            query.Append(" fragment F").Append(f).Append(" on Node { ");

            for (var i = 0; i < levels; i++)
            {
                query.Append(field).Append(" { ");
            }

            query.Append(f == fragments - 1 ? "fail" : $"...F{f + 1}");
            query.Append(' ').Append('}', levels + 1);
        }

        return query.ToString();
    }

    private static async Task<string[]> ExecuteAndReadErrorPathAsync(IRequestExecutor executor, string query)
    {
        var result = await executor.ExecuteAsync(query, TestContext.Current.CancellationToken);
        var errors = result.ExpectOperationResult().Errors;
        var error = Assert.Single(errors);
        Assert.Equal("failed", error.Message);
        return error.Path!.ToList().Select(s => s.ToString()!).ToArray();
    }

    private static ValueTask<IRequestExecutor> CreateExecutorAsync()
        => new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Node>()
            .ConfigureValidation((_, b) => b.ModifyOptions(o => o.MaxAllowedSelectionSetDepth = 1000))
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = false)
            .BuildRequestExecutorAsync();

    private static ValueTask<IRequestExecutor> CreateDelegateExecutorAsync()
        => new ServiceCollection()
            .AddGraphQL()
            .AddDocumentFromString(
                """
                schema { query: Node }

                type Node {
                  child: Node
                  children: [Node]
                  fail: String
                }
                """)
            .AddResolver("Node", "child", _ => new object())
            .AddResolver("Node", "children", _ => new object[] { new() })
            .AddResolver("Node", "fail", string? (_) => throw new GraphQLException("failed"))
            .ConfigureValidation((_, b) => b.ModifyOptions(o => o.MaxAllowedSelectionSetDepth = 1000))
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = false)
            .BuildRequestExecutorAsync();

    public class Node
    {
        public Node Child() => new();

        public Node[] Children() => [new()];

        public string? Fail() => throw new GraphQLException("failed");
    }
}
