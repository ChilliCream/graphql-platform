using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Types.Queries.Tests;

public class AnnotationBasedSchemaTests
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
    public async Task Execute_Query_With_FieldResult()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryWithFieldResult>()
                .AddQueryConventions()
                .ExecuteRequestAsync(
                    """
                    {
                      userById(id: "1") {
                        __typename
                      }
                    }
                    """);

        result.MatchSnapshot();
    }

    [Fact]
    public async Task Execute_Query_With_FieldResult_Error()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryWithFieldResult>()
                .AddQueryConventions()
                .ExecuteRequestAsync(
                    """
                    {
                      userById(id: "2") {
                        __typename
                      }
                    }
                    """);

        result.MatchSnapshot();
    }

    [Fact]
    public async Task Execute_Query_With_FieldResult_2()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryWithFieldResult>()
                .AddQueryConventions()
                .ExecuteRequestAsync(
                    """
                    {
                      userById2(id: "1") {
                        __typename
                      }
                    }
                    """);

        result.MatchSnapshot();
    }

    [Fact]
    public async Task Execute_Query_With_FieldResult_2_Error()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryWithFieldResult>()
                .AddQueryConventions()
                .ExecuteRequestAsync(
                    """
                    {
                      userById2(id: "2") {
                        __typename
                      }
                    }
                    """);

        result.MatchSnapshot();
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
    public async Task Execute_Query_With_Exceptions()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryWithException>()
                .AddQueryConventions()
                .ExecuteRequestAsync(
                    """
                    {
                      userById(id: "1") {
                        __typename
                      }
                    }
                    """);

        result.MatchSnapshot();
    }

    [Fact]
    public async Task Execute_Query_With_Exceptions_Error()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryWithException>()
                .AddQueryConventions()
                .ExecuteRequestAsync(
                    """
                    {
                      userById(id: "2") {
                        __typename
                      }
                    }
                    """);

        result.MatchSnapshot();
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
    public async Task Execute_Query_With_FieldResult_And_Exceptions_Success()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryWithFieldResultAndException>()
                .AddQueryConventions()
                .ExecuteRequestAsync(
                    """
                    {
                      userById(id: "1") {
                        __typename
                      }
                    }
                    """);

        result.MatchSnapshot();
    }

    [Fact]
    public async Task Execute_Query_With_FieldResult_And_Exceptions_Error_1()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryWithFieldResultAndException>()
                .AddQueryConventions()
                .ExecuteRequestAsync(
                    """
                    {
                      userById(id: "2") {
                        __typename
                      }
                    }
                    """);

        result.MatchSnapshot();
    }

    [Fact]
    public async Task Execute_Query_With_FieldResult_And_Exceptions_Error_2()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryWithFieldResultAndException>()
                .AddQueryConventions()
                .ExecuteRequestAsync(
                    """
                    {
                      userById(id: "3") {
                        __typename
                      }
                    }
                    """);

        result.MatchSnapshot();
    }

    [Fact]
    public async Task Execute_Query_With_FieldResult_And_Exceptions_Unexpected_Error()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryWithFieldResultAndException>()
                .AddQueryConventions()
                .ExecuteRequestAsync(
                    """
                    {
                      userById(id: "4") {
                        __typename
                      }
                    }
                    """);

        result.MatchSnapshot();
    }

    [Fact]
    public async Task Schema_Query_With_FieldResult_And_Paging()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryWithFieldResultAndPaging>()
                .AddQueryConventions()
                .AddFiltering()
                .AddSorting()
                .BuildSchemaAsync();

        schema.MatchSnapshot();
    }

    [Fact]
    public async Task Execute_Query_With_FieldResult_And_Paging()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryWithFieldResultAndPaging>()
                .AddQueryConventions()
                .AddFiltering()
                .AddSorting()
                .ExecuteRequestAsync(
                    """
                    {
                      users {
                        ... on UsersConnection {
                          nodes {
                            name
                          }
                        }
                        ... on Error {
                          message
                        }
                      }
                    }
                    """);

        result.MatchSnapshot();
    }

    [Fact]
    public async Task Execute_Query_With_FieldResult_And_Paging_Error()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryWithFieldResultAndPaging>()
                .AddQueryConventions()
                .AddFiltering()
                .AddSorting()
                .ExecuteRequestAsync(
                    """
                    {
                      users(error: true) {
                        ... on UsersConnection {
                          nodes {
                            name
                          }
                        }
                        ... on Error {
                          message
                          __typename
                        }
                      }
                    }
                    """);

        result.MatchSnapshot();
    }

    [Fact]
    public async Task Execute_Query_With_FieldResult_And_Paging_Filtering_Sorting()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryWithFieldResultAndPaging>()
                .AddQueryConventions()
                .AddFiltering()
                .AddSorting()
                .ExecuteRequestAsync(
                    """
                    {
                      usersWithFilter {
                        ... on UsersWithFilterConnection {
                          nodes {
                            name
                          }
                        }
                        ... on Error {
                          message
                        }
                      }
                    }
                    """);

        result.MatchSnapshot();
    }

    [Fact]
    public async Task Execute_Query_With_FieldResult_And_Paging_Filtering_Sorting_Error()
    {
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryWithFieldResultAndPaging>()
                .AddQueryConventions()
                .AddFiltering()
                .AddSorting()
                .ExecuteRequestAsync(
                    """
                    {
                      usersWithFilter(error: true) {
                        ... on UsersWithFilterConnection {
                          nodes {
                            name
                          }
                        }
                        ... on Error {
                          message
                        }
                      }
                    }
                    """);

        result.MatchSnapshot();
    }

    [Fact]
    public async Task Schema_Query_With_FieldResult_And_Scalar()
    {
        async Task Error() =>
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<UnionOnScalarFails>()
                .AddQueryConventions()
                .BuildSchemaAsync();

        var exception = await Assert.ThrowsAsync<SchemaException>(Error);
        Assert.Single(exception.Errors);
        exception.Errors[0].Message.MatchSnapshot();
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

        [Error<UserNotFound>]
        public FieldResult<User> GetUserById2(string id)
        {
            if (id == "1")
            {
                return new User("1", "Foo", "foo@bar.de", new AddressNotFound("1", "Failed"));
            }

            return new FieldResult<User>(new UserNotFound(id, "Failed"));
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

            if (id == "2")
            {
                return new UserNotFound(id, "Failed");
            }

            if (id == "3")
            {
                throw new InvalidUserIdException();
            }

            throw new Exception();
        }
    }

    public class QueryWithFieldResultAndPaging
    {
        [UsePaging]
        public FieldResult<IQueryable<User>, UserNotFound> GetUsers(bool error = false)
        {
            if (error)
            {
                return new UserNotFound("id", "Failed");
            }

            return new FieldResult<IQueryable<User>, UserNotFound>(
                new[]
                {
                    new User("1", "Foo", "foo@bar.de", new AddressNotFound("1", "Failed")),
                }.AsQueryable());
        }

        [UsePaging]
        [UseFiltering]
        [UseSorting]
        public FieldResult<IQueryable<User>, UserNotFound> GetUsersWithFilter(bool error = false)
        {
            if (error)
            {
                return new UserNotFound("id", "Failed");
            }

            return new FieldResult<IQueryable<User>, UserNotFound>(
                new[]
                {
                    new User("1", "Foo", "foo@bar.de", new AddressNotFound("1", "Failed")),
                }.AsQueryable());
        }
    }

    public class UnionOnScalarFails
    {
        public FieldResult<string, UserNotFound> GetUsers()
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
