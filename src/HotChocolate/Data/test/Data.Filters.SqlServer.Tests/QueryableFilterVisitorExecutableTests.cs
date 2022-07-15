using System.Threading.Tasks;
using HotChocolate.Execution;
using Xunit;

namespace HotChocolate.Data.Filters;

public class QueryableFilterVisitorExecutableTests
    : IClassFixture<SchemaCache>
{
    private static readonly Foo[] _fooEntities =
    {
        new() { Bar = true },
        new() { Bar = false }
    };

    private static readonly FooNullable[] _fooNullableEntities =
    {
            new FooNullable { Bar = true },
            new FooNullable { Bar = null },
            new FooNullable { Bar = false }
        };

    private readonly SchemaCache _cache;

    public QueryableFilterVisitorExecutableTests(
        SchemaCache cache)
    {
        _cache = cache;
    }

    [Fact]
    public async Task Create_BooleanEqual_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<Foo, FooFilterInput>(_fooEntities);

        // act
        // assert
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ rootExecutable(where: { bar: { eq: true}}){ bar}}")
                .Create());

        res1.MatchSqlSnapshot("true");

        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ rootExecutable(where: { bar: { eq: false}}){ bar}}")
                .Create());

        res2.MatchSqlSnapshot("false");
    }

    [Fact]
    public async Task Create_BooleanNotEqual_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<Foo, FooFilterInput>(_fooEntities);

        // act
        // assert
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ rootExecutable(where: { bar: { neq: true}}){ bar}}")
                .Create());

        res1.MatchSqlSnapshot("true");

        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ rootExecutable(where: { bar: { neq: false}}){ bar}}")
                .Create());

        res2.MatchSqlSnapshot("false");
    }

    [Fact]
    public async Task Create_NullableBooleanEqual_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<FooNullable, FooNullableFilterInput>(
            _fooNullableEntities);

        // act
        // assert
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ rootExecutable(where: { bar: { eq: true}}){ bar}}")
                .Create());

        res1.MatchSqlSnapshot("true");

        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ rootExecutable(where: { bar: { eq: false}}){ bar}}")
                .Create());

        res2.MatchSqlSnapshot("false");

        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ rootExecutable(where: { bar: { eq: null}}){ bar}}")
                .Create());

        res3.MatchSqlSnapshot("null");
    }

    [Fact]
    public async Task Create_NullableBooleanNotEqual_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<FooNullable, FooNullableFilterInput>(
            _fooNullableEntities);

        // act
        // assert
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ rootExecutable(where: { bar: { neq: true}}){ bar}}")
                .Create());

        res1.MatchSqlSnapshot("true");

        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ rootExecutable(where: { bar: { neq: false}}){ bar}}")
                .Create());

        res2.MatchSqlSnapshot("false");

        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ rootExecutable(where: { bar: { neq: null}}){ bar}}")
                .Create());

        res3.MatchSqlSnapshot("null");
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
