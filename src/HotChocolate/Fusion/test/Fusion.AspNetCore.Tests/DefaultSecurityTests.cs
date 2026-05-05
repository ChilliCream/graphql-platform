using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Extensions;
using HotChocolate.Execution;
using HotChocolate.Transport.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
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

        // FusionTestBase already defaults to Development.
        using var gateway = await CreateCompositeSchemaAsync(
            [("A", server1)],
            configureGatewayBuilder: b => b.AddHttpRequestInterceptor<DefaultHttpRequestInterceptor>());

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
            configureGatewayBuilder: b => b.AddHttpRequestInterceptor<DefaultHttpRequestInterceptor>(),
            environmentName: Environments.Production,
            disableDefaultSecurity: true);

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

    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    public async Task DefaultSecurity_FieldCycleDepthIsEnforced(string environment)
    {
        // arrange - 4 levels of `relatives` exceeds the default limit of 3
        using var server1 = CreateSourceSchema("A", CyclicSchema);

        using var gateway = await CreateCompositeSchemaAsync(
            [("A", server1)],
            configureGatewayBuilder: b => b.AddHttpRequestInterceptor<DefaultHttpRequestInterceptor>(),
            environmentName: environment);

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
    public async Task DefaultSecurity_Disabled_FieldCycleDepthIsNotEnforced()
    {
        // arrange
        using var server1 = CreateSourceSchema("A", CyclicSchema);

        using var gateway = await CreateCompositeSchemaAsync(
            [("A", server1)],
            configureGatewayBuilder: b => b.AddHttpRequestInterceptor<DefaultHttpRequestInterceptor>(),
            disableDefaultSecurity: true);

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
        Assert.Equal(JsonValueKind.Undefined, response.Errors.ValueKind);
        Assert.Equal(JsonValueKind.Object, response.Data.ValueKind);
    }

    [Fact]
    public async Task DefaultSecurity_InDevelopment_SchemaRequestsAreAllowed()
    {
        // arrange
        using var server1 = CreateSourceSchema("A", SimpleSchema);

        using var gateway = await CreateCompositeSchemaAsync(
            [("A", server1)]);

        // act
        using var response = await gateway.CreateClient().GetAsync(
            "http://localhost:5000/graphql/schema.graphql");

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DefaultSecurity_InProduction_SchemaRequestsAreDisabled()
    {
        // arrange
        using var server1 = CreateSourceSchema("A", SimpleSchema);

        using var gateway = await CreateCompositeSchemaAsync(
            [("A", server1)],
            environmentName: Environments.Production);

        // act
        using var response = await gateway.CreateClient().GetAsync(
            "http://localhost:5000/graphql/schema.graphql");

        // assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DefaultSecurity_Disabled_InProduction_SchemaRequestsAreAllowed()
    {
        // arrange
        using var server1 = CreateSourceSchema("A", SimpleSchema);

        using var gateway = await CreateCompositeSchemaAsync(
            [("A", server1)],
            environmentName: Environments.Production,
            disableDefaultSecurity: true);

        // act
        using var response = await gateway.CreateClient().GetAsync(
            "http://localhost:5000/graphql/schema.graphql");

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DefaultSecurity_InProduction_SchemaRequestsCanBeReEnabledPerEndpoint()
    {
        // arrange - default security disables schema requests in production,
        // but the per-endpoint WithOptions override should win.
        using var server1 = CreateSourceSchema("A", SimpleSchema);

        using var gateway = await CreateCompositeSchemaAsync(
            [("A", server1)],
            configureApplication: app => app
                .UseRouting()
                .UseEndpoints(endpoints => endpoints
                    .MapGraphQL()
                    .WithOptions(o => o.EnableSchemaRequests = true)),
            environmentName: Environments.Production);

        // act
        using var response = await gateway.CreateClient().GetAsync(
            "http://localhost:5000/graphql/schema.graphql");

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DefaultSecurity_InDevelopment_SchemaRequestsCanBeDisabledPerEndpoint()
    {
        // arrange - default security allows schema requests in development,
        // but the per-endpoint WithOptions override should win.
        using var server1 = CreateSourceSchema("A", SimpleSchema);

        using var gateway = await CreateCompositeSchemaAsync(
            [("A", server1)],
            configureApplication: app => app
                .UseRouting()
                .UseEndpoints(endpoints => endpoints
                    .MapGraphQL()
                    .WithOptions(o => o.EnableSchemaRequests = false)));

        // act
        using var response = await gateway.CreateClient().GetAsync(
            "http://localhost:5000/graphql/schema.graphql");

        // assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AllowOperationPlanRequests_True_With_OperationPlanHeader_Should_Include_OperationPlan()
    {
        // arrange
        using var server1 = CreateSourceSchema("A", SimpleSchema);

        using var gateway = await CreateCompositeSchemaAsync(
            [("A", server1)],
            configureGatewayBuilder: b =>
            {
                b.ConfigureSchemaServices((_, s) =>
                {
                    s.RemoveAll<IHttpRequestInterceptor>();
                    s.AddSingleton<IHttpRequestInterceptor, DefaultHttpRequestInterceptor>();
                });
                b.ModifyRequestOptions(o => o.AllowOperationPlanRequests = true);
            });

        // act
        using var response = await SendQueryAsync(gateway, includeOperationPlanHeader: true);

        // assert
        response.MatchSnapshot();
    }

    [Fact]
    public async Task AllowOperationPlanRequests_False_With_OperationPlanHeader_Should_Omit_OperationPlan()
    {
        // arrange
        using var server1 = CreateSourceSchema("A", SimpleSchema);

        using var gateway = await CreateCompositeSchemaAsync(
            [("A", server1)],
            configureGatewayBuilder: b =>
            {
                b.ConfigureSchemaServices((_, s) =>
                {
                    s.RemoveAll<IHttpRequestInterceptor>();
                    s.AddSingleton<IHttpRequestInterceptor, DefaultHttpRequestInterceptor>();
                });
                b.ModifyRequestOptions(o => o.AllowOperationPlanRequests = false);
            });

        // act
        using var response = await SendQueryAsync(gateway, includeOperationPlanHeader: true);

        // assert
        response.MatchSnapshot();
    }

    [Fact]
    public async Task AllowOperationPlanRequests_True_Without_OperationPlanHeader_Should_Omit_OperationPlan()
    {
        // arrange
        using var server1 = CreateSourceSchema("A", SimpleSchema);

        using var gateway = await CreateCompositeSchemaAsync(
            [("A", server1)],
            configureGatewayBuilder: b =>
            {
                b.ConfigureSchemaServices((_, s) =>
                {
                    s.RemoveAll<IHttpRequestInterceptor>();
                    s.AddSingleton<IHttpRequestInterceptor, DefaultHttpRequestInterceptor>();
                });
                b.ModifyRequestOptions(o => o.AllowOperationPlanRequests = true);
            });

        // act
        using var response = await SendQueryAsync(gateway, includeOperationPlanHeader: false);

        // assert
        response.MatchSnapshot();
    }

    [Fact]
    public async Task AllowOperationPlanRequests_False_With_PerRequestOverride_And_OperationPlanHeader_Should_Include_OperationPlan()
    {
        // arrange
        using var server1 = CreateSourceSchema("A", SimpleSchema);

        using var gateway = await CreateCompositeSchemaAsync(
            [("A", server1)],
            configureGatewayBuilder: b =>
            {
                b.ConfigureSchemaServices((_, s) =>
                {
                    s.RemoveAll<IHttpRequestInterceptor>();
                    s.AddSingleton<IHttpRequestInterceptor, AllowOperationPlanRequestInterceptor>();
                });
                b.ModifyRequestOptions(o => o.AllowOperationPlanRequests = false);
            });

        // act
        using var response = await SendQueryAsync(gateway, includeOperationPlanHeader: true);

        // assert
        response.MatchSnapshot();
    }

    [Fact]
    public async Task AllowOperationPlanRequests_False_With_PerRequestOverride_Without_OperationPlanHeader_Should_Omit_OperationPlan()
    {
        // arrange
        using var server1 = CreateSourceSchema("A", SimpleSchema);

        using var gateway = await CreateCompositeSchemaAsync(
            [("A", server1)],
            configureGatewayBuilder: b =>
            {
                b.ConfigureSchemaServices((_, s) =>
                {
                    s.RemoveAll<IHttpRequestInterceptor>();
                    s.AddSingleton<IHttpRequestInterceptor, AllowOperationPlanRequestInterceptor>();
                });
                b.ModifyRequestOptions(o => o.AllowOperationPlanRequests = false);
            });

        // act
        using var response = await SendQueryAsync(gateway, includeOperationPlanHeader: false);

        // assert
        response.MatchSnapshot();
    }

    private static Task<HttpResponseMessage> SendQueryAsync(Gateway gateway, bool includeOperationPlanHeader)
    {
        var httpClient = gateway.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost:5000/graphql");
        request.Content = new StringContent(
            """{"query":"{ field }"}""",
            Encoding.UTF8,
            "application/json");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/graphql-response+json"));

        if (includeOperationPlanHeader)
        {
            request.Headers.Add("Fusion-Operation-Plan", "1");
        }

        return httpClient.SendAsync(request);
    }

    private sealed class AllowOperationPlanRequestInterceptor : DefaultHttpRequestInterceptor
    {
        public override ValueTask OnCreateAsync(
            HttpContext context,
            IRequestExecutor requestExecutor,
            OperationRequestBuilder requestBuilder,
            CancellationToken cancellationToken)
        {
            requestBuilder.AllowOperationPlanRequest();

            return base.OnCreateAsync(context, requestExecutor, requestBuilder, cancellationToken);
        }
    }
}
