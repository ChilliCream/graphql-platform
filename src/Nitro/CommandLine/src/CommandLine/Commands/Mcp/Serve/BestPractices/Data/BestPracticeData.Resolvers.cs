using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.BestPractices.Models;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.BestPractices;

internal static partial class BestPracticeData
{
    private static void AddResolversDocuments(List<BestPracticeDocument> docs)
    {
        docs.Add(
            new BestPracticeDocument
            {
                Id = "resolver-best-practices",
                Title = "Implementing Field Resolvers",
                Category = BestPracticeCategory.Resolvers,
                Tags = ["hot-chocolate-16", "resolvers", "performance"],
                Styles = ["all"],
                Keywords = "resolver function method handler field data fetch return pure static",
                Abstract =
                    "How to implement field resolvers using pure functions, static methods on type extensions, and method-style resolvers. Covers the Pure resolver optimization.",
                Body = """
                # Implementing Field Resolvers

                ## When to Use

                Every GraphQL field needs a resolver. In Hot Chocolate 16, resolvers are implemented as static methods on type extension classes annotated with `[QueryType]`, `[MutationType]`, or `[ObjectType<T>]`. Understanding resolver patterns is fundamental to building any Hot Chocolate application.

                ## Example

                ### Basic Query Resolver

                ```csharp
                [QueryType]
                internal static class BookQueries
                {
                    public static async Task<Book?> GetBookByIdAsync(
                        int id,
                        IBookByIdDataLoader dataLoader,
                        CancellationToken cancellationToken)
                    {
                        return await dataLoader.LoadAsync(id, cancellationToken);
                    }
                }
                ```

                ### Type Extension Resolver

                Extend an existing type with computed fields:

                ```csharp
                [ObjectType<Book>]
                internal static partial class BookTypeExtensions
                {
                    public static async Task<Author?> GetAuthorAsync(
                        [Parent] Book book,
                        IAuthorByIdDataLoader dataLoader,
                        CancellationToken cancellationToken)
                    {
                        return await dataLoader.LoadAsync(book.AuthorId, cancellationToken);
                    }

                    // Pure resolver: no I/O, just computation from parent data
                    [NodeResolver]
                    public static string GetDisplayTitle([Parent] Book book)
                    {
                        return $"{book.Title} ({book.PublishedYear})";
                    }
                }
                ```

                ### Service Injection

                Inject services directly via method parameters:

                ```csharp
                [QueryType]
                internal static class BookQueries
                {
                    public static async Task<IEnumerable<Book>> GetBooksAsync(
                        AppDbContext dbContext,
                        CancellationToken cancellationToken)
                    {
                        return await dbContext.Books
                            .OrderBy(b => b.Title)
                            .ToListAsync(cancellationToken);
                    }
                }
                ```

                ## Key Points

                - Resolvers are static methods on classes annotated with `[QueryType]`, `[MutationType]`, or `[ObjectType<T>]`
                - Use `[Parent]` attribute to access the parent object in type extension resolvers
                - Services are injected via method parameters (preferred over constructor injection)
                - Always pass `CancellationToken` for async resolvers
                - Pure resolvers (no I/O, only computation from parent) are automatically optimized
                - The method name determines the GraphQL field name (with `Get` prefix stripped and camelCasing applied)
                - Type extension classes must be `partial` when using source generation

                ## Anti-patterns

                - **Instance methods on resolver classes**: Hot Chocolate 16 resolvers should be static methods. Instance methods add unnecessary allocation overhead.
                - **Injecting `IResolverContext` when specific parameters suffice**: Prefer typed parameters over `IResolverContext`. Only use `IResolverContext` when you need dynamic access to context data.
                - **Blocking async calls with `.Result` or `.GetAwaiter().GetResult()`**: Always use `async`/`await` in resolvers. Blocking calls can deadlock the execution engine.
                - **Heavy computation in resolvers without DataLoaders**: If a resolver fetches data by ID, use a DataLoader to enable batching. Direct database calls in resolvers cause N+1 problems.
                - **Not passing CancellationToken**: Always accept and forward `CancellationToken` to support cooperative cancellation when clients disconnect.

                ## Related Practices

                - [resolvers-parent] - Accessing parent object data
                - [resolvers-di] - Dependency injection patterns
                - [resolvers-async] - Async patterns and CancellationToken
                - [dataloader-basic] - DataLoader integration
                """
            });

        docs.Add(
            new BestPracticeDocument
            {
                Id = "resolvers-async",
                Title = "Async Resolvers and CancellationToken",
                Category = BestPracticeCategory.Resolvers,
                Tags = ["hot-chocolate-16", "performance"],
                Styles = ["all"],
                Keywords = "async await task asynchronous concurrent parallel promise non-blocking",
                Abstract =
                    "How to implement async resolvers correctly, pass CancellationToken, handle cooperative cancellation, and avoid async anti-patterns.",
                Body = """
                # Async Resolvers and CancellationToken

                ## When to Use

                Use async resolvers for any field resolution that involves I/O operations: database queries, HTTP calls, file access, or DataLoader loads. This is the vast majority of non-trivial resolvers.

                Always accept and pass `CancellationToken` in async resolvers. Hot Chocolate provides a `CancellationToken` that is triggered when the client disconnects or the request times out, enabling cooperative cancellation of in-flight work.

                ## Implementation

                ### Basic Async Resolver

                ```csharp
                [QueryType]
                public static class ProductQueries
                {
                    public static async Task<Product?> GetProductAsync(
                        int id,
                        IProductByIdDataLoader dataLoader,
                        CancellationToken cancellationToken)
                    {
                        return await dataLoader.LoadAsync(id, cancellationToken);
                    }
                }
                ```

                ### Passing CancellationToken Through the Call Chain

                ```csharp
                [MutationType]
                public static class OrderMutations
                {
                    public static async Task<Order> CreateOrderAsync(
                        CreateOrderInput input,
                        [Service] IOrderService orderService,
                        CancellationToken cancellationToken)
                    {
                        return await orderService.CreateAsync(input, cancellationToken);
                    }
                }

                public class OrderService(AppDbContext dbContext) : IOrderService
                {
                    public async Task<Order> CreateAsync(
                        CreateOrderInput input,
                        CancellationToken cancellationToken)
                    {
                        var order = new Order
                        {
                            CustomerId = input.CustomerId,
                            Status = OrderStatus.Pending
                        };

                        dbContext.Orders.Add(order);
                        await dbContext.SaveChangesAsync(cancellationToken);
                        return order;
                    }
                }
                ```

                ### Multiple Async Operations

                When you need multiple independent async calls, use `Task.WhenAll` for parallelism:

                ```csharp
                [ObjectType<Dashboard>]
                public static partial class DashboardType
                {
                    public static async Task<DashboardStats> GetStatsAsync(
                        [Parent] Dashboard dashboard,
                        [Service] IAnalyticsService analytics,
                        CancellationToken cancellationToken)
                    {
                        var ordersTask = analytics.GetOrderCountAsync(
                            dashboard.DateRange, cancellationToken);
                        var revenueTask = analytics.GetRevenueAsync(
                            dashboard.DateRange, cancellationToken);

                        await Task.WhenAll(ordersTask, revenueTask);

                        return new DashboardStats
                        {
                            OrderCount = await ordersTask,
                            TotalRevenue = await revenueTask
                        };
                    }
                }
                ```

                ### ValueTask for Hot Paths

                For resolvers that may complete synchronously (e.g., cache hits), use `ValueTask`:

                ```csharp
                [ObjectType<Product>]
                public static partial class ProductType
                {
                    public static ValueTask<decimal> GetConvertedPriceAsync(
                        [Parent] Product product,
                        [Service] ICurrencyCache currencyCache,
                        string targetCurrency,
                        CancellationToken cancellationToken)
                    {
                        if (currencyCache.TryGetRate(targetCurrency, out var rate))
                        {
                            return ValueTask.FromResult(product.Price * rate);
                        }

                        return LoadAndConvertAsync(product, currencyCache, targetCurrency, cancellationToken);
                    }

                    private static async ValueTask<decimal> LoadAndConvertAsync(
                        Product product,
                        ICurrencyCache cache,
                        string currency,
                        CancellationToken ct)
                    {
                        var rate = await cache.LoadRateAsync(currency, ct);
                        return product.Price * rate;
                    }
                }
                ```

                ## Anti-patterns

                **Using .Result or .Wait() to block:**

                ```csharp
                // BAD: Blocks the thread and risks deadlocks
                [QueryType]
                public static class Queries
                {
                    public static User GetUser(int id, AppDbContext db)
                    {
                        return db.Users.FindAsync(id).Result!; // DEADLOCK RISK
                    }
                }
                ```

                **Forgetting to pass CancellationToken:**

                ```csharp
                // BAD: Without CancellationToken, the work continues even after
                // the client disconnects, wasting server resources
                [QueryType]
                public static class Queries
                {
                    public static async Task<List<Product>> GetProducts(AppDbContext db)
                    {
                        return await db.Products.ToListAsync(); // No cancellation token!
                    }
                }
                ```

                **Unnecessary async wrapper:**

                ```csharp
                // BAD: async/await adds overhead when you can return the Task directly
                [QueryType]
                public static class Queries
                {
                    public static async Task<User?> GetUserAsync(
                        int id, IUserByIdDataLoader loader, CancellationToken ct)
                    {
                        return await loader.LoadAsync(id, ct);
                        // Could just be: return loader.LoadAsync(id, ct);
                        // But keep async/await when there's a try-catch or using statement
                    }
                }
                ```

                **Swallowing OperationCanceledException:**

                ```csharp
                // BAD: Catching cancellation silently hides the fact that the work was cancelled
                public static async Task<Data> GetDataAsync(
                    [Service] IDataService service, CancellationToken ct)
                {
                    try
                    {
                        return await service.FetchAsync(ct);
                    }
                    catch (OperationCanceledException)
                    {
                        return new Data(); // Silently returns empty data
                    }
                }
                ```

                ## Key Points

                - Always include `CancellationToken cancellationToken` as the last parameter in async resolvers
                - Pass the cancellation token through every async call in the chain
                - Use `Task.WhenAll` for independent parallel operations
                - Never use `.Result`, `.Wait()`, or `.GetAwaiter().GetResult()` — these block threads and risk deadlocks
                - Let `OperationCanceledException` propagate — Hot Chocolate handles it correctly
                - Use `ValueTask<T>` only when you have a hot path that frequently completes synchronously

                ## Related Practices

                - [resolvers-field] — For general resolver patterns
                - [resolvers-di] — For service injection
                - [configuration-performance] — For execution engine tuning
                """
            });

        docs.Add(
            new BestPracticeDocument
            {
                Id = "resolvers-di",
                Title = "Dependency Injection in Resolvers",
                Category = BestPracticeCategory.Resolvers,
                Tags = ["hot-chocolate-16", "code-first"],
                Styles = ["all"],
                Keywords = "dependency injection service provider inject constructor IoC container register",
                Abstract =
                    "How to inject services into resolvers using method parameters (preferred), [Service] attribute, and IResolverContext. Covers lifetime management.",
                Body = """
                # Dependency Injection in Resolvers

                ## When to Use

                Use dependency injection in resolvers whenever you need access to application services such as repositories, business logic services, external API clients, or database contexts.

                Hot Chocolate 16 supports parameter injection directly into resolver methods, making DI seamless and explicit. This is the preferred approach over constructor injection (which is not available in static type extensions) or manual service resolution from `IResolverContext`.

                ## Implementation

                ### Parameter Injection (Preferred)

                Services registered in the DI container are automatically injected as resolver method parameters:

                ```csharp
                [QueryType]
                public static class ProductQueries
                {
                    public static async Task<Product?> GetProductAsync(
                        int id,
                        IProductByIdDataLoader dataLoader,
                        CancellationToken cancellationToken)
                    {
                        return await dataLoader.LoadAsync(id, cancellationToken);
                    }
                }
                ```

                ### Using [Service] Attribute

                Use `[Service]` when parameter name ambiguity might cause the framework to interpret a parameter as a GraphQL argument:

                ```csharp
                [MutationType]
                public static class OrderMutations
                {
                    public static async Task<Order> PlaceOrderAsync(
                        PlaceOrderInput input,
                        [Service] IOrderService orderService,
                        [Service] IEmailService emailService,
                        CancellationToken cancellationToken)
                    {
                        var order = await orderService.PlaceAsync(input, cancellationToken);
                        await emailService.SendConfirmationAsync(order, cancellationToken);
                        return order;
                    }
                }
                ```

                ### DbContext Injection

                For EF Core `DbContext`, use `RegisterDbContextFactory` for scoped access:

                ```csharp
                // Program.cs
                builder.Services.AddDbContext<AppDbContext>(o =>
                    o.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

                builder.Services
                    .AddGraphQLServer()
                    .AddQueryType()
                    .AddTypes()
                    .RegisterDbContextFactory<AppDbContext>();
                ```

                ```csharp
                [QueryType]
                public static class UserQueries
                {
                    [UsePaging]
                    [UseFiltering]
                    [UseSorting]
                    public static IQueryable<User> GetUsers(AppDbContext dbContext)
                        => dbContext.Users;
                }
                ```

                ### Keyed Services

                Inject keyed services using `[Service]` with a key:

                ```csharp
                [QueryType]
                public static class ReportQueries
                {
                    public static async Task<Report> GetReportAsync(
                        [Service(Key = "pdf")] IReportGenerator pdfGenerator,
                        CancellationToken cancellationToken)
                    {
                        return await pdfGenerator.GenerateAsync(cancellationToken);
                    }
                }
                ```

                ### Multiple Services in a Resolver

                ```csharp
                [ObjectType<User>]
                public static partial class UserType
                {
                    public static async Task<UserProfile> GetProfileAsync(
                        [Parent] User user,
                        IUserProfileByUserIdDataLoader profileLoader,
                        [Service] IAvatarService avatarService,
                        CancellationToken cancellationToken)
                    {
                        var profile = await profileLoader.LoadAsync(user.Id, cancellationToken);
                        profile ??= new UserProfile { UserId = user.Id };
                        profile.AvatarUrl = avatarService.GetAvatarUrl(user.Email);
                        return profile;
                    }
                }
                ```

                ## Anti-patterns

                **Using IResolverContext.Services directly:**

                ```csharp
                // BAD: Manual service resolution loses type safety and makes dependencies implicit
                [QueryType]
                public static class UserQueries
                {
                    public static async Task<User?> GetUser(int id, IResolverContext context)
                    {
                        var dbContext = context.Services.GetRequiredService<AppDbContext>();
                        return await dbContext.Users.FindAsync(id);
                    }
                }
                ```

                **Injecting transient disposable services without scoping:**

                ```csharp
                // BAD: Transient IDisposable services may not be disposed properly
                [QueryType]
                public static class Queries
                {
                    public static async Task<Data> GetData(
                        [Service] HttpClient client, // Transient HttpClient — may leak
                        CancellationToken ct)
                    {
                        var response = await client.GetAsync("/api/data", ct);
                        return await response.Content.ReadFromJsonAsync<Data>(ct);
                    }
                }
                // Use IHttpClientFactory instead
                ```

                **Injecting too many services into a single resolver:**

                ```csharp
                // BAD: Too many dependencies suggest the resolver is doing too much
                public static async Task<Result> DoEverythingAsync(
                    Input input,
                    [Service] IServiceA a,
                    [Service] IServiceB b,
                    [Service] IServiceC c,
                    [Service] IServiceD d,
                    [Service] IServiceE e,
                    CancellationToken ct)
                {
                    // This resolver has too many responsibilities
                }
                ```

                ## Key Points

                - Use method parameter injection as the primary DI mechanism in resolvers
                - Use `[Service]` attribute when you need to disambiguate from GraphQL arguments
                - Register `DbContext` with `RegisterDbContextFactory<T>()` for proper scoping
                - DataLoaders are injectable directly — no `[Service]` attribute needed
                - `CancellationToken` is always available as a parameter
                - Keep resolvers thin — if you need many services, the resolver may have too many responsibilities

                ## Related Practices

                - [resolvers-field] — For general resolver patterns
                - [dataloader-service-scope] — For DataLoader DI scoping
                - [configuration-server-setup] — For service registration
                """
            });

        docs.Add(
            new BestPracticeDocument
            {
                Id = "resolvers-field",
                Title = "Implementing Field Resolvers",
                Category = BestPracticeCategory.Resolvers,
                Tags = ["hot-chocolate-16", "code-first"],
                Styles = ["all"],
                Keywords = "field resolver computed property virtual calculated derived extension type",
                Abstract =
                    "How to implement field resolvers using pure functions, static methods on type extensions, and method-style resolvers. Covers the Pure resolver optimization.",
                Body = """
                # Implementing Field Resolvers

                ## When to Use

                Field resolvers are the fundamental building blocks of a GraphQL server. Every field in your schema is backed by a resolver that produces its value. Use explicit resolvers when a field's value:

                - Is computed from other fields or external data
                - Requires fetching from a database or external service
                - Needs to transform the parent object's data
                - Depends on injected services

                For simple property access (e.g., `user.Name` maps to `User.Name`), Hot Chocolate generates resolvers automatically. You only need to write explicit resolvers for computed or dynamic fields.

                ## Implementation

                ### Type Extension Resolvers (Recommended)

                Define resolvers as static methods on type extension classes:

                ```csharp
                [ObjectType<User>]
                public static partial class UserType
                {
                    public static string GetFullName([Parent] User user)
                        => $"{user.FirstName} {user.LastName}";

                    public static async Task<IEnumerable<Order>> GetOrdersAsync(
                        [Parent] User user,
                        IOrdersByUserIdDataLoader dataLoader,
                        CancellationToken cancellationToken)
                    {
                        return await dataLoader.LoadAsync(user.Id, cancellationToken);
                    }
                }
                ```

                ### Pure Resolvers

                Pure resolvers are an optimization for fields that only depend on the parent object and have no side effects. Mark them with `[NodeResolver]` or let the source generator detect purity automatically:

                ```csharp
                [ObjectType<User>]
                public static partial class UserType
                {
                    // This is automatically detected as pure because it only
                    // uses [Parent] and has no other parameters
                    public static string GetDisplayName([Parent] User user)
                        => user.Name.ToUpperInvariant();

                    // Also pure — static computation with no service dependencies
                    public static int GetNameLength([Parent] User user)
                        => user.Name.Length;
                }
                ```

                Pure resolvers skip the async state machine and resolver middleware pipeline overhead, providing better performance for simple computed fields.

                ### Query Root Resolvers

                ```csharp
                [QueryType]
                public static class UserQueries
                {
                    public static async Task<User?> GetUserByIdAsync(
                        int id,
                        IUserByIdDataLoader dataLoader,
                        CancellationToken cancellationToken)
                    {
                        return await dataLoader.LoadAsync(id, cancellationToken);
                    }

                    [UsePaging]
                    [UseFiltering]
                    [UseSorting]
                    public static IQueryable<User> GetUsers(AppDbContext dbContext)
                        => dbContext.Users;
                }
                ```

                ### Mutation Resolvers

                ```csharp
                [MutationType]
                public static class UserMutations
                {
                    public static async Task<User> CreateUserAsync(
                        CreateUserInput input,
                        AppDbContext dbContext,
                        CancellationToken cancellationToken)
                    {
                        var user = new User { Name = input.Name, Email = input.Email };
                        dbContext.Users.Add(user);
                        await dbContext.SaveChangesAsync(cancellationToken);
                        return user;
                    }
                }
                ```

                ## Anti-patterns

                **Instance methods on type extensions:**

                ```csharp
                // BAD: Type extensions should use static methods
                [ObjectType<User>]
                public partial class UserType  // Not static
                {
                    public string GetFullName([Parent] User user) => user.Name;
                }
                ```

                **Blocking async calls:**

                ```csharp
                // BAD: .Result blocks the thread and can cause deadlocks
                [ObjectType<User>]
                public static partial class UserType
                {
                    public static Order GetLatestOrder([Parent] User user,
                        IOrdersByUserIdDataLoader loader)
                    {
                        var orders = loader.LoadAsync(user.Id).Result; // Deadlock risk!
                        return orders.First();
                    }
                }
                ```

                **Heavy computation in resolvers:**

                ```csharp
                // BAD: CPU-intensive work blocks the execution engine
                [ObjectType<User>]
                public static partial class UserType
                {
                    public static string GetPasswordHash([Parent] User user)
                        => BCrypt.HashPassword(user.Password, 12); // Blocks thread pool
                }
                ```

                ## Key Points

                - Use static methods on `[ObjectType<T>]` type extension classes as the standard resolver pattern
                - The `[Parent]` parameter gives access to the parent object
                - Pure resolvers (no service dependencies, only `[Parent]`) are automatically optimized
                - Always use async/await for I/O operations — never use `.Result` or `.Wait()`
                - Use `[QueryType]`, `[MutationType]`, and `[SubscriptionType]` for root type resolvers
                - Resolvers should be thin — delegate business logic to services and data access to DataLoaders

                ## Related Practices

                - [resolvers-parent] — For accessing parent data
                - [resolvers-di] — For injecting services
                - [resolvers-async] — For async patterns
                - [defining-types-object] — For type definitions
                """
            });

        docs.Add(
            new BestPracticeDocument
            {
                Id = "resolvers-global-state",
                Title = "Global State and Local State in Resolvers",
                Category = BestPracticeCategory.Resolvers,
                Tags = ["hot-chocolate-16", "code-first"],
                Styles = ["all"],
                Keywords = "global state request context shared data custom context property HttpContext claims user tenant",
                Abstract =
                    "How to pass data between middleware and resolvers using IResolverContext.ContextData (global state) and IResolverContext.LocalContextData (scoped state).",
                Body = """
                # Global State and Local State in Resolvers

                ## When to Use

                Use context data when you need to pass information between middleware components and resolvers, or between different resolvers within the same request. This is commonly needed for:

                - Passing authentication/authorization information from middleware to resolvers
                - Sharing computed values across multiple resolvers in the same request
                - Propagating request-level metadata (e.g., tenant ID, correlation ID)

                Hot Chocolate provides two scoping levels:
                - **Global state** (`ContextData`): Shared across all resolvers in the entire request
                - **Local state** (`ScopedContextData`): Scoped to the current resolver and its children

                ## Implementation

                ### Setting Global State in Middleware

                ```csharp
                public class TenantMiddleware
                {
                    private readonly RequestDelegate _next;

                    public TenantMiddleware(RequestDelegate next)
                    {
                        _next = next;
                    }

                    public async ValueTask InvokeAsync(IRequestContext context)
                    {
                        var httpContext = context.Services
                            .GetRequiredService<IHttpContextAccessor>()
                            .HttpContext!;

                        var tenantId = httpContext.Request.Headers["X-Tenant-Id"].ToString();

                        context.ContextData["TenantId"] = tenantId;

                        await _next(context);
                    }
                }
                ```

                ### Reading Global State in Resolvers

                Use `[GlobalState]` to access global state from the request context:

                ```csharp
                [QueryType]
                public static class ProductQueries
                {
                    [UsePaging]
                    public static IQueryable<Product> GetProducts(
                        [GlobalState("TenantId")] string tenantId,
                        AppDbContext dbContext)
                    {
                        return dbContext.Products.Where(p => p.TenantId == tenantId);
                    }
                }
                ```

                ### Using Scoped Context Data

                Scoped context data flows from a parent resolver to its children but is not visible to sibling or ancestor resolvers:

                ```csharp
                [ObjectType<Order>]
                public static partial class OrderType
                {
                    public static async Task<OrderSummary> GetSummaryAsync(
                        [Parent] Order order,
                        IResolverContext context,
                        [Service] IPricingService pricing,
                        CancellationToken cancellationToken)
                    {
                        var summary = await pricing.CalculateAsync(order, cancellationToken);

                        // Set scoped data visible to child resolvers of this field
                        context.ScopedContextData = context.ScopedContextData.SetItem(
                            "Currency", summary.Currency);

                        return summary;
                    }
                }

                [ObjectType<OrderSummary>]
                public static partial class OrderSummaryType
                {
                    public static string GetFormattedTotal(
                        [Parent] OrderSummary summary,
                        [ScopedState("Currency")] string currency)
                    {
                        return $"{summary.Total:N2} {currency}";
                    }
                }
                ```

                ### Setting Request-Level State

                ```csharp
                // In your ASP.NET Core middleware or GraphQL request middleware
                builder.Services
                    .AddGraphQLServer()
                    .AddQueryType()
                    .AddTypes()
                    .UseRequest(next => async context =>
                    {
                        context.ContextData["RequestTimestamp"] = DateTimeOffset.UtcNow;
                        await next(context);
                    });
                ```

                ## Anti-patterns

                **Using static fields for request state:**

                ```csharp
                // BAD: Static state is shared across all requests — race conditions
                public static class RequestState
                {
                    public static string? CurrentTenantId; // Shared across threads!
                }
                ```

                **Overusing global state for everything:**

                ```csharp
                // BAD: Passing data through global state when it should be a parameter
                context.ContextData["UserName"] = user.Name;
                context.ContextData["UserEmail"] = user.Email;
                context.ContextData["UserRole"] = user.Role;
                // Just pass the User object through the graph instead
                ```

                **Mutating scoped state from sibling resolvers:**

                ```csharp
                // BAD: Scoped state should flow parent → child, not between siblings
                [ObjectType<Order>]
                public static partial class OrderType
                {
                    public static decimal GetSubtotal([Parent] Order order, IResolverContext ctx)
                    {
                        var subtotal = order.Items.Sum(i => i.Price);
                        // This won't be visible to sibling fields like GetTax
                        ctx.ScopedContextData = ctx.ScopedContextData.SetItem("Subtotal", subtotal);
                        return subtotal;
                    }
                }
                ```

                ## Key Points

                - Use `[GlobalState("key")]` for request-wide data set by middleware (tenant ID, auth info)
                - Use `[ScopedState("key")]` for parent-to-child data flow within the resolver graph
                - Global state is mutable and shared across all resolvers in the request
                - Scoped state uses an immutable dictionary pattern — set it via `ScopedContextData.SetItem()`
                - Prefer passing data through the graph (parent objects, DataLoaders) over context state
                - Never use static fields or `AsyncLocal<T>` for request state — use the built-in context mechanisms

                ## Related Practices

                - [middleware-request] — For request middleware that sets global state
                - [middleware-field] — For field middleware with local state
                - [resolvers-di] — For dependency injection alternatives
                """
            });

        docs.Add(
            new BestPracticeDocument
            {
                Id = "resolvers-parent",
                Title = "Accessing the Parent Object in Resolvers",
                Category = BestPracticeCategory.Resolvers,
                Tags = ["hot-chocolate-16", "code-first"],
                Styles = ["all"],
                Keywords = "parent object hierarchy nested context chain parent reference accessor attribute",
                Abstract =
                    "How to access parent object data in type extension resolvers using [Parent] and when to use different parent access patterns.",
                Body = """
                # Accessing the Parent Object in Resolvers

                ## When to Use

                Use `[Parent]` in type extension resolvers whenever you need access to the object that owns the field being resolved. Every field in a type extension is resolved in the context of a parent object instance.

                The parent is the object type that the type extension extends. For example, in `[ObjectType<User>]`, the parent is a `User` instance. The parent is provided by the parent resolver in the graph (e.g., a query that returns a `User`).

                ## Implementation

                ### Basic [Parent] Access

                ```csharp
                [ObjectType<User>]
                public static partial class UserType
                {
                    public static string GetFullName([Parent] User user)
                        => $"{user.FirstName} {user.LastName}";

                    public static async Task<Department?> GetDepartmentAsync(
                        [Parent] User user,
                        IDepartmentByIdDataLoader dataLoader,
                        CancellationToken cancellationToken)
                    {
                        return await dataLoader.LoadAsync(user.DepartmentId, cancellationToken);
                    }
                }
                ```

                ### Using Parent Keys for DataLoader Calls

                The most common pattern is extracting a foreign key from the parent to feed into a DataLoader:

                ```csharp
                [ObjectType<Order>]
                public static partial class OrderType
                {
                    public static async Task<Customer?> GetCustomerAsync(
                        [Parent] Order order,
                        ICustomerByIdDataLoader dataLoader,
                        CancellationToken cancellationToken)
                    {
                        return await dataLoader.LoadAsync(order.CustomerId, cancellationToken);
                    }

                    public static async Task<IEnumerable<OrderItem>> GetItemsAsync(
                        [Parent] Order order,
                        IOrderItemsByOrderIdDataLoader dataLoader,
                        CancellationToken cancellationToken)
                    {
                        return await dataLoader.LoadAsync(order.Id, cancellationToken);
                    }
                }
                ```

                ### Computed Fields from Parent Properties

                ```csharp
                [ObjectType<Product>]
                public static partial class ProductType
                {
                    public static decimal GetPriceWithTax([Parent] Product product)
                        => product.Price * 1.20m;

                    public static bool GetIsAvailable([Parent] Product product)
                        => product.StockCount > 0 && product.IsActive;

                    public static string GetSlug([Parent] Product product)
                        => product.Name.ToLowerInvariant().Replace(' ', '-');
                }
                ```

                ### Conditional Resolution Based on Parent State

                ```csharp
                [ObjectType<User>]
                public static partial class UserType
                {
                    public static async Task<IEnumerable<Permission>> GetPermissionsAsync(
                        [Parent] User user,
                        IPermissionsByRoleDataLoader dataLoader,
                        CancellationToken cancellationToken)
                    {
                        if (user.RoleId is null)
                        {
                            return [];
                        }

                        return await dataLoader.LoadAsync(user.RoleId.Value, cancellationToken);
                    }
                }
                ```

                ## Anti-patterns

                **Fetching the parent again from the database:**

                ```csharp
                // BAD: The parent is already available — do not re-fetch it
                [ObjectType<User>]
                public static partial class UserType
                {
                    public static async Task<string> GetEmail(
                        [Parent] User user,
                        AppDbContext dbContext)
                    {
                        var freshUser = await dbContext.Users.FindAsync(user.Id);
                        return freshUser!.Email; // Unnecessary DB call
                    }
                }
                ```

                **Modifying the parent object in a resolver:**

                ```csharp
                // BAD: Resolvers should not mutate the parent object
                [ObjectType<User>]
                public static partial class UserType
                {
                    public static string GetDisplayName([Parent] User user)
                    {
                        user.Name = user.Name.Trim(); // Mutation! Side effect!
                        return user.Name;
                    }
                }
                ```

                **Using IResolverContext instead of [Parent]:**

                ```csharp
                // BAD: Verbose and loses type safety
                [ObjectType<User>]
                public static partial class UserType
                {
                    public static string GetFullName(IResolverContext context)
                    {
                        var user = context.Parent<User>();
                        return $"{user.FirstName} {user.LastName}";
                    }
                }
                ```

                ## Key Points

                - Use `[Parent]` to access the parent object in type extension resolvers
                - The parent parameter is typed to the class specified in `[ObjectType<T>]`
                - Extract foreign keys from the parent to pass into DataLoaders
                - Pure resolvers (only `[Parent]`, no services) are automatically optimized
                - Never re-fetch the parent from the database — it is already loaded
                - Never mutate the parent object — resolvers should be side-effect free

                ## Related Practices

                - [resolvers-field] — For field resolver patterns
                - [resolvers-di] — For injecting services alongside [Parent]
                - [defining-types-object] — For defining the type extensions
                """
            });
    }
}
