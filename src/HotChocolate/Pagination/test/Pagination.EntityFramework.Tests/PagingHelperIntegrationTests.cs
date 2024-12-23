using System.Collections.Immutable;
using HotChocolate.Data.TestContext;
using GreenDonut;
using GreenDonut.Selectors;
using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Types;
using HotChocolate.Types.Pagination;
using HotChocolate.Pagination;
using HotChocolate.Resolvers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Squadron;

namespace HotChocolate.Data;

[Collection(PostgresCacheCollectionFixture.DefinitionName)]
public class IntegrationPagingHelperTests(PostgreSqlResource resource)
{
    public PostgreSqlResource Resource { get; } = resource;

    private string CreateConnectionString()
        => Resource.GetConnectionString($"db_{Guid.NewGuid():N}");

    [Fact]
    public async Task GetDefaultPage()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);
        var queries = new List<QueryInfo>();
        using var capture = new CapturePagingQueryInterceptor(queries);

        // Act
        var result = await new ServiceCollection()
            .AddScoped(_ => new CatalogContext(connectionString))
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddPagingArguments()
            .ExecuteRequestAsync(
                OperationRequestBuilder.New()
                    .SetDocument(
                        """
                        {
                            brands {
                                nodes {
                                    id
                                    name
                                }
                                pageInfo {
                                    hasNextPage
                                    hasPreviousPage
                                    startCursor
                                    endCursor
                                }
                            }
                        }
                        """)
                    .Build());

        // Assert
        await Snapshot.Create()
            .AddQueries(queries)
            .Add(result, "Result")
            .MatchMarkdownAsync();
    }

    [Fact]
    public async Task GetDefaultPage2()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);
        var queries = new List<QueryInfo>();
        using var capture = new CapturePagingQueryInterceptor(queries);

        // Act
        var result = await new ServiceCollection()
            .AddScoped(_ => new CatalogContext(connectionString))
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddPagingArguments()
            .ExecuteRequestAsync(
                OperationRequestBuilder.New()
                    .SetDocument(
                        """
                        {
                            brands2 {
                                nodes {
                                    id
                                    name
                                }
                                pageInfo {
                                    hasNextPage
                                    hasPreviousPage
                                    startCursor
                                    endCursor
                                }
                            }
                        }
                        """)
                    .Build());

        // Assert
        await Snapshot.Create()
            .AddQueries(queries)
            .Add(result, "Result")
            .MatchMarkdownAsync();
    }

    [Fact]
    public async Task GetSecondPage_With_2_Items()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);
        var queries = new List<QueryInfo>();
        using var capture = new CapturePagingQueryInterceptor(queries);

        // Act
        var result = await new ServiceCollection()
            .AddScoped(_ => new CatalogContext(connectionString))
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddPagingArguments()
            .ExecuteRequestAsync(
                OperationRequestBuilder.New()
                    .SetDocument(
                        """
                        {
                            brands(first: 2, after: "QnJhbmQxNzoxOA==") {
                                nodes {
                                    id
                                    name
                                }
                                pageInfo {
                                    hasNextPage
                                    hasPreviousPage
                                    startCursor
                                    endCursor
                                }
                            }
                        }
                        """)
                    .Build());

        // Assert
        await Snapshot.Create()
            .AddQueries(queries)
            .Add(result, "Result")
            .MatchMarkdownAsync();
    }

    [Fact]
    public async Task GetDefaultPage_With_Nullable()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);
        var queries = new List<QueryInfo>();
        using var capture = new CapturePagingQueryInterceptor(queries);

        // Act
        var result = await new ServiceCollection()
            .AddScoped(_ => new CatalogContext(connectionString))
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddPagingArguments()
            .ExecuteRequestAsync(
                OperationRequestBuilder.New()
                    .SetDocument(
                        """
                        {
                            brandsNullable {
                                edges {
                                    cursor
                                }
                                nodes {
                                    id
                                    name
                                    displayName
                                    brandDetails {
                                        country {
                                            name
                                        }
                                    }
                                }
                                pageInfo {
                                    hasNextPage
                                    hasPreviousPage
                                    startCursor
                                    endCursor
                                }
                            }
                        }
                        """)
                    .Build());

        // Assert
        await Snapshot.Create()
            .AddQueries(queries)
            .Add(result, "Result")
            .MatchMarkdownAsync();
    }

    [Fact]
    public async Task GetDefaultPage_With_Nullable_SecondPage()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);
        var queries = new List<QueryInfo>();
        using var capture = new CapturePagingQueryInterceptor(queries);

        // Act
        var result = await new ServiceCollection()
            .AddScoped(_ => new CatalogContext(connectionString))
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddPagingArguments()
            .ExecuteRequestAsync(
                OperationRequestBuilder.New()
                    .SetDocument(
                        """
                        {
                            brandsNullable(first: 2, after: "QnJhbmQxMDpcbnVsbDoxMQ==") {
                                edges {
                                    cursor
                                }
                                nodes {
                                    id
                                    name
                                    displayName
                                    brandDetails {
                                        country {
                                            name
                                        }
                                    }
                                }
                                pageInfo {
                                    hasNextPage
                                    hasPreviousPage
                                    startCursor
                                    endCursor
                                }
                            }
                        }
                        """)
                    .Build());

        // Assert
        await Snapshot.Create()
            .AddQueries(queries)
            .Add(result, "Result")
            .MatchMarkdownAsync();
    }

    [Fact]
    public async Task GetDefaultPage_With_Nullable_Fallback()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);
        var queries = new List<QueryInfo>();
        using var capture = new CapturePagingQueryInterceptor(queries);

        // Act
        var result = await new ServiceCollection()
            .AddScoped(_ => new CatalogContext(connectionString))
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddPagingArguments()
            .ExecuteRequestAsync(
                OperationRequestBuilder.New()
                    .SetDocument(
                        """
                        {
                            brandsNullableFallback {
                                edges {
                                    cursor
                                }
                                nodes {
                                    id
                                    name
                                    displayName
                                    brandDetails {
                                        country {
                                            name
                                        }
                                    }
                                }
                                pageInfo {
                                    hasNextPage
                                    hasPreviousPage
                                    startCursor
                                    endCursor
                                }
                            }
                        }
                        """)
                    .Build());

        // Assert
        await Snapshot.Create()
            .AddQueries(queries)
            .Add(result, "Result")
            .MatchMarkdownAsync();
    }

    [Fact]
    public async Task GetDefaultPage_With_Nullable_Fallback_SecondPage()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);
        var queries = new List<QueryInfo>();
        using var capture = new CapturePagingQueryInterceptor(queries);

        // Act
        var result = await new ServiceCollection()
            .AddScoped(_ => new CatalogContext(connectionString))
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddPagingArguments()
            .ExecuteRequestAsync(
                OperationRequestBuilder.New()
                    .SetDocument(
                        """
                        {
                            brandsNullableFallback(first: 2, after: "QnJhbmQxMToxMg==") {
                                edges {
                                    cursor
                                }
                                nodes {
                                    id
                                    name
                                    displayName
                                    brandDetails {
                                        country {
                                            name
                                        }
                                    }
                                }
                                pageInfo {
                                    hasNextPage
                                    hasPreviousPage
                                    startCursor
                                    endCursor
                                }
                            }
                        }
                        """)
                    .Build());

        // Assert
        await Snapshot.Create()
            .AddQueries(queries)
            .Add(result, "Result")
            .MatchMarkdownAsync();
    }

    [Fact]
    public async Task GetDefaultPage_With_Deep()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);
        var queries = new List<QueryInfo>();
        using var capture = new CapturePagingQueryInterceptor(queries);

        // Act
        var result = await new ServiceCollection()
            .AddScoped(_ => new CatalogContext(connectionString))
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddPagingArguments()
            .ExecuteRequestAsync(
                OperationRequestBuilder.New()
                    .SetDocument(
                        """
                        {
                            brandsDeep {
                                edges {
                                    cursor
                                }
                                nodes {
                                    id
                                    name
                                    displayName
                                    brandDetails {
                                        country {
                                            name
                                        }
                                    }
                                }
                                pageInfo {
                                    hasNextPage
                                    hasPreviousPage
                                    startCursor
                                    endCursor
                                }
                            }
                        }
                        """)
                    .Build());

        // Assert
        await Snapshot.Create()
            .AddQueries(queries)
            .Add(result, "Result")
            .MatchMarkdownAsync();
    }

    [Fact]
    public async Task GetDefaultPage_With_Deep_SecondPage()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);
        var queries = new List<QueryInfo>();
        using var capture = new CapturePagingQueryInterceptor(queries);

        // Act
        var result = await new ServiceCollection()
            .AddScoped(_ => new CatalogContext(connectionString))
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddPagingArguments()
            .ExecuteRequestAsync(
                OperationRequestBuilder.New()
                    .SetDocument(
                        """
                        {
                            brandsDeep(first: 2, after: "Q291bnRyeTE6Mg==") {
                                edges {
                                    cursor
                                }
                                nodes {
                                    id
                                    name
                                    displayName
                                    brandDetails {
                                        country {
                                            name
                                        }
                                    }
                                }
                                pageInfo {
                                    hasNextPage
                                    hasPreviousPage
                                    startCursor
                                    endCursor
                                }
                            }
                        }
                        """)
                    .Build());

        // Assert
        await Snapshot.Create()
            .AddQueries(queries)
            .Add(result, "Result")
            .MatchMarkdownAsync();
    }

    [Fact]
    public async Task Nested_Paging_First_2()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);
        var queries = new List<QueryInfo>();
        using var capture = new CapturePagingQueryInterceptor(queries);

        // Act
        var result = await new ServiceCollection()
            .AddScoped(_ => new CatalogContext(connectionString))
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddTypeExtension(typeof(BrandExtensions))
            .AddPagingArguments()
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
            .ExecuteRequestAsync(
                OperationRequestBuilder.New()
                    .SetDocument(
                        """
                        {
                            brands(first: 2) {
                                edges {
                                    cursor
                                }
                                nodes {
                                    products(first: 2) {
                                        nodes {
                                            name
                                        }
                                        pageInfo {
                                            hasNextPage
                                            hasPreviousPage
                                            startCursor
                                            endCursor
                                        }
                                    }
                                }
                            }
                        }
                        """)
                    .Build());

        // Assert
        var operationResult = result.ExpectOperationResult();

