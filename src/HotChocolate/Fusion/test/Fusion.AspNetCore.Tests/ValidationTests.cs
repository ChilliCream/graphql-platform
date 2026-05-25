using HotChocolate.AspNetCore;
using HotChocolate.Execution;
using HotChocolate.Transport.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HotChocolate.Fusion;

public class ValidationTests : FusionTestBase
{
    [Fact]
    public async Task DisableIntrospection_NotAllowed()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              field: String
            }
            """
        );

        using var gateway = await CreateCompositeSchemaAsync(
            [
                ("A", server1)
            ],
            configureGatewayBuilder: b => b.DisableIntrospection());

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        using var result = await client.PostAsync(
            """
            { __schema { description } }
            """,
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
    public async Task DisableIntrospection_True_NotAllowed()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              field: String
            }
            """
        );

        using var gateway = await CreateCompositeSchemaAsync(
            [
                ("A", server1)
            ],
            configureGatewayBuilder: b => b.DisableIntrospection(disable: true));

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        using var result = await client.PostAsync(
            """
            { __schema { description } }
            """,
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
    public async Task DisableIntrospection_False_Allowed()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              field: String
            }
            """
        );

        using var gateway = await CreateCompositeSchemaAsync(
            [
                ("A", server1)
            ],
            configureGatewayBuilder: b =>
            {
                b.ConfigureSchemaServices((_, s) =>
                {
                    s.RemoveAll<IHttpRequestInterceptor>();
                    s.AddSingleton<IHttpRequestInterceptor, DefaultHttpRequestInterceptor>();
                });

                b.DisableIntrospection(disable: false);
            });

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        using var result = await client.PostAsync(
            """
            { __schema { description } }
            """,
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
    public async Task DisableIntrospection_Request_AllowIntrospection_Is_Allowed()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              field: String
            }
            """
        );

        using var gateway = await CreateCompositeSchemaAsync(
            [
                ("A", server1)
            ],
            configureGatewayBuilder: b =>
            {
                b.AddHttpRequestInterceptor<CustomHttpRequestInterceptor>();

                b.DisableIntrospection();
            });

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var snapshot = new Snapshot();

        using var result1 = await client.PostAsync(
            """
            { __schema { description } }
            """,
            new Uri("http://localhost:5000/graphql"));

        using var response1 = await result1.ReadAsResultAsync();
        snapshot.Add(response1);

        var innerClient = gateway.CreateClient();
        innerClient.DefaultRequestHeaders.Add("AllowIntrospection", "true");
        using var client2 = GraphQLHttpClient.Create(innerClient);

        using var result2 = await client2.PostAsync(
            """
            { __schema { description } }
            """,
            new Uri("http://localhost:5000/graphql"));

        using var response2 = await result2.ReadAsResultAsync();
        snapshot.Add(response2);

        // assert
        snapshot.MatchInline(
            """
            ---------------
            {
              "errors": [
                {
                  "message": "Custom introspection not allowed message",
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
            ---------------

            ---------------
            {
              "data": {
                "__schema": {
                  "description": null
                }
              }
            }
            ---------------

            """);
    }

    private class CustomHttpRequestInterceptor : DefaultHttpRequestInterceptor
    {
        public override ValueTask OnCreateAsync(
            HttpContext context,
            IRequestExecutor requestExecutor,
            OperationRequestBuilder requestBuilder,
            CancellationToken cancellationToken)
        {
            if (context.Request.Headers.ContainsKey("AllowIntrospection"))
            {
                requestBuilder.AllowIntrospection();
            }
            else
            {
                requestBuilder.SetIntrospectionNotAllowedMessage("Custom introspection not allowed message");
            }

            return base.OnCreateAsync(context, requestExecutor, requestBuilder, cancellationToken);
        }
    }
}
