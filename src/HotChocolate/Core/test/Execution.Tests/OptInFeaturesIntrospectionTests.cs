using CookieCrumble;
using HotChocolate.Types;

namespace HotChocolate.Execution;

public sealed class OptInFeaturesIntrospectionTests
{
    [Fact]
    public async Task Execute_IntrospectionOnSchema_MatchesSnapshot()
    {
        // arrange
        const string query =
            """
            {
                __schema {
                    optInFeatures
                    optInFeatureStability {
                        feature
                        stability
                    }
                }
            }
            """;

        var executor = CreateSchema().MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(query);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "__schema": {
                  "optInFeatures": [
                    "enumValueFeature1",
                    "enumValueFeature2",
                    "inputFieldFeature1",
                    "inputFieldFeature2",
                    "objectFieldArgFeature1",
                    "objectFieldArgFeature2",
                    "objectFieldFeature1",
                    "objectFieldFeature2"
                  ],
                  "optInFeatureStability": [
                    {
                      "feature": "enumValueFeature1",
                      "stability": "draft"
                    },
                    {
                      "feature": "enumValueFeature2",
                      "stability": "experimental"
                    }
                  ]
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Execute_IntrospectionOnObjectFields_MatchesSnapshot()
    {
        // arrange
        const string query =
            """
            {
                __type(name: "Query") {
                    fields(includeOptIn: ["objectFieldFeature1"]) {
                        requiresOptIn
                    }
                }
            }
            """;

        var executor = CreateSchema().MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(query);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "__type": {
                  "fields": [
                    {
                      "requiresOptIn": [
                        "objectFieldFeature1",
                        "objectFieldFeature2"
                      ]
                    }
                  ]
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Execute_IntrospectionOnObjectFieldsFeatureDoesNotExist_MatchesSnapshot()
    {
        // arrange
        const string query =
            """
            {
                __type(name: "Query") {
                    fields(includeOptIn: ["objectFieldFeatureDoesNotExist"]) {
                        requiresOptIn
                    }
                }
            }
            """;

        var executor = CreateSchema().MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(query);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "__type": {
                  "fields": []
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Execute_IntrospectionOnObjectFieldsNoIncludedFeatures_MatchesSnapshot()
    {
        // arrange
        const string query =
            """
            {
                __type(name: "Query") {
                    fields {
                        requiresOptIn
                    }
                }
            }
            """;

        var executor = CreateSchema().MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(query);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "__type": {
                  "fields": []
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Execute_IntrospectionOnArgs_MatchesSnapshot()
    {
        // arrange
        const string query =
            """
            {
                __type(name: "Query") {
                    fields(includeOptIn: ["objectFieldFeature1"]) {
                        args(includeOptIn: ["objectFieldArgFeature1"]) {
                            requiresOptIn
                        }
                    }
                }
            }
            """;

        var executor = CreateSchema().MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(query);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "__type": {
                  "fields": [
                    {
                      "args": [
                        {
                          "requiresOptIn": [
                            "objectFieldArgFeature1",
                            "objectFieldArgFeature2"
                          ]
                        }
                      ]
                    }
                  ]
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Execute_IntrospectionOnArgsFeatureDoesNotExist_MatchesSnapshot()
    {
        // arrange
        const string query =
            """
            {
                __type(name: "Query") {
                    fields(includeOptIn: ["objectFieldFeature1"]) {
                        args(includeOptIn: ["objectFieldArgFeatureDoesNotExist"]) {
                            requiresOptIn
                        }
                    }
                }
            }
            """;

        var executor = CreateSchema().MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(query);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "__type": {
                  "fields": [
                    {
                      "args": []
                    }
                  ]
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Execute_IntrospectionOnArgsNoIncludedFeatures_MatchesSnapshot()
    {
        // arrange
        const string query =
            """
            {
                __type(name: "Query") {
                    fields(includeOptIn: ["objectFieldFeature1"]) {
                        args {
                            requiresOptIn
                        }
                    }
                }
            }
            """;

        var executor = CreateSchema().MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(query);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "__type": {
                  "fields": [
                    {
                      "args": []
                    }
                  ]
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Execute_IntrospectionOnInputFields_MatchesSnapshot()
    {
        // arrange
        const string query =
            """
            {
                __type(name: "ExampleInput") {
                    inputFields(includeOptIn: ["inputFieldFeature1"]) {
                        requiresOptIn
                    }
                }
            }
            """;

        var executor = CreateSchema().MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(query);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "__type": {
                  "inputFields": [
                    {
                      "requiresOptIn": [
                        "inputFieldFeature1",
                        "inputFieldFeature2"
                      ]
                    }
                  ]
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Execute_IntrospectionOnInputFieldsFeatureDoesNotExist_MatchesSnapshot()
    {
        // arrange
        const string query =
            """
            {
                __type(name: "ExampleInput") {
                    inputFields(includeOptIn: ["inputFieldFeatureDoesNotExist"]) {
                        requiresOptIn
                    }
                }
            }
            """;

        var executor = CreateSchema().MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(query);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "__type": {
                  "inputFields": []
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Execute_IntrospectionOnInputFieldsNoIncludedFeatures_MatchesSnapshot()
    {
        // arrange
        const string query =
            """
            {
                __type(name: "ExampleInput") {
                    inputFields {
                        requiresOptIn
                    }
                }
            }
            """;

        var executor = CreateSchema().MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(query);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "__type": {
                  "inputFields": []
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Execute_IntrospectionOnEnumValues_MatchesSnapshot()
    {
        // arrange
        const string query =
            """
            {
                __type(name: "ExampleEnum") {
                    enumValues(includeOptIn: ["enumValueFeature1"]) {
                        requiresOptIn
                    }
                }
            }
            """;

        var executor = CreateSchema().MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(query);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "__type": {
                  "enumValues": [
                    {
                      "requiresOptIn": [
                        "enumValueFeature1",
                        "enumValueFeature2"
                      ]
                    }
                  ]
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Execute_IntrospectionOnEnumValuesFeatureDoesNotExist_MatchesSnapshot()
    {
        // arrange
        const string query =
            """
            {
                __type(name: "ExampleEnum") {
                    enumValues(includeOptIn: ["enumValueFeatureDoesNotExist"]) {
                        requiresOptIn
                    }
                }
            }
            """;

        var executor = CreateSchema().MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(query);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "__type": {
                  "enumValues": []
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Execute_IntrospectionOnEnumValuesNoIncludedFeatures_MatchesSnapshot()
    {
        // arrange
        const string query =
            """
            {
                __type(name: "ExampleEnum") {
                    enumValues {
                        requiresOptIn
                    }
                }
            }
            """;

        var executor = CreateSchema().MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(query);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "__type": {
                  "enumValues": []
                }
              }
            }
            """);
    }

    private static ISchema CreateSchema()
    {
        return SchemaBuilder.New()
            .SetSchema(
                s => s
                    .OptInFeatureStability("enumValueFeature1", "draft")
                    .OptInFeatureStability("enumValueFeature2", "experimental"))
            .AddQueryType<QueryType>()
            .AddType<ExampleInputType>()
            .AddType<ExampleEnumType>()
            .ModifyOptions(o => o.EnableOptInFeatures = true)
            .Create();
    }

    private sealed class QueryType : ObjectType
    {
        protected override void Configure(IObjectTypeDescriptor descriptor)
        {
            descriptor.Name(OperationTypeNames.Query);

            descriptor
                .Field("field")
                .Type<IntType>()
                .Argument(
                    "argument",
                    a => a
                        .Type<IntType>()
                        .RequiresOptIn("objectFieldArgFeature1")
                        .RequiresOptIn("objectFieldArgFeature2"))
                .Resolve(() => 1)
                .RequiresOptIn("objectFieldFeature1")
                .RequiresOptIn("objectFieldFeature2");
        }
    }

    private sealed class ExampleInputType : InputObjectType
    {
        protected override void Configure(IInputObjectTypeDescriptor descriptor)
        {
            descriptor
                .Field("field")
                .Type<IntType>()
                .RequiresOptIn("inputFieldFeature1")
                .RequiresOptIn("inputFieldFeature2");
        }
    }

    private sealed class ExampleEnumType : EnumType
    {
        protected override void Configure(IEnumTypeDescriptor descriptor)
        {
            descriptor
                .Name("ExampleEnum")
                .Value("VALUE")
                    .RequiresOptIn("enumValueFeature1")
                    .RequiresOptIn("enumValueFeature2");
        }
    }
}
