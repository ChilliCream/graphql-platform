namespace HotChocolate.Types;

public class NodeResolverIdParameterAnalyzerTests
{
    [Fact]
    public async Task FirstParameter_NamedId_NoError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Relay;
            using GreenDonut.Data;
            using System.Threading;
            using System.Threading.Tasks;

            namespace TestNamespace;

            [ObjectType<Product>]
            public static partial class ProductNode
            {
                [NodeResolver]
                public static async Task<Product?> GetProductAsync(
                    int id,
                    ProductService productService,
                    QueryContext<Product> query,
                    CancellationToken cancellationToken)
                    => await productService.GetProductByIdAsync(id, query, cancellationToken);
            }

            public class Product
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            public class ProductService
            {
                public Task<Product?> GetProductByIdAsync(int id, QueryContext<Product> query, CancellationToken ct) => default!;
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task IdParameter_NotInFirstPosition_RaisesError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Relay;
            using GreenDonut.Data;
            using System.Threading;
            using System.Threading.Tasks;

            namespace TestNamespace;

            [ObjectType<Product>]
            public static partial class ProductNode
            {
                [NodeResolver]
                public static async Task<Product?> GetProductAsync(
                    ProductService productService,
                    int id,
                    QueryContext<Product> query,
                    CancellationToken cancellationToken)
                    => await productService.GetProductByIdAsync(id, query, cancellationToken);
            }

            public class Product
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            public class ProductService
            {
                public Task<Product?> GetProductByIdAsync(int id, QueryContext<Product> query, CancellationToken ct) => default!;
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task ParameterEndingWithId_NotInFirstPosition_RaisesError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Relay;
            using GreenDonut.Data;
            using System.Threading;
            using System.Threading.Tasks;

            namespace TestNamespace;

            [ObjectType<Product>]
            public static partial class ProductNode
            {
                [NodeResolver]
                public static async Task<Product?> GetProductAsync(
                    ProductService productService,
                    int nodeId,
                    QueryContext<Product> query,
                    CancellationToken cancellationToken)
                    => await productService.GetProductByIdAsync(nodeId, query, cancellationToken);
            }

            public class Product
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            public class ProductService
            {
                public Task<Product?> GetProductByIdAsync(int id, QueryContext<Product> query, CancellationToken ct) => default!;
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task StringIdParameter_NotInFirstPosition_RaisesError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Relay;
            using GreenDonut.Data;
            using System.Threading;
            using System.Threading.Tasks;

            namespace TestNamespace;

            [ObjectType<Product>]
            public static partial class ProductNode
            {
                [NodeResolver]
                public static async Task<Product?> GetProductAsync(
                    ProductService productService,
                    string productId,
                    QueryContext<Product> query,
                    CancellationToken cancellationToken)
                    => await productService.GetProductByIdAsync(productId, query, cancellationToken);
            }

            public class Product
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            public class ProductService
            {
                public Task<Product?> GetProductByIdAsync(string id, QueryContext<Product> query, CancellationToken ct) => default!;
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task GuidIdParameter_NotInFirstPosition_RaisesError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Relay;
            using GreenDonut.Data;
            using System;
            using System.Threading;
            using System.Threading.Tasks;

            namespace TestNamespace;

            [ObjectType<Product>]
            public static partial class ProductNode
            {
                [NodeResolver]
                public static async Task<Product?> GetProductAsync(
                    ProductService productService,
                    Guid productId,
                    QueryContext<Product> query,
                    CancellationToken cancellationToken)
                    => await productService.GetProductByIdAsync(productId, query, cancellationToken);
            }

            public class Product
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            public class ProductService
            {
                public Task<Product?> GetProductByIdAsync(Guid id, QueryContext<Product> query, CancellationToken ct) => default!;
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task FirstParameter_NotNamedId_NoFixableParameter_RaisesError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Relay;
            using GreenDonut.Data;
            using System.Threading;
            using System.Threading.Tasks;

            namespace TestNamespace;

            [ObjectType<Product>]
            public static partial class ProductNode
            {
                [NodeResolver]
                public static async Task<Product?> GetProductAsync(
                    ProductService productService,
                    QueryContext<Product> query,
                    CancellationToken cancellationToken)
                    => await productService.GetProductByIdAsync(query, cancellationToken);
            }

            public class Product
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            public class ProductService
            {
                public Task<Product?> GetProductByIdAsync(QueryContext<Product> query, CancellationToken ct) => default!;
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task MethodWithoutNodeResolver_NoError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Relay;
            using GreenDonut.Data;
            using System.Threading;
            using System.Threading.Tasks;

            namespace TestNamespace;

            [ObjectType<Product>]
            public static partial class ProductNode
            {
                public static async Task<Product?> GetProductAsync(
                    ProductService productService,
                    int nodeId,
                    QueryContext<Product> query,
                    CancellationToken cancellationToken)
                    => await productService.GetProductByIdAsync(nodeId, query, cancellationToken);
            }

            public class Product
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            public class ProductService
            {
                public Task<Product?> GetProductByIdAsync(int id, QueryContext<Product> query, CancellationToken ct) => default!;
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task FirstParameter_NamedId_WithString_NoError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Relay;
            using GreenDonut.Data;
            using System.Threading;
            using System.Threading.Tasks;

            namespace TestNamespace;

            [ObjectType<Product>]
            public static partial class ProductNode
            {
                [NodeResolver]
                public static async Task<Product?> GetProductAsync(
                    string id,
                    ProductService productService,
                    QueryContext<Product> query,
                    CancellationToken cancellationToken)
                    => await productService.GetProductByIdAsync(id, query, cancellationToken);
            }

            public class Product
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            public class ProductService
            {
                public Task<Product?> GetProductByIdAsync(string id, QueryContext<Product> query, CancellationToken ct) => default!;
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }
}
