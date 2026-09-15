namespace HotChocolate.Types;

// A [NodeResolver][BatchResolver] method in an [ObjectType<T>] partial must register through
// INodeDescriptor<TNode>.ResolveNodeBatchWith(MethodInfo), not the classic per-id resolver path.
public class NodeResolverBatchResolverCompilationTests
{
    [Fact]
    public async Task NodeResolver_Should_RegisterBatch_When_AlsoMarkedBatchResolver()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Relay;

            namespace TestNamespace;

            [ObjectType<Product>]
            public static partial class ProductNode
            {
                [NodeResolver]
                [BatchResolver]
                public static Task<List<Product?>> GetProductsById(List<int> id)
                    => default!;
            }

            public class Product
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }
}
