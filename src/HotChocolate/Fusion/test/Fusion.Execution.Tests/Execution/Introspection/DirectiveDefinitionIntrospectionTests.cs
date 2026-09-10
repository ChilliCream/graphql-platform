using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution.Introspection;

public sealed class DirectiveDefinitionIntrospectionTests : FusionTestBase
{
    // A directive whose locations include DIRECTIVE_DEFINITION is introspected with that
    // location; the deprecation metadata fields resolve for every directive.
    [Fact]
    public async Task Directives_Locations_IncludeDirectiveDefinition()
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
                        __schema {
                            directives {
                                name
                                isDeprecated
                                deprecationReason
                                locations
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
                  "directives": [
                    {
                      "name": "requiresOptIn",
                      "isDeprecated": false,
                      "deprecationReason": null,
                      "locations": [
                        "FIELD_DEFINITION",
                        "ARGUMENT_DEFINITION",
                        "ENUM_VALUE",
                        "INPUT_FIELD_DEFINITION",
                        "DIRECTIVE_DEFINITION"
                      ]
                    },
                    {
                      "name": "defer",
                      "isDeprecated": false,
                      "deprecationReason": null,
                      "locations": [
                        "FRAGMENT_SPREAD",
                        "INLINE_FRAGMENT"
                      ]
                    },
                    {
                      "name": "skip",
                      "isDeprecated": false,
                      "deprecationReason": null,
                      "locations": [
                        "FIELD",
                        "FRAGMENT_SPREAD",
                        "INLINE_FRAGMENT"
                      ]
                    },
                    {
                      "name": "include",
                      "isDeprecated": false,
                      "deprecationReason": null,
                      "locations": [
                        "FIELD",
                        "FRAGMENT_SPREAD",
                        "INLINE_FRAGMENT"
                      ]
                    },
                    {
                      "name": "specifiedBy",
                      "isDeprecated": false,
                      "deprecationReason": null,
                      "locations": [
                        "SCALAR"
                      ]
                    },
                    {
                      "name": "oneOf",
                      "isDeprecated": false,
                      "deprecationReason": null,
                      "locations": [
                        "INPUT_OBJECT"
                      ]
                    }
                  ]
                }
              }
            }
            """);
    }
}
