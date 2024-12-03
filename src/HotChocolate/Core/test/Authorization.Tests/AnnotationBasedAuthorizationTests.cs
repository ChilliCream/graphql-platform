using System.Security.Claims;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Relay;
using HotChocolate.Utilities;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.WellKnownContextData;

namespace HotChocolate.Authorization;

public class AnnotationBasedAuthorizationTests
{
    [Fact]
    public async Task Authorize_Person_NoAccess()
    {
        // arrange
        var handler = new AuthHandler(
            resolver: AuthorizeResult.NotAllowed,
            validation: AuthorizeResult.Allowed);
        var services = CreateServices(handler);
        var executor = await services.GetRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              person(id: "UGVyc29uOmFiYw==") {
                name
              }
            }
            """);

        // assert
        Snapshot
            .Create()
            .Add(result)
            .MatchInline(
                """
                {
                  "errors": [
                    {
                      "message": "The current user is not authorized to access this resource.",
                      "locations": [
                        {
                          "line": 2,
                          "column": 3
                        }
                      ],
                      "path": [
                        "person"
                      ],
                      "extensions": {
                        "code": "AUTH_NOT_AUTHORIZED"
                      }
                    }
                  ],
                  "data": {
                    "person": null
                  }
                }
                """);
    }

    [Fact]
    public async Task Authorize_Person_NoAccess_EnsureNotCached()
    {
        // arrange
        var results = new Stack<AuthorizeResult>();
        results.Push(AuthorizeResult.NotAllowed);
        results.Push(AuthorizeResult.Allowed);

        var handler = new AuthHandler2(results);
        var services = CreateServices(handler);
        var executor = await services.GetRequestExecutorAsync();

        // act
        await executor.ExecuteAsync(
            """
            {
              person(id: "UGVyc29uOmFiYw==") {
                name
              }
            }
            """);

        var result = await executor.ExecuteAsync(
            """
            {
              person(id: "UGVyc29uOmFiYw==") {
                name
              }
            }
            """);

        // assert
        Snapshot
            .Create()
            .Add(result)
            .MatchInline(
                """
                {
                  "errors": [
                    {
                      "message": "The current user is not authorized to access this resource.",
                      "extensions": {
                        "code": "AUTH_NOT_AUTHORIZED"
                      }
                    }
                  ]
                }
                """);
    }

    [Fact]
    public async Task Authorize_Query_NoAccess()
    {
        // arrange
        var handler = new AuthHandler(
            resolver: AuthorizeResult.Allowed,
            validation: AuthorizeResult.NotAllowed);
        var services = CreateServices(handler);
        var executor = await services.GetRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              person(id: "UGVyc29uOmFiYw==") {
                name
              }
            }
            """);

        // assert
        Snapshot
            .Create()
            .Add(result)
            .MatchInline(
                """
                {
                  "errors": [
                    {
                      "message": "The current user is not authorized to access this resource.",
                      "extensions": {
                        "code": "AUTH_NOT_AUTHORIZED"
                      }
                    }
                  ]
                }
                """);

        Assert.NotNull(result.ContextData);
        Assert.True(result.ContextData!.TryGetValue(HttpStatusCode, out var value));
        Assert.Equal(401, value);
    }

    [Fact]
    public async Task Authorize_Person_AllowAnonymous()
    {
        // arrange
        var handler = new AuthHandler(
            resolver: AuthorizeResult.NotAllowed,
            validation: AuthorizeResult.Allowed);
        var services = CreateServices(handler);
        var executor = await services.GetRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              person(id: "UGVyc29uOmFiYw==") {
                name
              }
              person2(id: "UGVyc29uOmFiYw==") {
                name
              }
            }
            """);

        // assert
        Snapshot
            .Create()
            .Add(result)
            .MatchInline(
                """
                {
                  "errors": [
                    {
                      "message": "The current user is not authorized to access this resource.",
                      "locations": [
                        {
                          "line": 2,
                          "column": 3
                        }
                      ],
                      "path": [
                        "person"
                      ],
                      "extensions": {
                        "code": "AUTH_NOT_AUTHORIZED"
                      }
                    }
                  ],
                  "data": {
                    "person": null,
                    "person2": {
                      "name": "Joe"
                    }
                  }
                }
                """);
    }

    [Fact]
    public async Task Authorize_CityOrStreet_Skip_Auth_When_Street()
    {
        // arrange
        var handler = new AuthHandler(
            resolver: (context, _) =>
                context.Result is Street or null
                    ? AuthorizeResult.Allowed
                    : AuthorizeResult.NotAllowed,
            validation: (_, _) => AuthorizeResult.Allowed);
        var services = CreateServices(handler);
        var executor = await services.GetRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              cityOrStreet(street: true) {
                __typename
              }
            }
            """);

        // assert
        Snapshot
            .Create()
            .Add(result)
            .MatchInline(
                """
                {
                  "data": {
                    "cityOrStreet": {
                      "__typename": "Street"
                    }
                  }
                }
                """);
    }

    [Fact]
    public async Task Authorize_CityOrStreet_Enforce_Auth_When_City()
    {
        // arrange
        var handler = new AuthHandler(
            resolver: (context, _) => context.Result is Street
                ? AuthorizeResult.Allowed
                : AuthorizeResult.NotAllowed,
            validation: (_, _) => AuthorizeResult.Allowed);
        var services = CreateServices(handler);
        var executor = await services.GetRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              cityOrStreet(street: false) {
                __typename
              }
            }
            """);

        // assert
        Snapshot
            .Create()
            .Add(result)
            .MatchInline(
                """
                {
                  "errors": [
                    {
                      "message": "The current user is not authorized to access this resource.",
                      "locations": [
                        {
                          "line": 2,
                          "column": 3
                        }
                      ],
                      "path": [
                        "cityOrStreet"
                      ],
                      "extensions": {
                        "code": "AUTH_NOT_AUTHORIZED"
                      }
                    }
                  ],
                  "data": {
                    "cityOrStreet": null
                  }
                }
                """);
    }

    [Fact]
    public async Task Authorize_Field_Auth_Not_Allowed()
    {
        // arrange
        var handler = new AuthHandler(
            resolver: (_, _) => AuthorizeResult.NotAllowed,
            validation: (_, _) => AuthorizeResult.Allowed);
        var services = CreateServices(handler);
        var executor = await services.GetRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              thisIsAuthorized
            }
            """);

        // assert
        Snapshot
            .Create()
            .Add(result)
            .MatchInline(
                """
                {
                  "errors": [
                    {
                      "message": "The current user is not authorized to access this resource.",
                      "locations": [
                        {
                          "line": 2,
                          "column": 3
                        }
                      ],
                      "path": [
                        "thisIsAuthorized"
                      ],
                      "extensions": {
                        "code": "AUTH_NOT_AUTHORIZED"
                      }
                    }
                  ],
                  "data": {
                    "thisIsAuthorized": null
                  }
                }
                """);
    }

    [Fact]
    public async Task Authorize_Field_Auth_Not_Allowed_On_Validation()
    {
        // arrange
        var handler = new AuthHandler(
            resolver: (_, _) => AuthorizeResult.Allowed,
            validation: (_, d) => d.Policy.EqualsOrdinal("READ_AUTH")
                ? AuthorizeResult.NotAllowed
                : AuthorizeResult.Allowed);
        var services = CreateServices(handler);
        var executor = await services.GetRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              thisIsAuthorizedOnValidation
            }
            """);

        // assert
        Snapshot
            .Create()
            .Add(result)
            .MatchInline(
                """
                {
                  "errors": [
                    {
                      "message": "The current user is not authorized to access this resource.",
                      "extensions": {
                        "code": "AUTH_NOT_AUTHORIZED"
                      }
                    }
                  ]
                }
                """);

        Assert.NotNull(result.ContextData);
        Assert.True(result.ContextData!.TryGetValue(HttpStatusCode, out var value));
        Assert.Equal(401, value);
    }

    [Fact]
    public async Task Authorize_Schema_Field()
    {
        // arrange
        var handler = new AuthHandler(
            resolver: (_, _) => AuthorizeResult.Allowed,
            validation: (_, d) => d.Policy.EqualsOrdinal("READ_INTRO")
                ? AuthorizeResult.NotAllowed
                : AuthorizeResult.Allowed);
        var services = CreateServices(
            handler,
            options =>
            {
                options.ConfigureSchemaField =
                    descriptor => { descriptor.Authorize("READ_INTRO", ApplyPolicy.Validation); };
            });
        var executor = await services.GetRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              __schema {
                description
              }
            }
            """);

        // assert
        Snapshot
            .Create()
            .Add(result)
            .MatchInline(
                """
                {
                  "errors": [
                    {
                      "message": "The current user is not authorized to access this resource.",
                      "extensions": {
                        "code": "AUTH_NOT_AUTHORIZED"
                      }
                    }
                  ]
                }
                """);

        Assert.NotNull(result.ContextData);
        Assert.True(result.ContextData!.TryGetValue(HttpStatusCode, out var value));
        Assert.Equal(401, value);
    }

    [Fact]
    public async Task Authorize_Type_Field()
    {
        // arrange
        var handler = new AuthHandler(
            resolver: (_, _) => AuthorizeResult.Allowed,
            validation: (_, d) => d.Policy.EqualsOrdinal("READ_INTRO")
                ? AuthorizeResult.NotAllowed
                : AuthorizeResult.Allowed);
        var services = CreateServices(
            handler,
            options =>
            {
                options.ConfigureTypeField =
                    descriptor => { descriptor.Authorize("READ_INTRO", ApplyPolicy.Validation); };
            });
        var executor = await services.GetRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              __type(name: "Query") {
                name
              }
            }
            """);

        // assert
        Snapshot
            .Create()
            .Add(result)
            .MatchInline(
                """
                {
                  "errors": [
                    {
                      "message": "The current user is not authorized to access this resource.",
                      "extensions": {
                        "code": "AUTH_NOT_AUTHORIZED"
                      }
                    }
                  ]
                }
                """);

        Assert.NotNull(result.ContextData);
        Assert.True(result.ContextData!.TryGetValue(HttpStatusCode, out var value));
        Assert.Equal(401, value);
    }

    [Fact]
    public async Task Authorize_Node_Field()
    {
        // arrange
        var handler = new AuthHandler(
            resolver: (_, _) => AuthorizeResult.Allowed,
            validation: (_, d) => d.Policy.EqualsOrdinal("READ_NODE")
                ? AuthorizeResult.NotAllowed
                : AuthorizeResult.Allowed);
        var services = CreateServices(
            handler,
            options =>
            {
                options.ConfigureNodeFields =
                    descriptor => { descriptor.Authorize("READ_NODE", ApplyPolicy.Validation); };
            });
        var executor = await services.GetRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              node(id: "abc") {
                __typename
              }
            }
            """);

        // assert
        Snapshot
            .Create()
            .Add(result)
            .MatchInline(
                """
                {
                  "errors": [
                    {
                      "message": "The current user is not authorized to access this resource.",
                      "extensions": {
                        "code": "AUTH_NOT_AUTHORIZED"
                      }
                    }
                  ]
                }
                """);

        Assert.NotNull(result.ContextData);
        Assert.True(result.ContextData!.TryGetValue(HttpStatusCode, out var value));
        Assert.Equal(401, value);
    }

    [Fact]
    public async Task Authorize_Node_Field_Inferred()
    {
        // arrange
        var handler = new AuthHandler(
            resolver: (_, d) => d.Policy.EqualsOrdinal("READ_PERSON")
                ? AuthorizeResult.NotAllowed
                : AuthorizeResult.Allowed,
            validation: (_, _) => AuthorizeResult.Allowed);
        var services = CreateServices(handler);
        var executor = await services.GetRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              node(id: "UGVyc29uOmFiYw==") {
                __typename
              }
            }
            """);

        // assert
        Snapshot
            .Create()
            .Add(result)
            .MatchInline(
                """
                {
                  "errors": [
                    {
                      "message": "The current user is not authorized to access this resource.",
                      "locations": [
                        {
                          "line": 2,
                          "column": 3
                        }
                      ],
                      "path": [
                        "node"
                      ],
                      "extensions": {
                        "code": "AUTH_NOT_AUTHORIZED"
                      }
                    }
                  ],
                  "data": {
                    "node": null
                  }
                }
                """);
    }

    [Fact]
    public async Task Authorize_Node_Field_Inferred_Explicit_NodeResolver()
    {
        // arrange
        var handler = new AuthHandler(
            resolver: (_, d) => d.Policy.EqualsOrdinal("READ_STREET")
                ? AuthorizeResult.NotAllowed
                : AuthorizeResult.Allowed,
            validation: (_, _) => AuthorizeResult.Allowed);
        var services = CreateServices(handler);
        var executor = await services.GetRequestExecutorAsync();
        var id = Convert.ToBase64String("Street:1"u8);

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    """
                    query($id: ID!) {
                      node(id: $id) {
                        __typename
                      }
                    }
                    """)
                .SetVariableValues(new Dictionary<string, object?> { { "id", id }, })
                .Build());

        // assert
        Snapshot
            .Create()
            .Add(result)
            .MatchInline(
                """
                {
                  "errors": [
                    {
                      "message": "The current user is not authorized to access this resource.",
                      "locations": [
                        {
                          "line": 2,
                          "column": 3
                        }
                      ],
                      "path": [
                        "node"
                      ],
                      "extensions": {
                        "code": "AUTH_NOT_AUTHORIZED"
                      }
                    }
                  ],
                  "data": {
                    "node": null
                  }
                }
                """);
    }

    [Fact]
    public async Task Authorize_Node_Field_Inferred_Explicit_NodeResolver_TypePolicy()
    {
        // arrange
        var handler = new AuthHandler(
            resolver: (_, d) =>
                d.Policy.EqualsOrdinal("READ_STREET_ON_TYPE")
                    ? AuthorizeResult.NotAllowed
                    : AuthorizeResult.Allowed,
            validation: (_, _) => AuthorizeResult.Allowed);
        var services = CreateServices(handler);
        var executor = await services.GetRequestExecutorAsync();
        var id = Convert.ToBase64String("Street:1"u8);

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    """
                    query($id: ID!) {
                      node(id: $id) {
                        __typename
                      }
                    }
                    """)
                .SetVariableValues(new Dictionary<string, object?> { { "id", id }, })
                .Build());

        // assert
        Snapshot
            .Create()
            .Add(result)
            .MatchInline(
                """
                {
                  "errors": [
                    {
                      "message": "The current user is not authorized to access this resource.",
                      "locations": [
                        {
                          "line": 2,
                          "column": 3
                        }
                      ],
                      "path": [
                        "node"
                      ],
                      "extensions": {
                        "code": "AUTH_NOT_AUTHORIZED"
                      }
                    }
                  ],
                  "data": {
                    "node": null
                  }
                }
                """);
    }

    [Fact]
    public async Task Authorize_Nodes_Field()
    {
        // arrange
        var handler = new AuthHandler(
            resolver: (_, _) => AuthorizeResult.Allowed,
            validation: (_, d) => d.Policy.EqualsOrdinal("READ_NODE")
                ? AuthorizeResult.NotAllowed
                : AuthorizeResult.Allowed);
        var services = CreateServices(
            handler,
            options =>
            {
                options.ConfigureNodeFields =
                    descriptor => { descriptor.Authorize("READ_NODE", ApplyPolicy.Validation); };
            });
        var executor = await services.GetRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              nodes(ids: "abc") {
                __typename
              }
            }
            """);

        // assert
        Snapshot
            .Create()
            .Add(result)
            .MatchInline(
                """
                {
                  "errors": [
                    {
                      "message": "The current user is not authorized to access this resource.",
                      "extensions": {
                        "code": "AUTH_NOT_AUTHORIZED"
                      }
                    }
                  ]
                }
                """);

        Assert.NotNull(result.ContextData);
        Assert.True(result.ContextData!.TryGetValue(HttpStatusCode, out var value));
        Assert.Equal(401, value);
    }

    [Fact]
    public async Task Skip_Authorize_On_Node_Field()
    {
        // arrange
        var handler = new AuthHandler(
            resolver: (_, _) => AuthorizeResult.Allowed,
            validation: (_, d) => d.Policy.EqualsOrdinal("READ_NODE")
                ? AuthorizeResult.NotAllowed
                : AuthorizeResult.Allowed);
        var services = CreateServices(
            handler,
            options =>
            {
                options.ConfigureNodeFields =
                    descriptor => { descriptor.Authorize("READ_NODE", ApplyPolicy.Validation); };
            });
        var executor = await services.GetRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              nodes(ids: "abc") {
                __typename
              }
            }
            """);

        // assert
        Snapshot
            .Create()
            .Add(result)
            .MatchInline(
                """
                {
                  "errors": [
                    {
                      "message": "The current user is not authorized to access this resource.",
                      "extensions": {
                        "code": "AUTH_NOT_AUTHORIZED"
                      }
                    }
                  ]
                }
                """);

        Assert.NotNull(result.ContextData);
        Assert.True(result.ContextData!.TryGetValue(HttpStatusCode, out var value));
        Assert.Equal(401, value);
    }

    [Fact]
    public async Task Assert_UserState_Exists()
    {
        // arrange
        var handler = new AuthHandler(
            resolver: (ctx, _)
                => ctx.ContextData.ContainsKey(WellKnownContextData.UserState)
                    ? AuthorizeResult.Allowed
                    : AuthorizeResult.NotAllowed,
            validation: (ctx, _)
                => ctx.ContextData.ContainsKey(WellKnownContextData.UserState)
                    ? AuthorizeResult.Allowed
                    : AuthorizeResult.NotAllowed);

        var services = CreateServices(
            handler,
            options =>
            {
                options.ConfigureNodeFields =
                    descriptor => { descriptor.Authorize("READ_NODE"); };
            });

        var executor = await services.GetRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            builder =>
                builder
                    .SetDocument(
                        """
                        {
                          nodes(ids: "abc") {
                            __typename
                          }
                        }
                        """)
                    .SetUser(new ClaimsPrincipal()));

        // assert
        Snapshot
            .Create()
            .Add(result)
            .MatchInline(
                """
                {
                  "errors": [
                    {
                      "message": "The node ID string has an invalid format.",
                      "locations": [
                        {
                          "line": 2,
                          "column": 3
                        }
                      ],
                      "path": [
                        "nodes"
                      ],
                      "extensions": {
                        "originalValue": "abc"
                      }
                    }
                  ],
                  "data": null
                }
                """);
    }

    [Fact]
    public async Task Skip_After_Validation_For_Null()
    {
        // arrange
        var handler = new AuthHandler(
            resolver: (_, p)
                => p.Policy.EnsureGraphQLName().EqualsOrdinal("NULL")
                    ? AuthorizeResult.NotAllowed
                    : AuthorizeResult.Allowed,
            validation: (_, _)
                => AuthorizeResult.Allowed);

        var services = CreateServices(handler);

        var executor = await services.GetRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            builder =>
                builder
                    .SetDocument(
                        """
                        {
                          null
                        }
                        """)
                    .SetUser(new ClaimsPrincipal()));

        // assert
        Snapshot
            .Create()
            .Add(result)
            .MatchInline(
                """
                {
                  "data": {
                    "null": null
                  }
                }
                """);
    }

    private static IServiceProvider CreateServices(
        IAuthorizationHandler handler,
        Action<AuthorizationOptions>? configure = null)
        => new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType<Query>()
            .AddUnionType<ICityOrStreet>()
            .AddType<Street>()
            .AddTypeExtension(typeof(StreetExtensions))
            .AddType<City>()
            .AddGlobalObjectIdentification()
            .AddAuthorizationHandler(_ => handler)
            .ModifyAuthorizationOptions(configure ?? (_ => { }))
            .Services
            .BuildServiceProvider();

    [FooDirective]
    [Authorize("QUERY", ApplyPolicy.Validation)]
    [Authorize("QUERY2", ApplyPolicy.BeforeResolver)]
    public sealed class Query
    {
        [Authorize("NULL", ApplyPolicy.AfterResolver)]
        public string? Null() => null;

        [NodeResolver]
        public Person? GetPerson(string id)
            => new(id, "Joe");

        [AllowAnonymous]
        public Person? GetPerson2(string id)
            => new(id, "Joe");

        public ICityOrStreet? GetCityOrStreet(bool street)
            => street
                ? new Street("Somewhere")
                : new City("Else");

        [Authorize("READ_AUTH", ApplyPolicy.AfterResolver)]
        public bool? ThisIsAuthorized() => true;

        [Authorize("READ_AUTH", ApplyPolicy.Validation)]
        public bool? ThisIsAuthorizedOnValidation() => true;

        [ID(nameof(Person))]
        public string Test() => "abc";
    }

    [Authorize("READ_PERSON", ApplyPolicy.AfterResolver)]
    public sealed record Person(string Id, string? Name);

    [Authorize("READ_STREET_ON_TYPE", ApplyPolicy.BeforeResolver)]
    public sealed record Street(string? Value) : ICityOrStreet;

    [Authorize("READ_CITY", Apply = ApplyPolicy.AfterResolver)]
    public sealed record City(string? Value) : ICityOrStreet;

    [UnionType]
    public interface ICityOrStreet;

    [Node]
    [ExtendObjectType<Street>]
    public static class StreetExtensions
    {
        public static int Id => 1;

        [NodeResolver]
        [Authorize("READ_STREET", ApplyPolicy.BeforeResolver)]
        public static ValueTask<Street> GetStreetById(int id)
            => new(new Street($"abc_{id}"));
    }

    private sealed class AuthHandler : IAuthorizationHandler
    {
        private readonly Func<IMiddlewareContext, AuthorizeDirective, AuthorizeResult> _resolver;

        private readonly Func<AuthorizationContext, AuthorizeDirective, AuthorizeResult>
            _validation;

        public AuthHandler(AuthorizeResult result)
        {
            _resolver = (_, _) => result;
            _validation = (_, _) => result;
        }

        public AuthHandler(AuthorizeResult resolver, AuthorizeResult validation)
        {
            _resolver = (_, _) => resolver;
            _validation = (_, _) => validation;
        }

        public AuthHandler(
            Func<IMiddlewareContext, AuthorizeDirective, AuthorizeResult> resolver,
            Func<AuthorizationContext, AuthorizeDirective, AuthorizeResult> validation)
        {
            _resolver = resolver;
            _validation = validation;
        }

        public ValueTask<AuthorizeResult> AuthorizeAsync(
            IMiddlewareContext context,
            AuthorizeDirective directive,
            CancellationToken cancellationToken = default)
            => new(_resolver(context, directive));

        public ValueTask<AuthorizeResult> AuthorizeAsync(
            AuthorizationContext context,
            IReadOnlyList<AuthorizeDirective> directives,
            CancellationToken cancellationToken = default)
        {
            foreach (var directive in directives)
            {
                var result = _validation(context, directive);

                if (result is not AuthorizeResult.Allowed)
                {
                    return new(result);
                }
            }

            return new(AuthorizeResult.Allowed);
        }
    }

    private sealed class AuthHandler2 : IAuthorizationHandler
    {
        private readonly Stack<AuthorizeResult> _results;

        public AuthHandler2(Stack<AuthorizeResult> results)
        {
            _results = results;
        }

        public ValueTask<AuthorizeResult> AuthorizeAsync(
            IMiddlewareContext context,
            AuthorizeDirective directive,
            CancellationToken cancellationToken = default)
            => new(AuthorizeResult.Allowed);

        public ValueTask<AuthorizeResult> AuthorizeAsync(
            AuthorizationContext context,
            IReadOnlyList<AuthorizeDirective> directives,
            CancellationToken cancellationToken = default)
            => new(_results.Pop());
    }

    [DirectiveType(DirectiveLocation.Object)]
    public sealed class FooDirective;

    public sealed class FooDirectiveAttribute : ObjectTypeDescriptorAttribute
    {
        protected override void OnConfigure(
            IDescriptorContext context,
            IObjectTypeDescriptor descriptor,
            Type type)
            => descriptor.Directive(new FooDirective());
    }

    [Fact]
    public async Task Ensure_Authorization_Works_On_Subscription()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType(c => c.Field("n").Resolve("b"))
                .AddSubscriptionType<Subscription>()
                .AddInMemorySubscriptions()
                .AddAuthorizationHandler<MockAuth>()
                .ExecuteRequestAsync("subscription { onFoo }");

        result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "The current user is not authorized to access this resource.",
                  "extensions": {
                    "code": "AUTH_NOT_AUTHORIZED"
                  }
                }
              ]
            }
            """);
    }

    public class Subscription
    {
        [Authorize(Apply = ApplyPolicy.Validation)]
        [Subscribe]
        [Topic("Foo")]
        public string OnFoo([EventMessage] string message) => message;
    }

    public sealed class MockAuth : IAuthorizationHandler
    {
        public ValueTask<AuthorizeResult> AuthorizeAsync(
            IMiddlewareContext context,
            AuthorizeDirective directive,
            CancellationToken cancellationToken = default)
            => new(AuthorizeResult.NotAllowed);

        public ValueTask<AuthorizeResult> AuthorizeAsync(
            AuthorizationContext context,
            IReadOnlyList<AuthorizeDirective> directives,
            CancellationToken cancellationToken = default)
            => new(AuthorizeResult.NotAllowed);
    }
}
