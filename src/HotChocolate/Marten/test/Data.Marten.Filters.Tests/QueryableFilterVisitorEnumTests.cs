using CookieCrumble;
using HotChocolate.Data.Filters;
using HotChocolate.Execution;

namespace HotChocolate.Data;

[Collection(SchemaCacheCollectionFixture.DefinitionName)]
public class QueryableFilterVisitorEnumTests
{
    private static readonly Foo[] _fooEntities =
    [
        new() { BarEnum = FooEnum.BAR, },
        new() { BarEnum = FooEnum.BAZ, },
        new() { BarEnum = FooEnum.FOO, },
        new() { BarEnum = FooEnum.QUX, },
    ];

    private static readonly FooNullable[] _fooNullableEntities =
    [
        new() { BarEnum = FooEnum.BAR, },
        new() { BarEnum = FooEnum.BAZ, },
        new() { BarEnum = FooEnum.FOO, },
        new() { BarEnum = null, },
        new() { BarEnum = FooEnum.QUX, },
    ];

    private readonly SchemaCache _cache;

    public QueryableFilterVisitorEnumTests(SchemaCache cache)
    {
        _cache = cache;
    }

    [Fact]
    public async Task Create_EnumEqual_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<Foo, FooFilterInput>(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { barEnum: { eq: BAR } }) { barEnum } }")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { barEnum: { eq: FOO } }) { barEnum } }")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { barEnum: { eq: null } }) { barEnum } }")
                .Build());

        // assert
        await Snapshot
            .Create().AddResult(
                res1,
                "BAR").AddResult(
                res2,
                "FOO").AddResult(
                res3,
                "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_EnumNotEqual_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<Foo, FooFilterInput>(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { barEnum: { neq: BAR } }) { barEnum } }")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { barEnum: { neq: FOO } }) { barEnum } }")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { barEnum: { neq: null } }){ barEnum } }")
                .Build());

        // assert
        await Snapshot
            .Create().AddResult(
                res1,
                "BAR").AddResult(
                res2,
                "FOO").AddResult(
                res3,
                "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_EnumIn_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<Foo, FooFilterInput>(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { barEnum: { in: [ BAR FOO ]}}){ barEnum}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { barEnum: { in: [ FOO ]}}){ barEnum}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { barEnum: { in: [ null FOO ]}}){ barEnum}}")
                .Build());

        // assert
        await Snapshot
            .Create().AddResult(
                res1,
                "BarAndFoo").AddResult(
                res2,
                "FOO").AddResult(
                res3,
                "nullAndFoo")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_EnumNotIn_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<Foo, FooFilterInput>(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { barEnum: { nin: [ BAR FOO ] } }) { barEnum } }")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { barEnum: { nin: [ FOO ] } }) { barEnum } }")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { barEnum: { nin: [ null FOO ] } }) { barEnum } }")
                .Build());

        // assert
        await Snapshot
            .Create().AddResult(
                res1,
                "BarAndFoo").AddResult(
                res2,
                "FOO").AddResult(
                res3,
                "nullAndFoo")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_NullableEnumEqual_Expression()
    {
        var tester = _cache.CreateSchema<FooNullable, FooNullableFilterInput>(
            _fooNullableEntities);

        // act
        // assert
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { barEnum: { eq: BAR } }) { barEnum } }")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { barEnum: { eq: FOO } }) { barEnum } }")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { barEnum: { eq: null } }){ barEnum } }")
                .Build());

        // assert
        await Snapshot
            .Create().AddResult(
                res1,
                "BAR").AddResult(
                res2,
                "FOO").AddResult(
                res3,
                "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_NullableEnumNotEqual_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<FooNullable, FooNullableFilterInput>(_fooNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { barEnum: { neq: BAR } }) { barEnum } }")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { barEnum: { neq: FOO } }) { barEnum } }")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { barEnum: { neq: null } }) { barEnum } }")
                .Build());

        // assert
        await Snapshot
            .Create().AddResult(
                res1,
                "BAR").AddResult(
                res2,
                "FOO").AddResult(
                res3,
                "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_NullableEnumIn_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<FooNullable, FooNullableFilterInput>(_fooNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { barEnum: { in: [ BAR FOO ] } }) { barEnum } }")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { barEnum: { in: [ FOO ] } }) { barEnum } }")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { barEnum: { in: [ null FOO ] } }) { barEnum } }")
                .Build());

        // assert
        await Snapshot
            .Create().AddResult(
                res1,
                "BarAndFoo").AddResult(
                res2,
                "FOO").AddResult(
                res3,
                "nullAndFoo")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_NullableEnumNotIn_Expression()
    {
        // arrange
        var tester = _cache.CreateSchema<FooNullable, FooNullableFilterInput>(_fooNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { barEnum: { nin: [ BAR FOO ] } }){ barEnum } }")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { barEnum: { nin: [ FOO ] } }) { barEnum } }")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { barEnum: { nin: [ null FOO ] } }) { barEnum } }")
                .Build());

        // assert
        await Snapshot
            .Create().AddResult(
                res1,
                "BarAndFoo").AddResult(
                res2,
                "FOO").AddResult(
                res3,
                "nullAndFoo")
            .MatchAsync();
    }

    public class Foo
    {
        public int Id { get; set; }

        public FooEnum BarEnum { get; set; }
    }

    public class FooNullable
    {
        public int Id { get; set; }

        public FooEnum? BarEnum { get; set; }
    }

    public enum FooEnum
    {
        FOO,
        BAR,
        BAZ,
        QUX,
    }

    public class FooFilterInput : FilterInputType<Foo>
    {
    }

    public class FooNullableFilterInput : FilterInputType<FooNullable>
    {
    }
}
