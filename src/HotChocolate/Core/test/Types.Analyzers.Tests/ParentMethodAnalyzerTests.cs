namespace HotChocolate.Types;

public class ParentMethodAnalyzerTests
{
    [Fact]
    public async Task ParentMethod_MatchingType_NoError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Resolvers;
            using System.Threading.Tasks;

            namespace TestNamespace;

            public class ProductType : ObjectType<Product>
            {
                protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
                {
                    descriptor.Field("brand").Resolve(async ctx =>
                    {
                        var product = ctx.Parent<Product>();
                        var brandService = ctx.Service<BrandService>();
                        return await brandService.GetBrandByIdAsync(product.BrandId);
                    });
                }
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
    public async Task ParentMethod_TypeMismatch_RaisesError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Resolvers;
            using System.Threading.Tasks;

            namespace TestNamespace;

            public class ProductType : ObjectType<Product>
            {
                protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
                {
                    descriptor.Field("brand").Resolve(async ctx =>
                    {
                        var product = ctx.Parent<Brand>();
                        var brandService = ctx.Service<BrandService>();
                        return await brandService.GetBrandByIdAsync(product.Id);
                    });
                }
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
    public async Task ParentMethod_BaseClassType_NoError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Resolvers;
            using System.Threading.Tasks;

            namespace TestNamespace;

            public class ProductType : ObjectType<Product>
            {
                protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
                {
                    descriptor.Field("name").Resolve(ctx =>
                    {
                        var product = ctx.Parent<BaseProduct>();
                        return product.Name;
                    });
                }
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
    public async Task ParentMethod_InterfaceType_NoError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Resolvers;
            using System.Threading.Tasks;

            namespace TestNamespace;

            public class ProductType : ObjectType<Product>
            {
                protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
                {
                    descriptor.Field("name").Resolve(ctx =>
                    {
                        var product = ctx.Parent<IProduct>();
                        return product.Name;
                    });
                }
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
    public async Task ParentMethod_ObjectType_NoError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Resolvers;
            using System.Threading.Tasks;

            namespace TestNamespace;

            public class ProductType : ObjectType<Product>
            {
                protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
                {
                    descriptor.Field("name").Resolve(ctx =>
                    {
                        var product = ctx.Parent<object>();
                        return product.ToString();
                    });
                }
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
    public async Task ParentMethod_MultipleCallsOneMismatch_RaisesError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Resolvers;
            using System.Threading.Tasks;

            namespace TestNamespace;

            public class ProductType : ObjectType<Product>
            {
                protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
                {
                    descriptor.Field("test1").Resolve(ctx =>
                    {
                        var product = ctx.Parent<Product>();
                        return product.Name;
                    });

                    descriptor.Field("test2").Resolve(ctx =>
                    {
                        var brand = ctx.Parent<Brand>();
                        return brand.Name;
                    });
                }
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
    public async Task ParentMethod_ExpressionBody_TypeMismatch_RaisesError()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            ["""
            using HotChocolate;
            using HotChocolate.Types;
            using HotChocolate.Resolvers;
            using System.Threading.Tasks;

            namespace TestNamespace;

            public class ProductType : ObjectType<Product>
            {
                protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
                {
                    descriptor.Field("name").Resolve(ctx => ctx.Parent<Brand>().Name);
                }
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
