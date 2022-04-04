using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Tests;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Data.Sorting;

public class QueryableSortVisitorExpressionTests : IClassFixture<SchemaCache>
{
    private static readonly Foo[] _fooEntities =
    {
         new()
         {
             Name = "Sam",
             LastName = "Sampleman",
             Bars = Array.Empty<Bar>()
         },
         new()
         {
             Name = "Foo",
             LastName = "Galoo",
             Bars = new Bar[] { new() { Value = "A" } }
         }
     };

    private readonly SchemaCache _cache;

    public QueryableSortVisitorExpressionTests(SchemaCache cache)
    {
        _cache = cache;
    }

    [Fact]
    public async Task Create_StringConcatExpression()
    {
        // arrange
        IRequestExecutor? tester = _cache.CreateSchema<Foo, FooSortInputType>(_fooEntities);

        // act
        // assert
        IExecutionResult? res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
            .SetQuery("{ root(order: { displayName: DESC}){ name lastName}}")
            .Create());

        res1.MatchSnapshot("DESC");

        IExecutionResult? res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
            .SetQuery("{ root(order: { displayName: ASC}){ name lastName}}")
            .Create());

        res2.MatchSnapshot("ASC");
    }

    [Fact]
    public async Task Expression_WithMoreThanOneParameter_ThrowsException()
    {
        // arrange
        IRequestExecutorBuilder builder = new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x
                .Name("Query")
                .Field("Foo")
                .Resolve(Array.Empty<Foo>())
                .UseSorting())
            .AddType(new SortInputType<Foo>(x => x
                .Field(x => x.LastName)
                .Extend()
                .OnBeforeCreate(x => x.Expression = (Foo x, string bar) => x.LastName == bar)))
            .AddSorting();

        // act
        async Task<IRequestExecutor> Call() => await builder.BuildRequestExecutorAsync();

        // assert
        SchemaException ex = await Assert.ThrowsAsync<SchemaException>(Call);
        ex.Errors.Single().Message.MatchSnapshot();
    }

    [Fact]
    public async Task Create_CollectionLengthExpression()
    {
        // arrange
        IRequestExecutor? tester = _cache.CreateSchema<Foo, FooSortInputType>(_fooEntities);

        // act
        // assert
        IExecutionResult? res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
            .SetQuery("{ root(order: { barLength: ASC}){ name lastName}}")
            .Create());

        res1.MatchSnapshot("ASC");

        IExecutionResult? res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
            .SetQuery("{ root(order: { barLength: DESC}){ name lastName}}")
            .Create());

        res2.MatchSnapshot("DESC");
    }

    public class Foo
    {
        public int Id { get; set; }

        public string? Name { get; set; }

        public string? LastName { get; set; }

        public ICollection<Bar>? Bars { get; set; }
    }

    public class Bar
    {
        public int Id { get; set; }

        public string? Value { get; set; }
    }

    public class FooSortInputType : SortInputType<Foo>
    {
        protected override void Configure(ISortInputTypeDescriptor<Foo> descriptor)
        {
            descriptor.Field(x => x.Name + " " + x.LastName).Name("displayName");
            descriptor.Field(x => x.Bars!.Count).Name("barLength");
        }
    }
}
