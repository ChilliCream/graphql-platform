using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.BestPractices.Models;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.BestPractices;

internal static partial class BestPracticeData
{
    private static void AddDataLoaderDocuments(List<BestPracticeDocument> docs)
    {
        docs.Add(
            new BestPracticeDocument
            {
                Id = "dataloader-basic",
                Title = "Implementing DataLoaders with Source Generation",
                Category = BestPracticeCategory.DataLoader,
                Tags = ["hot-chocolate-16", "performance"],
                Styles = ["all"],
                Keywords = "batching n+1 nplusone batch fetch load group request cache deduplication",
                Abstract =
                    "How to implement efficient DataLoaders using the [DataLoader] attribute for automatic batching and caching. Covers naming conventions, parameter ordering, and the generated I{Name}DataLoader interface.",
                Body = """
                # Implementing DataLoaders with Source Generation

                ## When to Use

                Use DataLoaders whenever a resolver fetches data by key and the same field can be resolved multiple times within a single request. This is the primary mechanism for solving the N+1 problem in GraphQL.

                Without a DataLoader, a query that selects 50 users each with an `address` field would execute 50 individual database queries. A DataLoader batches these into a single query that fetches all 50 addresses at once.

                In Hot Chocolate 16, the `[DataLoader]` attribute with source generation is the recommended approach. It generates a strongly-typed DataLoader class with an `I{Name}DataLoader` interface, reducing boilerplate significantly compared to manual implementations.

                ## Implementation

                Define a static method annotated with `[DataLoader]`. The source generator produces the DataLoader class and interface automatically.

                ```csharp
                namespace MyApp.GraphQL.DataLoaders;

                public static partial class UserDataLoaders
                {
                    [DataLoader]
                    public static async Task<Dictionary<int, User>> GetUserByIdAsync(
                        IReadOnlyList<int> ids,
                        AppDbContext dbContext,
                        CancellationToken cancellationToken)
                    {
                        return await dbContext.Users
                            .Where(u => ids.Contains(u.Id))
                            .ToDictionaryAsync(u => u.Id, cancellationToken);
                    }
                }
                ```

                The source generator produces `IUserByIdDataLoader` which you inject into resolvers:

                ```csharp
                [QueryType]
                public static class UserQueries
                {
                    public static async Task<User?> GetUserAsync(
                        int id,
                        IUserByIdDataLoader userById,
                        CancellationToken cancellationToken)
                    {
                        return await userById.LoadAsync(id, cancellationToken);
                    }
                }
                ```

                ### Naming Convention

                The method name determines the generated interface name:

                | Method Name | Generated Interface |
                |---|---|
                | `GetUserByIdAsync` | `IUserByIdDataLoader` |
                | `GetProductBySkuAsync` | `IProductBySkuDataLoader` |
                | `GetOrdersByCustomerIdAsync` | `IOrdersByCustomerIdDataLoader` |

                ### Parameter Order

                The first parameter must be the keys collection (`IReadOnlyList<TKey>`). Services like `DbContext` follow as additional parameters. `CancellationToken` should be the last parameter.

                ## Anti-patterns

                **Fetching one-by-one inside a loop:**

                ```csharp
                // BAD: N+1 queries — each iteration hits the database
                [QueryType]
                public static class BadQueries
                {
                    public static async Task<List<Address>> GetAddresses(
                        [Parent] User user,
                        AppDbContext dbContext)
                    {
                        return await dbContext.Addresses
                            .Where(a => a.UserId == user.Id)
                            .ToListAsync();
                    }
                }
                ```

                **Returning a list instead of a dictionary:**

                ```csharp
                // BAD: DataLoader expects Dictionary<TKey, TValue>, not a list
                [DataLoader]
                public static async Task<List<User>> GetUserByIdAsync(
                    IReadOnlyList<int> ids,
                    AppDbContext dbContext,
                    CancellationToken cancellationToken)
                {
                    return await dbContext.Users
                        .Where(u => ids.Contains(u.Id))
                        .ToListAsync(cancellationToken);
                }
                ```

                ## Key Points

                - Always use `[DataLoader]` source generation instead of manually subclassing `DataLoaderBase<TKey, TValue>`
                - The first parameter must be `IReadOnlyList<TKey>` containing the batch keys
                - Return `Dictionary<TKey, TValue>` for single-value lookups
                - The DataLoader is scoped per-request, so cache entries are automatically cleaned up
                - Missing keys return `default(TValue)` (null for reference types) — no exception is thrown

                ## Related Practices

                - [dataloader-composite-keys] — For multi-column keys
                - [dataloader-greendonut-pagination] — For paginated relationships
                - [dataloader-service-scope] — For DbContext lifetime management
                """
            });

        docs.Add(
            new BestPracticeDocument
            {
                Id = "dataloader-cache-invalidation",
                Title = "DataLoader Cache Invalidation in Mutations",
                Category = BestPracticeCategory.DataLoader,
                Tags = ["hot-chocolate-16", "performance"],
                Styles = ["all"],
                Keywords = "cache invalidation stale data refresh clear mutation update dirty",
                Abstract =
                    "How to invalidate specific DataLoader cache entries after a mutation completes, preventing stale data within the same request.",
                Body = """
                # DataLoader Cache Invalidation in Mutations

                ## When to Use

                Use DataLoader cache invalidation when a mutation modifies an entity and the same request might subsequently read that entity through a DataLoader. Because DataLoaders cache by key within a single request, a mutation that updates an entity will return stale data from the cache unless you explicitly clear the entry.

                This typically happens when:
                - A mutation updates an entity and returns the updated entity in its payload
                - A subscription or subsequent resolver reads the same entity within the same execution
                - You have chained mutations in a single GraphQL request

                ## Implementation

                Use the `Clear` or `Remove` methods on the DataLoader to invalidate specific cache entries after a mutation:

                ```csharp
                namespace MyApp.GraphQL.Mutations;

                [MutationType]
                public static class UserMutations
                {
                    public static async Task<UserPayload> UpdateUserAsync(
                        UpdateUserInput input,
                        AppDbContext dbContext,
                        IUserByIdDataLoader userByIdDataLoader,
                        CancellationToken cancellationToken)
                    {
                        var user = await dbContext.Users.FindAsync(
                            new object[] { input.Id },
                            cancellationToken);

                        if (user is null)
                        {
                            return new UserPayload(null, "User not found.");
                        }

                        user.Name = input.Name;
                        user.Email = input.Email;
                        await dbContext.SaveChangesAsync(cancellationToken);

                        // Clear the cached entry so subsequent reads get fresh data
                        userByIdDataLoader.Remove(input.Id);

                        return new UserPayload(user, null);
                    }
                }

                public record UpdateUserInput(int Id, string Name, string Email);

                public record UserPayload(User? User, string? Error);
                ```

                ### Clearing the Entire Cache

                If a mutation affects many entities and you cannot enumerate all affected keys, clear the entire DataLoader cache:

                ```csharp
                [MutationType]
                public static class BulkMutations
                {
                    public static async Task<BulkUpdatePayload> BulkUpdateUsersAsync(
                        BulkUpdateInput input,
                        AppDbContext dbContext,
                        IUserByIdDataLoader userByIdDataLoader,
                        CancellationToken cancellationToken)
                    {
                        // Perform bulk update
                        await dbContext.Users
                            .Where(u => input.Ids.Contains(u.Id))
                            .ExecuteUpdateAsync(s => s.SetProperty(
                                u => u.Status, input.NewStatus), cancellationToken);

                        // Clear the entire DataLoader cache
                        userByIdDataLoader.ClearCache();

                        return new BulkUpdatePayload(input.Ids.Count);
                    }
                }
                ```

                ### Setting Updated Values Directly

                Instead of clearing the cache and forcing a re-fetch, you can set the updated value directly:

                ```csharp
                [MutationType]
                public static class UserMutations
                {
                    public static async Task<User> UpdateUserNameAsync(
                        int id,
                        string newName,
                        AppDbContext dbContext,
                        IUserByIdDataLoader userByIdDataLoader,
                        CancellationToken cancellationToken)
                    {
                        var user = await dbContext.Users.FindAsync(
                            new object[] { id },
                            cancellationToken);

                        user!.Name = newName;
                        await dbContext.SaveChangesAsync(cancellationToken);

                        // Set the updated entity directly in the cache
                        userByIdDataLoader.Set(id, user);

                        return user;
                    }
                }
                ```

                ## Anti-patterns

                **Ignoring cache invalidation entirely:**

                ```csharp
                // BAD: The mutation payload returns the user, but the DataLoader
                // still has the old version cached. If any other resolver
                // in the same request loads this user, it gets stale data.
                [MutationType]
                public static class UserMutations
                {
                    public static async Task<User> UpdateUserAsync(
                        UpdateUserInput input,
                        AppDbContext dbContext,
                        CancellationToken cancellationToken)
                    {
                        var user = await dbContext.Users.FindAsync(input.Id);
                        user!.Name = input.Name;
                        await dbContext.SaveChangesAsync(cancellationToken);
                        return user; // DataLoader cache is now stale
                    }
                }
                ```

                **Clearing cache before the write completes:**

                ```csharp
                // BAD: Cache is cleared before SaveChanges — if SaveChanges fails,
                // subsequent reads will re-fetch the old value anyway,
                // but you've lost the optimization of having it cached.
                userByIdDataLoader.Remove(input.Id);
                await dbContext.SaveChangesAsync(cancellationToken); // May fail
                ```

                ## Key Points

                - Call `Remove(key)` on the DataLoader after a mutation successfully updates an entity
                - Use `ClearCache()` when a bulk operation affects many entities
                - Use `Set(key, value)` when you already have the updated entity to avoid a re-fetch
                - Always invalidate after the database write succeeds, not before
                - DataLoader caches are per-request, so invalidation only matters within the same GraphQL request

                ## Related Practices

                - [dataloader-basic] — For basic DataLoader setup
                - [error-handling-mutation-conventions] — For typed mutation errors
                """
            });

        docs.Add(
            new BestPracticeDocument
            {
                Id = "dataloader-composite-keys",
                Title = "DataLoaders with Composite Keys",
                Category = BestPracticeCategory.DataLoader,
                Tags = ["hot-chocolate-16", "performance"],
                Styles = ["all"],
                Keywords = "composite key multi column compound key two columns record struct",
                Abstract =
                    "How to design DataLoaders that batch by multiple columns using a record key type. Covers key design, equality semantics, and the correct query pattern for multi-column lookups.",
                Body = """
                # DataLoaders with Composite Keys

                ## When to Use

                Use composite key DataLoaders when the entity you need to batch-load is identified by more than one column. Common examples include:

                - A `UserRole` identified by both `UserId` and `RoleId`
                - A `Translation` identified by `EntityId` and `LanguageCode`
                - A `TenantUser` identified by `TenantId` and `UserId`

                When the natural key is a single scalar (an `int` ID or a `Guid`), use a simple DataLoader instead. Composite key DataLoaders add overhead from record allocation and multi-column filtering.

                ## Implementation

                Define a `record` or `readonly record struct` for the key. Records provide value-based equality, which DataLoaders require for correct cache deduplication.

                ```csharp
                namespace MyApp.GraphQL.DataLoaders;

                public readonly record struct UserRoleKey(int UserId, int RoleId);

                public static partial class UserRoleDataLoaders
                {
                    [DataLoader]
                    public static async Task<Dictionary<UserRoleKey, UserRole>> GetUserRoleByKeyAsync(
                        IReadOnlyList<UserRoleKey> keys,
                        AppDbContext dbContext,
                        CancellationToken cancellationToken)
                    {
                        var userIds = keys.Select(k => k.UserId).Distinct().ToList();
                        var roleIds = keys.Select(k => k.RoleId).Distinct().ToList();

                        var results = await dbContext.UserRoles
                            .Where(ur => userIds.Contains(ur.UserId) && roleIds.Contains(ur.RoleId))
                            .ToListAsync(cancellationToken);

                        return results.ToDictionary(
                            ur => new UserRoleKey(ur.UserId, ur.RoleId));
                    }
                }
                ```

                Use the DataLoader in a resolver:

                ```csharp
                [ObjectType<User>]
                public static partial class UserTypeExtension
                {
                    public static async Task<UserRole?> GetRoleAsync(
                        [Parent] User user,
                        int roleId,
                        IUserRoleByKeyDataLoader dataLoader,
                        CancellationToken cancellationToken)
                    {
                        var key = new UserRoleKey(user.Id, roleId);
                        return await dataLoader.LoadAsync(key, cancellationToken);
                    }
                }
                ```

                ### Using `readonly record struct` vs `record class`

                Prefer `readonly record struct` for composite keys with 2-3 small fields. This avoids heap allocations when the key is used as a dictionary lookup:

                ```csharp
                // Preferred: stack-allocated, no GC pressure
                public readonly record struct TenantUserKey(Guid TenantId, Guid UserId);

                // Acceptable for larger keys, but allocates on the heap
                public record LargeCompositeKey(Guid TenantId, Guid UserId, string Region, int Version);
                ```

                ## Anti-patterns

                **Using anonymous types or tuples as keys:**

                ```csharp
                // BAD: Tuples have inconsistent equality across different contexts
                // and do not produce readable generated interface names
                [DataLoader]
                public static async Task<Dictionary<(int, int), UserRole>> GetUserRoleAsync(
                    IReadOnlyList<(int, int)> keys, ...)
                ```

                **Forgetting value equality:**

                ```csharp
                // BAD: Class without record semantics — uses reference equality by default
                public class UserRoleKey
                {
                    public int UserId { get; set; }
                    public int RoleId { get; set; }
                }
                ```

                **Over-fetching with Cartesian product:**

                ```csharp
                // BAD: Cartesian product fetches far more rows than needed
                var results = await dbContext.UserRoles
                    .Where(ur => userIds.Contains(ur.UserId))
                    .Where(ur => roleIds.Contains(ur.RoleId))
                    .ToListAsync(cancellationToken);
                // This returns ALL userRoles where userId matches AND roleId matches,
                // not just the specific (userId, roleId) pairs requested
                ```

                ## Key Points

                - Use `readonly record struct` for composite keys with 2-3 small fields to avoid heap allocations
                - Records provide value-based equality, which is required for correct DataLoader cache behavior
                - Filter the database query using the distinct values from each key component
                - After fetching, convert results back to dictionary using the composite key type
                - Name the key type descriptively (e.g., `UserRoleKey`) for readable generated interfaces

                ## Related Practices

                - [dataloader-basic] — For single-column key DataLoaders
                - [dataloader-nested] — For chaining DataLoaders across entity types
                """
            });

        docs.Add(
            new BestPracticeDocument
            {
                Id = "dataloader-greendonut-pagination",
                Title = "Paginated Relationships with GreenDonut.Data",
                Category = BestPracticeCategory.DataLoader,
                Tags = ["hot-chocolate-16", "performance", "relay"],
                Styles = ["all"],
                Keywords = "list array collection items children nested pagination child relationship batch page",
                Abstract =
                    "How to integrate GreenDonut's PagingArguments, QueryContext, and SortDefinition into DataLoaders that resolve paginated child collections.",
                Body = """
                # Paginated Relationships with GreenDonut.Data

                ## When to Use

                Use GreenDonut.Data pagination when a relationship returns a large number of child items and you want to offer cursor-based pagination at the DataLoader level. This is different from top-level query pagination — here the pagination applies to a nested field (e.g., `author { books(first: 10) }`).

                This pattern is appropriate when:
                - Child collections can be large (dozens or more items)
                - You want to support cursor-based pagination on nested relationships
                - You want efficient keyset pagination that integrates with the DataLoader batching

                ## Implementation

                Use `PagingArguments` from GreenDonut.Data to accept pagination parameters in your DataLoader:

                ```csharp
                using GreenDonut.Data;

                namespace MyApp.GraphQL.DataLoaders;

                public static partial class AuthorDataLoaders
                {
                    [DataLoader]
                    public static async Task<Dictionary<int, Page<Book>>> GetBooksByAuthorIdAsync(
                        IReadOnlyList<int> authorIds,
                        PagingArguments pagingArgs,
                        QueryContext queryContext,
                        AppDbContext dbContext,
                        CancellationToken cancellationToken)
                    {
                        return await dbContext.Books
                            .Where(b => authorIds.Contains(b.AuthorId))
                            .OrderBy(b => b.Title)
                            .ToBatchPageAsync(
                                b => b.AuthorId,
                                pagingArgs,
                                cancellationToken);
                    }
                }
                ```

                Wire it up in your type extension with `[UsePaging]`:

                ```csharp
                [ObjectType<Author>]
                public static partial class AuthorTypeExtension
                {
                    [UsePaging]
                    public static async Task<Page<Book>> GetBooksAsync(
                        [Parent] Author author,
                        PagingArguments pagingArgs,
                        QueryContext queryContext,
                        IBooksByAuthorIdDataLoader dataLoader,
                        CancellationToken cancellationToken)
                    {
                        return await dataLoader.LoadAsync(author.Id, cancellationToken);
                    }
                }
                ```

                ### Sorting with SortDefinition

                You can pass sort definitions through the `QueryContext` to control ordering:

                ```csharp
                [DataLoader]
                public static async Task<Dictionary<int, Page<Book>>> GetBooksByAuthorIdAsync(
                    IReadOnlyList<int> authorIds,
                    PagingArguments pagingArgs,
                    QueryContext queryContext,
                    AppDbContext dbContext,
                    CancellationToken cancellationToken)
                {
                    var query = dbContext.Books
                        .Where(b => authorIds.Contains(b.AuthorId));

                    // Apply sorting from query context if available
                    query = query.OrderBy(b => b.PublishedDate);

                    return await query.ToBatchPageAsync(
                        b => b.AuthorId,
                        pagingArgs,
                        cancellationToken);
                }
                ```

                ## Anti-patterns

                **Loading all children and paginating in memory:**

                ```csharp
                // BAD: Loads ALL books from the database, then slices in memory
                [DataLoader]
                public static async Task<Dictionary<int, Page<Book>>> GetBooksByAuthorIdAsync(
                    IReadOnlyList<int> authorIds,
                    PagingArguments pagingArgs,
                    AppDbContext dbContext,
                    CancellationToken cancellationToken)
                {
                    var allBooks = await dbContext.Books
                        .Where(b => authorIds.Contains(b.AuthorId))
                        .ToListAsync(cancellationToken); // Fetches everything

                    // Manual slicing loses database-level efficiency
                    return allBooks
                        .GroupBy(b => b.AuthorId)
                        .ToDictionary(g => g.Key, g => SliceManually(g, pagingArgs));
                }
                ```

                **Ignoring pagination on nested fields with large datasets:**

                ```csharp
                // BAD: Returns all children without pagination — may return thousands of rows
                [ObjectType<Author>]
                public static partial class AuthorTypeExtension
                {
                    public static async Task<IEnumerable<Book>> GetBooks(
                        [Parent] Author author,
                        IBooksByAuthorIdDataLoader dataLoader,
                        CancellationToken cancellationToken)
                    {
                        return await dataLoader.LoadAsync(author.Id, cancellationToken);
                    }
                }
                ```

                ## Key Points

                - Use `ToBatchPageAsync` for efficient database-level pagination within DataLoaders
                - The `PagingArguments` type carries `first`, `after`, `last`, and `before` cursor parameters
                - Cursor-based pagination on nested fields prevents unbounded result sets
                - The `Page<T>` return type integrates with Hot Chocolate's `[UsePaging]` to produce Relay connection types
                - Sorting must be applied before pagination to ensure stable cursor ordering

                ## Related Practices

                - [dataloader-basic] — For basic DataLoader implementation
                - [pagination-cursor] — For top-level cursor-based pagination
                - [pagination-keyset] — For keyset pagination internals
                """
            });

        docs.Add(
            new BestPracticeDocument
            {
                Id = "dataloader-nested",
                Title = "Nested DataLoaders and Cross-Entity Lookups",
                Category = BestPracticeCategory.DataLoader,
                Tags = ["hot-chocolate-16", "performance"],
                Styles = ["all"],
                Keywords = "nested relationship chain deep graph parent child one-to-many lookup hierarchy",
                Abstract =
                    "How to chain DataLoaders when resolving deeply nested relationships. Covers the Lookups API for sharing cached entities across DataLoader types.",
                Body = """
                # Nested DataLoaders and Cross-Entity Lookups

                ## When to Use

                Use nested DataLoaders when your GraphQL schema has multi-level relationships that each require their own batched data fetching. For example, a query like `orders { items { product { category } } }` requires chaining DataLoaders at each level: order items, products, and categories.

                This pattern is essential when you have deep graphs where child resolvers depend on keys obtained from parent resolvers. Each level independently batches its requests, so even deeply nested queries execute with minimal database round-trips.

                ## Implementation

                Define a DataLoader for each entity type and chain them in resolvers:

                ```csharp
                namespace MyApp.GraphQL.DataLoaders;

                public static partial class OrderDataLoaders
                {
                    [DataLoader]
                    public static async Task<ILookup<int, OrderItem>> GetOrderItemsByOrderIdAsync(
                        IReadOnlyList<int> orderIds,
                        AppDbContext dbContext,
                        CancellationToken cancellationToken)
                    {
                        var items = await dbContext.OrderItems
                            .Where(i => orderIds.Contains(i.OrderId))
                            .ToListAsync(cancellationToken);

                        return items.ToLookup(i => i.OrderId);
                    }

                    [DataLoader]
                    public static async Task<Dictionary<int, Product>> GetProductByIdAsync(
                        IReadOnlyList<int> ids,
                        AppDbContext dbContext,
                        CancellationToken cancellationToken)
                    {
                        return await dbContext.Products
                            .Where(p => ids.Contains(p.Id))
                            .ToDictionaryAsync(p => p.Id, cancellationToken);
                    }

                    [DataLoader]
                    public static async Task<Dictionary<int, Category>> GetCategoryByIdAsync(
                        IReadOnlyList<int> ids,
                        AppDbContext dbContext,
                        CancellationToken cancellationToken)
                    {
                        return await dbContext.Categories
                            .Where(c => ids.Contains(c.Id))
                            .ToDictionaryAsync(c => c.Id, cancellationToken);
                    }
                }
                ```

                Wire the resolvers to chain through each level:

                ```csharp
                [ObjectType<Order>]
                public static partial class OrderTypeExtension
                {
                    public static async Task<IEnumerable<OrderItem>> GetItemsAsync(
                        [Parent] Order order,
                        IOrderItemsByOrderIdDataLoader dataLoader,
                        CancellationToken cancellationToken)
                    {
                        return await dataLoader.LoadAsync(order.Id, cancellationToken);
                    }
                }

                [ObjectType<OrderItem>]
                public static partial class OrderItemTypeExtension
                {
                    public static async Task<Product?> GetProductAsync(
                        [Parent] OrderItem item,
                        IProductByIdDataLoader dataLoader,
                        CancellationToken cancellationToken)
                    {
                        return await dataLoader.LoadAsync(item.ProductId, cancellationToken);
                    }
                }

                [ObjectType<Product>]
                public static partial class ProductTypeExtension
                {
                    public static async Task<Category?> GetCategoryAsync(
                        [Parent] Product product,
                        ICategoryByIdDataLoader dataLoader,
                        CancellationToken cancellationToken)
                    {
                        return await dataLoader.LoadAsync(product.CategoryId, cancellationToken);
                    }
                }
                ```

                ### Group DataLoaders with ILookup

                When a parent has multiple children (one-to-many), return `ILookup<TKey, TValue>` instead of `Dictionary<TKey, TValue>`:

                ```csharp
                [DataLoader]
                public static async Task<ILookup<int, Review>> GetReviewsByProductIdAsync(
                    IReadOnlyList<int> productIds,
                    AppDbContext dbContext,
                    CancellationToken cancellationToken)
                {
                    var reviews = await dbContext.Reviews
                        .Where(r => productIds.Contains(r.ProductId))
                        .ToListAsync(cancellationToken);

                    return reviews.ToLookup(r => r.ProductId);
                }
                ```

                ## Anti-patterns

                **Loading children inline without a DataLoader:**

                ```csharp
                // BAD: This creates N+1 — each order triggers a separate DB call for items
                [ObjectType<Order>]
                public static partial class OrderTypeExtension
                {
                    public static async Task<List<OrderItem>> GetItems(
                        [Parent] Order order,
                        AppDbContext dbContext)
                    {
                        return await dbContext.OrderItems
                            .Where(i => i.OrderId == order.Id)
                            .ToListAsync();
                    }
                }
                ```

                **Joining eagerly in the parent DataLoader:**

                ```csharp
                // BAD: Fetching all nested data in a single DataLoader defeats the purpose
                // of the graph. Only fetch what the current level needs.
                [DataLoader]
                public static async Task<Dictionary<int, Order>> GetOrderByIdAsync(
                    IReadOnlyList<int> ids,
                    AppDbContext dbContext,
                    CancellationToken cancellationToken)
                {
                    return await dbContext.Orders
                        .Include(o => o.Items)         // Unnecessary eager loading
                            .ThenInclude(i => i.Product) // May not even be requested
                        .Where(o => ids.Contains(o.Id))
                        .ToDictionaryAsync(o => o.Id, cancellationToken);
                }
                ```

                ## Key Points

                - Each entity type should have its own DataLoader — do not combine multiple entity types into a single DataLoader
                - Use `ILookup<TKey, TValue>` for one-to-many relationships, `Dictionary<TKey, TValue>` for one-to-one
                - DataLoaders at each graph level batch independently, so even deeply nested queries are efficient
                - Avoid eager loading (`Include`) in DataLoaders — let the GraphQL engine drive which levels are resolved
                - The execution engine automatically batches sibling fields, so parallel branches of the graph share DataLoader batches

                ## Related Practices

                - [dataloader-basic] — For simple single-entity DataLoaders
                - [dataloader-composite-keys] — For DataLoaders with multi-column keys
                - [dataloader-greendonut-pagination] — For paginated child collections
                """
            });

        docs.Add(
            new BestPracticeDocument
            {
                Id = "dataloader-service-scope",
                Title = "DataLoader Service Scope and DbContext Lifetime",
                Category = BestPracticeCategory.DataLoader,
                Tags = ["hot-chocolate-16", "performance"],
                Styles = ["all"],
                Keywords = "dbcontext scope lifetime concurrency ef core entity framework pool factory",
                Abstract =
                    "How to correctly handle EF Core DbContext lifetime inside DataLoaders using the DataLoaderServiceScope attribute to avoid scope mismatches.",
                Body = """
                # DataLoader Service Scope and DbContext Lifetime

                ## When to Use

                Use `[DataLoaderServiceScope]` whenever your DataLoader batch method needs services with scoped lifetime, particularly EF Core `DbContext`. This is necessary because DataLoaders live for the entire request, but a `DbContext` may need its own scope to avoid concurrency issues.

                Hot Chocolate 16 provides the `[DataLoaderServiceScope]` attribute to automatically create a fresh DI scope for each DataLoader batch invocation. This prevents the common problem of a `DbContext` being reused across concurrent DataLoader executions within the same request.

                ## Implementation

                Apply `[DataLoaderServiceScope]` to your DataLoader method to get a scoped `DbContext` per batch:

                ```csharp
                namespace MyApp.GraphQL.DataLoaders;

                public static partial class UserDataLoaders
                {
                    [DataLoader]
                    [DataLoaderServiceScope]
                    public static async Task<Dictionary<int, User>> GetUserByIdAsync(
                        IReadOnlyList<int> ids,
                        AppDbContext dbContext,
                        CancellationToken cancellationToken)
                    {
                        return await dbContext.Users
                            .Where(u => ids.Contains(u.Id))
                            .ToDictionaryAsync(u => u.Id, cancellationToken);
                    }
                }
                ```

                ### Service Registration

                Register your `DbContext` as scoped (the default for `AddDbContext`):

                ```csharp
                var builder = WebApplication.CreateBuilder(args);

                builder.Services.AddDbContext<AppDbContext>(options =>
                    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

                builder.Services
                    .AddGraphQLServer()
                    .AddQueryType()
                    .AddTypes()
                    .RegisterDbContextFactory<AppDbContext>();
                ```

                ### When to Use DbContextFactory Instead

                For high-throughput scenarios, you can use `IDbContextFactory<T>` directly:

                ```csharp
                public static partial class UserDataLoaders
                {
                    [DataLoader]
                    public static async Task<Dictionary<int, User>> GetUserByIdAsync(
                        IReadOnlyList<int> ids,
                        IDbContextFactory<AppDbContext> dbContextFactory,
                        CancellationToken cancellationToken)
                    {
                        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

                        return await dbContext.Users
                            .Where(u => ids.Contains(u.Id))
                            .ToDictionaryAsync(u => u.Id, cancellationToken);
                    }
                }
                ```

                ## Anti-patterns

                **Sharing a single DbContext across concurrent DataLoaders:**

                ```csharp
                // BAD: Without [DataLoaderServiceScope], the same DbContext instance
                // may be used concurrently by multiple DataLoaders, causing
                // "A second operation was started on this context instance before a
                // previous operation completed" exceptions.
                [DataLoader]
                public static async Task<Dictionary<int, User>> GetUserByIdAsync(
                    IReadOnlyList<int> ids,
                    AppDbContext dbContext,  // Potentially shared with other DataLoaders
                    CancellationToken cancellationToken)
                {
                    return await dbContext.Users
                        .Where(u => ids.Contains(u.Id))
                        .ToDictionaryAsync(u => u.Id, cancellationToken);
                }
                ```

                **Creating DbContext manually without disposal:**

                ```csharp
                // BAD: Manual DbContext creation without proper disposal
                [DataLoader]
                public static async Task<Dictionary<int, User>> GetUserByIdAsync(
                    IReadOnlyList<int> ids,
                    IServiceProvider serviceProvider,
                    CancellationToken cancellationToken)
                {
                    var dbContext = serviceProvider.GetRequiredService<AppDbContext>();
                    // DbContext is never disposed — connection leak
                    return await dbContext.Users
                        .Where(u => ids.Contains(u.Id))
                        .ToDictionaryAsync(u => u.Id, cancellationToken);
                }
                ```

                ## Key Points

                - Use `[DataLoaderServiceScope]` when your DataLoader needs scoped services like `DbContext`
                - The attribute creates a fresh DI scope per batch invocation, preventing concurrency issues
                - Alternatively, inject `IDbContextFactory<T>` and create a `DbContext` per call with `await using`
                - Register your `DbContext` with `RegisterDbContextFactory<T>()` on the GraphQL server builder for seamless integration
                - Never manually resolve scoped services from `IServiceProvider` without managing their lifetime

                ## Related Practices

                - [dataloader-basic] — For basic DataLoader setup
                - [resolvers-di] — For dependency injection in resolvers
                - [configuration-server-setup] — For server configuration options
                """
            });
    }
}
