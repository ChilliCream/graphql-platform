using System;
using System.Threading.Tasks;
using HotChocolate.Data.Filters;
using HotChocolate.Execution;
using MongoDB.Bson.Serialization.Attributes;
using Squadron;
using Xunit;

namespace HotChocolate.Data.MongoDb.Filters;

public class MongoDbFilterVisitorBooleanTests
    : SchemaCache
    , IClassFixture<MongoResource>
{
    private static readonly Foo[] _fooEntities =
    {
            new Foo { Bar = true },
            new Foo { Bar = false }
        };

    private static readonly FooNullable[] _fooNullableEntities =
    {
            new FooNullable { Bar = true },
            new FooNullable { Bar = null },
            new FooNullable { Bar = false }
        };

    public MongoDbFilterVisitorBooleanTests(MongoResource resource)
    {
        Init(resource);
    }

    [Fact]
    public async Task Create_BooleanEqual_Expression()
    {
        // arrange
        var tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

        // act
        // assert
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { eq: true}}){ bar}}")
                .Create());

        res1.MatchDocumentSnapshot("true");

        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { eq: false}}){ bar}}")
                .Create());

        res2.MatchDocumentSnapshot("false");
    }

    [Fact]
    public async Task Create_BooleanNotEqual_Expression()
    {
        // arrange
        var tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

        // act
        // assert
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { neq: true}}){ bar}}")
                .Create());

        res1.MatchDocumentSnapshot("true");

        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { neq: false}}){ bar}}")
                .Create());

        res2.MatchDocumentSnapshot("false");
    }

    [Fact]
    public async Task Create_NullableBooleanEqual_Expression()
    {
        // arrange
        var tester = CreateSchema<FooNullable, FooNullableFilterType>(
            _fooNullableEntities);

        // act
        // assert
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { eq: true}}){ bar}}")
                .Create());

        res1.MatchDocumentSnapshot("true");

        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { eq: false}}){ bar}}")
                .Create());

        res2.MatchDocumentSnapshot("false");

        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { eq: null}}){ bar}}")
                .Create());

        res3.MatchDocumentSnapshot("null");
    }

    [Fact]
    public async Task Create_NullableBooleanNotEqual_Expression()
    {
        // arrange
        var tester = CreateSchema<FooNullable, FooNullableFilterType>(
            _fooNullableEntities);

        // act
        // assert
        var res1 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { neq: true}}){ bar}}")
                .Create());

        res1.MatchDocumentSnapshot("true");

        var res2 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { neq: false}}){ bar}}")
                .Create());

        res2.MatchDocumentSnapshot("false");

        var res3 = await tester.ExecuteAsync(
            QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { neq: null}}){ bar}}")
                .Create());

        res3.MatchDocumentSnapshot("null");
    }

    public class Foo
    {
        [BsonId]
        public Guid Id { get; set; } = Guid.NewGuid();

        public bool Bar { get; set; }
    }

    public class FooNullable
    {
        [BsonId]
        public Guid Id { get; set; } = Guid.NewGuid();

        public bool? Bar { get; set; }
    }

    public class FooFilterType
        : FilterInputType<Foo>
    {
    }

    public class FooNullableFilterType
        : FilterInputType<FooNullable>
    {
    }
}
