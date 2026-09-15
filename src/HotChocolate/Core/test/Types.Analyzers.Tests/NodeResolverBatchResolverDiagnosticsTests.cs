namespace HotChocolate.Types;

// The id-parameter validation loop applies to [NodeResolver][BatchResolver] methods too, using
// the same id rule as singular node resolvers.
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
