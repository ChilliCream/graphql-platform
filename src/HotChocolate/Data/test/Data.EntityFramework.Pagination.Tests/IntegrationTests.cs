using System.Reflection;
using System.Runtime.CompilerServices;
using HotChocolate.Data.Sorting;
using HotChocolate.Data.TestContext;
using HotChocolate.Execution;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Pagination;
using HotChocolate.Types.Pagination.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Squadron;

namespace HotChocolate.Data;

[Collection(PostgresCacheCollectionFixture.DefinitionName)]
public class IntegrationTests(PostgreSqlResource resource)
{
    private string CreateConnectionString()
        => resource.GetConnectionString($"db_{Guid.NewGuid():N}");

    [Fact]
    public async Task Paging_With_Default_Sorting()
    {
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        var executor = await new ServiceCollection()
            .AddScoped(_ => new CatalogContext(connectionString))
            .AddGraphQLServer()
            .AddQueryType<Query>()
            .AddDbContextCursorPagingProvider()
            .AddSorting()
            .BuildRequestExecutorAsync();

        var result = await executor.ExecuteAsync(q => q
            .SetDocument(
                """
                {
                    brands(first: 10) {
                        nodes {
                            name
                        }
                        pageInfo {
                            endCursor
                        }
                    }
                }
                """)
            .SetGlobalState("printSQL", true));

        result.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Paging_Next_2_With_Default_Sorting()
    {
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        var executor = await new ServiceCollection()
            .AddScoped(_ => new CatalogContext(connectionString))
            .AddGraphQLServer()
            .AddQueryType<Query>()
            .AddSorting()
            .AddDbContextCursorPagingProvider()
            .BuildRequestExecutorAsync();

        var result = await executor.ExecuteAsync(q => q
            .SetDocument(
                """
                {
                    brands(first: 2, after: "MTA=") {
                        nodes {
                            name
                        }
                        pageInfo {
                            endCursor
                        }
                    }
                }
                """)
            .SetGlobalState("printSQL", true));

        result.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Paging_With_Default_Sorting_And_TotalCount()
    {
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        var executor = await new ServiceCollection()
            .AddScoped(_ => new CatalogContext(connectionString))
            .AddGraphQLServer()
            .AddQueryType<Query>()
            .AddDbContextCursorPagingProvider()
            .AddSorting()
            .BuildRequestExecutorAsync();

        var result = await executor.ExecuteAsync(q => q
            .SetDocument(
                """
                {
                    products(first: 10) {
                        nodes {
                            name
                        }
                        pageInfo {
                            endCursor
                        }
                        totalCount
                    }
                }
                """)
            .SetGlobalState("printSQL", true));

        result.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Paging_With_Default_Only_TotalCount()
    {
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        var executor = await new ServiceCollection()
            .AddScoped(_ => new CatalogContext(connectionString))
            .AddGraphQLServer()
            .AddQueryType<Query>()
            .AddDbContextCursorPagingProvider()
            .AddSorting()
            .BuildRequestExecutorAsync();

        var result = await executor.ExecuteAsync(q => q
            .SetDocument(
                """
                {
                    products(first: 10) {
                        totalCount
                    }
                }
                """)
            .SetGlobalState("printSQL", true));

        result.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Paging_With_PagingFlags_Override()
    {
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        var executor = await new ServiceCollection()
            .AddScoped(_ => new CatalogContext(connectionString))
            .AddGraphQLServer()
            .AddQueryType<Query>()
            .AddTypeExtension(typeof(ProductConnectionExtensions))
            .AddDbContextCursorPagingProvider()
            .AddSorting()
            .BuildRequestExecutorAsync();

        var result = await executor.ExecuteAsync(q => q
            .SetDocument(
                """
                {
                    products(first: 10) {
                        pageCount
                    }
                }
                """)
            .SetGlobalState("printSQL", true));

        result.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Paging_With_User_Sorting()
    {
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        var executor = await new ServiceCollection()
            .AddScoped(_ => new CatalogContext(connectionString))
            .AddGraphQLServer()
            .AddQueryType<Query>()
            .AddDbContextCursorPagingProvider()
            .AddSorting()
            .BuildRequestExecutorAsync();

        var result = await executor.ExecuteAsync(q => q
            .SetDocument(
                """
                {
                    brands(first: 10, order: { name: ASC }) {
                        nodes {
                            name
                        }
                        pageInfo {
                            endCursor
                        }
                    }
                }
                """)
            .SetGlobalState("printSQL", true));

        result.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Paging_Next_2_With_User_Sorting()
    {
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        var executor = await new ServiceCollection()
            .AddScoped(_ => new CatalogContext(connectionString))
            .AddGraphQLServer()
            .AddQueryType<Query>()
            .AddDbContextCursorPagingProvider()
            .AddSorting()
            .BuildRequestExecutorAsync();

        var result = await executor.ExecuteAsync(q => q
            .SetDocument(
                """
                {
                    brands(first: 2, after: "QnJhbmQxNzoxOA==", order: { name: ASC }) {
                        nodes {
                            name
                        }
                        pageInfo {
                            endCursor
                        }
                    }
                }
                """)
            .SetGlobalState("printSQL", true));

        result.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Paging_First_10_With_Default_Sorting_HasNextPage()
    {
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        var executor = await new ServiceCollection()
            .AddScoped(_ => new CatalogContext(connectionString))
            .AddGraphQLServer()
            .AddQueryType<Query>()
            .AddSorting()
            .AddDbContextCursorPagingProvider()
            .BuildRequestExecutorAsync();

        var result = await executor.ExecuteAsync(q => q
            .SetDocument(
                """
                {
                    brands(first: 10) {
                        nodes {
                            name
                        }
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                            endCursor
                        }
                    }
                }
                """)
            .SetGlobalState("printSQL", true));

        result.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Paging_Last_10_With_Default_Sorting_HasPreviousPage()
    {
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        var executor = await new ServiceCollection()
            .AddScoped(_ => new CatalogContext(connectionString))
            .AddGraphQLServer()
            .AddQueryType<Query>()
            .AddSorting()
            .AddDbContextCursorPagingProvider()
            .BuildRequestExecutorAsync();

        var result = await executor.ExecuteAsync(q => q
            .SetDocument(
                """
                {
                    brands(last: 10) {
                        nodes {
                            name
                        }
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                            endCursor
                        }
                    }
                }
                """)
            .SetGlobalState("printSQL", true));

        result.MatchMarkdownSnapshot();
    }

    public class Query
    {
        [UsePaging]
        [UseSorting]
        public IQueryable<Brand> GetBrands(CatalogContext context, ISortingContext sorting)
        {
            sorting.Handled(false);
            sorting.OnAfterSortingApplied<IQueryable<Brand>>(
                static (sortingApplied, query) =>
                {
                    if (sortingApplied)
                    {
                        return ((IOrderedQueryable<Brand>)query).ThenBy(b => b.Id);
                    }

                    return query.OrderBy(b => b.Id);
                });

            return context.Brands;
        }

        [PageArgs]
        [UsePaging(IncludeTotalCount = true, ConnectionName = "Product")]
        [UseSorting]
        public IQueryable<Product> GetProducts(CatalogContext context, ISortingContext sorting)
        {
            sorting.Handled(false);
            sorting.OnAfterSortingApplied<IQueryable<Product>>(
                static (sortingApplied, query) =>
                {
                    if (sortingApplied)
                    {
                        return ((IOrderedQueryable<Product>)query).ThenBy(b => b.Id);
                    }

                    return query.OrderBy(b => b.Id);
                });

            return context.Products;
        }
    }

    [ExtendObjectType("ProductConnection")]
    public static class ProductConnectionExtensions
    {
        public static int GetPageCount([Parent] Connection<Product> connection)
            => connection.Edges.Count;
    }

    public sealed class PageArgsAttribute : ObjectFieldDescriptorAttribute
    {
        public PageArgsAttribute([CallerLineNumber] int order = 0)
        {
            Order = order;
        }

        protected override void OnConfigure(
            IDescriptorContext context,
            IObjectFieldDescriptor descriptor,
            MemberInfo member)
        {
            descriptor.Use(next => async ctx =>
            {
                if (ctx.IsSelected("pageCount"))
                {
                    ctx.SetPagingFlags(PagingFlags.Edges);
                }

                await next(ctx);
            });
        }
    }

    private static async Task SeedAsync(string connectionString)
    {
        await using var context = new CatalogContext(connectionString);
        await context.Database.EnsureCreatedAsync();

        var type = new ProductType { Name = "T-Shirt", };
        context.ProductTypes.Add(type);

        for (var i = 0; i < 100; i++)
        {
            var brand = new Brand
            {
                Name = "Brand" + i,
                DisplayName = i % 2 == 0 ? "BrandDisplay" + i : null,
                BrandDetails = new() { Country = new() { Name = "Country" + i } }
            };
            context.Brands.Add(brand);

            for (var j = 0; j < 100; j++)
            {
                var product = new Product { Name = $"Product {i}-{j}", Type = type, Brand = brand, };
                context.Products.Add(product);
            }
        }

        await context.SaveChangesAsync();
    }
}
