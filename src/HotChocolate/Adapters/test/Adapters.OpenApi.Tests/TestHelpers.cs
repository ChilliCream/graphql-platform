using HotChocolate.Authorization;
using HotChocolate.Execution.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Types.Relay;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Adapters.OpenApi;

internal static class TestHelpers
{
    public static IRequestExecutorBuilder AddBasicServer(this IRequestExecutorBuilder builder)
    {
        return builder.AddAuthorization()
            .ModifyOptions(o => o.SortFieldsByName = true)
            .AddQueryType<BasicServer.Query>()
            .AddMutationType<BasicServer.Mutation>();
    }

    private static class BasicServer
    {
        public class Query
        {
            public User? GetUserById([GraphQLDescription("The id of the user")][ID] int id, IResolverContext context)
            {
                if (id == 5)
                {
                    throw new GraphQLException(
                        ErrorBuilder.New()
                            .SetMessage("Something went wrong")
                            .SetPath(context.Path)
                            .Build());
                }

                if (id is < 1 or > 3)
                {
                    return null;
                }

                return new User(id);
            }

            public User? GetUserByName(string name)
                => new(1) { Name = name };

            [Authorize(Roles = [OpenApiTestBase.AdminRole])]
            public IEnumerable<User> GetUsers()
                => [new User(1), new User(2), new User(3)];

            public IEnumerable<User> GetUsersWithoutAuth()
                => [new User(1), new User(2), new User(3)];
        }

        public class Mutation
        {
            public User CreateUser(UserInput user)
            {
                return new User(user.Id);
            }

            public User UpdateUser(UserInput user)
            {
                return CreateUser(user);
            }
        }

        public class UserInput
        {
            [ID]
            public int Id { get; init; }

            public required string Name { get; init; }

            [GraphQLDescription("The user's email")]
            public required string Email { get; init; }
        }

        public sealed class User(int id)
        {
            [ID]
            public int Id { get; init; } = id;

            [GraphQLDescription("The name of the user")]
            public string Name { get; set; } = "User " + id;

            [GraphQLDeprecated("Deprecated for some reason")]
            public string? Email { get; set; } = id + "@example.com";

            public Address Address { get; set; } = new Address(id + " Street");

            public Preferences? Preferences { get; init; }
        }

        public sealed record Address(string Street);

        public sealed record Preferences(string Color);
    }
}
