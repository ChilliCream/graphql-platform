using System.Threading.Tasks;
using HotChocolate.Execution;
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
