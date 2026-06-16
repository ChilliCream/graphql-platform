using HotChocolate.Execution;

namespace HotChocolate.Data.Filters;

public class QueryableFilterVisitorVariablesTests(SchemaCache cache) : IClassFixture<SchemaCache>
{
    private static readonly Foo[] s_fooEntities =
    [
        new Foo { Bar = true },
        new Foo { Bar = false }
    ];

    [Fact]
    public async Task Create_BooleanEqual_Expression()
    {
        // arrange
        var tester = cache.CreateSchema<Foo, FooFilterInput>(s_fooEntities);
        const string query =
            "query Test($where: Boolean){ root(where: {bar: { eq: $where}}){ bar}}";

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(query)
                .SetVariableValues(new Dictionary<string, object?> { { "where", true } })
                .Build(),
            TestContext.Current.CancellationToken);

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(query)
                .SetVariableValues(new Dictionary<string, object?> { { "where", false } })
                .Build(),
            TestContext.Current.CancellationToken);

        // assert
        await Snapshot
            .Create()
            .Add(res1, "true")
            .Add(res2, "false")
            .MatchAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Create_BooleanEqual_Expression_NonNull()
    {
        // arrange
        var tester = cache.CreateSchema<Foo, FooFilterInput>(s_fooEntities);
        const string query =
            "query Test($where: Boolean!){ root(where: {bar: { eq: $where}}){ bar}}";

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(query)
                .SetVariableValues(new Dictionary<string, object?> { { "where", true } })
                .Build(),
            TestContext.Current.CancellationToken);

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(query)
                .SetVariableValues(new Dictionary<string, object?> { { "where", false } })
                .Build(),
            TestContext.Current.CancellationToken);

        // assert
        await Snapshot
            .Create()
            .Add(res1, "true")
            .Add(res2, "false")
            .MatchAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Error_When_FilterVariableExceedsMaxAllowedOperations()
    {
        // arrange
        var entities = new[]
        {
            new Foo { Bar = true },
            new Foo { Bar = false }
        };
        var convention = new FilterConvention(
            x => x
                .AddDefaults()
                .BindRuntimeType<Foo, FooFilterInput>()
                .MaxAllowedFilterOperations(2));
        var tester = cache.CreateSchemaWithConvention<Foo, FooFilterInput>(entities, convention);
        const string query =
            """
            query Test($where: FooFilterInput) {
              root(where: $where) {
                bar
              }
            }
            """;

        // act
        var result = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(query)
                .SetVariableValues(new Dictionary<string, object?>
                {
                    { "where", CreateOrFilter(3) }
                })
                .Build(),
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The filter argument contains 3 operations, which exceeds the maximum allowed number of 2.",
                  "path": [
                    "root"
                  ],
                  "extensions": {
                    "code": "HC0117",
                    "filterOperations": 3,
                    "maxAllowedFilterOperations": 2
                  }
                }
              ],
              "data": {
                "root": null
              }
            }
            """);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Error_When_FilterVariableExceedsDefaultMaxAllowedOperations()
    {
        // arrange
        var entities = new[]
        {
            new Foo { Bar = true },
            new Foo { Bar = false }
        };
        var tester = cache.CreateSchema<Foo, FooFilterInput>(entities);
        const string query =
            """
            query Test($where: FooFilterInput) {
              root(where: $where) {
                bar
              }
            }
            """;

        // act
        var result = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(query)
                .SetVariableValues(new Dictionary<string, object?>
                {
                    {
                        "where",
                        CreateOrFilter(
                            FilterConventionConfiguration.DefaultMaxAllowedFilterOperations + 1)
                    }
                })
                .Build(),
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The filter argument contains 65 operations, which exceeds the maximum allowed number of 64.",
                  "path": [
                    "root"
                  ],
                  "extensions": {
                    "code": "HC0117",
                    "filterOperations": 65,
                    "maxAllowedFilterOperations": 64
                  }
                }
              ],
              "data": {
                "root": null
              }
            }
            """);
    }

    [Fact]
    public async Task ExecuteAsync_Should_NotOverflow_When_FilterVariableContainsLargeOrArray()
    {
        // arrange
        var entities = new[]
        {
            new Foo { Bar = true },
            new Foo { Bar = false }
        };
        var convention = new FilterConvention(
            x => x
                .AddDefaults()
                .BindRuntimeType<Foo, FooFilterInput>()
                .MaxAllowedFilterOperations(null));
        var tester = cache.CreateSchemaWithConvention<Foo, FooFilterInput>(entities, convention);
        const string query =
            """
            query Test($where: FooFilterInput) {
              root(where: $where) {
                bar
              }
            }
            """;

        // act
        var result = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(query)
                .SetVariableValues(new Dictionary<string, object?>
                {
                    { "where", CreateOrFilter(5_000) }
                })
                .Build(),
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "root": [
                  {
                    "bar": true
                  }
                ]
              }
            }
            """);
    }

    private static Dictionary<string, object?> CreateOrFilter(int itemCount)
    {
        var items = new object?[itemCount];

        for (var i = 0; i < items.Length; i++)
        {
            items[i] = new Dictionary<string, object?>
            {
                {
                    "bar",
                    new Dictionary<string, object?>
                    {
                        { "eq", true }
                    }
                }
            };
        }

        return new Dictionary<string, object?>
        {
            { "or", items }
        };
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

    public class FooFilterInput : FilterInputType<Foo>;

    public class FooNullableFilterInput : FilterInputType<FooNullable>;
}
