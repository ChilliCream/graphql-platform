using System.Reflection;

namespace HotChocolate.Types;

public class SourceGeneratorUseConnectionTests
{
    [Fact]
    public async Task UseConnection_Should_ExposeConnectionType_When_ResolverReturnsGenericConnection()
    {
        // arrange
        var assembly = CompileConnectionAssembly();

        // act
        var sourceGenerated = await SourceGeneratorTestHelpers.ExecuteWithSourceGeneratorRegistrationAsync(
            assembly,
            registrationMethodName: "AddDemo",
            query:
            """
            {
              books {
                edges {
                  cursor
                  node {
                    id
                  }
                }
                nodes {
                  id
                }
                pageInfo {
                  hasNextPage
                  hasPreviousPage
                  startCursor
                  endCursor
                }
                totalCount
              }
            }
            """);

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(sourceGenerated.Schema, "Schema", MarkdownLanguages.GraphQL);
        snapshot.Add(sourceGenerated.Result, "Result", MarkdownLanguages.Json);
        await snapshot.MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    private static Assembly CompileConnectionAssembly()
    {
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using GreenDonut.Data;
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Pagination;

            [assembly: Module("Demo")]

            namespace Repro;

            public sealed class Book
            {
                public int Id { get; set; }

                public string Title { get; set; } = default!;
            }

            [QueryType]
            public static partial class BookQueries
            {
                [UseConnection]
                public static Task<Connection<Book>> GetBooksAsync(
                    PagingArguments paging,
                    CancellationToken cancellationToken)
                {
                    var edges = new IEdge<Book>[]
                    {
                        new Edge<Book>(new Book { Id = 1, Title = "A" }, "cursor1")
                    };
                    var info = new ConnectionPageInfo(
                        hasNextPage: false,
                        hasPreviousPage: false,
                        startCursor: "cursor1",
                        endCursor: "cursor1");

                    return Task.FromResult(new Connection<Book>(edges, info, 1));
                }
            }
            """;

        return SourceGeneratorTestHelpers.CompileReproAssembly(source, "SourceGeneratorUseConnection");
    }
}
