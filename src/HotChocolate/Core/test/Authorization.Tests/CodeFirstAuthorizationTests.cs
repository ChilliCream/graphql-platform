using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Authorization;

public class CodeFirstAuthorizationTests
{
    [Fact]
    public async Task Authorize_Field_Roles_Apply_And_Policy_Roles_Apply()
    {
        // arrange & act
        var schema = await new ServiceCollection()
            .AddGraphQLServer()
            .AddAuthorizationCore()
            .AddQueryType(d =>
            {
                d.Name("Query");
                d.Field("fieldRolesApply")
                    .Resolve("x")
                    .Authorize(["admin", "user"], ApplyPolicy.AfterResolver);
                d.Field("fieldPolicyRolesApply")
                    .Resolve("x")
                    .Authorize("READ", ["admin", "user"], ApplyPolicy.AfterResolver);
            })
            .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public async Task Authorize_Type_Roles_Apply_And_Policy_Roles_Apply()
    {
        // arrange & act
        var schema = await new ServiceCollection()
            .AddGraphQLServer()
            .AddAuthorizationCore()
            .AddQueryType(d =>
            {
                d.Name("Query");
                d.Authorize(["type_reader", "type_writer"], ApplyPolicy.AfterResolver);
                d.Authorize("READ", ["type_reader", "type_writer"], ApplyPolicy.AfterResolver);
                d.Field("field").Resolve("x");
            })
            .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public async Task Authorize_Type_Roles_Apply_And_Policy_Roles_Apply_Generic()
    {
        // arrange & act
        var schema = await new ServiceCollection()
            .AddGraphQLServer()
            .AddAuthorizationCore()
            .AddQueryType(d =>
            {
                d.Name("Query");
                d.Field("rolesApply")
                    .Type<RolesApplyType>()
                    .Resolve(new RolesApplyModel("a"));
                d.Field("policyRolesApply")
                    .Type<PolicyRolesApplyType>()
                    .Resolve(new PolicyRolesApplyModel("b"));
            })
            .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public async Task Authorize_Person_NoAccess()
    {
        // arrange
        var handler = new AuthHandler(
            resolver: AuthorizeResult.NotAllowed,
            validation: AuthorizeResult.Allowed);
        var services = CreateServices(handler);
        var executor = await services.GetRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync("{ person { name } }", TestContext.Current.CancellationToken);

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
        var executor = await services.GetRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync("{ person { name } }", TestContext.Current.CancellationToken);

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
        Assert.True(result.ContextData.TryGetValue(ExecutionContextData.HttpStatusCode, out var value));
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
        var executor = await services.GetRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              cityOrStreet(street: true) {
                __typename
              }
            }
            """,
            TestContext.Current.CancellationToken);

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
        var executor = await services.GetRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              cityOrStreet(street: false) {
                __typename
              }
            }
            """,
            TestContext.Current.CancellationToken);

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
        var executor = await services.GetRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              thisIsAuthorized
            }
            """,
            TestContext.Current.CancellationToken);

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
        var executor = await services.GetRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              thisIsAuthorizedOnValidation
            }
            """,
            TestContext.Current.CancellationToken);

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
        Assert.True(result.ContextData.TryGetValue(ExecutionContextData.HttpStatusCode, out var value));
        Assert.Equal(401, value);
    }

    [Fact]
    public async Task Authorize_Field_Validation_NoAccess_When_Type_Not_Authorized()
    {
        // arrange
        // only the field carries a validation policy; the query type itself is not authorized,
        // so request-level enforcement must be triggered by the field configuration alone.
        var handler = new AuthHandler(
            resolver: (_, _) => AuthorizeResult.Allowed,
            validation: (_, _) => AuthorizeResult.NotAllowed);

        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType(d => d
                    .Name("Query")
                    .Field("sensitiveData")
                    .Type<StringType>()
                    .Resolve("sensitive data")
                    .Authorize("READ_AUTH", ApplyPolicy.Validation))
                .AddAuthorizationHandler(_ => handler)
                .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync("{ sensitiveData }", TestContext.Current.CancellationToken);

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
        Assert.True(result.ContextData.TryGetValue(ExecutionContextData.HttpStatusCode, out var value));
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
                    descriptor => descriptor.Authorize("READ_INTRO", ApplyPolicy.Validation);
            });
        var executor = await services.GetRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              __schema {
                description
              }
            }
            """,
            TestContext.Current.CancellationToken);

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
        Assert.True(result.ContextData.TryGetValue(ExecutionContextData.HttpStatusCode, out var value));
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
                    descriptor => descriptor.Authorize("READ_INTRO", ApplyPolicy.Validation);
            });
        var executor = await services.GetRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              __type(name: "Query") {
                name
              }
            }
            """,
            TestContext.Current.CancellationToken);

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
        Assert.True(result.ContextData.TryGetValue(ExecutionContextData.HttpStatusCode, out var value));
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
                    descriptor => descriptor.Authorize("READ_NODE", ApplyPolicy.Validation);
            });
        var executor = await services.GetRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              node(id: "abc") {
                __typename
              }
            }
            """,
            TestContext.Current.CancellationToken);

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
        Assert.True(result.ContextData.TryGetValue(ExecutionContextData.HttpStatusCode, out var value));
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
                    descriptor => descriptor.Authorize("READ_NODE", ApplyPolicy.Validation);
            });
        var executor = await services.GetRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              nodes(ids: "abc") {
                __typename
              }
            }
            """,
            TestContext.Current.CancellationToken);

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
        Assert.True(result.ContextData.TryGetValue(ExecutionContextData.HttpStatusCode, out var value));
        Assert.Equal(401, value);
    }

    private static IServiceProvider CreateServices(
        AuthHandler handler,
        Action<AuthorizationOptions>? configure = null)
        => new ServiceCollection()
            .AddGraphQLServer()
            .DisableIntrospection(disable: false)
            .AddQueryType<QueryType>()
            .AddGlobalObjectIdentification(o => o.EnsureAllNodesCanBeResolved = false)
            .AddAuthorizationHandler(_ => handler)
            .ModifyAuthorizationOptions(configure ?? (_ => { }))
            .Services
            .BuildServiceProvider();

    private sealed class QueryType : ObjectType<Query>
    {
        protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
        {
            descriptor.Authorize("QUERY", ApplyPolicy.Validation);

            descriptor
                .Field(t => t.GetPerson())
                .Type<PersonType>();

            descriptor
                .Field(t => t.GetCityOrStreet(false))
                .Type<CityOrStreetType>();

            descriptor
                .Field("thisIsAuthorized")
                .Type<BooleanType>()
                .Resolve(true)
                .Authorize("READ_AUTH", ApplyPolicy.BeforeResolver);

            descriptor
                .Field("thisIsAuthorizedOnValidation")
                .Type<BooleanType>()
                .Resolve(true)
                .Authorize("READ_AUTH", ApplyPolicy.Validation);
        }
    }

    private sealed class PersonType : ObjectType<Person>
    {
        protected override void Configure(IObjectTypeDescriptor<Person> descriptor)
        {
            descriptor.Authorize("READ_PERSON", ApplyPolicy.BeforeResolver);
            descriptor.ImplementsNode();
            descriptor.Field("id").Resolve("abc");
        }
    }

    private sealed class CityType : ObjectType<City>
    {
        protected override void Configure(IObjectTypeDescriptor<City> descriptor)
        {
            descriptor.Authorize("READ_CITY", apply: ApplyPolicy.AfterResolver);
        }
    }

    private sealed class StreetType : ObjectType<Street>
    {
        protected override void Configure(IObjectTypeDescriptor<Street> descriptor) { }
    }

    private sealed class CityOrStreetType : UnionType<ICityOrStreet>
    {
        protected override void Configure(IUnionTypeDescriptor descriptor)
        {
            descriptor.Type<CityType>();
            descriptor.Type<StreetType>();
        }
    }

    private sealed class Query
    {
        public Person? GetPerson()
            => new("Joe");

        public ICityOrStreet? GetCityOrStreet(bool street)
            => street
                ? new Street("Somewhere")
                : new City("Else");
    }

    private sealed record Person(string? Name);

    private sealed record Street(string? Value) : ICityOrStreet;

    private sealed record City(string? Value) : ICityOrStreet;

    private interface ICityOrStreet;

    private sealed class AuthHandler : IAuthorizationHandler
    {
        private readonly Func<IMiddlewareContext, AuthorizeDirective, AuthorizeResult> _resolver;
        private readonly Func<AuthorizationContext, AuthorizeDirective, AuthorizeResult> _validation;

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

    private sealed record RolesApplyModel(string? Value);

    private sealed record PolicyRolesApplyModel(string? Value);

    private sealed class RolesApplyType : ObjectType<RolesApplyModel>
    {
        protected override void Configure(IObjectTypeDescriptor<RolesApplyModel> descriptor)
        {
            descriptor.Authorize(["reader", "writer"], ApplyPolicy.AfterResolver);
        }
    }

    private sealed class PolicyRolesApplyType : ObjectType<PolicyRolesApplyModel>
    {
        protected override void Configure(IObjectTypeDescriptor<PolicyRolesApplyModel> descriptor)
        {
            descriptor.Authorize("READ", ["reader", "writer"], ApplyPolicy.AfterResolver);
        }
    }
}
