using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Language;
using HotChocolate.Types.Pagination;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.CostAnalysis;

public sealed class StaticQueryAnalysisTests
{
    [Theory]
    [MemberData(nameof(ListQueryData))]
    public async Task Execute_ListQuery_ReturnsExpectedResult(
        int index,
        string schema,
        string operation)
    {
        // arrange
        var snapshot = new Snapshot(postFix: index.ToString());

        schema =
            $$"""
            type Query {
                {{schema}}
            }

            type Example {
                field1: Boolean!
                field2: Int!
            }
            """;

        operation =
            $$"""
            query {
                {{operation}}
            }
            """;

        var request =
            OperationRequestBuilder.New()
                .SetDocument(operation)
                .ReportCost()
                .Build();

        var requestExecutor = await CreateRequestExecutorBuilder()
            .AddDocumentFromString(schema)
            .BuildRequestExecutorAsync();

        // act
        var result = await requestExecutor.ExecuteAsync(request);

        // assert
        await snapshot
            .Add(Utf8GraphQLParser.Parse(operation), "Query")
            .AddResult(result.ExpectOperationResult(), "Result")
            .Add(schema, "Schema")
            .MatchMarkdownAsync();
    }

    [Theory]
    [MemberData(nameof(ConnectionQueryData))]
    public async Task Execute_ConnectionQuery_ReturnsExpectedResult(
        int index,
        string schema,
        string operation)
    {
        // arrange
        var snapshot = new Snapshot(postFix: index.ToString());

        schema =
            $$"""
            type Query {
                {{schema}}
            }

            type Example1 {
                field1: Boolean!
                field2(first: Int, after: String, last: Int, before: String): Examples2Connection
                    @listSize(slicingArguments: ["first", "last"], sizedFields: ["edges"])
            }

            type Example2 {
                field1: Boolean!
                field2: Int!
            }

            type Examples1Connection {
                pageInfo: PageInfo!
                edges: [Examples1Edge!]
                nodes: [Example1!]
            }

            type Examples2Connection {
                pageInfo: PageInfo!
                edges: [Examples2Edge!]
                nodes: [Example2!]
            }

            type Examples1Edge {
                cursor: String!
                node: Example1!
            }

            type Examples2Edge {
                cursor: String!
                node: Example2!
            }

            type PageInfo {
                hasNextPage: Boolean!
                hasPreviousPage: Boolean!
                startCursor: String
                endCursor: String
            }
            """;

        operation =
            $$"""
            query {
                {{operation}}
            }
            """;

        var request =
            OperationRequestBuilder.New()
                .SetDocument(operation)
                .ReportCost()
                .Build();

        var requestExecutor = await CreateRequestExecutorBuilder()
            .AddDocumentFromString(schema)
            .BuildRequestExecutorAsync();

        // act
        var result = await requestExecutor.ExecuteAsync(request);

        // assert
        await snapshot
            .Add(Utf8GraphQLParser.Parse(operation), "Query")
            .AddResult(result.ExpectOperationResult(), "Result")
            .Add(schema, "Schema")
            .MatchMarkdownAsync();
    }

    public static TheoryData<int, string, string> ListQueryData()
    {
        return new TheoryData<int, string, string>
        {
            // No @listSize directive.
            {
                0,
                "examples(limit: Int): [Example!]!",
                "examples(limit: 10) { field1, field2 }"
            },
            // @listSize directive without arguments.
            {
                1,
                "examples(limit: Int): [Example!]! @listSize",
                "examples(limit: 10) { field1, field2 }"
            },
            // @listSize directive with slicing arguments (integer limit in query).
            {
                2,
                """examples(limit: Int): [Example!]! @listSize(slicingArguments: ["limit"])""",
                "examples(limit: 10) { field1, field2 }"
            },
            // @listSize directive with slicing arguments (null limit in query).
            {
                3,
                """examples(limit: Int): [Example!]! @listSize(slicingArguments: ["limit"])""",
                "examples(limit: null) { field1, field2 }"
            },
            // @listSize directive with slicing arguments (no limit in query).
            // Error: "Expected 1 slicing argument, 0 provided.".
            {
                4,
                """examples(limit: Int): [Example!]! @listSize(slicingArguments: ["limit"])""",
                "examples { field1, field2 }"
            },
            // @listSize directive with slicing arguments (null limit in query, with assumedSize).
            {
                5,
                """
                examples(limit: Int): [Example!]!
                    @listSize(slicingArguments: ["limit"], assumedSize: 10)
                """,
                "examples(limit: null) { field1, field2 }"
            },
            // @listSize directive with slicing arguments (no limit in query, with assumedSize).
            // Error: "Expected 1 slicing argument, 0 provided.".
            {
                6,
                """
                examples(limit: Int): [Example!]!
                    @listSize(slicingArguments: ["limit"], assumedSize: 10)
                """,
                "examples { field1, field2 }"
            },
            // @listSize directive with slicing arguments.
            // (no limit in query, with requireOneSlicingArgument: false).
            {
                7,
                """
                examples(limit: Int): [Example!]!
                    @listSize(slicingArguments: ["limit"], requireOneSlicingArgument: false)
                """,
                "examples { field1, field2 }"
            },
            // @listSize directive with slicing arguments.
            // (no limit in query, with assumedSize and requireOneSlicingArgument: false).
            {
                8,
                """
                examples(limit: Int): [Example!]!
                    @listSize(
                        slicingArguments: ["limit"],
                        assumedSize: 10,
                        requireOneSlicingArgument: false
                    )
                """,
                "examples { field1, field2 }"
            }
        };
    }

