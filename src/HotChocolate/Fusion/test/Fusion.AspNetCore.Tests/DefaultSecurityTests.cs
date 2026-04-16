using HotChocolate.AspNetCore;
using HotChocolate.Transport.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace HotChocolate.Fusion;

public class DefaultSecurityTests : FusionTestBase
{
    private const string SimpleSchema =
        """
        type Query {
          field: String
        }
        """;

    private const string CyclicSchema =
        """
        type Query {
          human: Human
        }

        type Human {
          name: String
          relatives: [Human]
        }
        """;

    [Fact]
    public async Task DefaultSecurity_InProduction_IntrospectionIsDisabled()
    {
        // arrange
        using var server1 = CreateSourceSchema("A", SimpleSchema);

        // Override the test base's default Development environment with Production.
        using var gateway = await CreateCompositeSchemaAsync(
            [("A", server1)],
            environmentName: Environments.Production);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        using var result = await client.PostAsync(
            "{ __schema { description } }",
            new Uri("http://localhost:5000/graphql"));

        // assert
        using var response = await result.ReadAsResultAsync();
        response.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "Introspection is not allowed for the current request.",
                  "locations": [
                    {
                      "line": 1,
                      "column": 3
                    }
                  ],
                  "extensions": {
                    "code": "HC0046",
                    "field": "__schema"
                  }
                }
              ]
            }
            """);
    }

    [Fact]
    public async Task DefaultSecurity_InDevelopment_IntrospectionIsAllowed()
    {
        // arrange
        using var server1 = CreateSourceSchema("A", SimpleSchema);

        // FusionTestBase already defaults to Development, so no configureServices needed.
        using var gateway = await CreateCompositeSchemaAsync(
            [("A", server1)],
            configureGatewayBuilder: b => b.ConfigureSchemaServices((_, s) =>
            {
                s.RemoveAll<IHttpRequestInterceptor>();
                s.AddSingleton<IHttpRequestInterceptor, DefaultHttpRequestInterceptor>();
            }));

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        using var result = await client.PostAsync(
            "{ __schema { description } }",
            new Uri("http://localhost:5000/graphql"));

        // assert
        using var response = await result.ReadAsResultAsync();
        response.MatchInlineSnapshot(
            """
            {
              "data": {
                "__schema": {
                  "description": null
                }
              }
            }
            """);
    }

    [Fact]
    public async Task DefaultSecurity_Disabled_InProduction_IntrospectionIsAllowed()
    {
        // arrange
        using var server1 = CreateSourceSchema("A", SimpleSchema);

        using var gateway = await CreateCompositeSchemaAsync(
            [("A", server1)],
            configureGatewayBuilder: b =>
            {
                b.DisableIntrospection(disable: false);
                b.ConfigureSchemaServices((_, s) =>
                {
                    s.RemoveAll<IHttpRequestInterceptor>();
                    s.AddSingleton<IHttpRequestInterceptor, DefaultHttpRequestInterceptor>();
                });
            },
            environmentName: Environments.Production);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        using var result = await client.PostAsync(
            "{ __schema { description } }",
            new Uri("http://localhost:5000/graphql"));

        // assert
        using var response = await result.ReadAsResultAsync();
        response.MatchInlineSnapshot(
            """
            {
              "data": {
                "__schema": {
                  "description": null
                }
              }
            }
            """);
    }

    [Fact]
    public async Task DefaultSecurity_InProduction_FieldCycleDepthIsEnforced()
    {
        // arrange - 4 levels of `relatives` exceeds the default limit of 3
        using var server1 = CreateSourceSchema("A", CyclicSchema);

        using var gateway = await CreateCompositeSchemaAsync(
            [("A", server1)],
            environmentName: Environments.Production);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        using var result = await client.PostAsync(
            """
            {
              human {
                relatives {
                  relatives {
                    relatives {
                      relatives {
                        name
                      }
                    }
                  }
                }
              }
            }
            """,
            new Uri("http://localhost:5000/graphql"));

        // assert
        using var response = await result.ReadAsResultAsync();
        response.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "Maximum allowed coordinate cycle depth was exceeded.",
                  "locations": [
                    {
                      "line": 6,
                      "column": 11
                    }
                  ],
                  "path": [
                    "human",
                    "relatives",
                    "relatives",
                    "relatives"
                  ],
                  "extensions": {
                    "code": "HC0087"
                  }
                }
              ]
            }
            """);
    }

    [Fact]
    public async Task DefaultSecurity_InDevelopment_FieldCycleDepthIsNotEnforced()
    {
        // arrange - 4 levels of `relatives` exceeds the limit but the rule is inactive in Development
        // FusionTestBase already defaults to Development, so no configureServices needed.
        using var server1 = CreateSourceSchema("A", CyclicSchema);

        using var gateway = await CreateCompositeSchemaAsync(
            [("A", server1)],
            configureGatewayBuilder: b => b.ConfigureSchemaServices((_, s) =>
            {
                s.RemoveAll<IHttpRequestInterceptor>();
                s.AddSingleton<IHttpRequestInterceptor, DefaultHttpRequestInterceptor>();
            }));

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        using var result = await client.PostAsync(
            """
            {
              human {
                relatives {
                  relatives {
                    relatives {
                      relatives {
                        name
                      }
                    }
                  }
                }
              }
            }
            """,
            new Uri("http://localhost:5000/graphql"));

        // assert - query passes validation and executes (no HC0087 error)
        using var response = await result.ReadAsResultAsync();
        response.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task DefaultSecurity_Disabled_InProduction_FieldCycleDepthIsNotEnforced()
    {
        // arrange
        using var server1 = CreateSourceSchema("A", CyclicSchema);

        using var gateway = await CreateCompositeSchemaAsync(
            [("A", server1)],
            configureGatewayBuilder: b =>
            {
                b.RemoveMaxAllowedFieldCycleDepthRule();
                b.ConfigureSchemaServices((_, s) =>
                {
                    s.RemoveAll<IHttpRequestInterceptor>();
                    s.AddSingleton<IHttpRequestInterceptor, DefaultHttpRequestInterceptor>();
                });
            },
            environmentName: Environments.Production);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        using var result = await client.PostAsync(
            """
            {
              human {
                relatives {
                  relatives {
                    relatives {
                      relatives {
                        name
                      }
                    }
                  }
                }
              }
            }
            """,
            new Uri("http://localhost:5000/graphql"));

        // assert - query passes validation and executes (no HC0087 error)
        using var response = await result.ReadAsResultAsync();
        response.MatchMarkdownSnapshot();
    }
}
