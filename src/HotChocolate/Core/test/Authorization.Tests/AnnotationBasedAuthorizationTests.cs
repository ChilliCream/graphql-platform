using CookieCrumble;
using HotChocolate.Authorization;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Relay;
using HotChocolate.Utilities;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.WellKnownContextData;

namespace HotChocolate.AspNetCore.Authorization;

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
              person(id: "UGVyc29uCmRhYmM=") {
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
              person(id: "UGVyc29uCmRhYmM=") {
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
    public async Task Authorize_CityOrStreet_Skip_Auth_When_Street()
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
                    descriptor =>
                    {
                        descriptor.Authorize("READ_INTRO", ApplyPolicy.Validation);
                    };
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
                    descriptor =>
                    {
                        descriptor.Authorize("READ_INTRO", ApplyPolicy.Validation);
                    };
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
                    descriptor =>
                    {
                        descriptor.Authorize("READ_NODE", ApplyPolicy.Validation);
                    };
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
                    descriptor =>
                    {
                        descriptor.Authorize("READ_NODE", ApplyPolicy.Validation);
                    };
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
                    descriptor =>
                    {
                        descriptor.Authorize("READ_NODE", ApplyPolicy.Validation);
                    };
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

    private static IServiceProvider CreateServices(
        AuthHandler handler,
        Action<AuthorizationOptions>? configure = null)
        => new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType<Query>()
            .AddUnionType<ICityOrStreet>()
            .AddType<Street>()
            .AddType<City>()
            .AddGlobalObjectIdentification()
            .AddAuthorizationHandler(_ => handler)
            .ModifyAuthorizationOptions(configure ?? (_ => { }))
            .Services
            .BuildServiceProvider();

    [FooDirective]
    [Authorize("QUERY", ApplyPolicy.Validation)]
    public sealed class Query
    {
        [NodeResolver]
        public Person? GetPerson(string id)
            => new(id, "Joe");

        public ICityOrStreet? GetCityOrStreet(bool street)
            => street
                ? new Street("Somewhere")
                : new City("Else");

        [Authorize("READ_AUTH")]
        public bool? ThisIsAuthorized() => true;

        [Authorize("READ_AUTH", ApplyPolicy.Validation)]
        public bool? ThisIsAuthorizedOnValidation() => true;

        [ID(nameof(Person))]
        public string Test() => "abc";
    }

    [Authorize("READ_PERSON")]
    public sealed record Person(string Id, string? Name);

    public sealed record Street(string? Value) : ICityOrStreet;

    [Authorize("READ_CITY", Apply = ApplyPolicy.AfterResolver)]
    public sealed record City(string? Value) : ICityOrStreet;

    [UnionType]
    public interface ICityOrStreet { }

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

    [DirectiveType(DirectiveLocation.Object)]
    public sealed class FooDirective { }

    public sealed class FooDirectiveAttribute : ObjectTypeDescriptorAttribute
    {
        public override void OnConfigure(
            IDescriptorContext context,
            IObjectTypeDescriptor descriptor,
            Type type)
            => descriptor.Directive(new FooDirective());
    }
}
