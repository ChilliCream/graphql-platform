using HotChocolate.Data.Filters;
using HotChocolate.Execution;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Squadron;

namespace HotChocolate.Data.MongoDb.Filters;

public class MongoDbFilterVisitorListTests
    : SchemaCache
    , IClassFixture<MongoResource>
{
    private static readonly Foo[] s_fooEntities =
    [
        new()
        {
            FooNested =
            [
                new FooNested { Bar = "a" },
                new FooNested { Bar = "a" },
                new FooNested { Bar = "a" }
            ]
        },
        new()
        {
            FooNested =
            [
                new FooNested { Bar = "c" },
                new FooNested { Bar = "a" },
                new FooNested { Bar = "a" }
            ]
        },
        new()
        {
            FooNested =
            [
                new FooNested { Bar = "a" },
                new FooNested { Bar = "d" },
                new FooNested { Bar = "b" }
            ]
        },
        new()
        {
            FooNested =
            [
                new FooNested { Bar = "c" },
                new FooNested { Bar = "d" },
                new FooNested { Bar = "b" }
            ]
        },
        new()
        {
            FooNested =
            [
                new FooNested { Bar = null },
                new FooNested { Bar = "d" },
                new FooNested { Bar = "b" }
            ]
        },
        new() { FooNested = null },
        new() { FooNested = Array.Empty<FooNested>() }
    ];

    private static readonly FooSimple[] s_fooSimple =
    [
        new()
        {
            Bar =
            [
                "a",
                "a",
                "a"
            ]
        },
        new()
        {
            Bar =
            [
                "c",
                "a",
                "a"
            ]
        },
        new()
        {
            Bar =
            [
                "a",
                "d",
                "b"
            ]
        },
        new()
        {
            Bar =
            [
                "c",
                "d",
                "b"
            ]
        },
        new()
        {
            Bar =
            [
                null,
                "d",
                "b"
            ]
        },
        new() { Bar = null },
        new() { Bar = Array.Empty<string>() }
    ];

    public MongoDbFilterVisitorListTests(MongoResource resource)
    {
        Init(resource);
    }

    [Fact]
    public async Task Create_ArraySomeObjectStringEqualWithNull_Expression()
    {
        // arrange
        var tester = CreateSchema<Foo, FooFilterType>(s_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    """
                    {
                        root(where: { fooNested: { some: { bar: { eq: "a" } } } }) {
                            fooNested {
                                bar
                            }
                        }
                    }
                    """)
                .Build(),
            TestContext.Current.CancellationToken);

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(where: { fooNested: { some: {bar: { eq: \"d\"}}}}){ fooNested {bar}}}")
                .Build(),
            TestContext.Current.CancellationToken);

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(where: { fooNested: { some: {bar: { eq: null}}}}){ fooNested {bar}}}")
                .Build(),
            TestContext.Current.CancellationToken);

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "a")
            .AddResult(res2, "d")
            .AddResult(res3, "null")
            .MatchAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Create_ArrayNoneObjectStringEqual_Expression()
    {
        // arrange
        var tester = CreateSchema<Foo, FooFilterType>(s_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(where: { fooNested: { none: {bar: { eq: \"a\"}}}}){ fooNested {bar}}}")
                .Build(),
            TestContext.Current.CancellationToken);

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(where: { fooNested: { none: {bar: { eq: \"d\"}}}}){ fooNested {bar}}}")
                .Build(),
            TestContext.Current.CancellationToken);

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(where: { fooNested: { none: {bar: { eq: null}}}}){ fooNested {bar}}}")
                .Build(),
            TestContext.Current.CancellationToken);

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "a")
            .AddResult(res2, "d")
            .AddResult(res3, "null")
            .MatchAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Create_ArrayAllObjectStringEqual_Expression()
    {
        // arrange
        var tester = CreateSchema<Foo, FooFilterType>(s_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(where: { fooNested: { all: {bar: { eq: \"a\"}}}}){ fooNested {bar}}}")
                .Build(),
            TestContext.Current.CancellationToken);

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(where: { fooNested: { all: {bar: { eq: \"d\"}}}}){ fooNested {bar}}}")
                .Build(),
            TestContext.Current.CancellationToken);

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(where: { fooNested: { all: {bar: { eq: null}}}}){ fooNested {bar}}}")
                .Build(),
            TestContext.Current.CancellationToken);

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "a")
            .AddResult(res2, "d")
            .AddResult(res3, "null")
            .MatchAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Create_ArrayAnyObjectStringEqual_Expression()
    {
        // arrange
        var tester = CreateSchema<Foo, FooFilterType>(s_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { fooNested: { any: false}}){ fooNested {bar}}}")
                .Build(),
            TestContext.Current.CancellationToken);

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { fooNested: { any: true}}){ fooNested {bar}}}")
                .Build(),
            TestContext.Current.CancellationToken);

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { fooNested: { all: null}}){ fooNested {bar}}}")
                .Build(),
            TestContext.Current.CancellationToken);

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "false")
            .AddResult(res2, "true")
            .AddResult(res3, "null")
            .MatchAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Create_ArraySomeStringEqualWithNull_Expression()
    {
        // arrange
        var tester = CreateSchema<FooSimple, FooSimpleFilterType>(s_fooSimple);

        // act
        // assert
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    """
                    {
                        root(where: { bar: { some: { eq: "a" } } }) {
                            bar
                        }
                    }
                    """)
                .Build(),
            TestContext.Current.CancellationToken);

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(where: { bar: { some: { eq: \"d\"}}}){ bar }}")
                .Build(),
            TestContext.Current.CancellationToken);

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(where: { bar: { some: { eq: null}}}){ bar }}")
                .Build(),
            TestContext.Current.CancellationToken);

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "a")
            .AddResult(res2, "d")
            .AddResult(res3, "null")
            .MatchAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Create_ArrayNoneStringEqual_Expression()
    {
        // arrange
        var tester = CreateSchema<FooSimple, FooSimpleFilterType>(s_fooSimple);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(where: { bar: { none: { eq: \"a\"}}}){ bar }}")
                .Build(),
            TestContext.Current.CancellationToken);

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(where: { bar: { none: { eq: \"d\"}}}){ bar }}")
                .Build(),
            TestContext.Current.CancellationToken);

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(where: { bar: { none: { eq: null}}}){ bar }}")
                .Build(),
            TestContext.Current.CancellationToken);

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "a")
            .AddResult(res2, "d")
            .AddResult(res3, "null")
            .MatchAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Create_ArrayAllStringEqual_Expression()
    {
        // arrange
        var tester = CreateSchema<FooSimple, FooSimpleFilterType>(s_fooSimple);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(where: { bar: { all: { eq: \"a\"}}}){ bar }}")
                .Build(),
            TestContext.Current.CancellationToken);

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(where: { bar: { all: { eq: \"d\"}}}){ bar }}")
                .Build(),
            TestContext.Current.CancellationToken);

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    "{ root(where: { bar: { all: { eq: null}}}){ bar }}")
                .Build(),
            TestContext.Current.CancellationToken);

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "a")
            .AddResult(res2, "d")
            .AddResult(res3, "null")
            .MatchAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Create_ArrayAnyStringEqual_Expression()
    {
        // arrange
        var tester = CreateSchema<FooSimple, FooSimpleFilterType>(s_fooSimple);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { bar: { any: false}}){ bar }}")
                .Build(),
            TestContext.Current.CancellationToken);

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { bar: { any: true}}){ bar }}")
                .Build(),
            TestContext.Current.CancellationToken);

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument("{ root(where: { bar: { all: null}}){ bar }}")
                .Build(),
            TestContext.Current.CancellationToken);

        // assert
        await Snapshot
            .Create()
            .AddResult(res1, "false")
            .AddResult(res2, "true")
            .AddResult(res3, "null")
            .MatchAsync(TestContext.Current.CancellationToken);
    }

    public class Foo
    {
        [BsonId]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid Id { get; set; } = Guid.NewGuid();

        public IEnumerable<FooNested?>? FooNested { get; set; }
    }

    public class FooSimple
    {
        [BsonId]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid Id { get; set; } = Guid.NewGuid();

        public IEnumerable<string?>? Bar { get; set; }
    }

    public class FooNested
    {
        [BsonId]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid Id { get; set; } = Guid.NewGuid();

        public string? Bar { get; set; }
    }

    public class FooFilterType
        : FilterInputType<Foo>
    {
        protected override void Configure(IFilterInputTypeDescriptor<Foo> descriptor)
        {
            descriptor.Field(t => t.FooNested);
        }
    }

    public class FooSimpleFilterType
        : FilterInputType<FooSimple>
    {
        protected override void Configure(IFilterInputTypeDescriptor<FooSimple> descriptor)
        {
            descriptor.Field(t => t.Bar);
        }
    }
}
