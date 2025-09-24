using HotChocolate.AspNetCore;
using HotChocolate.Execution;
using HotChocolate.Transport.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HotChocolate.Fusion;

public class ValidationTests : FusionTestBase
{
    [Fact]
    public async Task DisableIntrospection_NotAllowed()
    {
        await new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType(d => d.Name("Query").Field("foo").Resolve("bar"))
            .DisableIntrospection()
            .ExecuteRequestAsync(
                OperationRequestBuilder
                    .New()
                    .SetDocument("{ __schema { description } }")
                    .Build())
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task DisableIntrospection_NotAllowed_CustomMessage()
    {
        await new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType(d => d.Name("Query").Field("foo").Resolve("bar"))
            .DisableIntrospection()
            .ExecuteRequestAsync(
                OperationRequestBuilder
                    .New()
                    .SetDocument("{ __schema { description } }")
                    .SetIntrospectionNotAllowedMessage("Bar")
                    .Build())
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task DisableIntrospection_True_NotAllowed()
    {
        await new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType(d => d.Name("Query").Field("foo").Resolve("bar"))
            .DisableIntrospection(disable: true)
            .ExecuteRequestAsync(
                OperationRequestBuilder
                    .New()
                    .SetDocument("{ __schema { description } }")
                    .Build())
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task DisableIntrospection_False_Allowed()
    {
        await new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType(d => d.Name("Query").Field("foo").Resolve("bar"))
            .DisableIntrospection(disable: false)
            .ExecuteRequestAsync(
                OperationRequestBuilder
                    .New()
                    .SetDocument("{ __schema { description } }")
                    .Build())
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task DisableIntrospection_Request_AllowIntrospection_Is_Allowed()
    {
        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType(d => d.Name("Query").Field("foo").Resolve("bar"))
                .DisableIntrospection()
                .BuildRequestExecutorAsync();

        var results = new List<string>();

        var result =
            await executor.ExecuteAsync(
                OperationRequestBuilder
                    .New()
                    .SetDocument("{ __schema { description } }")
                    .AllowIntrospection()
                    .Build());
        results.Add(result.ToJson());

        result =
            await executor.ExecuteAsync(
                OperationRequestBuilder
                    .New()
                    .SetDocument("{ __schema { description } }")
                    .Build());
        results.Add(result.ToJson());

        results.MatchSnapshot();
    }
}

public class ErrorFilterTests : FusionTestBase
{
    [Fact]
    public async Task AddErrorFilter_Delegate()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              field: String @error
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
                b.AddErrorFilter(error => error.WithMessage("REPLACED MESSAGE").WithCode("CUSTOM_CODE"));
            });

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        using var result = await client.PostAsync(
            """
            {
              field
            }
            """,
            new Uri("http://localhost:5000/graphql"));

        // assert
        using var response = await result.ReadAsResultAsync();
        response.MatchInlineSnapshot(
            """
            {
              "data": {
                "field": null
              },
              "errors": [
                {
                  "message": "REPLACED MESSAGE",
                  "path": [
                    "field"
                  ],
                  "extensions": {
                    "code": "CUSTOM_CODE"
                  }
                }
              ]
            }
            """);
    }

    [Fact]
    public async Task AddErrorFilter_Class()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              field: String @error
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
                b.AddErrorFilter<DummyErrorFilter>();
            });

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        using var result = await client.PostAsync(
            """
            {
              field
            }
            """,
            new Uri("http://localhost:5000/graphql"));

        // assert
        using var response = await result.ReadAsResultAsync();
        response.MatchInlineSnapshot(
            """
            {
              "data": {
                "field": null
              },
              "errors": [
                {
                  "message": "Unexpected Execution Error",
                  "path": [
                    "field"
                  ],
                  "extensions": {
                    "code": "Foo123"
                  }
                }
              ]
            }
            """);
    }

    [Fact]
    public async Task AddErrorFilter_Class_With_Service_Dependency()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              field: String @error
            }
            """
        );

        using var gateway = await CreateCompositeSchemaAsync(
            [
                ("A", server1)
            ],
            configureServices: s => s.AddSingleton<SomeService>(),
            configureGatewayBuilder: b =>
            {
                b.ConfigureSchemaServices((_, s) =>
                {
                    s.RemoveAll<IHttpRequestInterceptor>();
                    s.AddSingleton<IHttpRequestInterceptor, DefaultHttpRequestInterceptor>();
                });
                b.AddErrorFilter<DummyErrorFilterWithDependency>();
            });

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        using var result = await client.PostAsync(
            """
            {
              field
            }
            """,
            new Uri("http://localhost:5000/graphql"));

        // assert
        using var response = await result.ReadAsResultAsync();
        response.MatchInlineSnapshot(
            """
            {
              "data": {
                "field": null
              },
              "errors": [
                {
                  "message": "Unexpected Execution Error",
                  "path": [
                    "field"
                  ],
                  "extensions": {
                    "code": "Foo123"
                  }
                }
              ]
            }
            """);
    }

    [Fact]
    public async Task AddErrorFilter_Factory()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              field: String @error
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
                b.AddErrorFilter(_ => new DummyErrorFilter());
            });

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        using var result = await client.PostAsync(
            """
            {
              field
            }
            """,
            new Uri("http://localhost:5000/graphql"));

        // assert
        using var response = await result.ReadAsResultAsync();
        response.MatchInlineSnapshot(
            """
            {
              "data": {
                "field": null
              },
              "errors": [
                {
                  "message": "Unexpected Execution Error",
                  "path": [
                    "field"
                  ],
                  "extensions": {
                    "code": "Foo123"
                  }
                }
              ]
            }
            """);
    }

    [Fact]
    public async Task AddErrorFilter_Factory_With_Service_Dependency()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              field: String @error
            }
            """
        );

        using var gateway = await CreateCompositeSchemaAsync(
            [
                ("A", server1)
            ],
            configureServices: s => s.AddSingleton<SomeService>(),
            configureGatewayBuilder: b =>
            {
                b.ConfigureSchemaServices((_, s) =>
                {
                    s.RemoveAll<IHttpRequestInterceptor>();
                    s.AddSingleton<IHttpRequestInterceptor, DefaultHttpRequestInterceptor>();
                });
                b.AddErrorFilter(sp => new DummyErrorFilterWithDependency(sp.GetRequiredService<SomeService>()));
            });

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        using var result = await client.PostAsync(
            """
            {
              field
            }
            """,
            new Uri("http://localhost:5000/graphql"));

        // assert
        using var response = await result.ReadAsResultAsync();
        response.MatchInlineSnapshot(
            """
            {
              "data": {
                "field": null
              },
              "errors": [
                {
                  "message": "Unexpected Execution Error",
                  "path": [
                    "field"
                  ],
                  "extensions": {
                    "code": "Foo123"
                  }
                }
              ]
            }
            """);
    }

    public class DummyErrorFilter : IErrorFilter
    {
        public IError OnError(IError error)
        {
            return error.WithCode("Foo123");
        }
    }

#pragma warning disable CS9113 // Parameter is unread.
    public class DummyErrorFilterWithDependency(SomeService service) : IErrorFilter
    {
        public IError OnError(IError error)
        {
            return error.WithCode("Foo123");
        }
    }
#pragma warning restore CS9113 // Parameter is unread.

    public class SomeService;
}
