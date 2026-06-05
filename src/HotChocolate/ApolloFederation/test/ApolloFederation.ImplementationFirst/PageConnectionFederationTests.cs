using HotChocolate.Execution;
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
            .BuildSchemaAsync();

        // assert
        schema.MatchSnapshot();
    }
}
