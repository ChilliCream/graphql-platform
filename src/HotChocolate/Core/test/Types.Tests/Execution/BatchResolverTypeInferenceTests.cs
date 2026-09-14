using HotChocolate.Tests;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution;

public class BatchResolverTypeInferenceTests
{
    [Fact]
    public async Task BatchResolver_Should_InferFieldType_From_Member_When_DiscoveredByReflection()
    {
        // arrange & act & assert
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<AttributeBatchQuery>()
            .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken)
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task BatchResolver_Should_InferFieldType_From_Member_When_DeclaredThroughFieldSelector()
    {
        // arrange & act & assert
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<FieldSelectorBatchQuery>(d =>
            {
                d.Field(q => q.GetNonNullProducts(default!));
                d.Field(q => q.GetNullableProducts(default!));
                d.Field(q => q.GetForcedNonNullProducts(default!));
                d.Field(q => q.GetIds(default!));
            })
            .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken)
            .MatchSnapshotAsync();
    }

    public record BatchProduct(int Id, string Name);

    public class AttributeBatchQuery
    {
        [BatchResolver]
        public List<BatchProduct> GetNonNullProducts(List<int> id)
            => id.ConvertAll(i => new BatchProduct(i, $"Product {i}"));

        [BatchResolver]
        public List<BatchProduct?> GetNullableProducts(List<int> id)
            => id.ConvertAll(i => (BatchProduct?)new BatchProduct(i, $"Product {i}"));

        [BatchResolver]
        [GraphQLNonNullType]
        public List<BatchProduct?> GetForcedNonNullProducts(List<int> id)
            => id.ConvertAll(i => (BatchProduct?)new BatchProduct(i, $"Product {i}"));

        [BatchResolver]
        [GraphQLType<IdType>]
        public List<int> GetIds(List<int> id)
            => id;
    }

    public class FieldSelectorBatchQuery
    {
        [BatchResolver]
        public List<BatchProduct> GetNonNullProducts(List<int> id)
            => id.ConvertAll(i => new BatchProduct(i, $"Product {i}"));

        [BatchResolver]
        public List<BatchProduct?> GetNullableProducts(List<int> id)
            => id.ConvertAll(i => (BatchProduct?)new BatchProduct(i, $"Product {i}"));

        [BatchResolver]
        [GraphQLNonNullType]
        public List<BatchProduct?> GetForcedNonNullProducts(List<int> id)
            => id.ConvertAll(i => (BatchProduct?)new BatchProduct(i, $"Product {i}"));

        [BatchResolver]
        [GraphQLType<IdType>]
        public List<int> GetIds(List<int> id)
            => id;
    }
}
