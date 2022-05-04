using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Execution;
using Xunit;

namespace HotChocolate.Data.Filters;

public class QueryableFilterVisitorExpressionTests : IClassFixture<SchemaCache>
{
    private static readonly Foo[] _fooEntities =
    {
        new Foo { Name = "Foo", LastName = "Galoo", Bars = new Bar[]{ new Bar { Value="A"} } },
        new Foo { Name = "Sam", LastName = "Sampleman", Bars = Array.Empty<Bar>() }
    };

    private readonly SchemaCache _cache;

    public QueryableFilterVisitorExpressionTests(SchemaCache cache)
    {
        _cache = cache;
    }

    [Fact]
    public async Task Create_StringConcatExpression()
    {
        // arrange
        IRequestExecutor? tester = _cache.CreateSchema<Foo, FooFilterInputType>(_fooEntities);

        // act
        // assert
        IExecutionResult? res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
            .SetQuery("{ root(where: { displayName: { eq: \"Sam Sampleman\"}}){ name lastName}}")
            .Create());

        res1.MatchSqlSnapshot("Sam_Sampleman");

        IExecutionResult? res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
            .SetQuery("{ root(where: { displayName: { eq: \"NoMatch\"}}){ name lastName}}")
            .Create());

        res2.MatchSqlSnapshot("NoMatch");

        IExecutionResult? res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
            .SetQuery("{ root(where: { displayName: { eq: null}}){ name lastName}}")
            .Create());

        res3.MatchSqlSnapshot("null");
    }

    [Fact]
    public async Task Create_CollectionLengthExpression()
    {
        // arrange
        IRequestExecutor? tester = _cache.CreateSchema<Foo, FooFilterInputType>(_fooEntities);

        // act
        // assert
        IExecutionResult? res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
            .SetQuery("{ root(where: { barLength: { eq: 1}}){ name lastName}}")
            .Create());

        res1.MatchSqlSnapshot("1");

        IExecutionResult? res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
            .SetQuery("{ root(where: { barLength: { eq: 0}}){ name lastName}}")
            .Create());

        res2.MatchSqlSnapshot("0");

        IExecutionResult? res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
            .SetQuery("{ root(where: { barLength: { eq: null}}){ name lastName}}")
            .Create());

        res3.MatchSqlSnapshot("null");
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

    public class FooFilterInputType : FilterInputType<Foo>
    {
        protected override void Configure(IFilterInputTypeDescriptor<Foo> descriptor)
        {
            descriptor.Field(x => x.Name + " " + x.LastName).Name("displayName");
            descriptor.Field(x => x.Bars!.Count).Name("barLength");
        }
    }
}
