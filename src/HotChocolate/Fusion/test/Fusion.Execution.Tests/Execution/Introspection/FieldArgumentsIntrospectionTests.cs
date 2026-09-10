using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution.Introspection;

public sealed class FieldArgumentsIntrospectionTests : FusionTestBase
{
    // __Field.args excludes deprecated arguments by default and returns the remaining
    // arguments without leaving gaps, even when a deprecated argument precedes a
    // non-deprecated one.
    [Fact]
    public async Task Fields_Args_ExcludeDeprecatedArguments()
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
                        field(
                            deprecatedArg: String @deprecated(reason: "old")
                            normalArg: String
                        ): String
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
                            fields {
                                name
                                args {
                                    name
                                }
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
                      "name": "field",
                      "args": [
                        {
                          "name": "normalArg"
                        }
                      ]
                    }
                  ]
                }
              }
            }
            """);
    }
}
