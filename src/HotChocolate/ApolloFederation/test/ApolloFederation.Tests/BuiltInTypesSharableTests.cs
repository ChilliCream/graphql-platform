using HotChocolate.ApolloFederation.Types;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.ApolloFederation;

public class BuiltInTypesSharableTests
{
    [Fact]
    public async Task Ensure_PagingInfo_Is_Sharable()
    {
        // arrange
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddApolloFederation()
            .AddQueryType<Query>()
            .BuildSchemaAsync();

        // act/assert
        schema.MatchSnapshot();
    }

    [Fact]
    public async Task Ensure_PagingInfo_Is_Sharable_When_Sharable_Already_Registered()
    {
        // arrange
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddApolloFederation()
            .AddQueryType<Query>()
            .AddType(typeof(ShareableDirective))
            .BuildSchemaAsync();

        // act/assert
        schema.MatchSnapshot();
    }


    public class Query
    {
        [UsePaging]
        public IQueryable<Address> GetAddresses() => throw new NotImplementedException();
    }

    public class Address
    {
        public required string Street { get; set; }
    }
}
