using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Types.Queries.Tests;

public class CodeFirstSchemaTests
{
    [Fact]
    public async Task Schema_Query_With_FieldResult()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryWithFieldResultType>()
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
                .AddQueryType<QueryWithFieldResultType>()
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
                .AddQueryType<QueryWithFieldResultType>()
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
    public async Task Schema_Query_With_Exceptions()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryWithExceptionType>()
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
                .AddQueryType<QueryWithExceptionType>()
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
                .AddQueryType<QueryWithExceptionType>()
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
                .AddQueryType<QueryWithFieldResultAndExceptionType>()
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
                .AddQueryType<QueryWithFieldResultAndExceptionType>()
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
                .AddQueryType<QueryWithFieldResultAndExceptionType>()
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
                .AddQueryType<QueryWithFieldResultAndExceptionType>()
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
                .AddQueryType<QueryWithFieldResultAndExceptionType>()
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
                .AddQueryType<QueryWithFieldResultAndPagingType>()
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
                .AddQueryType<QueryWithFieldResultAndPagingType>()
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
                .AddQueryType<QueryWithFieldResultAndPagingType>()
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
                .AddQueryType<QueryWithFieldResultAndPagingType>()
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
                .AddQueryType<QueryWithFieldResultAndPagingType>()
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
                .AddQueryType<UnionOnScalarFailsType>()
                .AddQueryConventions()
                .BuildSchemaAsync();

