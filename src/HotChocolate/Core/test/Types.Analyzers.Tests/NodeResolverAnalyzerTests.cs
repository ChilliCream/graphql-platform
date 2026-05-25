namespace HotChocolate.Types;

public class NodeResolverAnalyzerTests
{
    [Fact]
    public async Task NodeResolver_WithIdAttribute_RaisesError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Relay;
            using System.Threading.Tasks;

            namespace TestNamespace;

            [ObjectType<Product>]
            internal static partial class ProductType
            {
                [NodeResolver]
                public static Task<Product?> GetProductAsync([ID] int id)
                    => Task.FromResult<Product?>(null);
            }

            internal class Product
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task NodeResolver_WithIdGenericAttribute_RaisesError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Relay;
            using System.Threading.Tasks;

            namespace TestNamespace;

            [ObjectType<Product>]
            internal static partial class ProductType
            {
                [NodeResolver]
                public static Task<Product?> GetProductAsync([ID<Product>] int id)
                    => Task.FromResult<Product?>(null);
            }

            internal class Product
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task NodeResolver_WithoutIdAttribute_NoError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Relay;
            using System.Threading.Tasks;

            namespace TestNamespace;

            [ObjectType<Product>]
            internal static partial class ProductType
            {
                [NodeResolver]
                public static Task<Product?> GetProductAsync(int id)
                    => Task.FromResult<Product?>(null);
            }

            internal class Product
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task NodeResolver_NonPublic_RaisesError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Relay;
            using System.Threading.Tasks;

            namespace TestNamespace;

            [ObjectType<Product>]
            internal static partial class ProductType
            {
                [NodeResolver]
                internal static Task<Product?> GetProductAsync(int id)
                    => Task.FromResult<Product?>(null);
            }

            internal class Product
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task NodeResolver_Private_RaisesError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Relay;
            using System.Threading.Tasks;

            namespace TestNamespace;

            [ObjectType<Product>]
            internal static partial class ProductType
            {
                [NodeResolver]
                private static Task<Product?> GetProductAsync(int id)
                    => Task.FromResult<Product?>(null);
            }

            internal class Product
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task NodeResolver_Public_NoError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Relay;
            using System.Threading.Tasks;

            namespace TestNamespace;

            [ObjectType<Product>]
            internal static partial class ProductType
            {
                [NodeResolver]
                public static Task<Product?> GetProductAsync(int id)
                    => Task.FromResult<Product?>(null);
            }

            internal class Product
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task NodeResolver_NonStatic_WithIdAttribute_RaisesError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Relay;
            using System.Threading.Tasks;

            namespace TestNamespace;

            public class ProductService
            {
                [NodeResolver]
                public Task<Product?> GetProductAsync([ID] int id)
                    => Task.FromResult<Product?>(null);
            }

            internal class Product
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task NodeResolver_NonStatic_Private_RaisesError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Relay;
            using System.Threading.Tasks;

            namespace TestNamespace;

            public class ProductService
            {
                [NodeResolver]
                private Task<Product?> GetProductAsync(int id)
                    => Task.FromResult<Product?>(null);
            }

            internal class Product
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task NodeResolver_BothIssues_RaisesMultipleErrors()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Relay;
            using System.Threading.Tasks;

            namespace TestNamespace;

            [ObjectType<Product>]
            internal static partial class ProductType
            {
                [NodeResolver]
                private static Task<Product?> GetProductAsync([ID] int id)
                    => Task.FromResult<Product?>(null);
            }

            internal class Product
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }
}