#if NET8_0
        await Snapshot.Create()
#else
        await Snapshot.Create("NET9_0")
#endif
            .AddQueries(queries)
            .Add(operationResult.WithExtensions(ImmutableDictionary<string, object?>.Empty))
            .MatchMarkdownAsync();
    }

    [Fact]
    public async Task Nested_Paging_First_2_With_Projections()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        var queries = new List<QueryInfo>();
        using var capture = new CapturePagingQueryInterceptor(queries);

        // Act
        var result = await new ServiceCollection()
            .AddScoped(_ => new CatalogContext(connectionString))
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddTypeExtension(typeof(BrandExtensionsWithSelect))
            .AddPagingArguments()
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
            .ExecuteRequestAsync(
                OperationRequestBuilder.New()
                    .SetDocument(
                        """
                        {
                            brands(first: 2) {
                                edges {
                                    cursor
                                }
                                nodes {
                                    products(first: 2) {
                                        nodes {
                                            name
                                        }
                                        pageInfo {
                                            hasNextPage
                                            hasPreviousPage
                                            startCursor
                                            endCursor
                                        }
                                    }
                                }
                            }
                        }
                        """)
                    .Build());

        // Assert
        var operationResult = result.ExpectOperationResult();

#if NET8_0
        await Snapshot.Create()
