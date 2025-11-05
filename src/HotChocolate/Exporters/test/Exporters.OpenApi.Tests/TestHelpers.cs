using HotChocolate.Authorization;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Fusion;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using HotChocolate.Resolvers;
using HotChocolate.Types.Relay;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Exporters.OpenApi;

internal static class TestHelpers
{
    public static async Task<IFusionGatewayBuilder> AddBasicServerAsync(
        this IFusionGatewayBuilder builder,
        TestServerSession session)
    {
        // TODO: This should also have auth like the regular stuff
        var subgraph = session.CreateServer(
            services =>
            {
                services
                    .AddGraphQLServer()
                    .AddBasicServer();
            },
            app =>
            {
                app
                    .UseRouting()
                    .UseEndpoints(endpoints => endpoints.MapGraphQL());
            });

        var schema = await subgraph.Services.GetSchemaAsync();

        var sourceSchema = new SourceSchemaText("A", schema.ToString());

        builder.Services.AddSingleton<IHttpClientFactory>(_ => new MockHttpClientFactory(subgraph));

        builder
            .AddHttpClientConfiguration("A", new Uri("http://localhost:5000/graphql"));

        var compositionLog = new CompositionLog();
        var composerOptions = new SchemaComposerOptions
        {
            Merger =
            {
                EnableGlobalObjectIdentification = true
            }
        };
        var composer = new SchemaComposer([sourceSchema], composerOptions, compositionLog);
        var result = composer.Compose();

        if (!result.IsSuccess)
        {
            throw new InvalidOperationException("Failed to compose schema.");
        }

        builder.AddInMemoryConfiguration(result.Value.ToSyntaxNode());

        return builder;
    }

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
            public User? GetUserById([GraphQLDescription("The id of the user")] [ID] int id, IResolverContext context)
            {
                if (id == 5)
                {
                    throw new GraphQLException(
                        ErrorBuilder.New()
                            .SetMessage("Something went wrong")
                            .SetPath(context.Path)
                            .Build());
                }

                if (id < 1 || id > 3)
                {
                    return null;
                }

                return new User(id);
            }

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

    private class MockHttpClientFactory(TestServer subgraph) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            return subgraph.CreateClient();
        }
    }
}
