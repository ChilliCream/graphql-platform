using HotChocolate.ApolloFederation.Resolvers;
using HotChocolate.ApolloFederation.Types;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.ApolloFederation.Extensions;

public class ApolloFederationRequestExecutorBuilderExtensionsTests
{
    [Fact]
    public async Task AddApolloFederation_Should_LinkVersion_When_VersionSetThroughOptions()
    {
        // arrange
        var builder = new ServiceCollection()
            .AddGraphQL()
            .AddApolloFederation(o => o.Version = FederationVersion.Federation22)
            .AddQueryType<Query>();

        // act
        var schema = await builder.BuildSchemaAsync();

        // assert
        var link = schema.Directives.FirstOrDefault("link");
        Assert.Equal(
            "https://specs.apollo.dev/federation/v2.2",
            link?.GetArgumentValue<string>("url"));
    }

    [Fact]
    public async Task AddApolloFederation_Should_KeepVersion_When_OptionsConfiguredAfterVersion()
    {
        // arrange
        var builder = new ServiceCollection()
            .AddGraphQL()
            .AddApolloFederation(FederationVersion.Federation22)
            .AddApolloFederation(o => o.MaxAllowedRepresentations = 20_000)
            .AddQueryType<Query>();

        // act
        var schema = await builder.BuildSchemaAsync();

        // assert
        var link = schema.Directives.FirstOrDefault("link");
        Assert.Equal(
            "https://specs.apollo.dev/federation/v2.2",
            link?.GetArgumentValue<string>("url"));
    }

    [Fact]
    public async Task AddApolloFederation_Should_KeepLimit_When_VersionConfiguredAfterOptions()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddApolloFederation(o => o.MaxAllowedRepresentations = 1)
            .AddApolloFederation(FederationVersion.Federation22)
            .AddType<Product>()
            .AddQueryType<Query>()
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            query {
                _entities(representations: [
                    { id: "1", __typename: "Product" }
                    { id: "2", __typename: "Product" }
                ]) {
                    ... on Product { id }
                }
            }
            """);

        // assert
        var error = Assert.Single(result.ExpectOperationResult().Errors!);
        Assert.Equal("HC0140", error.Code);
    }

    public sealed class Query
    {
        public string Ping() => "pong";
    }

    [ReferenceResolver(EntityResolver = nameof(Get))]
    public sealed class Product
    {
        [Key]
        public string Id { get; set; } = null!;

        public static Product Get(string id) => new() { Id = id };
    }
}
