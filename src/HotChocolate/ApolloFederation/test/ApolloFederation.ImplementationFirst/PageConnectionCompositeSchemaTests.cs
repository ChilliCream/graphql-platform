using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.ApolloFederation;

// Counterpart to PageConnectionFederationTests: the composite schema path also makes
// connections and page info shareable (AddSourceSchemaDefaults turns on
// ApplyShareableToConnections, ApplyShareableToPageInfo and ApplyShareableToNodeFields).
// Unlike the Apollo Federation interceptor, every composite site registers the Shareable
// directive type before applying it, so building the schema must not crash.
public class PageConnectionCompositeSchemaTests
{
    [Fact]
    public async Task PageConnection_Field_With_CompositeSourceSchema_Builds_Schema()
    {
        // arrange & act
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddSourceSchemaDefaults()
            .AddImplementationFirstTypes()
            .AddPagingArguments()
            .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        // assert
        schema.MatchSnapshot();
    }
}
