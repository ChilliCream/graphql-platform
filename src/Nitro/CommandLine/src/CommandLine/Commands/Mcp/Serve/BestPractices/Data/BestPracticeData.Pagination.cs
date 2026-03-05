using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.BestPractices.Models;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.BestPractices;

internal static partial class BestPracticeData
{
    private static void AddPaginationDocuments(List<BestPracticeDocument> docs)
    {
        docs.Add(
            new BestPracticeDocument
            {
                Id = "pagination-cursor",
                Title = "Cursor-Based Pagination with UsePaging",
                Category = BestPracticeCategory.Pagination,
                Tags = ["hot-chocolate-16", "relay", "performance"],
                Styles = ["all"],
                Keywords = "list array collection items multiple return several IEnumerable connections edges nodes paging page pages results",
                Abstract =
                    "How to implement Relay-compliant cursor pagination using [UsePaging], IQueryable<T>, and the connection type pattern.",
                Body = """
                # Cursor-Based Pagination with UsePaging

                ## When to Use

                Use cursor-based pagination when you need stable, consistent pagination that works well with real-time data. Cursor pagination is the default recommendation for GraphQL APIs because:

                - Cursors are stable even when items are inserted or deleted between pages
                - It aligns with the Relay connection specification, enabling Relay client compatibility
                - It works efficiently with keyset pagination at the database level

                Use cursor pagination for most list fields. Only use offset pagination when clients explicitly need page-number-based navigation (e.g., "go to page 5").

                ## Implementation

                ### Basic Cursor Pagination

                ```csharp
                [QueryType]
                public static class ProductQueries
                {
                    [UsePaging]
                    public static IQueryable<Product> GetProducts(AppDbContext dbContext)
                        => dbContext.Products.OrderBy(p => p.Id);
                }
                ```

                This generates Relay-compliant connection types:

                ```graphql
                type Query {
                  products(first: Int, after: String, last: Int, before: String): ProductsConnection
                }

                type ProductsConnection {
                  edges: [ProductsEdge!]
                  nodes: [Product!]
                  pageInfo: PageInfo!
                  totalCount: Int!
                }

                type ProductsEdge {
                  cursor: String!
                  node: Product!
                }

                type PageInfo {
                  hasNextPage: Boolean!
                  hasPreviousPage: Boolean!
                  startCursor: String
                  endCursor: String
                }
                ```

                ### With Filtering and Sorting

                ```csharp
                [QueryType]
                public static class ProductQueries
                {
                    [UsePaging]
                    [UseFiltering]
                    [UseSorting]
                    public static IQueryable<Product> GetProducts(AppDbContext dbContext)
                        => dbContext.Products;
                }
                ```

                The attribute order matters: `[UsePaging]` must come before `[UseFiltering]` and `[UseSorting]` in the attribute list. They are applied in reverse order in the pipeline.

                ### Configuring Page Sizes

                ```csharp
                [QueryType]
                public static class ProductQueries
                {
                    [UsePaging(DefaultPageSize = 10, MaxPageSize = 50, IncludeTotalCount = true)]
                    public static IQueryable<Product> GetProducts(AppDbContext dbContext)
                        => dbContext.Products.OrderBy(p => p.Id);
                }
                ```

                ### Global Paging Configuration

                ```csharp
                builder.Services
                    .AddGraphQLServer()
                    .AddQueryType()
                    .AddTypes()
                    .SetPagingOptions(new PagingOptions
                    {
                        DefaultPageSize = 10,
                        MaxPageSize = 100,
                        IncludeTotalCount = true
                    });
                ```

                ### Client Queries

                ```graphql
                # First page
                query {
                  products(first: 10) {
                    edges {
                      cursor
                      node {
                        id
                        name
                      }
                    }
                    pageInfo {
                      hasNextPage
                      endCursor
                    }
                  }
                }

                # Next page using cursor
                query {
                  products(first: 10, after: "Y3Vyc29yOjEw") {
                    edges {
                      cursor
                      node {
                        id
                        name
                      }
                    }
                    pageInfo {
                      hasNextPage
                      endCursor
                    }
                  }
                }
                ```

                ## Anti-patterns

                **Returning materialized lists with [UsePaging]:**

                ```csharp
                // BAD: Loading all items into memory defeats the purpose of pagination
                [UsePaging]
                public static async Task<List<Product>> GetProducts(AppDbContext dbContext)
                {
                    return await dbContext.Products.ToListAsync(); // Loads everything!
                }
                ```

                **Not ordering the queryable:**

                ```csharp
                // BAD: Without a stable sort order, cursor pagination produces unpredictable results
                [UsePaging]
                public static IQueryable<Product> GetProducts(AppDbContext dbContext)
                    => dbContext.Products; // No OrderBy — unstable cursor order!
                ```

                **Setting MaxPageSize too high:**

                ```csharp
                // BAD: Allowing clients to request 10,000 items defeats pagination
                [UsePaging(MaxPageSize = 10000)]
                public static IQueryable<Product> GetProducts(AppDbContext dbContext)
                    => dbContext.Products.OrderBy(p => p.Id);
                ```

                ## Key Points

                - Use `[UsePaging]` on `IQueryable<T>` return types for efficient database-level pagination
                - Always apply a stable sort order to the queryable before pagination
                - Configure `MaxPageSize` to prevent clients from requesting excessive data
                - Enable `IncludeTotalCount` only when needed — it requires an additional `COUNT` query
                - Attribute order matters: `[UsePaging]` before `[UseFiltering]` before `[UseSorting]`
                - Return `IQueryable<T>`, not `List<T>` — pagination must be applied at the database level

                ## Related Practices

                - [pagination-offset] — For offset-based pagination
                - [pagination-keyset] — For keyset pagination internals
                - [filtering-basic] — For adding filtering to paginated queries
                """
            });

        docs.Add(
            new BestPracticeDocument
            {
                Id = "pagination-keyset",
                Title = "Keyset Pagination with GreenDonut.Data",
                Category = BestPracticeCategory.Pagination,
                Tags = ["hot-chocolate-16", "performance"],
                Styles = ["all"],
                Keywords = "list array collection items seek performance large dataset scroll infinite results",
                Abstract =
                    "How to implement stable, high-performance keyset pagination using GreenDonut's PagingArguments and cursor encoding for large datasets.",
                Body = """
                # Keyset Pagination with GreenDonut.Data

                ## When to Use

                Use keyset pagination when you need efficient, stable pagination on large datasets. Keyset pagination (also called "seek pagination") uses the values of the last returned row as the starting point for the next page, avoiding the performance penalty of OFFSET.

                This approach is ideal for:

                - Datasets with millions of rows where OFFSET would be slow
                - Real-time feeds where items are inserted frequently
                - APIs that need consistent pagination even when data changes

                GreenDonut.Data provides `PagingArguments` and `Page<T>` types that integrate keyset pagination with Hot Chocolate's cursor connection types.

                ## Implementation

                ### Basic Keyset Pagination

                ```csharp
                using GreenDonut.Data;

                [QueryType]
                public static class ProductQueries
                {
                    [UsePaging]
                    public static async Task<Page<Product>> GetProductsAsync(
                        PagingArguments pagingArgs,
                        AppDbContext dbContext,
                        CancellationToken cancellationToken)
                    {
                        return await dbContext.Products
                            .OrderBy(p => p.Id)
                            .ToPageAsync(pagingArgs, cancellationToken);
                    }
                }
                ```

                ### Keyset Pagination with Custom Sorting

                ```csharp
                [QueryType]
                public static class ProductQueries
                {
                    [UsePaging]
                    public static async Task<Page<Product>> GetProductsAsync(
                        PagingArguments pagingArgs,
                        AppDbContext dbContext,
                        CancellationToken cancellationToken)
                    {
                        return await dbContext.Products
                            .OrderByDescending(p => p.CreatedAt)
                            .ThenBy(p => p.Id)  // Tie-breaker for stable ordering
                            .ToPageAsync(pagingArgs, cancellationToken);
                    }
                }
                ```

                ### Batch Keyset Pagination in DataLoaders

                For paginated child collections, use `ToBatchPageAsync`:

                ```csharp
                public static partial class OrderDataLoaders
                {
                    [DataLoader]
                    public static async Task<Dictionary<int, Page<OrderItem>>> GetItemsByOrderIdAsync(
                        IReadOnlyList<int> orderIds,
                        PagingArguments pagingArgs,
                        AppDbContext dbContext,
                        CancellationToken cancellationToken)
                    {
                        return await dbContext.OrderItems
                            .Where(i => orderIds.Contains(i.OrderId))
                            .OrderBy(i => i.Id)
                            .ToBatchPageAsync(
                                i => i.OrderId,
                                pagingArgs,
                                cancellationToken);
                    }
                }
                ```

                ### How Cursors Work

                Keyset cursors encode the sort key values of the last item. For example, with `OrderBy(p => p.CreatedAt).ThenBy(p => p.Id)`:

                ```
                Cursor encodes: { CreatedAt: "2024-01-15T10:30:00Z", Id: 42 }
                Next page WHERE: (CreatedAt < '2024-01-15T10:30:00Z')
                                 OR (CreatedAt = '2024-01-15T10:30:00Z' AND Id > 42)
                ```

                This generates a `WHERE` clause that uses the index efficiently instead of skipping rows.

                ## Anti-patterns

                **Using OFFSET for large datasets:**

                ```csharp
                // BAD: OFFSET 1000000 scans and discards a million rows
                [UseOffsetPaging]
                public static IQueryable<LogEntry> GetLogs(AppDbContext dbContext)
                    => dbContext.LogEntries.OrderBy(l => l.Timestamp);
                // For large tables, use keyset pagination instead
                ```

                **Not including a tie-breaker column:**

                ```csharp
                // BAD: Without a unique tie-breaker, rows with the same sort value
                // may be skipped or duplicated across pages
                [UsePaging]
                public static async Task<Page<Product>> GetProductsAsync(
                    PagingArguments pagingArgs,
                    AppDbContext dbContext,
                    CancellationToken cancellationToken)
                {
                    return await dbContext.Products
                        .OrderBy(p => p.Price) // Many products have the same price!
                        .ToPageAsync(pagingArgs, cancellationToken);
                    // Add .ThenBy(p => p.Id) as a tie-breaker
                }
                ```

                **Manually parsing cursor strings:**

                ```csharp
                // BAD: Cursor encoding is an internal implementation detail
                var decoded = Base64.Decode(cursor);
                var id = int.Parse(decoded);
                // Use PagingArguments — it handles cursor decoding automatically
                ```

                ## Key Points

                - Keyset pagination uses sort key values instead of OFFSET for O(1) seek performance
                - Use `ToPageAsync` for top-level queries and `ToBatchPageAsync` for DataLoader-level pagination
                - Always include a unique tie-breaker column (typically the primary key) in your sort order
                - Cursors encode sort key values and are opaque to clients
                - Keyset pagination integrates with `[UsePaging]` to produce Relay connection types
                - This approach scales to millions of rows without degradation

                ## Related Practices

                - [pagination-cursor] — For the [UsePaging] connection type pattern
                - [dataloader-greendonut-pagination] — For paginated DataLoaders
                - [configuration-performance] — For performance tuning
                """
            });

        docs.Add(
            new BestPracticeDocument
            {
                Id = "pagination-offset",
                Title = "Offset-Based Pagination with UseOffsetPaging",
                Category = BestPracticeCategory.Pagination,
                Tags = ["hot-chocolate-16"],
                Styles = ["all"],
                Keywords = "list array collection items skip take page number pages limit results offset",
                Abstract =
                    "How to implement offset/limit pagination using [UseOffsetPaging]. When to choose offset over cursor pagination and trade-offs.",
                Body = """
                # Offset-Based Pagination with UseOffsetPaging

                ## When to Use

                Use offset-based pagination when your UI requires traditional page-number navigation (e.g., "Page 1 of 10", "Go to page 5"). This is common in:

                - Admin dashboards with page number navigation
                - Search results with "page X of Y" display
                - Reports where users jump to specific pages

                Offset pagination is simpler to implement on the client side but has trade-offs: it is less stable than cursor pagination when data changes between pages, and it becomes slower on large datasets because the database must skip rows.

                For most GraphQL APIs, prefer cursor-based pagination unless you have a specific UI requirement for page numbers.

                ## Implementation

                ### Basic Offset Pagination

                ```csharp
                [QueryType]
                public static class OrderQueries
                {
                    [UseOffsetPaging]
                    public static IQueryable<Order> GetOrders(AppDbContext dbContext)
                        => dbContext.Orders.OrderByDescending(o => o.CreatedAt);
                }
                ```

                This generates a `CollectionSegment` type:

                ```graphql
                type Query {
                  orders(skip: Int, take: Int): OrderCollectionSegment
                }

                type OrderCollectionSegment {
                  items: [Order!]
                  pageInfo: CollectionSegmentInfo!
                  totalCount: Int!
                }

                type CollectionSegmentInfo {
                  hasNextPage: Boolean!
                  hasPreviousPage: Boolean!
                }
                ```

                ### With Filtering and Sorting

                ```csharp
                [QueryType]
                public static class OrderQueries
                {
                    [UseOffsetPaging]
                    [UseFiltering]
                    [UseSorting]
                    public static IQueryable<Order> GetOrders(AppDbContext dbContext)
                        => dbContext.Orders;
                }
                ```

                ### Configuring Page Sizes

                ```csharp
                [QueryType]
                public static class OrderQueries
                {
                    [UseOffsetPaging(DefaultPageSize = 25, MaxPageSize = 100, IncludeTotalCount = true)]
                    public static IQueryable<Order> GetOrders(AppDbContext dbContext)
                        => dbContext.Orders.OrderByDescending(o => o.CreatedAt);
                }
                ```

                ### Client Queries

                ```graphql
                # First page
                query {
                  orders(skip: 0, take: 25) {
                    items {
                      id
                      status
                      createdAt
                    }
                    pageInfo {
                      hasNextPage
                      hasPreviousPage
                    }
                    totalCount
                  }
                }

                # Page 3
                query {
                  orders(skip: 50, take: 25) {
                    items {
                      id
                      status
                      createdAt
                    }
                    pageInfo {
                      hasNextPage
                      hasPreviousPage
                    }
                    totalCount
                  }
                }
                ```

                ## Anti-patterns

                **Using offset pagination on large datasets without a cap:**

                ```csharp
                // BAD: OFFSET 1000000 is extremely slow on most databases
                [UseOffsetPaging(MaxPageSize = 1000)]
                public static IQueryable<LogEntry> GetLogs(AppDbContext dbContext)
                    => dbContext.LogEntries; // Millions of rows — offset will be very slow
                // Use cursor/keyset pagination for large datasets
                ```

                **Not providing a default sort order:**

                ```csharp
                // BAD: Without ordering, skip/take produces non-deterministic results
                [UseOffsetPaging]
                public static IQueryable<Order> GetOrders(AppDbContext dbContext)
                    => dbContext.Orders; // No OrderBy — results vary between requests!
                ```

                **Implementing manual skip/take instead of using the attribute:**

                ```csharp
                // BAD: Manual pagination loses type generation and pageInfo
                [QueryType]
                public static class OrderQueries
                {
                    public static async Task<List<Order>> GetOrders(
                        int skip, int take, AppDbContext dbContext)
                    {
                        return await dbContext.Orders
                            .Skip(skip)
                            .Take(take)
                            .ToListAsync();
                    }
                }
                ```

                ## Key Points

                - Use `[UseOffsetPaging]` for skip/take pagination with `CollectionSegment` types
                - Offset pagination is best for UIs with page-number navigation
                - Always provide a stable sort order — offset pagination without ordering is non-deterministic
                - Be aware that offset pagination degrades on large datasets (high skip values are slow)
                - Set `MaxPageSize` to prevent clients from requesting too many items
                - Prefer cursor pagination for infinite-scroll UIs and large datasets

                ## Related Practices

                - [pagination-cursor] — For cursor-based pagination (recommended default)
                - [pagination-keyset] — For keyset pagination on large datasets
                - [filtering-basic] — For adding filtering to paginated queries
                """
            });
    }
}
