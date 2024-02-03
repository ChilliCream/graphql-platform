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

// 1. error when used with scalars


public class SchemaTests
{
    [Fact]
    public async Task Schema_Query_With_FieldResult()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryWithFieldResult>()
                .AddQueryConventions()
                .BuildSchemaAsync();
        
        schema.MatchSnapshot();
    }
    
    [Fact]
    public async Task Schema_Query_With_Exceptions()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryWithException>()
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

    public class QueryWithException
    {
        [Error<UserNotFoundException>]
        public User GetUserById(string id)
        {
            if (id == "1")
            {
                return new User("1", "Foo", "foo@bar.de", new AddressNotFound("1", "Failed"));
            }

            throw new UserNotFoundException();
        }
    }
    
    public class QueryWithFieldResult
    {
        public FieldResult<User, UserNotFound> GetUserById(string id)
        {
            if (id == "1")
            {
                return new User("1", "Foo", "foo@bar.de", new AddressNotFound("1", "Failed"));
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
    
    public record User(string Id, string Name, string Email, FieldResult<Address, AddressNotFound> Address);

    public record Address(string Id, string Street, string City);
    
    public sealed record UserNotFound(string Id, string Message);
    
    public sealed record AddressNotFound(string Id, string Message);
    
    public sealed class UserNotFoundException : Exception;

    
}
