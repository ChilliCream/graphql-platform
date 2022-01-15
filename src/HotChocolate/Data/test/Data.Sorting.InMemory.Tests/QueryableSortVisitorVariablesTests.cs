using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Utilities;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Tests;
using HotChocolate.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Snapshooter;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Data.Sorting;

public class QueryableSortVisitorVariablesTests
    : IClassFixture<SchemaCache>
{
    private static readonly Foo[] _fooEntities =
    {
            new() { Bar = true },
            new() { Bar = false }
        };

    [Fact]
    public async Task Create_Boolean_OrderBy()
    {
        // arrange
        IRequestExecutor tester = await CreateSchema<Foo, FooSortType>(_fooEntities);
        const string query =
            "query Test($order: SortEnumType){ root(order: { bar: $order}){ bar}}";

        // act
        IExecutionResult res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query)
                .AddVariableValue("order", "ASC")
                .Create());

        IExecutionResult res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query)
                .AddVariableValue("order", "DESC")
                .Create());

        // assert
        res1.MatchSnapshot("ASC");
        res2.MatchSnapshot("DESC");
    }

    [Fact]
    public async Task Create_Boolean_OrderBy_NonNull()
    {
        // arrange
        IRequestExecutor tester = await CreateSchema<Foo, FooSortType>(_fooEntities);
        const string query =
            "query Test($order: SortEnumType!){ root(order: { bar: $order}){ bar}}";

        // act
        IExecutionResult res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query)
                .AddVariableValue("order", "ASC")
                .Create());

        IExecutionResult res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query)
                .AddVariableValue("order", "DESC")
                .Create());

        // assert
        res1.MatchSnapshot("ASC");
        res2.MatchSnapshot("DESC");
    }

    [Fact]
    public async Task Integration_Create_Boolean_OrderBy()
    {
        // arrange
        TestServer server = CreateServer<Foo, FooSortType>(_fooEntities);
        const string query =
            "query Test($order: SortEnumType){ root(order: { bar: $order}){ bar}}";

        // act
        ClientQueryRequest request1 =
            new() { Query = query, Variables = new() { ["order"] = "ASC" } };
        ClientQueryResult response1 = await server.PostAsync(request1);

        ClientQueryRequest request2 =
            new() { Query = query, Variables = new() { ["order"] = "DESC" } };
        ClientQueryResult response2 = await server.PostAsync(request2);

        // assert
        response1.MatchSnapshot(new SnapshotNameExtension("ASC"));
        response2.MatchSnapshot(new SnapshotNameExtension("DESC"));
    }

    [Fact]
    public async Task Integration_Create_Boolean_OrderBy_NonNull()
    {
        // arrange
        TestServer server = CreateServer<Foo, FooSortType>(_fooEntities);
        const string query =
            "query Test($order: SortEnumType!){ root(order: { bar: $order}){ bar}}";

        // act
        ClientQueryRequest request1 =
            new() { Query = query, Variables = new() { ["order"] = "ASC" } };
        ClientQueryResult response1 = await server.PostAsync(request1);

        ClientQueryRequest request2 =
            new() { Query = query, Variables = new() { ["order"] = "DESC" } };
        ClientQueryResult response2 = await server.PostAsync(request2);

        // assert
        response1.MatchSnapshot(new SnapshotNameExtension("ASC"));
        response2.MatchSnapshot(new SnapshotNameExtension("DESC"));
    }

    private TestServer CreateServer<TEntity, T>(TEntity?[] entities)
        where TEntity : class
        where T : SortInputType<TEntity>
    {
        IWebHostBuilder builder = new WebHostBuilder()
            .ConfigureServices((_, services) =>
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
    }
}

public class Foo
{
    public int Id { get; set; }

    public bool Bar { get; set; }
}

public class FooSortType
    : SortInputType<Foo>
{
}
