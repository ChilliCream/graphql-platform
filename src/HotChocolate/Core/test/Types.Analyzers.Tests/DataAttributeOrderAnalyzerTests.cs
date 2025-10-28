namespace HotChocolate.Types;

public class DataAttributeOrderAnalyzerTests
{
    [Fact]
    public async Task CorrectOrder_AllAttributes_NoError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate;
            using HotChocolate.Data;
            using HotChocolate.Types;
            using System.Linq;

            namespace TestNamespace;

            [ObjectType<Product>]
            public static partial class ProductResolvers
            {
                [UsePaging]
                [UseProjection]
                [UseFiltering]
                [UseSorting]
                public static IQueryable<Product> GetProducts(ProductService productService)
                    => productService.GetProducts();
            }

            public class Product
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            public class ProductService
            {
                public IQueryable<Product> GetProducts() => null!;
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task CorrectOrder_SomeAttributes_NoError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate;
            using HotChocolate.Data;
            using HotChocolate.Types;
            using System.Linq;

            namespace TestNamespace;

            [ObjectType<Product>]
            public static partial class ProductResolvers
            {
                [UseProjection]
                [UseFiltering]
                [UseSorting]
                public static IQueryable<Product> GetProducts(ProductService productService)
                    => productService.GetProducts();
            }

            public class Product
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            public class ProductService
            {
                public IQueryable<Product> GetProducts() => null!;
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task CorrectOrder_WithCustomAttributes_NoError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate;
            using HotChocolate.Data;
            using HotChocolate.Types;
            using System;
            using System.Linq;

            namespace TestNamespace;

            [ObjectType<Product>]
            public static partial class ProductResolvers
            {
                [UsePaging]
                [UseProjection]
                [Obsolete]
                [UseFiltering]
                [UseSorting]
                public static IQueryable<Product> GetProducts(ProductService productService)
                    => productService.GetProducts();
            }

            public class Product
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            public class ProductService
            {
                public IQueryable<Product> GetProducts() => null!;
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task IncorrectOrder_ProjectionBeforePaging_RaisesError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate;
            using HotChocolate.Data;
            using HotChocolate.Types;
            using System.Linq;

            namespace TestNamespace;

            [ObjectType<Product>]
            public static partial class ProductResolvers
            {
                [UseProjection]
                [UseFiltering]
                [UsePaging]
                [UseSorting]
                public static IQueryable<Product> GetProducts(ProductService productService)
                    => productService.GetProducts();
            }

            public class Product
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            public class ProductService
            {
                public IQueryable<Product> GetProducts() => null!;
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task IncorrectOrder_SortingBeforeFiltering_RaisesError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate;
            using HotChocolate.Data;
            using HotChocolate.Types;
            using System.Linq;

            namespace TestNamespace;

            [ObjectType<Product>]
            public static partial class ProductResolvers
            {
                [UseSorting]
                [UseFiltering]
                [UsePaging]
                public static IQueryable<Product> GetProducts(ProductService productService)
                    => productService.GetProducts();
            }

            public class Product
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            public class ProductService
            {
                public IQueryable<Product> GetProducts() => null!;
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task IncorrectOrder_CompleteReverse_RaisesError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate;
            using HotChocolate.Data;
            using HotChocolate.Types;
            using System.Linq;

            namespace TestNamespace;

            [ObjectType<Product>]
            public static partial class ProductResolvers
            {
                [UseSorting]
                [UseFiltering]
                [UseProjection]
                [UsePaging]
                public static IQueryable<Product> GetProducts(ProductService productService)
                    => productService.GetProducts();
            }

            public class Product
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            public class ProductService
            {
                public IQueryable<Product> GetProducts() => null!;
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task SingleAttribute_NoError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate;
            using HotChocolate.Data;
            using HotChocolate.Types;
            using System.Linq;

            namespace TestNamespace;

            [ObjectType<Product>]
            public static partial class ProductResolvers
            {
                [UseFiltering]
                public static IQueryable<Product> GetProducts(ProductService productService)
                    => productService.GetProducts();
            }

            public class Product
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            public class ProductService
            {
                public IQueryable<Product> GetProducts() => null!;
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task NoDataAttributes_NoError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate;
            using HotChocolate.Types;
            using System;

            namespace TestNamespace;

            [ObjectType<Product>]
            public static partial class ProductResolvers
            {
                [Obsolete]
                public static string GetName(Product product) => product.Name;
            }

            public class Product
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }
}
