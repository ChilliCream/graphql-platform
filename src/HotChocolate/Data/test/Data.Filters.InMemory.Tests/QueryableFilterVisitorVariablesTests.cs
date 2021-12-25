using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Tests;
using Xunit;

namespace HotChocolate.Data.Filters;

public class QueryableFilterVisitorVariablesTests
    : IClassFixture<SchemaCache>
{
    private static readonly Foo[] _fooEntities = new[]
    {
            new Foo { Bar = true },
            new Foo { Bar = false }
        };

    private readonly SchemaCache _cache;

    public QueryableFilterVisitorVariablesTests(SchemaCache cache)
    {
        _cache = cache;
    }

    [Fact]
    public async Task Create_BooleanEqual_Expression()
    {
        // arrange
        IRequestExecutor? tester = _cache.CreateSchema<Foo, FooFilterInput>(_fooEntities);
        const string query =
            "query Test($where: Boolean){ root(where: {bar: { eq: $where}}){ bar}}";

        // act
        // assert
        IExecutionResult res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query)
                .AddVariableValue("where", true)
                .Create());
        res1.MatchSnapshot("true");

        IExecutionResult res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query)
                .AddVariableValue("where", false)
                .Create());

        res2.MatchSnapshot("false");
    }

    [Fact]
    public async Task Create_BooleanEqual_Expression_NonNull()
    {
        // arrange
        IRequestExecutor? tester = _cache.CreateSchema<Foo, FooFilterInput>(_fooEntities);
        const string query =
            "query Test($where: Boolean!){ root(where: {bar: { eq: $where}}){ bar}}";

        // act
        // assert
        IExecutionResult res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query)
                .AddVariableValue("where", true)
                .Create());
        res1.MatchSnapshot("true");

        IExecutionResult res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery(query)
                .AddVariableValue("where", false)
                .Create());

        res2.MatchSnapshot("false");
    }

    public class Foo
    {
        public int Id { get; set; }

        public bool Bar { get; set; }
    }

    public class FooNullable
    {
        public int Id { get; set; }

        public bool? Bar { get; set; }
    }

    public class FooFilterInput
        : FilterInputType<Foo>
    {
    }

    public class FooNullableFilterInput
        : FilterInputType<FooNullable>
    {
    }
}
