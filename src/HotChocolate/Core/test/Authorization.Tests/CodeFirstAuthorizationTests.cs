using CookieCrumble;
using HotChocolate.Authorization;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.AspNetCore.Authorization;

public class CodeFirstAuthorizationTests
{
    [Fact]
    public async Task Authorize_Person_NoAccess()
    {
        // arrange
        var handler = new AuthHandler(AuthorizeResult.NotAllowed);
        var services = CreateServices(handler);
        var executor = await services.GetRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync("{ person { name } }");

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
                          "line": 1,
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
            (_, _) => AuthorizeResult.Allowed,
            (_, _) => AuthorizeResult.NotAllowed);
        var services = CreateServices(handler);
        var executor = await services.GetRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync("{ person { name } }");

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
                          "line": 1,
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
    public async Task Authorize_CityOrStreet_Skip_Auth_When_Street()
    {
        // arrange
        var handler = new AuthHandler(
            (context, _) => context.Result is Street
                ? AuthorizeResult.Allowed
                : AuthorizeResult.NotAllowed,
            (_, _) => AuthorizeResult.Allowed);
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
            (context, _) => context.Result is Street
                ? AuthorizeResult.Allowed
                : AuthorizeResult.NotAllowed,
            (_, _) => AuthorizeResult.Allowed);
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
        var handler = new AuthHandler(AuthorizeResult.NotAllowed);
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

    private static IServiceProvider CreateServices(AuthHandler handler)
        => new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType<QueryType>()
            .AddAuthorizationHandler(_ => handler)
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
                .Field(t => t.GetCityOrStreet(default))
                .Type<CityOrStreetType>();

            descriptor
                .Field("thisIsAuthorized")
                .Type<BooleanType>()
                .Resolve(true)
                .Authorize("READ_AUTH");
        }
    }

    private sealed class PersonType : ObjectType<Person>
    {
        protected override void Configure(IObjectTypeDescriptor<Person> descriptor)
        {
            descriptor.Authorize("READ_PERSON");
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
        protected override void Configure(IObjectTypeDescriptor<Street> descriptor)
        {
        }
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
            => street ? new Street("Somewhere") : new City("Else");
    }

    private sealed record Person(string? Name);

    private sealed record Street(string? Value) : ICityOrStreet;

    private sealed record City(string? Value) : ICityOrStreet;

    private interface ICityOrStreet { }

    private sealed class AuthHandler : IAuthorizationHandler
    {
        private readonly Func<IMiddlewareContext, AuthorizeDirective, AuthorizeResult> _func1;
        private readonly Func<AuthorizationContext, AuthorizeDirective, AuthorizeResult> _func2;

        public AuthHandler(AuthorizeResult result)
        {
            _func1 = (_, _) => result;
            _func2 = (_, _) => result;
        }

        public AuthHandler(
            Func<IMiddlewareContext, AuthorizeDirective, AuthorizeResult> func1,
            Func<AuthorizationContext, AuthorizeDirective, AuthorizeResult> func2)
        {
            _func1 = func1;
            _func2 = func2;
        }

        public ValueTask<AuthorizeResult> AuthorizeAsync(
            IMiddlewareContext context,
            AuthorizeDirective directive,
            CancellationToken cancellationToken = default)
            => new(_func1(context, directive));

        public ValueTask<AuthorizeResult> AuthorizeAsync(
            AuthorizationContext context,
            IReadOnlyList<AuthorizeDirective> directives,
            CancellationToken cancellationToken = default)
        {
            foreach (var directive in directives)
            {
                var result = _func2(context, directive);
                if (result is not AuthorizeResult.Allowed)
                {
                    return new(result);
                }
            }

            return new(AuthorizeResult.Allowed);
        }
    }
}
