using HotChocolate.Execution;
using HotChocolate.Types.Relay;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types.Composite;

public static class NodeFieldInaccessibleTests
{
    [Fact]
    public static async Task NodeFields_Should_NotBeInaccessible_When_OptionsAreDefault()
    {
        // arrange & act
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .AddGlobalObjectIdentification()
                .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public static async Task NodeFields_Should_BeInaccessibleAndShareable_When_OptionIsEnabled()
    {
        // arrange & act
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .AddGlobalObjectIdentification()
                .ModifyOptions(o => o.ApplyInaccessibleToNodeFields = true)
                .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public static async Task NodeFields_Should_BeShareable_When_ShareableIsOffAndInaccessibleIsOn()
    {
        // arrange & act
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .AddGlobalObjectIdentification()
                .ModifyOptions(o => o.ApplyInaccessibleToNodeFields = true)
                .ModifyOptions(o => o.ApplyShareableToNodeFields = false)
                .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        // assert
        schema.MatchSnapshot();
    }

    public class Query
    {
        public Product GetProduct(int id)
            => throw new NotImplementedException();
    }

    [Node]
    public class Product
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;

        public static Product GetProduct(int id)
            => throw new NotImplementedException();
    }
}
