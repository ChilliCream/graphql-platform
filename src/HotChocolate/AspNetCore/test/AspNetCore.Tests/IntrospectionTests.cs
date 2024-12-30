using System.Net;
using HotChocolate.AspNetCore.Tests.Utilities;
using HotChocolate.Transport;
using HotChocolate.Transport.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HotChocolate.AspNetCore;

public class IntrospectionTests(TestServerFactory serverFactory) : ServerTestBase(serverFactory)
{
    [Fact]
    public async Task Introspection_Request_When_Development_Success()
    {
        // arrange
        var client = GetClient(Environments.Development);

        // act
        var response = await client.PostAsync(
            """
            {
                __type(name: "Query") {
                    name
                }
            }
            """,
            Url);

        // assert
        response.HttpResponseMessage.MatchMarkdownSnapshot();
    }

    [Theory]
    [InlineData("Staging")]
    [InlineData("Production")]
    public async Task Introspection_Request_When_NOT_Development_Fail(string environment)
    {
        // arrange
        var client = GetClient(environment);

        // act
        var response = await client.PostAsync(
            """
            {
                __type(name: "Query") {
                    name
                }
            }
            """,
            Url);

        // assert
        response.HttpResponseMessage.MatchMarkdownSnapshot();
    }

    [Theory]
    [InlineData("Staging")]
    [InlineData("Production")]
    public async Task Introspection_Request_With_Rule_Removed_Fail(string environment)
    {
        // arrange
        var client = GetClient(environment, removeRule: true);

        // act
        var response = await client.PostAsync(
            """
            {
                __type(name: "Query") {
                    name
                }
            }
            """,
            Url);

        // assert
        response.HttpResponseMessage.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Introspection_OfType_Depth_1_BadRequest()
    {
        // arrange
        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddGraphQL()
                .SetIntrospectionAllowedDepth(
                    maxAllowedOfTypeDepth: 1,
                    maxAllowedListRecursiveDepth: 1));

        var request = new GraphQLHttpRequest(
            new OperationRequest(
                """
                {
                  __schema {
                    types {
                      ofType {
                        ofType {
                          name
                        }
                      }
                    }
                  }
                }
                """),
            new Uri("http://localhost:5000/graphql"));

        // act
        var client = new DefaultGraphQLHttpClient(server.CreateClient());
        using var response = await client.SendAsync(request);

        // assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Introspection_OfType_Depth_1_Depth_Analysis_Disabled()
    {
        // arrange
        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddGraphQL()
                .SetIntrospectionAllowedDepth(
                    maxAllowedOfTypeDepth: 1,
                    maxAllowedListRecursiveDepth: 1)
                .Services
                .AddValidation()
                .ConfigureValidation(b => b.Modifiers.Add(o => o.DisableDepthRule = true)));

        var request = new GraphQLHttpRequest(
            new OperationRequest(
                """
                {
                  __schema {
                    types {
                      ofType {
                        ofType {
                          name
                        }
                      }
                    }
                  }
                }
                """),
            new Uri("http://localhost:5000/graphql"));

        // act
        var client = new DefaultGraphQLHttpClient(server.CreateClient());
        using var response = await client.SendAsync(request);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Introspection_Disabled_BadRequest()
    {
        // arrange
        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddGraphQL()
                .DisableIntrospection());

        var request = new GraphQLHttpRequest(
            new OperationRequest(
                """
                {
                  __schema {
                    types {
                      ofType {
                        ofType {
                          name
                        }
                      }
                    }
                  }
                }
                """),
            new Uri("http://localhost:5000/graphql"));

        // act
        var client = new DefaultGraphQLHttpClient(server.CreateClient());
        using var response = await client.SendAsync(request);

        // assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Introspection_OfType_Depth_2_OK()
    {
        // arrange
        var server = CreateStarWarsServer(
            configureServices: s => s
                .AddGraphQL()
                .SetIntrospectionAllowedDepth(
                    maxAllowedOfTypeDepth: 2,
                    maxAllowedListRecursiveDepth: 1));

        var request = new GraphQLHttpRequest(
            new OperationRequest(
                """
                {
                  __schema {
                    types {
                      ofType {
                        ofType {
                          name
                        }
                      }
                    }
                  }
                }
                """),
            new Uri("http://localhost:5000/graphql"));

        // act
        var client = new DefaultGraphQLHttpClient(server.CreateClient());
        using var response = await client.SendAsync(request);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private GraphQLHttpClient GetClient(string environment, bool removeRule = false)
    {
        var server = CreateStarWarsServer(
            environment: environment,
            configureServices: s =>
            {
                if (removeRule)
                {
                    s.AddGraphQL()
                        .DisableIntrospection(disable: false);
                }
            });
        return new DefaultGraphQLHttpClient(server.CreateClient());
    }
}
