namespace HotChocolate.Types;

// A [NodeResolver][BatchResolver] method used to register through
// INodeDescriptor<TNode>.ResolveNodeBatchWith(MethodInfo) before the id-parameter validation
// loop ran, so InvalidNodeResolverArgumentName and TooManyNodeResolverArguments never fired for
// it. The loop must run for batch node resolvers too, using the same id rule as singular node
// resolvers (parameter named `id` or ending in `Id`, otherwise the first parameter).
public class NodeResolverBatchResolverDiagnosticsTests
{
    [Fact]
    public async Task NodeResolver_Should_ReportInvalidArgumentName_When_BatchIdParameterIsMisnamed()
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
                public static Task<List<Product?>> GetProductsById([Argument] List<int> key)
                    => default!;
            }

            public class Product
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }
            """).MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task NodeResolver_Should_ReportTooManyArguments_When_BatchResolverHasTwoArgumentParameters()
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
                public static Task<List<Product?>> GetProductsById(
                    List<int> id,
                    [Argument("id")] List<int> alsoId)
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