#else
        await Snapshot.Create("NET9_0")
#endif
            .AddQueries(queries)
            .Add(operationResult, "Result")
            .MatchMarkdownAsync();
    }

    [Fact]
    public async Task Paging_Empty_PagingArgs()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);
        var queries = new List<QueryInfo>();
        using var capture = new CapturePagingQueryInterceptor(queries);

        // Act
        await using var context = new CatalogContext(connectionString);

        var pagingArgs = new PagingArguments();
        var result = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(pagingArgs);

        // Assert
        await Snapshot.Create()
            .AddQueries(queries)
            .Add(
                new
                {
                    result.HasNextPage,
                    result.HasPreviousPage,
                    First = result.First?.Id,
                    FirstCursor = result.First is not null ? result.CreateCursor(result.First) : null,
                    Last = result.Last?.Id,
                    LastCursor = result.Last is not null ? result.CreateCursor(result.Last) : null
                })
            .Add(result.Items)
            .MatchMarkdownAsync();
    }

    [Fact]
    public async Task Paging_First_5()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);
        var queries = new List<QueryInfo>();
        using var capture = new CapturePagingQueryInterceptor(queries);

        // Act
        await using var context = new CatalogContext(connectionString);

        var pagingArgs = new PagingArguments { First = 5 };
        var result = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(pagingArgs);

        // Assert
        await Snapshot.Create()
            .AddQueries(queries)
            .Add(
                new
                {
                    result.HasNextPage,
                    result.HasPreviousPage,
                    First = result.First?.Id,
                    FirstCursor = result.First is not null ? result.CreateCursor(result.First) : null,
                    Last = result.Last?.Id,
                    LastCursor = result.Last is not null ? result.CreateCursor(result.Last) : null
                })
            .Add(result.Items)
            .MatchMarkdownAsync();
    }

    [Fact]
    public async Task Paging_First_5_After_Id_13()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);
        var queries = new List<QueryInfo>();
        using var capture = new CapturePagingQueryInterceptor(queries);

        // Act
        await using var context = new CatalogContext(connectionString);

        var pagingArgs = new PagingArguments
        {
            First = 5,
            After = "QnJhbmQxMjoxMw=="
        };
        var result = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(pagingArgs);

        // Assert
        await Snapshot.Create()
            .AddQueries(queries)
            .Add(
                new
                {
                    result.HasNextPage,
                    result.HasPreviousPage,
                    First = result.First?.Id,
                    FirstCursor = result.First is not null ? result.CreateCursor(result.First) : null,
                    Last = result.Last?.Id,
                    LastCursor = result.Last is not null ? result.CreateCursor(result.Last) : null
                })
            .Add(result.Items)
            .MatchMarkdownAsync();
    }

    [Fact]
    public async Task Paging_Last_5()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);
        var queries = new List<QueryInfo>();
        using var capture = new CapturePagingQueryInterceptor(queries);

        // Act
        await using var context = new CatalogContext(connectionString);

        var pagingArgs = new PagingArguments { Last = 5 };
        var result = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(pagingArgs);

        // Assert
        await Snapshot.Create()
            .AddQueries(queries)
            .Add(
                new
                {
                    result.HasNextPage,
                    result.HasPreviousPage,
                    First = result.First?.Id,
                    FirstCursor = result.First is not null ? result.CreateCursor(result.First) : null,
                    Last = result.Last?.Id,
                    LastCursor = result.Last is not null ? result.CreateCursor(result.Last) : null
                })
            .Add(result.Items)
            .MatchMarkdownAsync();
    }

    [Fact]
    public async Task Paging_First_5_Before_Id_96()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);
        var queries = new List<QueryInfo>();
        using var capture = new CapturePagingQueryInterceptor(queries);

        // Act
        await using var context = new CatalogContext(connectionString);

        var pagingArgs = new PagingArguments
        {
            Last = 5,
            Before = "QnJhbmQ5NTo5Ng=="
        };
        var result = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(pagingArgs);

        // Assert
        await Snapshot.Create()
            .AddQueries(queries)
            .Add(
                new
                {
                    result.HasNextPage,
                    result.HasPreviousPage,
                    First = result.First?.Id,
                    FirstCursor = result.First is not null ? result.CreateCursor(result.First) : null,
                    Last = result.Last?.Id,
                    LastCursor = result.Last is not null ? result.CreateCursor(result.Last) : null
                })
            .Add(result.Items)
            .MatchMarkdownAsync();
    }

    [Fact]
    public async Task BatchPaging_First_5()
    {
        // Arrange
#if NET8_0
        var snapshot = Snapshot.Create();
#else
        var snapshot = Snapshot.Create("NET9_0");
#endif

        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);
        var queries = new List<QueryInfo>();
        using var capture = new CapturePagingQueryInterceptor(queries);

        // Act
        await using var context = new CatalogContext(connectionString);

        var pagingArgs = new PagingArguments { First = 2 };

        var results = await context.Products
            .Where(t => t.BrandId == 1 || t.BrandId == 2 || t.BrandId == 3)
            .OrderBy(p => p.Name)
            .ThenBy(p => p.Id)
            .ToBatchPageAsync(k => k.BrandId, pagingArgs);

        // Assert
        foreach (var page in results)
        {
            snapshot.Add(
                new
                {
                    First = page.Value.CreateCursor(page.Value.First!),
                    Last = page.Value.CreateCursor(page.Value.Last!),
                    page.Value.Items
                },
                name: page.Key.ToString());
        }

        snapshot.AddQueries(queries);
        snapshot.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task BatchPaging_Last_5()
    {
        // Arrange
#if NET8_0
        var snapshot = Snapshot.Create();
#else
        var snapshot = Snapshot.Create("NET9_0");
#endif

        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);
        var queries = new List<QueryInfo>();
        using var capture = new CapturePagingQueryInterceptor(queries);

        // Act
        await using var context = new CatalogContext(connectionString);

        var pagingArgs = new PagingArguments { Last = 2 };

        var results = await context.Products
            .Where(t => t.BrandId == 1 || t.BrandId == 2 || t.BrandId == 3)
            .OrderBy(p => p.Id)
            .ToBatchPageAsync(k => k.BrandId, pagingArgs);

        // Assert
        foreach (var page in results)
        {
            snapshot.Add(
                new
                {
                    First = page.Value.CreateCursor(page.Value.First!),
                    Last = page.Value.CreateCursor(page.Value.Last!),
                    page.Value.Items
                },
                name: page.Key.ToString());
        }

        snapshot.AddQueries(queries);
        snapshot.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Map_Page_To_Connection_With_Dto()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);
        var queries = new List<QueryInfo>();
        using var capture = new CapturePagingQueryInterceptor(queries);

        // Act
        var result = await new ServiceCollection()
            .AddScoped(_ => new CatalogContext(connectionString))
            .AddGraphQL()
            .AddQueryType<QueryConnection>()
            .AddTypeExtension(typeof(BrandConnectionEdgeExtensions))
            .AddPagingArguments()
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
            .ExecuteRequestAsync(
                OperationRequestBuilder.New()
                    .SetDocument(
                        """
                        {
                            brands(first: 2) {
                                edges {
                                    cursor
                                    displayName
                                    node {
                                        id
                                        name
                                    }
                                }
                            }
                        }
                        """)
                    .Build());

        // Assert
        var operationResult = result.ExpectOperationResult();

        await Snapshot.Create()
            .AddQueries(queries)
            .Add(operationResult)
            .MatchMarkdownAsync();
    }

    [Fact]
    public async Task Map_Page_To_Connection_With_Dto_2()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);
        var queries = new List<QueryInfo>();
        using var capture = new CapturePagingQueryInterceptor(queries);

        // Act
        var result = await new ServiceCollection()
            .AddScoped(_ => new CatalogContext(connectionString))
            .AddGraphQL()
            .AddQueryType<QueryConnection2>()
            .AddTypeExtension(typeof(BrandConnectionEdgeExtensions2))
            .AddPagingArguments()
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
            .ExecuteRequestAsync(
                OperationRequestBuilder.New()
                    .SetDocument(
                        """
                        {
                            brands(first: 2) {
                                edges {
                                    cursor
                                    displayName
                                    node {
                                        id
                                        name
                                    }
                                }
                            }
                        }
                        """)
                    .Build());

        // Assert
        var operationResult = result.ExpectOperationResult();

        await Snapshot.Create()
            .AddQueries(queries)
            .Add(operationResult)
            .MatchMarkdownAsync();
    }

    [Fact]
    public async Task Ensure_Nullable_Connections_Dont_Throw()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedFooAsync(connectionString);
        var queries = new List<QueryInfo>();
        using var capture = new CapturePagingQueryInterceptor(queries);

        // Act
        var result = await new ServiceCollection()
            .AddScoped(_ => new FooBarContext(connectionString))
            .AddGraphQL()
            .AddQueryType<QueryNullable>()
            .AddPagingArguments()
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
            .ExecuteRequestAsync(
                OperationRequestBuilder.New()
                    .SetDocument(
                        """
                        {
                            foos(first: 10) {
                                edges {
                                    cursor
                                }
                                nodes {
                                    id
                                    name
                                    bar {
                                        id
                                        description
                                    }
                                }
                                pageInfo {
                                    hasNextPage
                                    hasPreviousPage
                                    startCursor
                                    endCursor
                                }
                            }
                        }
                        """)
                    .Build());

        // Assert
        var operationResult = result.ExpectOperationResult();

