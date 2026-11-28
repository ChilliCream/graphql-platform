using HotChocolate.Execution.Caching;
using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution;

public sealed class VariableCoercionPlanCacheTests
{
    private const string Document =
        """
        query A($value: Int) { number(value: $value) }
        query B($value: String) { text(value: $value) }
        """;

    [Fact]
    public void GetOrCreate_Should_ReturnSamePlan_When_OperationNodeIsKnown()
    {
        // arrange
        var schema = CreateSchema();
        var cache = new VariableCoercionPlanCache(capacity: 16);
        var operation = GetOperation("query($value: Int) { number(value: $value) }");

        // act
        var firstPlan = cache.GetOrCreate(operation, schema);
        var secondPlan = cache.GetOrCreate(operation, schema);

        // assert
        Assert.Same(firstPlan, secondPlan);
        Assert.Equal(1, cache.Count);
    }

    [Fact]
    public void GetOrCreate_Should_ReturnPlanOfOperation_When_OperationNodesDiffer()
    {
        // arrange
        var schema = CreateSchema();
        var cache = new VariableCoercionPlanCache(capacity: 16);
        var first = GetOperation("query($value: Int) { number(value: $value) }");
        var second = GetOperation("query($value: String) { text(value: $value) }");

        // act
        var firstPlan = cache.GetOrCreate(first, schema);
        var secondPlan = cache.GetOrCreate(second, schema);

        // assert
        Assert.Equal("Int", firstPlan.GetInputType(0)?.NamedType().Name);
        Assert.Equal("String", secondPlan.GetInputType(0)?.NamedType().Name);
        Assert.Equal(2, cache.Count);
    }

    [Fact]
    public void GetOrCreate_Should_ReturnNullInputType_When_VariableTypeIsNotAnInputType()
    {
        // arrange
        var schema = CreateSchema();
        var cache = new VariableCoercionPlanCache(capacity: 16);
        var operation = GetOperation("query($value: Query) { number }");

        // act
        var plan = cache.GetOrCreate(operation, schema);

        // assert
        Assert.Null(plan.GetInputType(0));
    }

    [Fact]
    public async Task Capacity_Should_EqualPreparedOperationCacheSize_When_SizeIsConfigured()
    {
        // arrange
        const int cacheCapacity = 517;
        var services = new ServiceCollection();
        services
            .AddGraphQL()
            .ModifyOptions(o => o.PreparedOperationCacheSize = cacheCapacity)
            .AddQueryType(d => d.Field("foo").Resolve(""));
        var executor = await services.BuildServiceProvider().GetRequestExecutorAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        // act
        var cache = executor.Schema.Services.GetRequiredService<VariableCoercionPlanCache>();

        // assert
        Assert.Equal(cacheCapacity, cache.Capacity);
    }

    [Fact]
    public async Task ExecuteAsync_Should_CoerceVariablesOfSelectedOperation_When_DocumentHasTwoOperations()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var executor = await CreateExecutorAsync();
        var results = new List<string>();

        // act
        foreach (var (operationName, variableValues) in new[]
        {
            ("A", """{ "value": 1 }"""),
            ("B", """{ "value": "b" }"""),
            ("A", """{ "value": 2 }""")
        })
        {
            await using var result = await executor.ExecuteAsync(
                CreateRequest(operationName, variableValues),
                cancellationToken);
            results.Add(result.ToJson());
        }

        // assert
        results.MatchInlineSnapshots(
            [
                """
                {
                  "data": {
                    "number": 1
                  }
                }
                """,
                """
                {
                  "data": {
                    "text": "b"
                  }
                }
                """,
                """
                {
                  "data": {
                    "number": 2
                  }
                }
                """
            ]);
    }

    [Fact]
    public async Task ExecuteAsync_Should_CacheOnePlanPerOperation_When_OperationsAreRepeated()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var executor = await CreateExecutorAsync();
        var cache = executor.Schema.Services.GetRequiredService<VariableCoercionPlanCache>();

        // act
        foreach (var (operationName, variableValues) in new[]
        {
            ("A", """{ "value": 1 }"""),
            ("B", """{ "value": "b" }"""),
            ("A", """{ "value": 2 }""")
        })
        {
            await using var result = await executor.ExecuteAsync(
                CreateRequest(operationName, variableValues),
                cancellationToken);
        }

        // assert
        Assert.Equal(2, cache.Count);
    }

    [Fact]
    public async Task ExecuteAsync_Should_ReportVariableError_When_OperationWasCoercedBefore()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var executor = await CreateExecutorAsync();
        await using var first = await executor.ExecuteAsync(
            CreateRequest("A", """{ "value": 1 }"""),
            cancellationToken);

        // act
        await using var second = await executor.ExecuteAsync(
            CreateRequest("A", """{ "value": "a" }"""),
            cancellationToken);

        // assert
        second.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "Int cannot coerce the given value JSON element of type `String` to a runtime value.",
                  "extensions": {
                    "inputPath": [
                      "value"
                    ],
                    "fieldType": "Int"
                  }
                }
              ]
            }
            """);
    }

    private static IOperationRequest CreateRequest(string operationName, string variableValues)
        => OperationRequestBuilder
            .New()
            .SetDocument(Document)
            .SetOperationName(operationName)
            .SetVariableValues(variableValues)
            .Build();

    private static async Task<IRequestExecutor> CreateExecutorAsync()
        => await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(ConfigureQuery)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

    private static Schema CreateSchema()
        => SchemaBuilder.New().AddQueryType(ConfigureQuery).Create();

    private static void ConfigureQuery(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name("Query");

        descriptor
            .Field("number")
            .Argument("value", a => a.Type<IntType>())
            .Type<IntType>()
            .Resolve(ctx => ctx.ArgumentValue<int?>("value"));

        descriptor
            .Field("text")
            .Argument("value", a => a.Type<StringType>())
            .Type<StringType>()
            .Resolve(ctx => ctx.ArgumentValue<string?>("value"));
    }

    private static OperationDefinitionNode GetOperation(string document)
        => (OperationDefinitionNode)Utf8GraphQLParser.Parse(document).Definitions[0];
}
