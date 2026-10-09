using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution;
using HotChocolate.Types;

namespace HotChocolate.ApolloFederation.Directives;

public class TagDirectiveTests
{
    [Fact]
    public async Task Tag_InApolloFederationMode_ExcludesDirectiveDefinitionLocation()
    {
        // arrange & act
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddApolloFederation()
            .AddQueryType<Query>()
            .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        // assert
        var tag = schema.DirectiveTypes["tag"];
        Assert.False(tag.Locations.HasFlag(DirectiveLocation.DirectiveDefinition));
    }

    public class Query
    {
        [Tag("public")]
        public string Foo() => "bar";
    }
}
