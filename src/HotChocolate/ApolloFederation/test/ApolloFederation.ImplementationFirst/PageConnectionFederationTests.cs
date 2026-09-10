using HotChocolate.ApolloFederation.Types;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.ApolloFederation;

public class PageConnectionFederationTests
{
    [Fact]
    public async Task PageConnection_Field_With_ApolloFederation_Builds_Schema()
    {
        // arrange & act
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddApolloFederation()
            .AddImplementationFirstTypes()
            .AddPagingArguments()
            .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public async Task PageConnection_Field_With_Explicit_Shareable_PageCursor_Builds_Schema()
    {
        // arrange & act
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddApolloFederation()
            .AddImplementationFirstTypes()
            .AddPagingArguments()
            .AddTypeExtension(new ObjectTypeExtension(d => d.Name("PageCursor").Shareable()))
            .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        // assert
        schema.MatchSnapshot();
    }
}
