using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types.Composite;

public static class CollectionSegmentShareableTests
{
    [Fact]
    public static async Task CollectionSegment_Should_Not_Be_Shareable_When_Options_Are_Default()
    {
        // arrange & act
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public static async Task CollectionSegment_Should_Be_Shareable_When_Option_Is_Enabled()
    {
        // arrange & act
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .ModifyOptions(o => o.ApplyShareableToCollectionSegments = true)
                .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public static async Task CollectionSegmentInfo_Should_Be_Shareable_When_Option_Is_Enabled()
    {
        // arrange & act
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .ModifyOptions(o => o.ApplyShareableToCollectionSegmentInfo = true)
                .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public static async Task CollectionSegment_Should_Be_Shareable_When_SourceSchemaDefaults_Are_Applied()
    {
        // arrange & act
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddSourceSchemaDefaults()
                .AddQueryType<Query>()
                .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        // assert
        schema.MatchSnapshot();
    }

    public class Query
    {
        [UseOffsetPaging]
        public IQueryable<Product> GetProducts()
            => throw new NotImplementedException();
    }

    public class Product
    {
        public string Name { get; set; } = null!;
    }
}
