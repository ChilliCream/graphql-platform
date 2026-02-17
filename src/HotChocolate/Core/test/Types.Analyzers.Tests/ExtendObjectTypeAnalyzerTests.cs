namespace HotChocolate.Types;

public class ExtendObjectTypeAnalyzerTests
{
    [Fact]
    public async Task ExtendObjectType_WithGenericType_RaisesInfo()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate.Types;

            namespace TestNamespace;

            [ExtendObjectType<Product>]
            public static partial class ProductExtensions
            {
                public static string GetDisplayName(Product product)
                    => $"{product.Name} (ID: {product.Id})";
            }

            public class Product
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task ObjectType_WithGenericType_NoInfo()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate.Types;

            namespace TestNamespace;

            [ObjectType<Product>]
            public static partial class ProductNode
            {
                public static string GetDisplayName(Product product)
                    => $"{product.Name} (ID: {product.Id})";
            }

            public class Product
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task ExtendObjectType_MultipleClasses_RaisesInfoForAll()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate.Types;

            namespace TestNamespace;

            [ExtendObjectType<Product>]
            public static partial class ProductExtensions
            {
                public static string GetDisplayName(Product product)
                    => $"{product.Name} (ID: {product.Id})";
            }

            [ExtendObjectType<Brand>]
            public static partial class BrandExtensions
            {
                public static string GetDisplayName(Brand brand)
                    => brand.Name;
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
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task MixedAttributes_OnlyRaisesInfoForExtendObjectType()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate.Types;

            namespace TestNamespace;

            [ObjectType<Product>]
            public static partial class ProductNode
            {
                public static string GetProductName(Product product)
                    => product.Name;
            }

            [ExtendObjectType<Brand>]
            public static partial class BrandExtensions
            {
                public static string GetBrandName(Brand brand)
                    => brand.Name;
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
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }
}
