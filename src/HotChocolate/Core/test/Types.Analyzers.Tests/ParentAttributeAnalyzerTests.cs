namespace HotChocolate.Types;

public class ParentAttributeAnalyzerTests
{
    [Fact]
    public async Task ParentAttribute_MatchingType_NoError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate;
            using HotChocolate.Types;
            using System.Threading.Tasks;

            namespace TestNamespace;

            [ObjectType<Product>]
            public static partial class ProductNode
            {
                [BindMember(nameof(Product.BrandId))]
                public static async Task<Brand?> GetBrandAsync(
                    [Parent] Product product,
                    BrandService brandService)
                    => await brandService.GetBrandByIdAsync(product.BrandId);
            }

            public class Product
            {
                public int Id { get; set; }
                public int BrandId { get; set; }
                public string Name { get; set; }
            }

            public class Brand
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            public class BrandService
            {
                public Task<Brand?> GetBrandByIdAsync(int id) => Task.FromResult<Brand?>(null);
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task ParentAttribute_TypeMismatch_RaisesError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate;
            using HotChocolate.Types;
            using System.Threading.Tasks;

            namespace TestNamespace;

            [ObjectType<Product>]
            public static partial class ProductNode
            {
                [BindMember(nameof(Product.BrandId))]
                public static async Task<Brand?> GetBrandAsync(
                    [Parent] Brand product,
                    BrandService brandService)
                    => await brandService.GetBrandByIdAsync(product.Id);
            }

            public class Product
            {
                public int Id { get; set; }
                public int BrandId { get; set; }
                public string Name { get; set; }
            }

            public class Brand
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            public class BrandService
            {
                public Task<Brand?> GetBrandByIdAsync(int id) => Task.FromResult<Brand?>(null);
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task ParentAttribute_BaseClass_NoError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate;
            using HotChocolate.Types;
            using System.Threading.Tasks;

            namespace TestNamespace;

            [ObjectType<Product>]
            public static partial class ProductNode
            {
                public static string GetName([Parent] BaseProduct product)
                    => product.Name;
            }

            public class BaseProduct
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            public class Product : BaseProduct
            {
                public int BrandId { get; set; }
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task ParentAttribute_Interface_NoError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate;
            using HotChocolate.Types;
            using System.Threading.Tasks;

            namespace TestNamespace;

            [ObjectType<Product>]
            public static partial class ProductNode
            {
                public static string GetName([Parent] IProduct product)
                    => product.Name;
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
                public int BrandId { get; set; }
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task ParentAttribute_NoParentAttribute_NoError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate;
            using HotChocolate.Types;
            using System.Threading.Tasks;

            namespace TestNamespace;

            [ObjectType<Product>]
            public static partial class ProductNode
            {
                public static string GetName(Brand brand)
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
    public async Task ParentAttribute_MultipleParameters_OnlyParentChecked()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate;
            using HotChocolate.Types;
            using System.Threading.Tasks;

            namespace TestNamespace;

            [ObjectType<Product>]
            public static partial class ProductNode
            {
                [BindMember(nameof(Product.BrandId))]
                public static async Task<Brand?> GetBrandAsync(
                    [Parent] Brand product,
                    Brand otherBrand,
                    BrandService brandService)
                    => await brandService.GetBrandByIdAsync(product.Id);
            }

            public class Product
            {
                public int Id { get; set; }
                public int BrandId { get; set; }
                public string Name { get; set; }
            }

            public class Brand
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            public class BrandService
            {
                public Task<Brand?> GetBrandByIdAsync(int id) => Task.FromResult<Brand?>(null);
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task ParentAttribute_WithRequires_TypeMismatch_RaisesError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate;
            using HotChocolate.Types;
            using System.Threading.Tasks;

            namespace TestNamespace;

            [ObjectType<Product>]
            public static partial class ProductNode
            {
                [BindMember(nameof(Product.BrandId))]
                public static async Task<Brand?> GetBrandAsync(
                    [Parent(requires: nameof(Product.BrandId))] Brand product,
                    BrandService brandService)
                    => await brandService.GetBrandByIdAsync(product.Id);
            }

            public class Product
            {
                public int Id { get; set; }
                public int BrandId { get; set; }
                public string Name { get; set; }
            }

            public class Brand
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            public class BrandService
            {
                public Task<Brand?> GetBrandByIdAsync(int id) => Task.FromResult<Brand?>(null);
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }
}