        var exception = await Assert.ThrowsAsync<SchemaException>(Error);
        Assert.Single(exception.Errors);
        exception.Errors[0].Message.MatchSnapshot();
    }

    [Fact]
    public async Task Throw_SchemaError_When_FieldResult_Has_No_Errors()
    {
        async Task Error()
            => await new ServiceCollection()
                .AddGraphQL()
                .AddQueryConventions()
                .AddQueryType<InvalidQuery>()
                .BuildSchemaAsync();

        var exception = await Assert.ThrowsAsync<SchemaException>(Error);
        Assert.Single(exception.Errors);
        exception.Errors[0].Message.MatchSnapshot();
    }

    [Fact]
    public async Task Throw_SchemaError_When_FieldResult_Has_No_Errors_1()
    {
        async Task Error()
            => await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<InvalidQuery>()
                .BuildSchemaAsync();

        var exception = await Assert.ThrowsAsync<SchemaException>(Error);
        Assert.Single(exception.Errors);
        exception.Errors[0].Message.MatchSnapshot();
    }

    [Fact]
    public async Task Throw_SchemaError_When_FieldResult_Has_No_Errors_2()
    {
        async Task Error()
            => await new ServiceCollection()
                .AddGraphQL()
                .AddQueryConventions()
                .AddQueryType<InvalidQueryTask>()
                .BuildSchemaAsync();

        var exception = await Assert.ThrowsAsync<SchemaException>(Error);
        Assert.Single(exception.Errors);
        exception.Errors[0].Message.MatchSnapshot();
    }

    [Fact]
    public async Task Throw_SchemaError_When_FieldResult_Has_No_Errors_3()
    {
        async Task Error()
            => await new ServiceCollection()
                .AddGraphQL()
                .AddQueryConventions()
                .AddQueryType<InvalidQueryValueTask>()
                .BuildSchemaAsync();

        var exception = await Assert.ThrowsAsync<SchemaException>(Error);
        Assert.Single(exception.Errors);
        exception.Errors[0].Message.MatchSnapshot();
    }

    [Fact]
    public async Task FieldResult_With_Errors_Are_Valid()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryConventions()
                .AddQueryType<ValidQueryValueTask>()
                .BuildSchemaAsync();

        schema.MatchSnapshot();
    }

    public class QueryWithFieldResultType : ObjectType
    {
        protected override void Configure(IObjectTypeDescriptor descriptor)
        {
            descriptor
                .Field("userById")
                .Argument("id", a => a.Type<NonNullType<StringType>>())
                .Resolve<FieldResult<User, UserNotFound>>(
                    ctx =>
                    {
                        var id = ctx.ArgumentValue<string>("id");

                        if (id == "1")
                        {
                            return new User("1", "Foo", "foo@bar.de", new AddressNotFound("1", "Failed"));
                        }

                        return new UserNotFound(id, "Failed");
                    });
        }
    }

    public class QueryWithExceptionType : ObjectType
    {
        protected override void Configure(IObjectTypeDescriptor descriptor)
        {
            descriptor
                .Field("userById")
                .Argument("id", a => a.Type<NonNullType<StringType>>())
                .Error<UserNotFoundException>()
                .Resolve(
                    ctx =>
                    {
                        var id = ctx.ArgumentValue<string>("id");

                        if (id == "1")
                        {
                            return new User("1", "Foo", "foo@bar.de", new AddressNotFound("1", "Failed"));
                        }

                        throw new UserNotFoundException();
                    });
        }
    }

    public class QueryWithFieldResultAndExceptionType : ObjectType
    {
        protected override void Configure(IObjectTypeDescriptor descriptor)
        {
            descriptor
                .Field("userById")
                .Argument("id", a => a.Type<NonNullType<StringType>>())
                .Error<UserNotFound>()
                .Error<InvalidUserIdException>()
                .Type<NonNullType<ObjectType<User>>>()
                .Resolve(
                    ctx =>
                    {
                        var id = ctx.ArgumentValue<string>("id");

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
                    });
        }
    }

    public class QueryWithFieldResultAndPagingType : ObjectType
    {
        protected override void Configure(IObjectTypeDescriptor descriptor)
        {
            descriptor
                .Field("users")
                .Argument("error", a => a.Type<NonNullType<BooleanType>>().DefaultValue(false))
                .UsePaging()
                .Resolve<FieldResult<IQueryable<User>, UserNotFound>>(
                    ctx =>
                    {
                        var error = ctx.ArgumentValue<bool>("error");

                        if (error)
                        {
                            return new UserNotFound("id", "Failed");
                        }

                        return new FieldResult<IQueryable<User>, UserNotFound>(
                            new[]
                            {
                                new User("1", "Foo", "foo@bar.de", new AddressNotFound("1", "Failed")),
                            }.AsQueryable());
                    });

            descriptor
                .Field("usersWithFilter")
                .Argument("error", a => a.Type<NonNullType<BooleanType>>().DefaultValue(false))
                .UsePaging()
                .UseFiltering()
                .UseSorting()
                .Resolve(
                    ctx =>
                    {
                        var error = ctx.ArgumentValue<bool>("error");

                        if (error)
                        {
                            return new UserNotFound("id", "Failed");
                        }

                        return new FieldResult<IQueryable<User>, UserNotFound>(
                            new[]
                            {
                                new User("1", "Foo", "foo@bar.de", new AddressNotFound("1", "Failed")),
                            }.AsQueryable());
                    });
        }
    }

    public class UnionOnScalarFailsType : ObjectType
    {
        protected override void Configure(IObjectTypeDescriptor descriptor)
        {
            descriptor
                .Field("userById")
                .Argument("id", a => a.Type<NonNullType<StringType>>())
                .Error<UserNotFound>()
                .Resolve("some string");
        }
    }

    public record User(string Id, string Name, string Email, FieldResult<Address, AddressNotFound> Address);

    public record Address(string Id, string Street, string City);

    public sealed record UserNotFound(string Id, string Message);

    public sealed record AddressNotFound(string Id, string Message);

    public sealed class UserNotFoundException : Exception;

    public sealed class InvalidUserIdException : Exception;

    public class InvalidQuery
    {
        public FieldResult<Foo> Foo() => default!;
    }

    public class InvalidQueryTask
    {
        public Task<FieldResult<Foo>> Foo() => default!;
    }

    public class InvalidQueryValueTask
    {
        public Task<FieldResult<Foo>> Foo() => default!;
    }

    public class ValidQueryValueTask
    {
        [Error<ArgumentException>]
        public Task<FieldResult<Foo>> Foo() => default!;
    }

    public class Foo
    {
        public string Bar => default!;
    }
}
