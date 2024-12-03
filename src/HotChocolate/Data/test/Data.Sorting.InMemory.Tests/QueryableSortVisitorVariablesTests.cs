using HotChocolate.AspNetCore.Tests.Utilities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution;
using HotChocolate.Types;

namespace HotChocolate.Data.Sorting;

public class QueryableSortVisitorVariablesTests : IClassFixture<SchemaCache>
{
    private static readonly Foo[] _fooEntities =
    [
        new() { Bar = true, },
        new() { Bar = false, },
    ];

    [Fact]
    public async Task Create_Boolean_OrderBy()
    {
        // arrange
        var tester = await CreateSchema<Foo, FooSortType>(_fooEntities);
        const string query =
            "query Test($order: SortEnumType){ root(order: { bar: $order}){ bar}}";

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(query)
                .SetVariableValues(new Dictionary<string, object?> { { "order", "ASC" }, })
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(query)
                .SetVariableValues(new Dictionary<string, object?> { { "order", "DESC" }, })
                .Build());

        // assert
        await Snapshot
            .Create()
            .Add(res1, "ASC")
            .Add(res2, "DESC")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_Boolean_OrderBy_NonNull()
    {
        // arrange
        var tester = await CreateSchema<Foo, FooSortType>(_fooEntities);
        const string query =
            "query Test($order: SortEnumType!){ root(order: { bar: $order}){ bar}}";

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(query)
                .SetVariableValues(new Dictionary<string, object?> { { "order", "ASC" }, })
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(query)
                .SetVariableValues(new Dictionary<string, object?> { { "order", "DESC" }, })
                .Build());

        // assert
        await Snapshot
            .Create()
            .Add(res1, "ASC")
            .Add(res2, "DESC")
            .MatchAsync();
    }

    [Fact]
    public async Task Integration_Create_Boolean_OrderBy()
    {
        // arrange
        var server = CreateServer<Foo, FooSortType>(_fooEntities);
        const string query =
            "query Test($order: SortEnumType){ root(order: { bar: $order}){ bar}}";

        // act
        ClientQueryRequest request1 =
            new() { Query = query, Variables = new() { ["order"] = "ASC", }, };
        var res1 = await server.PostAsync(request1);

        ClientQueryRequest request2 =
            new() { Query = query, Variables = new() { ["order"] = "DESC", }, };
        var res2 = await server.PostAsync(request2);

        // assert
        await Snapshot
            .Create()
            .Add(res1, "ASC")
            .Add(res2, "DESC")
            .MatchAsync();
    }

    [Fact]
    public async Task Integration_Create_Boolean_OrderBy_NonNull()
    {
        // arrange
        var server = CreateServer<Foo, FooSortType>(_fooEntities);
        const string query =
            "query Test($order: SortEnumType!){ root(order: { bar: $order}){ bar}}";

        // act
        ClientQueryRequest request1 =
            new() { Query = query, Variables = new() { ["order"] = "ASC", }, };
        var res1 = await server.PostAsync(request1);

        ClientQueryRequest request2 =
            new() { Query = query, Variables = new() { ["order"] = "DESC", }, };
        var res2 = await server.PostAsync(request2);

        // assert
        await Snapshot
            .Create()
            .Add(res1, "ASC")
            .Add(res2, "DESC")
            .MatchAsync();
    }

    private TestServer CreateServer<TEntity, T>(TEntity?[] entities)
        where TEntity : class
        where T : SortInputType<TEntity>
    {
        var builder = new WebHostBuilder()
            .ConfigureServices(
                (_, services) =>
                {
                    services.AddRouting();
                    services.AddGraphQLServer()
                        .AddSorting()
                        .AddQueryType(
                            c =>
                            {
                                c
                                    .Name("Query")
                                    .Field("root")
                                    .Resolve(entities)
                                    .UseSorting<T>();

                                c
                                    .Name("Query")
                                    .Field("rootExecutable")
                                    .Resolve(entities.AsExecutable())
                                    .UseSorting<T>();
                            })
                        .BuildRequestExecutorAsync();
                })
            .Configure(x => x.UseRouting().UseEndpoints(c => c.MapGraphQL()));
        return new TestServer(builder);
    }

    private ValueTask<IRequestExecutor> CreateSchema<TEntity, T>(TEntity?[] entities)
        where TEntity : class
        where T : SortInputType<TEntity>
    {
        return new ServiceCollection()
            .AddGraphQL()
            .AddSorting()
            .AddQueryType(
                descriptor =>
                {
                    descriptor
                        .Name("Query")
                        .Field("root")
                        .Resolve(entities)
                        .UseSorting<T>();

                    descriptor
                        .Name("Query")
                        .Field("rootExecutable")
                        .Resolve(entities.AsExecutable())
                        .UseSorting<T>();
                })
            .BuildRequestExecutorAsync();
    }

    public class Foo
    {
        public int Id { get; set; }

        public bool Bar { get; set; }
    }

    public class FooSortType : SortInputType<Foo> { }
}
