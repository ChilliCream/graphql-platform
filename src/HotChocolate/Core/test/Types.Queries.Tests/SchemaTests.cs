using System;
using System.Linq;
using System.Threading.Tasks;
using CookieCrumble;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Types.Queries.Tests;

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
    public async Task Schema_Query_With_FieldResult_And_Exceptions()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryWithFieldResultAndException>()
                .AddQueryConventions()
                .BuildSchemaAsync();
        
        schema.MatchSnapshot();
    }
    
     
    [Fact]
    public async Task Schema_Query_With_FieldResult_And_Paging()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryWithFieldResultAndPaging>()
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
    
    public class QueryWithFieldResultAndException
    {
        [Error<InvalidUserIdException>]
        public FieldResult<User, UserNotFound> GetUserById(string id)
        {
            if (id == "1")
            {
                return new User("1", "Foo", "foo@bar.de", new AddressNotFound("1", "Failed"));
            }

            return new UserNotFound(id, "Failed");
        }
    }
    
    public class QueryWithFieldResultAndPaging
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

    public sealed class InvalidUserIdException : Exception;
}
