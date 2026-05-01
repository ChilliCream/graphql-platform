using System.Text.Json;
using HotChocolate.AspNetCore.Tests.Utilities;
using HotChocolate.Transport.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;

namespace HotChocolate.AspNetCore;

public class DefaultSecurityTests(TestServerFactory serverFactory) : ServerTestBase(serverFactory)
{
    [Fact]
    public async Task DefaultSecurity_InProduction_IntrospectionIsDisabled()
    {
        // arrange
        using var server = CreateServer(environment: Environments.Production);

        // act
        using var client = GraphQLHttpClient.Create(server.CreateClient());
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
        using var server = CreateServer(environment: Environments.Development);

        // act
        using var client = GraphQLHttpClient.Create(server.CreateClient());
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
        using var server = CreateServer(
            environment: Environments.Production,
            disableDefaultSecurity: true);

        // act
        using var client = GraphQLHttpClient.Create(server.CreateClient());
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

    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    public async Task DefaultSecurity_FieldCycleDepthIsEnforced(string environment)
    {
        // arrange - 4 levels of `relatives` exceeds the default limit of 3
        using var server = CreateServer(environment: environment);

        // act
        using var client = GraphQLHttpClient.Create(server.CreateClient());
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
    public async Task DefaultSecurity_Disabled_FieldCycleDepthIsNotEnforced()
    {
        // arrange
        using var server = CreateServer(disableDefaultSecurity: true);

        // act
        using var client = GraphQLHttpClient.Create(server.CreateClient());
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

        // assert - query passes validation (no HC0087 error)
        using var response = await result.ReadAsResultAsync();
        Assert.Equal(JsonValueKind.Undefined, response.Errors.ValueKind);
    }

    private TestServer CreateServer(
        string environment = "Development",
        bool disableDefaultSecurity = false)
    {
        var mockHostEnvironment = new Mock<IHostEnvironment>();
        mockHostEnvironment.Setup(env => env.EnvironmentName).Returns(environment);

        return ServerFactory.Create(
            services =>
            {
                services
                    .AddSingleton(mockHostEnvironment.Object)
                    .AddRouting()
                    .AddGraphQLServer(disableDefaultSecurity: disableDefaultSecurity)
                    .AddQueryType<Query>();
            },
            app => app
                .UseRouting()
                .UseEndpoints(endpoints => endpoints.MapGraphQL()));
    }

    public class Query
    {
        public Human? Human => null;
    }

    public class Human
    {
        public string? Name { get; set; }

        public List<Human>? Relatives { get; set; }
    }
}
