using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution.Introspection;

public sealed class OptInFeaturesIntrospectionTests : FusionTestBase
{
    // An opt-in field is absent from __Type.fields when the caller supplies no includeOptIn
    // argument and EnableOptInFeatures is on.
    [Fact]
    public async Task Fields_OptInField_HiddenByDefault()
    {
        // arrange
        var services = new ServiceCollection();
        services.AddHttpClient();
        services
            .AddGraphQLGateway()
            .ModifyOptions(o => o.EnableOptInFeatures = true)
            .AddInMemoryConfiguration(
                ComposeSchemaDocument(
                    """
                    type Query {
                        field: String
                        experimentalField: String @requiresOptIn(feature: "experimental")
                    }

                    directive @requiresOptIn(feature: String!) repeatable on
                        | FIELD_DEFINITION
                        | ARGUMENT_DEFINITION
                        | ENUM_VALUE
                        | INPUT_FIELD_DEFINITION
                        | DIRECTIVE_DEFINITION
                    """))
            .UseDefaultPipeline();

        var executor = await services
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    """
                    {
                        __type(name: "Query") {
                            fields {
                                name
                            }
                        }
                    }
                    """)
                .Build(),
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "__type": {
                  "fields": [
                    {
                      "name": "field"
                    }
                  ]
                }
              }
            }
            """);
    }

    // An opt-in field appears in __Type.fields when the caller passes includeOptIn with the
    // matching feature name.
    [Fact]
    public async Task Fields_OptInField_RevealedByIncludeOptIn()
    {
        // arrange
        var services = new ServiceCollection();
        services.AddHttpClient();
        services
            .AddGraphQLGateway()
            .ModifyOptions(o => o.EnableOptInFeatures = true)
            .AddInMemoryConfiguration(
                ComposeSchemaDocument(
                    """
                    type Query {
                        field: String
                        experimentalField: String @requiresOptIn(feature: "experimental")
                    }

                    directive @requiresOptIn(feature: String!) repeatable on
                        | FIELD_DEFINITION
                        | ARGUMENT_DEFINITION
                        | ENUM_VALUE
                        | INPUT_FIELD_DEFINITION
                        | DIRECTIVE_DEFINITION
                    """))
            .UseDefaultPipeline();

        var executor = await services
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    """
                    {
                        __type(name: "Query") {
                            fields(includeOptIn: ["experimental"]) {
                                name
                            }
                        }
                    }
                    """)
                .Build(),
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "__type": {
                  "fields": [
                    {
                      "name": "experimentalField"
                    },
                    {
                      "name": "field"
                    }
                  ]
                }
              }
            }
            """);
    }

    // __Schema.optInFeatures and optInFeatureStability expose the collected opt-in metadata
    // from the composed execution schema.
    [Fact]
    public async Task Schema_OptInFeatures_AndStability_Returned()
    {
        // arrange
        var services = new ServiceCollection();
        services.AddHttpClient();
        services
            .AddGraphQLGateway()
            .ModifyOptions(o => o.EnableOptInFeatures = true)
            .AddInMemoryConfiguration(
                ComposeSchemaDocument(
                    """
                    schema @optInFeatureStability(feature: "experimental", stability: "EXPERIMENTAL") {
                        query: Query
                    }

                    type Query {
                        field: String
                        experimentalField: String @requiresOptIn(feature: "experimental")
                    }

                    directive @requiresOptIn(feature: String!) repeatable on
                        | FIELD_DEFINITION
                        | ARGUMENT_DEFINITION
                        | ENUM_VALUE
                        | INPUT_FIELD_DEFINITION
                        | DIRECTIVE_DEFINITION

                    directive @optInFeatureStability(feature: String!, stability: String!) repeatable on SCHEMA
                    """))
            .UseDefaultPipeline();

        var executor = await services
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
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
                    """)
                .Build(),
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "__schema": {
                  "optInFeatures": [
                    "experimental"
                  ],
                  "optInFeatureStability": [
                    {
                      "feature": "experimental",
                      "stability": "EXPERIMENTAL"
                    }
                  ]
                }
              }
            }
            """);
    }

    // When EnableOptInFeatures is NOT active, the includeOptIn argument does not exist
    // on __Type.fields and the query is rejected with a validation error.
    [Fact]
    public async Task Fields_IncludeOptInArg_AbsentWhenDisabled()
    {
        // arrange
        var services = new ServiceCollection();
        services.AddHttpClient();
        services
            .AddGraphQLGateway()
            .AddInMemoryConfiguration(
                ComposeSchemaDocument(
                    """
                    type Query {
                        field: String
                    }
                    """))
            .UseDefaultPipeline();

        var executor = await services
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    """
                    {
                        __type(name: "Query") {
                            fields(includeOptIn: ["experimental"]) {
                                name
                            }
                        }
                    }
                    """)
                .Build(),
            TestContext.Current.CancellationToken);

        // assert
        var operationResult = result.ExpectOperationResult();
        Assert.Null(operationResult.Data);
        Assert.NotNull(operationResult.Errors);
        Assert.True(operationResult.Errors.Count > 0);
    }

    // When EnableOptInFeatures is NOT active, the optInFeatures field does not exist
    // on __Schema and the query is rejected with a validation error.
    [Fact]
    public async Task Schema_OptInFeatures_AbsentWhenDisabled()
    {
        // arrange
        var services = new ServiceCollection();
        services.AddHttpClient();
        services
            .AddGraphQLGateway()
            .AddInMemoryConfiguration(
                ComposeSchemaDocument(
                    """
                    type Query {
                        field: String
                    }
                    """))
            .UseDefaultPipeline();

        var executor = await services
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    """
                    {
                        __schema {
                            optInFeatures
                        }
                    }
                    """)
                .Build(),
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The field `optInFeatures` does not exist on the type `__Schema`.",
                  "locations": [
                    {
                      "line": 3,
                      "column": 9
                    }
                  ],
                  "extensions": {
                    "type": "__Schema",
                    "field": "optInFeatures",
                    "responseName": "optInFeatures",
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Field-Selections"
                  }
                }
              ]
            }
            """);
    }

    // With EnableOptInFeatures enabled, a normal operation that selects an opt-in field
    // passes validation and executes; the opt-in flag affects introspection only.
    [Fact]
    public async Task NormalOperation_OptInField_ExecutesSuccessfully()
    {
        // arrange
        var services = new ServiceCollection();
        services.AddHttpClient();
        services
            .AddGraphQLGateway()
            .ModifyOptions(o => o.EnableOptInFeatures = true)
            .AddInMemoryConfiguration(
                ComposeSchemaDocument(
                    """
                    type Query {
                        field: String
                        experimentalField: String @requiresOptIn(feature: "experimental")
                    }

                    directive @requiresOptIn(feature: String!) repeatable on
                        | FIELD_DEFINITION
                        | ARGUMENT_DEFINITION
                        | ENUM_VALUE
                        | INPUT_FIELD_DEFINITION
                        | DIRECTIVE_DEFINITION
                    """))
            .UseDefaultPipeline();

        var executor = await services
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    """
                    {
                        experimentalField
                    }
                    """)
                .Build(),
            TestContext.Current.CancellationToken);

        // assert
        // Opt-in filtering is introspection-only: selecting an @requiresOptIn field is
        // accepted, so the operation validates and produces a data payload. A data payload
        // is only produced once validation passes, so its presence proves the field is not
        // rejected at selection.
        var operationResult = result.ExpectOperationResult();
        Assert.True(operationResult.Data.HasValue);
    }

    // __Field.requiresOptIn lists the features required to include that field.
    [Fact]
    public async Task Field_RequiresOptIn_ListsFeatures()
    {
        // arrange
        var services = new ServiceCollection();
        services.AddHttpClient();
        services
            .AddGraphQLGateway()
            .ModifyOptions(o => o.EnableOptInFeatures = true)
            .AddInMemoryConfiguration(
                ComposeSchemaDocument(
                    """
                    type Query {
                        field: String
                        experimentalField: String @requiresOptIn(feature: "experimental")
                    }

                    directive @requiresOptIn(feature: String!) repeatable on
                        | FIELD_DEFINITION
                        | ARGUMENT_DEFINITION
                        | ENUM_VALUE
                        | INPUT_FIELD_DEFINITION
                        | DIRECTIVE_DEFINITION
                    """))
            .UseDefaultPipeline();

        var executor = await services
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    """
                    {
                        __type(name: "Query") {
                            fields(includeOptIn: ["experimental"]) {
                                name
                                requiresOptIn
                            }
                        }
                    }
                    """)
                .Build(),
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "__type": {
                  "fields": [
                    {
                      "name": "experimentalField",
                      "requiresOptIn": [
                        "experimental"
                      ]
                    },
                    {
                      "name": "field",
                      "requiresOptIn": []
                    }
                  ]
                }
              }
            }
            """);
    }
}
