using GreenDonut.Data;

namespace HotChocolate.Types;

public class QueryContextConnectionAnalyzerTests
{
    [Fact]
    public async Task CorrectGenericTypeMatch_NoError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Pagination;
            using GreenDonut.Data;
            using System.Threading;
            using System.Threading.Tasks;

            namespace TestNamespace;

            [ObjectType<Product>]
            public static partial class ProductResolvers
            {
                public static Task<PageConnection<Product>> GetProductsAsync(
                    PagingArguments pagingArgs,
                    QueryContext<Product> query,
                    ProductService productService,
                    CancellationToken cancellationToken)
                    => throw new System.InvalidOperationException();
            }

            public class Product
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            public class ProductService
            {
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task TypeMismatch_WithObjectType_RaisesError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Pagination;
            using GreenDonut.Data;
            using System.Threading;
            using System.Threading.Tasks;

            namespace TestNamespace;

            [ObjectType<Product>]
            public static partial class ProductResolvers
            {
                public static Task<PageConnection<Product>> GetProductsAsync(
                    PagingArguments pagingArgs,
                    QueryContext<Brand> query,
                    ProductService productService,
                    CancellationToken cancellationToken)
                    => throw new System.InvalidOperationException();
            }

            public class Product
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            public class Brand
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            public class ProductService
            {
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task TypeMismatch_WithInterfaceType_RaisesError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Pagination;
            using GreenDonut.Data;
            using System.Threading;
            using System.Threading.Tasks;

            namespace TestNamespace;

            [InterfaceType<IProduct>]
            public static partial class ProductResolvers
            {
                public static Task<PageConnection<Product>> GetProductsAsync(
                    PagingArguments pagingArgs,
                    QueryContext<Brand> query,
                    ProductService productService,
                    CancellationToken cancellationToken)
                    => throw new System.InvalidOperationException();
            }

            public interface IProduct
            {
                int Id { get; }
                string Name { get; }
            }

            public class Product : IProduct
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            public class Brand
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            public class ProductService
            {
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task TypeMismatch_WithQueryType_RaisesError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Pagination;
            using GreenDonut.Data;
            using System.Threading;
            using System.Threading.Tasks;

            namespace TestNamespace;

            [QueryType]
            public static partial class Query
            {
                public static Task<PageConnection<Product>> GetProductsAsync(
                    PagingArguments pagingArgs,
                    QueryContext<Brand> query,
                    ProductService productService,
                    CancellationToken cancellationToken)
                    => throw new System.InvalidOperationException();
            }

            public class Product
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            public class Brand
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            public class ProductService
            {
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task TypeMismatch_WithMutationType_RaisesError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Pagination;
            using GreenDonut.Data;
            using System.Threading;
            using System.Threading.Tasks;

            namespace TestNamespace;

            [MutationType]
            public static partial class Mutation
            {
                public static Task<PageConnection<Product>> AddProductsAsync(
                    PagingArguments pagingArgs,
                    QueryContext<Brand> query,
                    ProductService productService,
                    CancellationToken cancellationToken)
                    => throw new System.InvalidOperationException();
            }

            public class Product
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            public class Brand
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            public class ProductService
            {
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task TypeMismatch_WithSubscriptionType_RaisesError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Pagination;
            using GreenDonut.Data;
            using System.Threading;
            using System.Threading.Tasks;

            namespace TestNamespace;

            [SubscriptionType]
            public static partial class Subscription
            {
                public static Task<PageConnection<Product>> OnProductAddedAsync(
                    PagingArguments pagingArgs,
                    QueryContext<Brand> query,
                    ProductService productService,
                    CancellationToken cancellationToken)
                    => throw new System.InvalidOperationException();
            }

            public class Product
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            public class Brand
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            public class ProductService
            {
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task NoQueryContextParameter_NoError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Pagination;
            using GreenDonut.Data;
            using System.Threading;
            using System.Threading.Tasks;

            namespace TestNamespace;

            [ObjectType<Product>]
            public static partial class ProductResolvers
            {
                public static Task<PageConnection<Product>> GetProductsAsync(
                    PagingArguments pagingArgs,
                    ProductService productService,
                    CancellationToken cancellationToken)
                    => throw new System.InvalidOperationException();
            }

            public class Product
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            public class ProductService
            {
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task NoConnectionReturnType_NoError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate;
            using HotChocolate.Types;
            using GreenDonut.Data;
            using System.Threading;
            using System.Threading.Tasks;
            using System.Collections.Generic;

            namespace TestNamespace;

            [ObjectType<Product>]
            public static partial class ProductResolvers
            {
                public static Task<IEnumerable<Product>> GetProductsAsync(
                    QueryContext<Brand> query,
                    ProductService productService,
                    CancellationToken cancellationToken)
                    => throw new System.InvalidOperationException();
            }

            public class Product
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            public class Brand
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            public class ProductService
            {
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task CorrectGenericTypeMatch_WithConnection_NoError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Pagination;
            using GreenDonut.Data;
            using System.Threading;
            using System.Threading.Tasks;

            namespace TestNamespace;

            [ObjectType<Product>]
            public static partial class ProductResolvers
            {
                public static Task<Connection<Product>> GetProductsAsync(
                    PagingArguments pagingArgs,
                    QueryContext<Product> query,
                    ProductService productService,
                    CancellationToken cancellationToken)
                    => throw new System.InvalidOperationException();
            }

            public class Product
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            public class ProductService
            {
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task TypeMismatch_WithConnection_RaisesError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Types.Pagination;
            using GreenDonut.Data;
            using System.Threading;
            using System.Threading.Tasks;

            namespace TestNamespace;

            [ObjectType<Product>]
            public static partial class ProductResolvers
            {
                public static Task<Connection<Product>> GetProductsAsync(
                    PagingArguments pagingArgs,
                    QueryContext<Brand> query,
                    ProductService productService,
                    CancellationToken cancellationToken)
                    => throw new System.InvalidOperationException();
            }

            public class Product
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            public class Brand
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            public class ProductService
            {
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }
}
