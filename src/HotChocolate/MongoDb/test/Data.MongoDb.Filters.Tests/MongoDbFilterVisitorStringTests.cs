using CookieCrumble;
using HotChocolate.Data.Filters;
using HotChocolate.Execution;
using MongoDB.Bson.Serialization.Attributes;
using Squadron;

namespace HotChocolate.Data.MongoDb.Filters;

public class MongoDbFilterVisitorStringTests
    : SchemaCache
    , IClassFixture<MongoResource>
{
    private static readonly Foo[] _fooEntities =
    [
        new() { Bar = "testatest", },
        new() { Bar = "testbtest", },
    ];

    private static readonly FooNullable[] _fooNullableEntities =
    [
        new() { Bar = "testatest", },
        new() { Bar = "testbtest", },
        new() { Bar = null, },
    ];

    public MongoDbFilterVisitorStringTests(MongoResource resource)
    {
        Init(resource);
    }

    [Fact]
    public async Task Create_StringEqual_Expression()
    {
        // arrange
        var tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { eq: \"testatest\"}}){ bar}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { eq: \"testbtest\"}}){ bar}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { eq: null}}){ bar}}")
                .Build());

        // arrange
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        Snapshot
                            .Create(), res1, "testatest"), res2, "testbtest"), res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_StringNotEqual_Expression()
    {
        // arrange
        var tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { neq: \"testatest\"}}){ bar}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { neq: \"testbtest\"}}){ bar}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { neq: null}}){ bar}}")
                .Build());

        // arrange
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        Snapshot
                            .Create(), res1, "testatest"), res2, "testbtest"), res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_StringIn_Expression()
    {
        // arrange
        var tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { bar: { in: [ \"testatest\"  \"testbtest\" ]}}){ bar}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { in: [\"testbtest\" null]}}){ bar}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { in: [ \"testatest\" ]}}){ bar}}")
                .Build());

        // arrange
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        Snapshot
                            .Create(), res1, "testatestAndtestb"), res2, "testbtestAndNull"), res3, "testatest")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_StringNotIn_Expression()
    {
        // arrange
        var tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { bar: { nin: [ \"testatest\"  \"testbtest\" ]}}){ bar}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { nin: [\"testbtest\" null]}}){ bar}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { nin: [ \"testatest\" ]}}){ bar}}")
                .Build());

        // arrange
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        Snapshot
                            .Create(), res1, "testatestAndtestb"), res2, "testbtestAndNull"), res3, "testatest")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_StringContains_Expression()
    {
        // arrange
        var tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { contains: \"a\" }}){ bar}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { contains: \"b\" }}){ bar}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { contains: null }}){ bar}}")
                .Build());

        // arrange
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        Snapshot
                            .Create(), res1, "a"), res2, "b"), res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_StringNoContains_Expression()
    {
        // arrange
        var tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { ncontains: \"a\" }}){ bar}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { ncontains: \"b\" }}){ bar}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { ncontains: null }}){ bar}}")
                .Build());

        // arrange
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        Snapshot
                            .Create(), res1, "a"), res2, "b"), res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_StringStartsWith_Expression()
    {
        // arrange
        var tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { startsWith: \"testa\" }}){ bar}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { startsWith: \"testb\" }}){ bar}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { startsWith: null }}){ bar}}")
                .Build());

        // arrange
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        Snapshot
                            .Create(), res1, "testa"), res2, "testb"), res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_StringNotStartsWith_Expression()
    {
        // arrange
        var tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { nstartsWith: \"testa\" }}){ bar}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { nstartsWith: \"testb\" }}){ bar}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { nstartsWith: null }}){ bar}}")
                .Build());

        // arrange
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        Snapshot
                            .Create(), res1, "testa"), res2, "testb"), res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_StringEndsWith_Expression()
    {
        // arrange
        var tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { endsWith: \"atest\" }}){ bar}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { endsWith: \"btest\" }}){ bar}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { endsWith: null }}){ bar}}")
                .Build());

        // arrange
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        Snapshot
                            .Create(), res1, "atest"), res2, "btest"), res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_StringNotEndsWith_Expression()
    {
        // arrange
        var tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { nendsWith: \"atest\" }}){ bar}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { nendsWith: \"btest\" }}){ bar}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { nendsWith: null }}){ bar}}")
                .Build());

        // arrange
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        Snapshot
                            .Create(), res1, "atest"), res2, "btest"), res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_NullableStringEqual_Expression()
    {
        // arrange
        var tester = CreateSchema<FooNullable, FooNullableFilterType>(_fooNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { eq: \"testatest\"}}){ bar}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { eq: \"testbtest\"}}){ bar}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { eq: null}}){ bar}}")
                .Build());

        // arrange
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        Snapshot
                            .Create(), res1, "testatest"), res2, "testbtest"), res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_NullableStringNotEqual_Expression()
    {
        // arrange
        var tester = CreateSchema<FooNullable, FooNullableFilterType>(_fooNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { neq: \"testatest\"}}){ bar}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { neq: \"testbtest\"}}){ bar}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { neq: null}}){ bar}}")
                .Build());

        // arrange
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        Snapshot
                            .Create(), res1, "testatest"), res2, "testbtest"), res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_NullableStringIn_Expression()
    {
        // arrange
        var tester = CreateSchema<FooNullable, FooNullableFilterType>(_fooNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { bar: { in: [ \"testatest\"  \"testbtest\" ]}}){ bar}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { in: [\"testbtest\" null]}}){ bar}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { in: [ \"testatest\" ]}}){ bar}}")
                .Build());

        // arrange
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        Snapshot
                            .Create(), res1, "testatestAndtestb"), res2, "testbtestAndNull"), res3, "testatest")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_NullableStringNotIn_Expression()
    {
        // arrange
        var tester = CreateSchema<FooNullable, FooNullableFilterType>(_fooNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument(
                    "{ root(where: { bar: { nin: [ \"testatest\"  \"testbtest\" ]}}){ bar}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { nin: [\"testbtest\" null]}}){ bar}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { nin: [ \"testatest\" ]}}){ bar}}")
                .Build());

        // arrange
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        Snapshot
                            .Create(), res1, "testatestAndtestb"), res2, "testbtestAndNull"), res3, "testatest")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_NullableStringContains_Expression()
    {
        // arrange
        var tester = CreateSchema<FooNullable, FooNullableFilterType>(_fooNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { contains: \"a\" }}){ bar}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { contains: \"b\" }}){ bar}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { contains: null }}){ bar}}")
                .Build());

        // arrange
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        Snapshot
                            .Create(), res1, "a"), res2, "b"), res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_NullableStringNoContains_Expression()
    {
        // arrange
        var tester = CreateSchema<FooNullable, FooNullableFilterType>(_fooNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { ncontains: \"a\" }}){ bar}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { ncontains: \"b\" }}){ bar}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { ncontains: null }}){ bar}}")
                .Build());

        // arrange
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        Snapshot
                            .Create(), res1, "a"), res2, "b"), res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_NullableStringStartsWith_Expression()
    {
        // arrange
        var tester = CreateSchema<FooNullable, FooNullableFilterType>(_fooNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { startsWith: \"testa\" }}){ bar}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { startsWith: \"testb\" }}){ bar}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { startsWith: null }}){ bar}}")
                .Build());

        // arrange
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        Snapshot
                            .Create(), res1, "testa"), res2, "testb"), res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_NullableStringNotStartsWith_Expression()
    {
        // arrange
        var tester = CreateSchema<FooNullable, FooNullableFilterType>(_fooNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { nstartsWith: \"testa\" }}){ bar}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { nstartsWith: \"testb\" }}){ bar}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { nstartsWith: null }}){ bar}}")
                .Build());

        // arrange
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        Snapshot
                            .Create(), res1, "testa"), res2, "testb"), res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_NullableStringEndsWith_Expression()
    {
        // arrange
        var tester = CreateSchema<FooNullable, FooNullableFilterType>(_fooNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { endsWith: \"atest\" }}){ bar}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { endsWith: \"btest\" }}){ bar}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { endsWith: null }}){ bar}}")
                .Build());

        // arrange
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        Snapshot
                            .Create(), res1, "atest"), res2, "btest"), res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_NullableStringNotEndsWith_Expression()
    {
        // arrange
        var tester = CreateSchema<FooNullable, FooNullableFilterType>(_fooNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { nendsWith: \"atest\" }}){ bar}}")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { nendsWith: \"btest\" }}){ bar}}")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { nendsWith: null }}){ bar}}")
                .Build());

        // arrange
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        Snapshot
                            .Create(), res1, "atest"), res2, "btest"), res3, "null")
            .MatchAsync();
    }

    public class Foo
    {
        [BsonId]
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Bar { get; set; } = null!;
    }

    public class FooNullable
    {
        [BsonId]
        public Guid Id { get; set; } = Guid.NewGuid();

        public string? Bar { get; set; }
    }

    public class FooFilterType : FilterInputType<Foo>
    {
        protected override void Configure(
            IFilterInputTypeDescriptor<Foo> descriptor)
        {
            descriptor.Field(t => t.Bar);
        }
    }

    public class FooNullableFilterType : FilterInputType<FooNullable>
    {
        protected override void Configure(
            IFilterInputTypeDescriptor<FooNullable> descriptor)
        {
            descriptor.Field(t => t.Bar);
        }
    }
}
