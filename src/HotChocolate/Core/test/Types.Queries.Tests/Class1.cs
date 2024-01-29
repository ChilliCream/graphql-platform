using System;
using System.Linq;
using System.Threading.Tasks;
using CookieCrumble;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Types;
using HotChocolate.Types.Pagination;
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
                .AddQueryConventions()
                .BuildSchemaAsync();
        
        schema.MatchSnapshot();
    }
    
    [Fact]
    public async Task Foo1()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query1>()
                .AddQueryConventions()
                .BuildSchemaAsync();
        
        schema.MatchSnapshot();
    }
    
     
    [Fact]
    public async Task Foo2()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query2>()
                .AddQueryConventions()
                .BuildSchemaAsync();
        
        schema.MatchSnapshot();
    }

    public class Query
    {
        [Error<UserNotFoundException>]
        public User GetUserById(string id)
        {
            if (id == "1")
            {
                return new User("1", "Foo", "foo@bar.de");
            }

            throw new UserNotFoundException();
        }
    }
    
    public class Query1
    {
        public FieldResult<User, UserNotFound> GetUserById(string id)
        {
            if (id == "1")
            {
                return new User("1", "Foo", "foo@bar.de");
            }

            return new UserNotFound(id, "Failed");
        }
    }
    
    public class Query2
    {
        [UsePaging]
        public FieldResult<IQueryable<User>, UserNotFound> GetUsers()
        {
            return new UserNotFound("id", "Failed");
        }
    }
    
    public record User(string Id, string Name, string Email);
    
    public sealed record UserNotFound(string Id, string Message);
    
    public sealed class UserNotFoundException : Exception;

    
}
