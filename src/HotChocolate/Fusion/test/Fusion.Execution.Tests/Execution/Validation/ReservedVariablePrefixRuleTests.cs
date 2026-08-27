using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution.Validation;

public sealed class ReservedVariablePrefixRuleTests : FusionTestBase
{
    [Fact]
    public async Task Execute_Should_RejectOperation_When_VariableName_UsesReservedSlotPrefix()
    {
        // arrange
        var executor = await CreateExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    """
                    query($__fusion_policy_0: Boolean!) {
                      field @include(if: $__fusion_policy_0)
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
                  "message": "The operation defines the variable '__fusion_policy_0', which uses the reserved '__fusion' prefix.",
                  "locations": [
                    {
                      "line": 1,
                      "column": 7
                    }
                  ],
                  "extensions": {
                    "code": "FUSION_RESERVED_VARIABLE_PREFIX",
                    "variableName": "__fusion_policy_0"
                  }
                },
                {
                  "message": "The document uses the variable '__fusion_policy_0', which is reserved by the '__fusion' prefix.",
                  "locations": [
                    {
                      "line": 2,
                      "column": 22
                    }
                  ],
                  "extensions": {
                    "code": "FUSION_RESERVED_VARIABLE_PREFIX",
                    "variableName": "__fusion_policy_0"
                  }
                }
              ]
            }
            """);
    }

    [Fact]
    public async Task Execute_Should_RejectOperation_When_VariableName_CollidesWithRequirementNamespace()
    {
        // arrange
        var executor = await CreateExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    """
                    query($__fusion_1_id: Boolean!) {
                      field @include(if: $__fusion_1_id)
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
                  "message": "The operation defines the variable '__fusion_1_id', which uses the reserved '__fusion' prefix.",
                  "locations": [
                    {
                      "line": 1,
                      "column": 7
                    }
                  ],
                  "extensions": {
                    "code": "FUSION_RESERVED_VARIABLE_PREFIX",
                    "variableName": "__fusion_1_id"
                  }
                },
                {
                  "message": "The document uses the variable '__fusion_1_id', which is reserved by the '__fusion' prefix.",
                  "locations": [
                    {
                      "line": 2,
                      "column": 22
                    }
                  ],
                  "extensions": {
                    "code": "FUSION_RESERVED_VARIABLE_PREFIX",
                    "variableName": "__fusion_1_id"
                  }
                }
              ]
            }
            """);
    }

    [Fact]
    public async Task Execute_Should_RejectEachVariable_When_MultipleVariableNames_UseReservedPrefix()
    {
        // arrange
        var executor = await CreateExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    """
                    query($__fusion_policy_0: Boolean!, $__fusion_policy_1: Boolean!) {
                      a: field @include(if: $__fusion_policy_0)
                      b: field @include(if: $__fusion_policy_1)
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
                  "message": "The operation defines the variable '__fusion_policy_0', which uses the reserved '__fusion' prefix.",
                  "locations": [
                    {
                      "line": 1,
                      "column": 7
                    }
                  ],
                  "extensions": {
                    "code": "FUSION_RESERVED_VARIABLE_PREFIX",
                    "variableName": "__fusion_policy_0"
                  }
                },
                {
                  "message": "The operation defines the variable '__fusion_policy_1', which uses the reserved '__fusion' prefix.",
                  "locations": [
                    {
                      "line": 1,
                      "column": 37
                    }
                  ],
                  "extensions": {
                    "code": "FUSION_RESERVED_VARIABLE_PREFIX",
                    "variableName": "__fusion_policy_1"
                  }
                },
                {
                  "message": "The document uses the variable '__fusion_policy_0', which is reserved by the '__fusion' prefix.",
                  "locations": [
                    {
                      "line": 2,
                      "column": 25
                    }
                  ],
                  "extensions": {
                    "code": "FUSION_RESERVED_VARIABLE_PREFIX",
                    "variableName": "__fusion_policy_0"
                  }
                },
                {
                  "message": "The document uses the variable '__fusion_policy_1', which is reserved by the '__fusion' prefix.",
                  "locations": [
                    {
                      "line": 3,
                      "column": 25
                    }
                  ],
                  "extensions": {
                    "code": "FUSION_RESERVED_VARIABLE_PREFIX",
                    "variableName": "__fusion_policy_1"
                  }
                }
              ]
            }
            """);
    }

    [Fact]
    public void Validate_Should_RejectFragmentArgument_When_FragmentVariableName_UsesReservedSlotPrefix()
    {
        // arrange
        // Fragment variable definitions are an experimental, opt-in parser feature (see
        // ParserOptionsExperimental.AllowFragmentVariables), so the document is built directly
        // rather than through the executor pipeline, which parses with the default options.
        var document = Utf8GraphQLParser.Parse(
            """
            query {
              ...Frag
            }

            fragment Frag($__fusion_policy_0: Boolean! = true) on Query {
              field @include(if: $__fusion_policy_0)
            }
            """,
            new ParserOptions(allowFragmentVariables: true));
        var context = new DocumentValidatorContext();
        context.Initialize(
            CreateCompositeSchema(),
            documentId: default,
            document,
            maxAllowedErrors: 10,
            maxLocationsPerError: 5,
            maxAllowedFragmentVisits: 1_000,
            features: null);
        var rule = new ReservedVariablePrefixRule();

        // act
        rule.Validate(context, document);

        // assert
        context.Errors.Select(error => error.Message).MatchInlineSnapshot(
            """
            [
              "The fragment defines the variable '__fusion_policy_0', which uses the reserved '__fusion' prefix.",
              "The document uses the variable '__fusion_policy_0', which is reserved by the '__fusion' prefix."
            ]
            """);
    }

    [Fact]
    public async Task Execute_Should_RejectUsage_When_ArgumentValue_ReferencesReservedSlotPrefix()
    {
        // arrange
        var executor = await CreateExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    """
                    query {
                      field @include(if: $__fusion_policy_0)
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
                  "message": "The document uses the variable '__fusion_policy_0', which is reserved by the '__fusion' prefix.",
                  "locations": [
                    {
                      "line": 2,
                      "column": 22
                    }
                  ],
                  "extensions": {
                    "code": "FUSION_RESERVED_VARIABLE_PREFIX",
                    "variableName": "__fusion_policy_0"
                  }
                },
                {
                  "message": "The following variables were not declared: __fusion_policy_0.",
                  "locations": [
                    {
                      "line": 1,
                      "column": 1
                    }
                  ],
                  "extensions": {
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-All-Variable-Uses-Defined"
                  }
                }
              ]
            }
            """);
    }

    [Fact]
    public async Task Execute_Should_Allow_When_VariableName_HasSingleUnderscorePrefix()
    {
        // arrange
        var executor = await CreateExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    """
                    query($_fusion_policy_0: Boolean!) {
                      __typename @include(if: $_fusion_policy_0)
                    }
                    """)
                .SetVariableValues(new Dictionary<string, object?> { ["_fusion_policy_0"] = true })
                .Build(),
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "__typename": "Query"
              }
            }
            """);
    }

    [Fact]
    public async Task Execute_Should_Allow_When_VariableName_DoesNotUseReservedPrefix()
    {
        // arrange
        var executor = await CreateExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    """
                    query($id: Boolean!) {
                      __typename @include(if: $id)
                    }
                    """)
                .SetVariableValues(new Dictionary<string, object?> { ["id"] = true })
                .Build(),
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "__typename": "Query"
              }
            }
            """);
    }

    private static async Task<IRequestExecutor> CreateExecutorAsync()
    {
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

        return await services
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);
    }
}
