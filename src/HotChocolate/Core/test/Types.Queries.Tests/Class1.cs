using System;
using System.Threading.Tasks;
using CookieCrumble;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Types.Queries.Tests;

public class Class1
{
    [Fact]
    public async Task Foo()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .BuildSchemaAsync();
        
        schema.MatchSnapshot();
    }

    public class Query
    {
        [Error<UserNotFoundException>]
        public User GetUserById(string id)                              // UserByIdResult
        {
            if (id == "1")
            {
                return new User("1", "Foo", "foo@bar.de");
            }

            throw new UserNotFoundException();
        }
    }
    
    public record User(string Id, string Name, string Email);

    public record UserNotFound(string Message);
    
    public sealed class UserNotFoundException : Exception;
}
