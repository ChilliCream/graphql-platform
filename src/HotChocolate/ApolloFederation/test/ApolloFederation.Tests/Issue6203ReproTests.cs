using HotChocolate.ApolloFederation.Resolvers;
using HotChocolate.ApolloFederation.Types;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.ApolloFederation;

public class Issue6203ReproTests
{
    [Fact]
    public async Task Required_External_Field_Is_Populated_On_Entity_Extension()
    {
        var result = await new ServiceCollection()
            .AddGraphQL()
            .AddApolloFederation()
            .AddQueryType<Query>()
            .AddType<User>()
            .ExecuteRequestAsync(
                """
                {
                  _entities(representations: [{ __typename: "User", id: "1", firstName: "Jane" }]) {
                    ... on User {
                      fullName
                    }
                  }
                }
                """);

        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "_entities": [
                  {
                    "fullName": "Jane Doe"
                  }
                ]
              }
            }
            """);
    }

    public sealed class Query
    {
        public string Ping() => "pong";
    }

    [Key("id")]
    [ExtendServiceType]
    [ReferenceResolver(EntityResolver = nameof(ResolveById))]
    public sealed class User
    {
        [External]
        public string Id { get; set; } = null!;

        [External]
        public string? FirstName { get; private set; }

        [Requires("firstName")]
        public string? FullName => FirstName is null ? null : $"{FirstName} Doe";

        public static User ResolveById(string id) => new() { Id = id };
    }
}
