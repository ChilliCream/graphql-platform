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
    public async Task DoStuff()
    {
        // arrange
        var services = CreateServices();
        var executor = await services.GetRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync("{ person { name } }");

        // assert
        Snapshot
            .Create()
            .Add(result)
            .MatchInline(
                @"");
    }

    private static IServiceProvider CreateServices()
        => new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType<QueryType>()
            .AddAuthorization()
            .AddAuthorizationHandler<AuthHandler>()
            .Services
            .BuildServiceProvider();


    private sealed class QueryType : ObjectType<Query>
    {
        protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
        {
            descriptor.Field(t => t.GetPerson()).Type<PersonType>();
        }
    }

    private sealed class PersonType : ObjectType<Person>
    {
        protected override void Configure(IObjectTypeDescriptor<Person> descriptor)
        {
            descriptor.Authorize("READ_PERSON");
        }
    }

    private sealed class Query
    {
        public Person GetPerson() => new("Joe");
    }

    private sealed record Person(string Name);

    private sealed class AuthHandler : IAuthorizationHandler
    {
        public ValueTask<AuthorizeResult> AuthorizeAsync(
            IMiddlewareContext context,
            AuthorizeDirective directive)
            => new(AuthorizeResult.Allowed);
    }
}