#if NET8_0
        await Snapshot.Create()
#else
        await Snapshot.Create("NET9_0")
#endif
            .AddQueries(queries)
            .Add(operationResult, "Result")
            .MatchMarkdownAsync();
    }

    [Fact]
    public async Task Ensure_Nullable_Connections_Dont_Throw_2()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedFooAsync(connectionString);
        var queries = new List<QueryInfo>();
        using var capture = new CapturePagingQueryInterceptor(queries);

        // Act
        var result = await new ServiceCollection()
            .AddScoped(_ => new FooBarContext(connectionString))
            .AddGraphQL()
            .AddQueryType<QueryNullable>()
            .AddPagingArguments()
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
            .ExecuteRequestAsync(
                OperationRequestBuilder.New()
                    .SetDocument(
                        """
                        {
                            foos(first: 10) {
                                edges {
                                    cursor
                                }
                                nodes {
                                    id
                                    name
                                    bar {
                                        id
                                        description
                                        someField1
                                        someField2
                                    }
                                }
                                pageInfo {
                                    hasNextPage
                                    hasPreviousPage
                                    startCursor
                                    endCursor
                                }
                            }
                        }
                        """)
                    .Build());

        // Assert
        var operationResult = result.ExpectOperationResult();

#if NET8_0
        await Snapshot.Create()
