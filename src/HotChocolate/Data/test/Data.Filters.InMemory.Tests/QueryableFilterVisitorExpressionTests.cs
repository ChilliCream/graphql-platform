using System;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Tests;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Data.Filters;

public class QueryableFilterVisitorExpressionTests : IClassFixture<SchemaCache>
{
    private static readonly Foo[] _fooEntities =
    {
            new Foo { Name = "Foo", LastName = "Galoo" },
             new Foo { Name = "Sam", LastName = "Sampleman" }
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

        res1.MatchSnapshot("Sam_Sampleman");

        IExecutionResult? res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
            .SetQuery("{ root(where: { displayName: { eq: \"NoMatch\"}}){ name lastName}}")
            .Create());

        res2.MatchSnapshot("NoMatch");

        IExecutionResult? res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
            .SetQuery("{ root(where: { displayName: { eq: null}}){ name lastName}}")
            .Create());

        res3.MatchSnapshot("null");
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
                .UseFiltering())
            .AddType(new FilterInputType<Foo>(x => x
                .Field(x => x.LastName)
                .Extend()
                .OnBeforeCreate(x => x.Expression = (Foo x, string bar) => x.LastName == bar)))
            .AddFiltering();

        // act
        async Task<IRequestExecutor> Call() => await builder.BuildRequestExecutorAsync();

        // assert
        SchemaException ex = await Assert.ThrowsAsync<SchemaException>(Call);
        ex.Errors.Single().Message.MatchSnapshot();
    }

    public class Foo
    {
        public int Id { get; set; }

        public string? Name { get; set; }

        public string? LastName { get; set; }
    }

    public class FooFilterInputType : FilterInputType<Foo>
    {
        protected override void Configure(IFilterInputTypeDescriptor<Foo> descriptor)
        {
            descriptor.Field(x => x.Name + " " + x.LastName).Name("displayName");
        }
    }
}
