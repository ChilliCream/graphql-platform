using HotChocolate.Execution;

namespace HotChocolate.Data.Filters;

public class QueryableFilterVisitorVariablesTests(SchemaCache cache) : IClassFixture<SchemaCache>
{
    private static readonly Foo[] _fooEntities =
    [
        new Foo { Bar = true, },
        new Foo { Bar = false, },
    ];

    [Fact]
    public async Task Create_BooleanEqual_Expression()
    {
        // arrange
        var tester = cache.CreateSchema<Foo, FooFilterInput>(_fooEntities);
        const string query =
            "query Test($where: Boolean){ root(where: {bar: { eq: $where}}){ bar}}";

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(query)
                .SetVariableValues(new Dictionary<string, object?> { { "where", true }, })
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(query)
                .SetVariableValues(new Dictionary<string, object?> { { "where", false }, })
                .Build());

        // assert
        await Snapshot
            .Create()
            .Add(res1, "true")
            .Add(res2, "false")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_BooleanEqual_Expression_NonNull()
    {
        // arrange
        var tester = cache.CreateSchema<Foo, FooFilterInput>(_fooEntities);
        const string query =
            "query Test($where: Boolean!){ root(where: {bar: { eq: $where}}){ bar}}";

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(query)
                .SetVariableValues(new Dictionary<string, object?> { { "where", true}, })
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(query)
                .SetVariableValues(new Dictionary<string, object?> { { "where", false}, })
                .Build());

        // assert
        await Snapshot
            .Create()
            .Add(res1, "true")
            .Add(res2, "false")
            .MatchAsync();
    }

    [Fact]
    public async Task ExecuteAsync_Should_Error_When_FilterVariableExceedsMaxAllowedOperations()
    {
        // arrange
        var convention = new FilterConvention(
            x => x
                .AddDefaults()
                .BindRuntimeType<Foo, FooFilterInput>()
                .MaxAllowedFilterOperations(2));
        var tester = cache.CreateSchemaWithConvention<Foo, FooFilterInput>(_fooEntities, convention);
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
                    { "where", CreateOrFilter(3) },
                })
                .Build());

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The filter argument contains 3 operations, which exceeds the maximum allowed number of 2.",
                  "locations": [
                    {
                      "line": 2,
                      "column": 3
                    }
                  ],
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
        var tester = cache.CreateSchema<Foo, FooFilterInput>(_fooEntities);
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
                            FilterConventionDefinition.DefaultMaxAllowedFilterOperations + 1)
                    },
                })
                .Build());

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The filter argument contains 65 operations, which exceeds the maximum allowed number of 64.",
                  "locations": [
                    {
                      "line": 2,
                      "column": 3
                    }
                  ],
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
        var convention = new FilterConvention(
            x => x
                .AddDefaults()
                .BindRuntimeType<Foo, FooFilterInput>()
                .MaxAllowedFilterOperations(null));
        var tester = cache.CreateSchemaWithConvention<Foo, FooFilterInput>(_fooEntities, convention);
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
                    { "where", CreateOrFilter(5_000) },
                })
                .Build());

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
                        { "eq", true },
                    }
                },
            };
        }

        return new Dictionary<string, object?>
        {
            { "or", items },
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
