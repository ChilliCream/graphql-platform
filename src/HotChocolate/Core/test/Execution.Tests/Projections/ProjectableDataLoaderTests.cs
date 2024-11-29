using System.Linq.Expressions;
using GreenDonut;
using GreenDonut.Selectors;
using HotChocolate.Execution.Processing;
using HotChocolate.Execution.TestContext;
using HotChocolate.Types;
using HotChocolate.Types.Relay;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Squadron;

namespace HotChocolate.Execution.Projections;

[Collection(PostgresCacheCollectionFixture.DefinitionName)]
public class ProjectableDataLoaderTests(PostgreSqlResource resource)
{
    public PostgreSqlResource Resource { get; } = resource;

    private string CreateConnectionString()
        => Resource.GetConnectionString($"db_{Guid.NewGuid():N}");

    [Fact]
    public async Task Brand_With_Name()
    {
        // Arrange
        var queries = new List<string>();
        var connectionString = CreateConnectionString();
        await CatalogContext.SeedAsync(connectionString);

        // Act
        var result = await new ServiceCollection()
            .AddScoped(_ => queries)
            .AddTransient(_ => new CatalogContext(connectionString))
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddPagingArguments()
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
            .ExecuteRequestAsync(
                """
                {
                    brandById(id: 1) {
                        name
                    }
                }
                """);

        Snapshot.Create()
            .AddSql(queries)
            .AddResult(result)
            .MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Brand_With_Name_Selector_is_Null()
    {
        // Arrange
        var queries = new List<string>();
        var connectionString = CreateConnectionString();
        await CatalogContext.SeedAsync(connectionString);

        // Act
        var result = await new ServiceCollection()
            .AddScoped(_ => queries)
            .AddTransient(_ => new CatalogContext(connectionString))
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddPagingArguments()
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
            .ExecuteRequestAsync(
                """
                {
                    brandByIdSelectorNull(id: 1) {
                        name
                    }
                }
                """);

        Snapshot.Create()
            .AddSql(queries)
            .AddResult(result)
            .MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Manual_Include_Of_Brand()
    {
        // Arrange
        var queries = new List<string>();
        var connectionString = CreateConnectionString();
        await CatalogContext.SeedAsync(connectionString);

        // Act
        var result = await new ServiceCollection()
            .AddScoped(_ => queries)
            .AddTransient(_ => new CatalogContext(connectionString))
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddPagingArguments()
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
            .ExecuteRequestAsync(
                """
                {
                    productByIdWithBrand(id: 1) {
                        name
                    }
                }
                """);

        Snapshot.Create()
            .AddSql(queries)
            .AddResult(result)
            .MatchMarkdownSnapshot();
    }

    [Fact(Skip = "Include does not work")]
    public async Task Manual_Include_And_Observe_Brand()
    {
        // Arrange
        var queries = new List<string>();
        var connectionString = CreateConnectionString();
        await CatalogContext.SeedAsync(connectionString);

        // Act
        var result = await new ServiceCollection()
            .AddScoped(_ => queries)
            .AddTransient(_ => new CatalogContext(connectionString))
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddTypeExtension<ProductExtensions>()
            .AddPagingArguments()
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
            .ExecuteRequestAsync(
                """
                {
                    productByIdWithBrandNoSelection(id: 1) {
                        name
                        brand {
                            name
                        }
                    }
                }
                """);

        Snapshot.Create()
            .AddSql(queries)
            .AddResult(result)
            .MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Do_Not_Project()
    {
        // Arrange
        var queries = new List<string>();
        var connectionString = CreateConnectionString();
        await CatalogContext.SeedAsync(connectionString);

        // Act
        var result = await new ServiceCollection()
            .AddScoped(_ => queries)
            .AddTransient(_ => new CatalogContext(connectionString))
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddPagingArguments()
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
            .ExecuteRequestAsync(
                """
                {
                    brandByIdNoProjection(id: 1) {
                        name
                    }
                }
                """);

        Snapshot.Create()
            .AddSql(queries)
            .AddResult(result)
            .MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Project_And_Do_Not_Project()
    {
        // Arrange
        var queries = new List<string>();
        var connectionString = CreateConnectionString();
        await CatalogContext.SeedAsync(connectionString);

        // Act
        var result = await new ServiceCollection()
            .AddScoped(_ => queries)
            .AddTransient(_ => new CatalogContext(connectionString))
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddPagingArguments()
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
            .ExecuteRequestAsync(
                """
                {
                    brandByIdNoProjection(id: 1) {
                        name
                    }
                    brandById(id: 1) {
                        name
                    }
                }
                """);

        Snapshot.Create()
            .AddSql(queries)
            .AddResult(result)
            .MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Product_With_Name_And_Brand_With_Name()
    {
        // Arrange
        var queries = new List<string>();
        var connectionString = CreateConnectionString();
        await CatalogContext.SeedAsync(connectionString);

        // Act
        var result = await new ServiceCollection()
            .AddScoped(_ => queries)
            .AddTransient(_ => new CatalogContext(connectionString))
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddPagingArguments()
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
            .ExecuteRequestAsync(
                """
                {
                    productById(id: 1) {
                        name
                        brand {
                            name
                        }
                    }
                }
                """);

        Snapshot.Create()
            .AddSql(queries)
            .AddResult(result)
            .MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Force_A_Branch()
    {
        // Arrange
        var queries = new List<string>();
        var connectionString = CreateConnectionString();
        await CatalogContext.SeedAsync(connectionString);

        // Act
        var result = await new ServiceCollection()
            .AddScoped(_ => queries)
            .AddTransient(_ => new CatalogContext(connectionString))
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddPagingArguments()
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
            .ExecuteRequestAsync(
                """
                {
                    a: productById(id: 1) {
                        name
                        brand {
                            name
                        }
                    }
                    b: productById(id: 1) {
                        id
                        brand {
                            id
                        }
                    }
                }
                """);

        Snapshot.Create()
            .AddSql(queries)
            .AddResult(result)
            .MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Branches_Are_Merged()
    {
        // Arrange
        var queries = new List<string>();
        var connectionString = CreateConnectionString();
        await CatalogContext.SeedAsync(connectionString);

        // Act
        var result = await new ServiceCollection()
            .AddScoped(_ => queries)
            .AddTransient(_ => new CatalogContext(connectionString))
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddPagingArguments()
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
            .ExecuteRequestAsync(
                """
                {
                    a: productById(id: 1) {
                        name
                        brand {
                            name
                        }
                    }
                    b: productById(id: 2) {
                        name
                        brand {
                            name
                        }
                    }
                }
                """);

        Snapshot.Create()
            .AddSql(queries)
            .AddResult(result)
            .MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Brand_Details_Country_Name()
    {
        // Arrange
        var queries = new List<string>();
        var connectionString = CreateConnectionString();
        await CatalogContext.SeedAsync(connectionString);

        // Act
        var result = await new ServiceCollection()
            .AddScoped(_ => queries)
            .AddTransient(_ => new CatalogContext(connectionString))
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddPagingArguments()
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
            .ExecuteRequestAsync(
                """
                {
                    brandById(id: 1) {
                        name
                        details {
                            country {
                                name
                            }
                        }
                    }
                }
                """);

        Snapshot.Create()
            .AddSql(queries)
            .AddResult(result)
            .MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Brand_Details_Country_Name_With_Details_As_Custom_Resolver()
    {
        // Arrange
        var queries = new List<string>();
        var connectionString = CreateConnectionString();
        await CatalogContext.SeedAsync(connectionString);

        // Act
        var result = await new ServiceCollection()
            .AddScoped(_ => queries)
            .AddTransient(_ => new CatalogContext(connectionString))
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddTypeExtension<BrandExtensions>()
            .AddPagingArguments()
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
            .ExecuteRequestAsync(
                """
                {
                    brandById(id: 1) {
                        name
                        details {
                            country {
                                name
                            }
                        }
                    }
                }
                """);

        Snapshot.Create()
            .AddSql(queries)
            .AddResult(result)
            .MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Brand_Details_Requires_Brand_Name()
    {
        // Arrange
        var queries = new List<string>();
        var connectionString = CreateConnectionString();
        await CatalogContext.SeedAsync(connectionString);

        // Act
        var result = await new ServiceCollection()
            .AddScoped(_ => queries)
            .AddTransient(_ => new CatalogContext(connectionString))
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddTypeExtension<BrandExtensionsWithRequirement>()
            .AddPagingArguments()
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
            .ExecuteRequestAsync(
                """
                {
                    brandById(id: 1) {
                        details
                    }
                }
                """);

        Snapshot.Create()
            .AddSql(queries)
            .AddResult(result)
            .MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Brand_Details_Requires_Brand_Name_With_Proper_Type()
    {
        // Arrange
        var queries = new List<string>();
        var connectionString = CreateConnectionString();
        await CatalogContext.SeedAsync(connectionString);

        // Act
        var result = await new ServiceCollection()
            .AddScoped(_ => queries)
            .AddTransient(_ => new CatalogContext(connectionString))
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddType<BrandWithRequirementType>()
            .AddPagingArguments()
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
            .ExecuteRequestAsync(
                """
                {
                    brandById(id: 1) {
                        details
                    }
                }
                """);

        Snapshot.Create()
            .AddSql(queries)
            .AddResult(result)
            .MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Brand_Details_Requires_Brand_Name_With_Proper_Type_With_Explicit_Generic()
    {
        // Arrange
        var queries = new List<string>();
        var connectionString = CreateConnectionString();
        await CatalogContext.SeedAsync(connectionString);

        // Act
        var result = await new ServiceCollection()
            .AddScoped(_ => queries)
            .AddTransient(_ => new CatalogContext(connectionString))
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddType<BrandWithRequirementTypeWithGeneric>()
            .AddPagingArguments()
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
            .ExecuteRequestAsync(
                """
                {
                    brandById(id: 1) {
                        details
                    }
                }
                """);

        Snapshot.Create()
            .AddSql(queries)
            .AddResult(result)
            .MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Brand_Details_Requires_Brand_Name_2()
    {
        // Arrange
        var queries = new List<string>();
        var connectionString = CreateConnectionString();
        await CatalogContext.SeedAsync(connectionString);

        // Act
        var result = await new ServiceCollection()
            .AddScoped(_ => queries)
            .AddTransient(_ => new CatalogContext(connectionString))
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddTypeExtension<BrandExtensionsWithRequirement>()
            .AddPagingArguments()
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
            .ExecuteRequestAsync(
                """
                {
                    brandById(id: 1) {
                        name
                        details
                    }
                }
                """);

        Snapshot.Create()
            .AddSql(queries)
            .AddResult(result)
            .MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Brand_Products_TypeName()
    {
        // Arrange
        var queries = new List<string>();
        var connectionString = CreateConnectionString();
        await CatalogContext.SeedAsync(connectionString);

        // Act
        var result = await new ServiceCollection()
            .AddScoped(_ => queries)
            .AddTransient(_ => new CatalogContext(connectionString))
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddPagingArguments()
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
            .ExecuteRequestAsync(
                """
                {
                    brandById(id: 1) {
                        products {
                            __typename
                        }
                    }
                }
                """);

        // at the moment we do not support projections on lists
        // so products will be empty, and we will just select the brand.Id
        Snapshot.Create()
            .AddSql(queries)
            .AddResult(result)
            .MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Brand_Only_TypeName()
    {
        // Arrange
        var queries = new List<string>();
        var connectionString = CreateConnectionString();
        await CatalogContext.SeedAsync(connectionString);

        // Act
        var result = await new ServiceCollection()
            .AddScoped(_ => queries)
            .AddTransient(_ => new CatalogContext(connectionString))
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddTypeExtension<BrandExtensionsWithRequirement>()
            .AddPagingArguments()
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
            .ExecuteRequestAsync(
                """
                {
                    brandById(id: 1) {
                        __typename
                    }
                }
                """);

        Snapshot.Create()
            .AddSql(queries)
            .AddResult(result)
            .MatchMarkdownSnapshot();
    }


    [Fact]
    public async Task Brand_With_Id_And_Name_Over_Node()
    {
        // Arrange
        var queries = new List<string>();
        var connectionString = CreateConnectionString();
        await CatalogContext.SeedAsync(connectionString);

        // Act
        var result = await new ServiceCollection()
            .AddScoped(_ => queries)
            .AddTransient(_ => new CatalogContext(connectionString))
            .AddGraphQL()
            .AddQueryType<NodeQuery>()
            .AddGlobalObjectIdentification()
            .AddPagingArguments()
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
            .ExecuteRequestAsync(
                """
                {
                    node(id: "QnJhbmQ6MQ==") {
                        id
                        ... on Brand {
                            name
                        }
                    }
                }
                """);

        Snapshot.Create()
            .AddSql(queries)
            .AddResult(result)
            .MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Brand_With_Name_Over_Node()
    {
        // Arrange
        var queries = new List<string>();
        var connectionString = CreateConnectionString();
        await CatalogContext.SeedAsync(connectionString);

        // Act
        var result = await new ServiceCollection()
            .AddScoped(_ => queries)
            .AddTransient(_ => new CatalogContext(connectionString))
            .AddGraphQL()
            .AddQueryType<NodeQuery>()
            .AddGlobalObjectIdentification()
            .AddPagingArguments()
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
            .ExecuteRequestAsync(
                """
                {
                    node(id: "QnJhbmQ6MQ==") {
                        ... on Brand {
                            name
                        }
                    }
                }
                """);

        Snapshot.Create()
            .AddSql(queries)
            .AddResult(result)
            .MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Brand_With_Default_Field_Over_Node()
    {
        // Arrange
        var queries = new List<string>();
        var connectionString = CreateConnectionString();
        await CatalogContext.SeedAsync(connectionString);

        // Act
        var result = await new ServiceCollection()
            .AddScoped(_ => queries)
            .AddTransient(_ => new CatalogContext(connectionString))
            .AddGraphQL()
            .AddQueryType<NodeQuery>()
            .AddGlobalObjectIdentification()
            .AddPagingArguments()
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
            .ExecuteRequestAsync(
                """
                {
                    node(id: "QnJhbmQ6MQ==") {
                        __typename
                    }
                }
                """);

        Snapshot.Create()
            .AddSql(queries)
            .AddResult(result)
            .MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Project_Key_To_Collection_Expression()
    {
        // Arrange
        var queries = new List<string>();
        var connectionString = CreateConnectionString();
        await CatalogContext.SeedAsync(connectionString);

        var services = new ServiceCollection()
            .AddScoped(_ => queries)
            .AddTransient(_ => new CatalogContext(connectionString))
            .AddDataLoader(
                sp => new ProductByBrandIdDataLoader(
                    sp,
                    sp.GetRequiredService<List<string>>(),
                    sp.GetRequiredService<IBatchScheduler>(),
                    sp.GetRequiredService<DataLoaderOptions>()))
            .BuildServiceProvider();

        // Act
        await using var scope = services.CreateAsyncScope();
        var dataLoader = scope.ServiceProvider.GetRequiredService<ProductByBrandIdDataLoader>();
        await dataLoader.LoadAsync(1);

        // Assert
        Snapshot.Create()
            .AddSql(queries)
            .MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Project_Key_To_Collection_Expression_Integration()
    {
        // Arrange
        var queries = new List<string>();
        var connectionString = CreateConnectionString();
        await CatalogContext.SeedAsync(connectionString);

        // Act
        var result = await new ServiceCollection()
            .AddScoped(_ => queries)
            .AddTransient(_ => new CatalogContext(connectionString))
            .AddGraphQLServer()
            .AddQueryType<BrandsQuery>()
            .AddTypeExtension(typeof(BrandListExtensions))
            .ExecuteRequestAsync(
                """
                {
                    brandById(id: 1) {
                        products {
                            name
                        }
                    }
                }
                """);
        // Assert
        Snapshot.Create()
            .AddSql(queries)
            .Add(result, "Result")
            .MatchMarkdownSnapshot();
    }

    public class Query
    {
        public async Task<Brand?> GetBrandByIdAsync(
            int id,
            ISelection selection,
            BrandByIdDataLoader brandById,
            CancellationToken cancellationToken)
            => await brandById.Select(selection).LoadAsync(id, cancellationToken);

        public async Task<Brand?> GetBrandByIdNoProjectionAsync(
            int id,
            ISelection selection,
            BrandByIdDataLoader brandById,
            CancellationToken cancellationToken)
            => await brandById.LoadAsync(id, cancellationToken);

        public async Task<Product?> GetProductByIdAsync(
            int id,
            ISelection selection,
            ProductByIdDataLoader productById,
            CancellationToken cancellationToken)
            => await productById.Select(selection).LoadAsync(id, cancellationToken);

        public async Task<Product?> GetProductByIdWithBrandAsync(
            int id,
            ISelection selection,
            ProductByIdDataLoader productById,
            CancellationToken cancellationToken)
            => await productById.Select(selection).Include(c => c.Brand).LoadAsync(id, cancellationToken);

        public async Task<Product?> GetProductByIdWithBrandNoSelectionAsync(
            int id,
            ISelection selection,
            ProductByIdDataLoader productById,
            CancellationToken cancellationToken)
            => await productById.Include(c => c.Brand).LoadAsync(id, cancellationToken);

        public async Task<Brand?> GetBrandByIdSelectorNullAsync(
            int id,
            BrandByIdDataLoader brandById,
            CancellationToken cancellationToken)
            => await brandById.Select(default(Expression<Func<Brand, Brand>>)).LoadAsync(id, cancellationToken);
    }

    public class NodeQuery
    {
        [NodeResolver]
        public async Task<Brand?> GetBrandByIdAsync(
            int id,
            ISelection selection,
            BrandByIdDataLoader brandById,
            CancellationToken cancellationToken)
            => await brandById.Select(selection).LoadAsync(id, cancellationToken);

        [NodeResolver]
        public async Task<Product?> GetProductByIdAsync(
            int id,
            CancellationToken cancellationToken)
        {
            await Task.Run(() => new InvalidOperationException(), cancellationToken);
            return default!;
        }
    }

    public class BrandsQuery
    {
        public async Task<IEnumerable<Brand>> GetBrandByIdAsync(
            int id,
            ISelection selection,
            CatalogContext context,
            CancellationToken cancellationToken)
            => await context.Brands
                .Select(selection.AsSelector<Brand>())
                .Take(2)
                .ToListAsync(cancellationToken);
    }

    [ExtendObjectType<Brand>]
    public class BrandExtensions
    {
        [BindMember(nameof(Brand.Details))]
        public BrandDetails GetDetails(
            [Parent] Brand brand)
            => new() { Country = new Country { Name = "Germany" } };
    }

    [ExtendObjectType<Brand>]
    public class BrandExtensionsWithRequirement
    {
        [BindMember(nameof(Brand.Details))]
        public string GetDetails(
            [Parent(requires: nameof(Brand.Name))] Brand brand)
            => "Brand Name:" + brand.Name;
    }

    [ExtendObjectType<Brand>]
    public class BrandListExtensions
    {
        [BindMember(nameof(Brand.Products))]
        public async Task<IEnumerable<Product>?> GetProductsAsync(
            [Parent] Brand brand,
            ProductByBrandIdDataLoader2 productByBrandId,
            ISelection selection,
            CancellationToken cancellationToken)
            => await productByBrandId.Select(selection).LoadAsync(brand.Id, cancellationToken);
    }

    public class BrandWithRequirementType : ObjectType<Brand>
    {
        protected override void Configure(IObjectTypeDescriptor<Brand> descriptor)
        {
            descriptor
                .Field(t => t.Details)
                .ParentRequires(nameof(Brand.Name))
                .Resolve(ctx => "Brand Name:" + ctx.Parent<Brand>().Name);
        }
    }

    public class BrandWithRequirementTypeWithGeneric : ObjectType<Brand>
    {
        protected override void Configure(IObjectTypeDescriptor<Brand> descriptor)
        {
            descriptor
                .Field(t => t.Details)
                .ParentRequires<Brand>(nameof(Brand.Name))
                .Resolve(ctx => "Brand Name:" + ctx.Parent<Brand>().Name);
        }
    }

    [ExtendObjectType<Product>]
    public class ProductExtensions
    {
        [BindMember(nameof(Product.Brand))]
        public Task<Brand> GetBrandAsync(
            [Parent] Product product,
            BrandByIdDataLoader brandById)
            => brandById.LoadRequiredAsync(product.BrandId);
    }

    public class BrandByIdDataLoader : StatefulBatchDataLoader<int, Brand>
    {
        private readonly IServiceProvider _services;
        private readonly List<string> _queries;

        public BrandByIdDataLoader(IServiceProvider services,
            List<string> queries,
            IBatchScheduler batchScheduler,
            DataLoaderOptions options) : base(batchScheduler, options)
        {
            _services = services;
            _queries = queries;

            PromiseCacheObserver
                .Create<int, Brand, Product>(
                    p =>
                    {
                        if (p.Brand is not null)
                        {
                            return new KeyValuePair<int, Brand>(p.Brand.Id, p.Brand);
                        }

                        return null;
                    },
                    this)
                .Accept(this);
        }

        protected override async Task<IReadOnlyDictionary<int, Brand>> LoadBatchAsync(
            IReadOnlyList<int> keys,
            DataLoaderFetchContext<Brand> context,
            CancellationToken cancellationToken)
        {
            var catalogContext = _services.GetRequiredService<CatalogContext>();

            var query = catalogContext.Brands
                .Where(t => keys.Contains(t.Id))
                .Select(b => b.Id, context.GetSelector());

            lock (_queries)
            {
                _queries.Add(query.ToQueryString());
            }

            var x = await query.ToDictionaryAsync(t => t.Id, cancellationToken);

            return x;
        }
    }

    public class ProductByIdDataLoader(
        IServiceProvider services,
        List<string> queries,
        IBatchScheduler batchScheduler,
        DataLoaderOptions options)
        : StatefulBatchDataLoader<int, Product>(batchScheduler, options)
    {
        protected override async Task<IReadOnlyDictionary<int, Product>> LoadBatchAsync(
            IReadOnlyList<int> keys,
            DataLoaderFetchContext<Product> context,
            CancellationToken cancellationToken)
        {
            var catalogContext = services.GetRequiredService<CatalogContext>();

            var query = catalogContext.Products
                .Where(t => keys.Contains(t.Id))
                .Select(b => b.Id, context.GetSelector());

            lock (queries)
            {
                queries.Add(query.ToQueryString());
            }

            var x = await query.ToDictionaryAsync(t => t.Id, cancellationToken);

            return x;
        }
    }

    public class ProductByBrandIdDataLoader(
        IServiceProvider services,
        List<string> queries,
        IBatchScheduler batchScheduler,
        DataLoaderOptions options)
        : StatefulBatchDataLoader<int, Product[]>(batchScheduler, options)
    {
        protected override async Task<IReadOnlyDictionary<int, Product[]>> LoadBatchAsync(
            IReadOnlyList<int> keys,
            DataLoaderFetchContext<Product[]> context,
            CancellationToken cancellationToken)
        {
            var catalogContext = services.GetRequiredService<CatalogContext>();
            var selector = new DefaultSelectorBuilder();
            selector.Add<Product>(t => new Product { Name = t.Name });

            var query = catalogContext.Brands
                .Where(t => keys.Contains(t.Id))
                .Select(t => t.Id, t => t.Products, selector);

            lock (queries)
            {
                queries.Add(query.ToQueryString());
            }

            var x = await query.ToDictionaryAsync(t => t.Key, t => t.Value.ToArray(), cancellationToken);

            return x;
        }
    }

    public class ProductByBrandIdDataLoader2(
        IServiceProvider services,
        List<string> queries,
        IBatchScheduler batchScheduler,
        DataLoaderOptions options)
        : StatefulBatchDataLoader<int, Product[]>(batchScheduler, options)
    {
        protected override async Task<IReadOnlyDictionary<int, Product[]>> LoadBatchAsync(
            IReadOnlyList<int> keys,
            DataLoaderFetchContext<Product[]> context,
            CancellationToken cancellationToken)
        {
            var catalogContext = services.GetRequiredService<CatalogContext>();

            var query = catalogContext.Brands
                .Where(t => keys.Contains(t.Id))
                .Select(t => t.Id, t => t.Products, context.GetSelector());

            lock (queries)
            {
                queries.Add(query.ToQueryString());
            }

            var x = await query.ToDictionaryAsync(t => t.Key, t => t.Value.ToArray(), cancellationToken);

            return x;
        }
    }
}

file static class Extensions
{
    public static Snapshot AddSql(this Snapshot snapshot, List<string> queries)
    {
        snapshot.Add(string.Join("\n", queries), "SQL");
        return snapshot;
    }

    public static Snapshot AddResult(this Snapshot snapshot, IExecutionResult result)
    {
        snapshot.Add(result, "Result");
        return snapshot;
    }
}
