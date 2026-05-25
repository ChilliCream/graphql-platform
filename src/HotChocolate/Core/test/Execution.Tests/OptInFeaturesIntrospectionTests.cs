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
                        name
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
                      "name": "field1",
                      "requiresOptIn": [
                        "objectFieldFeature1",
                        "objectFieldFeature2"
                      ]
                    },
                    {
                      "name": "field2",
                      "requiresOptIn": []
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
                        name
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
                      "name": "field2",
                      "requiresOptIn": []
                    }
                  ]
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
                        name
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
                      "name": "field2",
                      "requiresOptIn": []
                    }
                  ]
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
                        name
                        args(includeOptIn: ["objectFieldArgFeature1"]) {
                            name
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
                      "name": "field1",
                      "args": [
                        {
                          "name": "argument1",
                          "requiresOptIn": [
                            "objectFieldArgFeature1",
                            "objectFieldArgFeature2"
                          ]
                        },
                        {
                          "name": "argument2",
                          "requiresOptIn": []
                        }
                      ]
                    },
                    {
                      "name": "field2",
                      "args": []
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
                        name
                        args(includeOptIn: ["objectFieldArgFeatureDoesNotExist"]) {
                            name
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
                      "name": "field1",
                      "args": [
                        {
                          "name": "argument2",
                          "requiresOptIn": []
                        }
                      ]
                    },
                    {
                      "name": "field2",
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
                        name
                        args {
                            name
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
                      "name": "field1",
                      "args": [
                        {
                          "name": "argument2",
                          "requiresOptIn": []
                        }
                      ]
                    },
                    {
                      "name": "field2",
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
                        name
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
                      "name": "field1",
                      "requiresOptIn": [
                        "inputFieldFeature1",
                        "inputFieldFeature2"
                      ]
                    },
                    {
                      "name": "field2",
                      "requiresOptIn": []
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
                        name
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
                      "name": "field2",
                      "requiresOptIn": []
                    }
                  ]
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
                        name
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
                      "name": "field2",
                      "requiresOptIn": []
                    }
                  ]
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
                        name
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
                      "name": "VALUE1",
                      "requiresOptIn": [
                        "enumValueFeature1",
                        "enumValueFeature2"
                      ]
                    },
                    {
                      "name": "VALUE2",
                      "requiresOptIn": []
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
                        name
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
                      "name": "VALUE2",
                      "requiresOptIn": []
                    }
                  ]
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
                        name
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
                      "name": "VALUE2",
                      "requiresOptIn": []
                    }
                  ]
                }
              }
            }
            """);
    }

    private static Schema CreateSchema()
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
            .Use(next => next)
            .Create();
    }

    private sealed class QueryType : ObjectType
    {
        protected override void Configure(IObjectTypeDescriptor descriptor)
        {
            descriptor.Name(OperationTypeNames.Query);

            descriptor
                .Field("field1")
                .Type<IntType>()
                .Argument(
                    "argument1",
                    a => a
                        .Type<IntType>()
                        .RequiresOptIn("objectFieldArgFeature1")
                        .RequiresOptIn("objectFieldArgFeature2"))
                .Argument(
                    "argument2",
                    a => a.Type<IntType>())
                .Argument(
                    "argument3",
                    a => a
                        .Type<IntType>()
                        .Deprecated())
                .RequiresOptIn("objectFieldFeature1")
                .RequiresOptIn("objectFieldFeature2");

            descriptor
                .Field("field2")
                .Type<IntType>();

            descriptor
                .Field("field3")
                .Type<IntType>()
                .Deprecated();
        }
    }

    private sealed class ExampleInputType : InputObjectType
    {
        protected override void Configure(IInputObjectTypeDescriptor descriptor)
        {
            descriptor
                .Field("field1")
                .Type<IntType>()
                .RequiresOptIn("inputFieldFeature1")
                .RequiresOptIn("inputFieldFeature2");

            descriptor
                .Field("field2")
                .Type<IntType>();

            descriptor
                .Field("field3")
                .Type<IntType>()
                .Deprecated();
        }
    }

    private sealed class ExampleEnumType : EnumType
    {
        protected override void Configure(IEnumTypeDescriptor descriptor)
        {
            descriptor.Name("ExampleEnum");

            descriptor
                .Value("VALUE1")
                .RequiresOptIn("enumValueFeature1")
                .RequiresOptIn("enumValueFeature2");

            descriptor
                .Value("VALUE2");

            descriptor
                .Value("VALUE3")
                .Deprecated();
        }
    }
}