#else
        await Snapshot.Create("NET9_0")
#endif
            .AddQueries(queries)
            .Add(operationResult, "Result")
            .MatchMarkdownAsync();
    }

    private static async Task SeedAsync(string connectionString)
    {
        await using var context = new CatalogContext(connectionString);
        await context.Database.EnsureCreatedAsync();

        var type = new ProductType
        {
            Name = "T-Shirt",
        };
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
                var product = new Product
                {
                    Name = $"Product {i}-{j}",
                    Type = type,
                    Brand = brand,
                };
                context.Products.Add(product);
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedFooAsync(string connectionString)
    {
        await using var context = new FooBarContext(connectionString);
        await context.Database.EnsureCreatedAsync();

        context.Bars.Add(
            new Bar
            {
                Id = 1,
                Description = "Bar 1",
                SomeField1 = "abc",
                SomeField2 = null
            });

        context.Bars.Add(
            new Bar
            {
                Id = 2,
                Description = "Bar 2",
                SomeField1 = "def",
                SomeField2 = "ghi"
            });

        context.Foos.Add(
            new Foo
            {
                Id = 1,
                Name = "Foo 1",
                BarId = null
            });

        context.Foos.Add(
            new Foo
            {
                Id = 2,
                Name = "Foo 2",
                BarId = 1
            });

        await context.SaveChangesAsync();
    }

    public class Query
    {
        [UsePaging]
        public async Task<Connection<Brand>> GetBrandsAsync(
            CatalogContext context,
            PagingArguments arguments,
            CancellationToken ct)
        {
            return await context.Brands
                .OrderBy(t => t.Name)
                .ThenBy(t => t.Id)
                .ToPageAsync(arguments, cancellationToken: ct)
                .ToConnectionAsync();
        }

        [UsePaging]
        public async Task<Connection<Brand>> GetBrands2Async(
            CatalogContext context,
            PagingArguments arguments,
            CancellationToken ct)
        {
            var page = await context.Brands
                .OrderBy(t => t.Name)
                .ThenBy(t => t.Id)
                .ToPageAsync(arguments, cancellationToken: ct);

            return page.ToConnection();
        }

        [UsePaging]
        public async Task<Connection<Brand>> GetBrandsNullable(
            CatalogContext context,
            PagingArguments arguments,
            CancellationToken ct)
        {
            return await context.Brands
                .OrderBy(t => t.Name)
                .ThenBy(x => x.AlwaysNull)
                .ThenBy(t => t.Id)
                .ToPageAsync(arguments, cancellationToken: ct)
                .ToConnectionAsync();
        }

        [UsePaging]
        public async Task<Connection<Brand>> GetBrandsNullableFallback(
            CatalogContext context,
            PagingArguments arguments,
            CancellationToken ct)
        {
            return await context.Brands
                .OrderBy(t => t.DisplayName ?? t.Name)
                .ThenBy(t => t.Id)
                .ToPageAsync(arguments, cancellationToken: ct)
                .ToConnectionAsync();
        }

        [UsePaging]
        public async Task<Connection<Brand>> GetBrandsDeep(
            CatalogContext context,
            PagingArguments arguments,
            CancellationToken ct)
        {
            return await context.Brands
                .OrderBy(x => x.BrandDetails.Country.Name)
                .ThenBy(t => t.Id)
                .ToPageAsync(arguments, cancellationToken: ct)
                .ToConnectionAsync();
        }
    }

    public class QueryConnection
    {
        [UsePaging(ConnectionName = "BrandConnection")]
        public async Task<Connection<BrandDto>> GetBrandsAsync(
            CatalogContext context,
            PagingArguments arguments,
            CancellationToken ct)
        {
            return await context.Brands
                .OrderBy(t => t.Name)
                .ThenBy(t => t.Id)
                .ToPageAsync(arguments, cancellationToken: ct)
                .ToConnectionAsync((brand, page) => new BrandEdge(brand, edge => page.CreateCursor(edge.Brand)));
        }
    }

    public class QueryConnection2
    {
        [UsePaging(ConnectionName = "BrandConnection")]
        public async Task<Connection<BrandDto>> GetBrandsAsync(
            CatalogContext context,
            PagingArguments arguments,
            CancellationToken ct)
        {
            return await context.Brands
                .OrderBy(t => t.Name)
                .ThenBy(t => t.Id)
                .ToPageAsync(arguments, cancellationToken: ct)
                .ToConnectionAsync((brand, cursor) => new BrandEdge2(brand, cursor));
        }
    }

    public class QueryNullable
    {
        [UsePaging]
        public async Task<Connection<Foo>> GetFoosAsync(
            FooBarContext context,
            PagingArguments arguments,
            ISelection selection,
            IResolverContext rc,
            CancellationToken ct)
        {
            return await context.Foos
                .OrderBy(t => t.Name)
                .ThenBy(t => t.Id)
                .Select(selection.AsSelector<Foo>())
                .ToPageAsync(arguments, cancellationToken: ct)
                .ToConnectionAsync();
        }
    }

    [ExtendObjectType("BrandConnectionEdge")]
    public class BrandConnectionEdgeExtensions
    {
        public string? GetDisplayName([Parent] BrandEdge edge)
            => edge.Brand.DisplayName;
    }

    [ExtendObjectType("BrandConnectionEdge")]
    public class BrandConnectionEdgeExtensions2
    {
        public string? GetDisplayName([Parent] BrandEdge2 edge)
            => edge.Brand.DisplayName;
    }

    public class BrandEdge : Edge<BrandDto>
    {
        public BrandEdge(Brand brand, Func<BrandEdge, string> cursor)
            : base(new BrandDto(brand.Id, brand.Name), edge => cursor((BrandEdge)edge))
        {
            Brand = brand;
        }

        public Brand Brand { get; }
    }

    public class BrandEdge2 : Edge<BrandDto>
    {
        public BrandEdge2(Brand brand, string cursor)
            : base(new BrandDto(brand.Id, brand.Name), cursor)
        {
            Brand = brand;
        }

        public Brand Brand { get; }
    }


    public class BrandDto
    {
        public BrandDto(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public int Id { get; }

        public string Name { get; }
    }

    [ExtendObjectType<Brand>]
    public static class BrandExtensions
    {
        [UsePaging]
        public static async Task<Connection<Product>> GetProducts(
            [Parent] Brand brand,
            ProductsByBrandDataLoader dataLoader,
            PagingArguments arguments,
            IResolverContext context,
            CancellationToken cancellationToken)
            => await dataLoader
                .WithPagingArguments(arguments)
                .LoadAsync(brand.Id, cancellationToken)
                .ToConnectionAsync();
    }

    [ExtendObjectType<Brand>]
    public static class BrandExtensionsWithSelect
    {
        [UsePaging]
        public static async Task<Connection<Product>> GetProducts(
            [Parent] Brand brand,
            ProductsByBrandDataLoader dataLoader,
            ISelection selection,
            PagingArguments arguments,
            CancellationToken cancellationToken)
            => await dataLoader
                .WithPagingArguments(arguments)
                .Select(selection)
                .LoadAsync(brand.Id, cancellationToken)
                .ToConnectionAsync();
    }

    public class ProductsByBrandDataLoader : StatefulBatchDataLoader<int, Page<Product>>
    {
        private readonly IServiceProvider _services;

        public ProductsByBrandDataLoader(
            IServiceProvider services,
            IBatchScheduler batchScheduler,
            DataLoaderOptions options)
            : base(batchScheduler, options)
        {
            _services = services;
        }

        protected override async Task<IReadOnlyDictionary<int, Page<Product>>> LoadBatchAsync(
            IReadOnlyList<int> keys,
            DataLoaderFetchContext<Page<Product>> context,
            CancellationToken cancellationToken)
        {
            var pagingArgs = context.GetPagingArguments();
            var selector = context.GetSelector();

            await using var scope = _services.CreateAsyncScope();
            await using var catalogContext = scope.ServiceProvider.GetRequiredService<CatalogContext>();

            return await catalogContext.Products
                .Where(t => keys.Contains(t.BrandId))
                .Select(b => b.BrandId, selector)
                .OrderBy(t => t.Name).ThenBy(t => t.Id)
                .ToBatchPageAsync(t => t.BrandId, pagingArgs, cancellationToken);
        }
    }
}

file static class Extensions
{
    public static Snapshot AddQueries(
        this Snapshot snapshot,
        List<QueryInfo> queries)
    {
        for (var i = 0; i < queries.Count; i++)
        {
            snapshot
                .Add(queries[i].QueryText, $"SQL {i}", "sql")
                .Add(queries[i].ExpressionText, $"Expression {i}");
        }

        return snapshot;
    }
}

file sealed class CapturePagingQueryInterceptor(List<QueryInfo> queries) : PagingQueryInterceptor
{
    public override void OnBeforeExecute<T>(IQueryable<T> query)
    {
        queries.Add(
            new QueryInfo
            {
                ExpressionText = query.Expression.ToString(),
                QueryText = query.ToQueryString()
            });
    }
}
