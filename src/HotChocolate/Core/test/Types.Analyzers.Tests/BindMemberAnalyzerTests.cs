namespace HotChocolate.Types;

public class BindMemberAnalyzerTests
{
    [Fact]
    public async Task BindMember_WithNameof_ValidMember_NoError()
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
                public static Task<Brand?> GetBrandAsync(Product product)
                    => Task.FromResult<Brand?>(null);
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
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task BindMember_WithString_ValidMember_NoError()
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
                [BindMember("BrandId")]
                public static Task<Brand?> GetBrandAsync(Product product)
                    => Task.FromResult<Brand?>(null);
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
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task BindMember_WithNameof_WrongType_RaisesError()
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
                [BindMember(nameof(ProductGroup.CategoryId))]
                public static Task<Brand?> GetBrandAsync(Product product)
                    => Task.FromResult<Brand?>(null);
            }

            public class Product
            {
                public int Id { get; set; }
                public int BrandId { get; set; }
                public string Name { get; set; }
            }

            public class ProductGroup
            {
                public int Id { get; set; }
                public int CategoryId { get; set; }
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
    public async Task BindMember_WithString_InvalidMember_RaisesError()
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
                [BindMember("DoesNotExist")]
                public static Task<Brand?> GetBrandAsync(Product product)
                    => Task.FromResult<Brand?>(null);
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
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task BindMember_WithNameof_InvalidMember_RaisesError()
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
                [BindMember(nameof(Product.DoesNotExist))]
                public static Task<Brand?> GetBrandAsync(Product product)
                    => Task.FromResult<Brand?>(null);
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
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task BindMember_WithNameofSimple_ValidMember_NoError()
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
                [BindMember(nameof(BrandId))]
                public static Task<Brand?> GetBrandAsync(Product product)
                    => Task.FromResult<Brand?>(null);

                public static int BrandId => 1;
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
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task BindMember_NonGenericObjectType_NoError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate;
            using HotChocolate.Types;
            using System.Threading.Tasks;

            namespace TestNamespace;

            [ObjectType]
            public static partial class ProductNode
            {
                [BindMember("BrandId")]
                public static Task<Brand?> GetBrandAsync(Product product)
                    => Task.FromResult<Brand?>(null);
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
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }

    [Fact]
    public async Task BindMember_WithNameof_InheritedMember_NoError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate;
            using HotChocolate.Types;
            using System.Threading.Tasks;

            namespace TestNamespace;

            [ObjectType<DerivedProduct>]
            public static partial class DerivedProductNode
            {
                [BindMember(nameof(DerivedProduct.BrandId))]
                public static Task<Brand?> GetBrandAsync(DerivedProduct product)
                    => Task.FromResult<Brand?>(null);
            }

            public class ProductBase
            {
                public int Id { get; set; }
                public int BrandId { get; set; }
            }

            public class DerivedProduct : ProductBase
            {
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
    public async Task BindMember_WithString_InheritedMember_NoError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate;
            using HotChocolate.Types;
            using System.Threading.Tasks;

            namespace TestNamespace;

            [ObjectType<DerivedProduct>]
            public static partial class DerivedProductNode
            {
                [BindMember("BrandId")]
                public static Task<Brand?> GetBrandAsync(DerivedProduct product)
                    => Task.FromResult<Brand?>(null);
            }

            public class ProductBase
            {
                public int Id { get; set; }
                public int BrandId { get; set; }
            }

            public class DerivedProduct : ProductBase
            {
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
    public async Task BindMember_MultipleErrors_RaisesMultipleErrors()
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
                [BindMember("DoesNotExist")]
                public static Task<Brand?> GetBrandAsync(Product product)
                    => Task.FromResult<Brand?>(null);

                [BindMember(nameof(ProductGroup.CategoryId))]
                public static Task<Category?> GetCategoryAsync(Product product)
                    => Task.FromResult<Category?>(null);
            }

            public class Product
            {
                public int Id { get; set; }
                public int BrandId { get; set; }
                public string Name { get; set; }
            }

            public class ProductGroup
            {
                public int Id { get; set; }
                public int CategoryId { get; set; }
            }

            public class Brand
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            public class Category
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }
            """],
            enableAnalyzers: true).MatchMarkdownAsync();
    }
}