    public static TheoryData<int, string, string> ConnectionQueryData()
    {
        return new TheoryData<int, string, string>
        {
            // Nested connections. @listSize directives with slicing arguments and sizedFields.
            {
                0,
                """
                examples1(first: Int, after: String, last: Int, before: String): Examples1Connection
                    @listSize(slicingArguments: ["first", "last"], sizedFields: ["edges", "nodes"])
                """,
                """
                examples1(first: 10) {          # Examples1Connection x1
                    pageInfo {                  # PageInfo x1
                        hasNextPage             # Boolean x1
                    }
                    edges {                     # Examples1Edge x10
                        node {                  # Example1 x10
                            field1              # Boolean x10
                            field2(first: 10) { # Examples2Connection x10
                                pageInfo {      # PageInfo x10
                                    hasNextPage # Boolean x10
                                }
                                edges {         # Examples2Edge x(10x10)
                                    node {      # Example2 x(10x10)
                                        field1  # Boolean x(10x10)
                                        field2  # Int x(10x10)
                                    }
                                }
                            }
                        }
                    }
                    nodes {                     # Example1 x10
                        field1                  # Boolean x10
                    }
                }
                """
            }
        };
    }

    private static IRequestExecutorBuilder CreateRequestExecutorBuilder()
    {
        return new ServiceCollection()
            .AddGraphQLServer()
            .ModifyCostOptions(o => o.DefaultResolverCost = null)
            .AddResolver(
                "Query",
                "example",
                _ => new Example(true, 1))
            .AddResolver(
                "Query",
                "examples",
                _ => new List<Example> { new(true, 1) })
            .AddResolver(
                "Query",
                "examples1",
                _ => new Connection<Example1>(
                    [
                        new Edge<Example1>(
                            new Example1(
                                true,
                                new Connection<Example2>(
                                    [new Edge<Example2>(new Example2(true, 1), "start")],
                                    new ConnectionPageInfo(true, false, "start", "end"))),
                            "start")
                    ],
                    new ConnectionPageInfo(true, false, "start", "end")))
            .AddResolver("Example", "field1", context => context.Parent<Example>().Field1)
            .AddResolver("Example1", "field1", context => context.Parent<Example1>().Field1)
            .AddResolver("Example2", "field1", context => context.Parent<Example2>().Field1)
            .AddResolver("Example", "field2", context => context.Parent<Example>().Field2)
            .AddResolver("Example1", "field2", context => context.Parent<Example1>().Field2)
            .AddResolver("Example2", "field2", context => context.Parent<Example2>().Field2)
            .AddResolver(
                "Examples1Connection",
                "pageInfo",
                context => context.Parent<Connection<Example1>>().Info)
            .AddResolver(
                "Examples2Connection",
                "pageInfo",
                context => context.Parent<Connection<Example2>>().Info)
            .AddResolver(
                "Examples1Connection",
                "edges",
                context => context.Parent<Connection<Example1>>().Edges)
            .AddResolver(
                "Examples2Connection",
                "edges",
                context => context.Parent<Connection<Example2>>().Edges)
            .AddResolver(
                "Examples1Connection",
                "nodes",
                context => context.Parent<Connection<Example1>>().Edges.Select(e => e.Node))
            .AddResolver(
                "Examples2Connection",
                "nodes",
                context => context.Parent<Connection<Example2>>().Edges.Select(e => e.Node))
            .AddResolver(
                "Examples1Edge",
                "cursor",
                context => context.Parent<Edge<Example1>>().Cursor)
            .AddResolver(
                "Examples2Edge",
                "cursor",
                context => context.Parent<Edge<Example2>>().Cursor)
            .AddResolver(
                "Examples1Edge",
                "node",
                context => context.Parent<Edge<Example1>>().Node)
            .AddResolver(
                "Examples2Edge",
                "node",
                context => context.Parent<Edge<Example2>>().Node)
            .AddResolver(
                "PageInfo",
                "hasNextPage",
                context => context.Parent<ConnectionPageInfo>().HasNextPage)
            .AddResolver(
                "PageInfo",
                "hasPreviousPage",
                context => context.Parent<ConnectionPageInfo>().HasPreviousPage)
            .AddResolver(
                "PageInfo",
                "startCursor",
                context => context.Parent<ConnectionPageInfo>().StartCursor)
            .AddResolver(
                "PageInfo",
                "endCursor",
                context => context.Parent<ConnectionPageInfo>().EndCursor);
    }

    private sealed record Example(bool Field1, int Field2);
    private sealed record Example1(bool Field1, Connection<Example2> Field2);
    private sealed record Example2(bool Field1, int Field2);
}
