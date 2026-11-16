using System.Data.Common;
using System.Text.Json;
using CookieCrumble.Resources;
using GreenDonut.Data;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Pagination;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data;

[Collection(PostgresCacheCollectionFixture.DefinitionName)]
public class StreamPageConnectionIntegrationTests(PostgreSqlResource resource)
{
    public enum PagingScenario
    {
        First,
        FirstAfter,
        Last,
        LastBefore
    }

    private string CreateConnectionString()
        => resource.GetConnectionString($"db_{Guid.NewGuid():N}");

    private static async Task<string> GetPagingArgumentsAsync(
        IRequestExecutor executor,
        CommandCapture commands,
        PagingScenario scenario)
    {
        switch (scenario)
        {
            case PagingScenario.First:
                return "first: 2";

            case PagingScenario.FirstAfter:
            case PagingScenario.LastBefore:
            {
                var first = scenario is PagingScenario.FirstAfter ? 2 : 4;
                var seedResult = await executor.ExecuteAsync(
                    OperationRequestBuilder.New()
                        .SetDocument($"{{ streamed(first: {first}) {{ pageInfo {{ endCursor }} }} }}")
                        .Build(),
                    Xunit.TestContext.Current.CancellationToken);
                using var document = JsonDocument.Parse(seedResult.ToJson());
                var cursor = document.RootElement
                    .GetProperty("data")
                    .GetProperty("streamed")
                    .GetProperty("pageInfo")
                    .GetProperty("endCursor")
                    .GetString()
                    ?? throw new InvalidOperationException("The seed page did not return an end cursor.");
                await seedResult.DisposeAsync();
                commands.Commands.Clear();

                return scenario is PagingScenario.FirstAfter
                    ? $"first: 2, after: \"{cursor}\""
                    : $"last: 2, before: \"{cursor}\"";
            }

            case PagingScenario.Last:
                return "last: 2";

            default:
                throw new ArgumentOutOfRangeException(nameof(scenario));
        }
    }

    [Fact]
    public async Task Execute_Should_MatchPageConnection_When_PagingScenariosAreUsed()
    {
        // arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);
        var commands = new CommandCapture();
        var executor = await CreateExecutorAsync(connectionString, commands);
        var firstResult = string.Empty;

        // act
        foreach (var scenario in Enum.GetValues<PagingScenario>())
        {
            var arguments = await GetPagingArgumentsAsync(executor, commands, scenario);
            var result = await executor.ExecuteAsync(
                OperationRequestBuilder.New()
                    .SetDocument(
                        $$"""
                        {
                          paged({{arguments}}) {
                            nodes { id name }
                            edges { cursor node { id name } }
                            pageInfo {
                              hasNextPage
                              hasPreviousPage
                              startCursor
                              endCursor
                              forwardCursors { page cursor }
                              backwardCursors { page cursor }
                            }
                            totalCount
                          }
                          streamed({{arguments}}) {
                            nodes { id name }
                            edges { cursor node { id name } }
                            pageInfo {
                              hasNextPage
                              hasPreviousPage
                              startCursor
                              endCursor
                              forwardCursors { page cursor }
                              backwardCursors { page cursor }
                            }
                            totalCount
                          }
                        }
                        """)
                    .Build(),
                Xunit.TestContext.Current.CancellationToken);
            using var document = JsonDocument.Parse(result.ToJson());
            var data = document.RootElement.GetProperty("data");
            var paged = data.GetProperty("paged").GetRawText();
            var streamed = data.GetProperty("streamed").GetRawText();
            Assert.Equal(paged, streamed);

            if (scenario is PagingScenario.First)
            {
                firstResult = streamed;
            }

            await result.DisposeAsync();
        }

        // assert
        firstResult.MatchInlineSnapshot(
            """
            {
              "nodes": [
                {
                  "id": 1,
                  "name": "Item 1"
                },
                {
                  "id": 2,
                  "name": "Item 2"
                }
              ],
              "edges": [
                {
                  "cursor": "e30x",
                  "node": {
                    "id": 1,
                    "name": "Item 1"
                  }
                },
                {
                  "cursor": "e30y",
                  "node": {
                    "id": 2,
                    "name": "Item 2"
                  }
                }
              ],
              "pageInfo": {
                "hasNextPage": true,
                "hasPreviousPage": false,
                "startCursor": "e30x",
                "endCursor": "e30y",
                "forwardCursors": [
                  {
                    "page": 2,
                    "cursor": "ezB8MXw2fTI="
                  },
                  {
                    "page": 3,
                    "cursor": "ezF8MXw2fTI="
                  }
                ],
                "backwardCursors": []
              },
              "totalCount": 6
            }
            """);
    }

    [Fact]
    public async Task TotalCount_Should_PropagateNonNullViolation_When_StreamCountIsUnknown()
    {
        // arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);
        var executor = await CreateExecutorAsync(connectionString, new CommandCapture());

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ unknownCount(first: 2) { totalCount } }")
                .Build(),
            Xunit.TestContext.Current.CancellationToken);
        var resultJson = result.ToJson();
        await result.DisposeAsync();

        // assert
        resultJson.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "Cannot return null for non-nullable field.",
                  "path": [
                    "unknownCount",
                    "totalCount"
                  ],
                  "extensions": {
                    "code": "HC0018"
                  }
                }
              ],
              "data": null
            }
            """);
    }

    [Fact]
    public async Task Execute_Should_RunOnlyTheCountQuery_When_OnlyTotalCountIsSelected()
    {
        // arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);
        var commands = new CommandCapture();
        var executor = await CreateExecutorAsync(connectionString, commands);

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ streamed(first: 2) { totalCount } }")
                .Build(),
            Xunit.TestContext.Current.CancellationToken);
        var resultJson = result.ToJson();
        await result.DisposeAsync();

        // assert
        resultJson.MatchInlineSnapshot(
            """
            {
              "data": {
                "streamed": {
                  "totalCount": 6
                }
              }
            }
            """);
        commands.Commands.MatchInlineSnapshot(
            """
            [
              "SELECT count(*)::int\nFROM \"Items\" AS i"
            ]
            """);
    }

    [Fact]
    public async Task Execute_Should_RunOnlyTheRowQuery_When_TotalCountIsNotSelected()
    {
        // arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);
        var commands = new CommandCapture();
        var executor = await CreateExecutorAsync(connectionString, commands);

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ streamed(first: 2) { nodes { id } } }")
                .Build(),
            Xunit.TestContext.Current.CancellationToken);
        var resultJson = result.ToJson();
        await result.DisposeAsync();

        // assert
        resultJson.MatchInlineSnapshot(
            """
            {
              "data": {
                "streamed": {
                  "nodes": [
                    {
                      "id": 1
                    },
                    {
                      "id": 2
                    }
                  ]
                }
              }
            }
            """);
        commands.Commands
            .Select(command => command.Contains("@__p_0", StringComparison.Ordinal)
                ? command.Replace("@__p_0", "@parameter", StringComparison.Ordinal)
                : command.Replace("LIMIT @p", "LIMIT @parameter", StringComparison.Ordinal))
            .MatchInlineSnapshot(
                """
                [
                  "SELECT i.\"Id\", i.\"Name\"\nFROM \"Items\" AS i\nORDER BY i.\"Id\"\nLIMIT @parameter"
                ]
                """);
    }

    private static async Task<IRequestExecutor> CreateExecutorAsync(
        string connectionString,
        CommandCapture commands)
        => await new ServiceCollection()
            .AddDbContextFactory<PagingContext>(options => options
                .UseNpgsql(connectionString)
                .AddInterceptors(commands))
            .AddGraphQLServer()
            .AddQueryType<QueryType>()
            .AddType<PageItemConnectionType>()
            .AddType<StreamItemConnectionType>()
            .AddType<PageItemEdgeType>()
            .AddType<StreamItemEdgeType>()
            .AddPagingArguments()
            .ModifyPagingOptions(options => options.EnableRelativeCursors = true)
            .BuildRequestExecutorAsync(cancellationToken: Xunit.TestContext.Current.CancellationToken);

    private static async Task SeedAsync(string connectionString)
    {
        var options = new DbContextOptionsBuilder<PagingContext>()
            .UseNpgsql(connectionString)
            .Options;
        await using var context = new PagingContext(options);
        await context.Database.EnsureCreatedAsync(Xunit.TestContext.Current.CancellationToken);

        for (var i = 1; i <= 6; i++)
        {
            context.Items.Add(new Item { Id = i, Name = $"Item {i}" });
        }

        await context.SaveChangesAsync(Xunit.TestContext.Current.CancellationToken);
    }

    public sealed class Item
    {
        public int Id { get; set; }

        public required string Name { get; set; }
    }

    public sealed class PagingContext(DbContextOptions<PagingContext> options) : DbContext(options)
    {
        public DbSet<Item> Items => Set<Item>();
    }

    public sealed class Query
    {
        [UseConnection(IncludeTotalCount = true, EnableRelativeCursors = true)]
        public async Task<PageConnection<Item>> GetPaged(
            PagingArguments arguments,
            IResolverContext resolverContext,
            [Service] IDbContextFactory<PagingContext> contextFactory,
            CancellationToken cancellationToken)
        {
            if (IsRelativeCursorSelected(resolverContext))
            {
                arguments = arguments with { EnableRelativeCursors = true };
            }

            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
            return await context.Items
                .OrderBy(item => item.Id)
                .ToPageAsync(arguments, cancellationToken);
        }

        [UseConnection(IncludeTotalCount = true, EnableRelativeCursors = true)]
        public async Task<StreamPageConnection<Item>> GetStreamed(
            PagingArguments arguments,
            IResolverContext resolverContext,
            [Service] IDbContextFactory<PagingContext> contextFactory,
            CancellationToken cancellationToken)
        {
            if (IsRelativeCursorSelected(resolverContext))
            {
                arguments = arguments with { EnableRelativeCursors = true };
            }

            var context = await contextFactory.CreateDbContextAsync(cancellationToken);
            var page = await context.Items
                .OrderBy(item => item.Id)
                .ToStreamPageAsync(arguments, context, cancellationToken);

            return page;
        }

        [UseConnection(IncludeTotalCount = true, EnableRelativeCursors = true)]
        public async Task<StreamPageConnection<Item>> GetUnknownCount(
            [Service] IDbContextFactory<PagingContext> contextFactory,
            CancellationToken cancellationToken)
        {
            var context = await contextFactory.CreateDbContextAsync(cancellationToken);
            var page = await context.Items
                .OrderBy(item => item.Id)
                .ToStreamPageAsync(
                    new PagingArguments(first: 2),
                    context,
                    cancellationToken);

            return page;
        }

        private static bool IsRelativeCursorSelected(IResolverContext context)
        {
            var connectionType = (IObjectTypeDefinition)context.Selection.Type.NamedType();
            var connectionSelectionSet = context.Operation.GetSelectionSet(context.Selection, connectionType);

            foreach (var connectionSelection in connectionSelectionSet.Selections)
            {
                if (connectionSelection.Field.Name != "pageInfo")
                {
                    continue;
                }

                var pageInfoType = (IObjectTypeDefinition)connectionSelection.Type.NamedType();
                var pageInfoSelectionSet = context.Operation.GetSelectionSet(connectionSelection, pageInfoType);

                foreach (var pageInfoSelection in pageInfoSelectionSet.Selections)
                {
                    if (pageInfoSelection.IsIncluded(context.IncludeConditionFlags)
                        && (pageInfoSelection.Field.Name is "forwardCursors" or "backwardCursors"))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }

    public sealed class QueryType : ObjectType<Query>
    {
        protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
        {
            descriptor
                .Field(query => query.GetPaged(default!, default!, default!, default))
                .AddPagingArguments()
                .Type<NonNullType<PageItemConnectionType>>();
            descriptor
                .Field(query => query.GetStreamed(default!, default!, default!, default))
                .AddPagingArguments()
                .Type<NonNullType<StreamItemConnectionType>>();
            descriptor
                .Field(query => query.GetUnknownCount(default!, default))
                .AddPagingArguments()
                .Type<NonNullType<StreamItemConnectionType>>();
        }
    }

    public sealed class PageItemConnectionType : ObjectType<PageConnection<Item>>
    {
        protected override void Configure(IObjectTypeDescriptor<PageConnection<Item>> descriptor)
        {
            descriptor.Name("PagedItemConnection");
            descriptor.Field(connection => connection.Edges)
                .Type<ListType<NonNullType<PageItemEdgeType>>>();
        }
    }

    public sealed class StreamItemConnectionType : ObjectType<StreamPageConnection<Item>>
    {
        protected override void Configure(IObjectTypeDescriptor<StreamPageConnection<Item>> descriptor)
        {
            descriptor.Name("StreamedItemConnection");
            descriptor.Field(connection => connection.GetEdgesAsync(default))
                .Type<ListType<NonNullType<StreamItemEdgeType>>>();
        }
    }

    public sealed class PageItemEdgeType : ObjectType<PageEdge<Item>>
    {
        protected override void Configure(IObjectTypeDescriptor<PageEdge<Item>> descriptor)
            => descriptor.Name("PagedItemEdge");
    }

    public sealed class StreamItemEdgeType : ObjectType<StreamPageEdge<Item>>
    {
        protected override void Configure(IObjectTypeDescriptor<StreamPageEdge<Item>> descriptor)
            => descriptor.Name("StreamedItemEdge");
    }

    private sealed class CommandCapture : DbCommandInterceptor
    {
        public List<string> Commands { get; } = [];

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            lock (Commands)
            {
                Commands.Add(command.CommandText);
            }

            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }
}
