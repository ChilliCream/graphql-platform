using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.BestPractices.Models;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.BestPractices;

internal static partial class BestPracticeData
{
    private static void AddSecurityDocuments(List<BestPracticeDocument> docs)
    {
        docs.Add(
            new BestPracticeDocument
            {
                Id = "security-authorization",
                Title = "Authorization with @authorize and Policies",
                Category = BestPracticeCategory.Security,
                Tags = ["hot-chocolate-16"],
                Styles = ["all"],
                Keywords = "auth authorization authenticate permission role policy claim protect guard login access",
                Abstract =
                    "How to implement field-level and type-level authorization using [Authorize], policy-based authorization, and resource-based authorization in Hot Chocolate.",
                Body = """
                # Authorization with @authorize and Policies

                ## When to Use

                Use GraphQL authorization when you need to restrict access to fields, types, or operations based on the authenticated user's identity or permissions. Hot Chocolate integrates with ASP.NET Core's authorization system, supporting:

                - Simple `[Authorize]` for authenticated-only access
                - Policy-based authorization for role or claim checks
                - Field-level authorization for fine-grained access control
                - Type-level authorization for restricting entire types

                ## Implementation

                ### Setup Authorization

                ```csharp
                var builder = WebApplication.CreateBuilder(args);

                builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options =>
                    {
                        options.Authority = builder.Configuration["Auth:Authority"];
                        options.Audience = builder.Configuration["Auth:Audience"];
                    });

                builder.Services.AddAuthorization(options =>
                {
                    options.AddPolicy("IsAdmin", policy =>
                        policy.RequireRole("Admin"));

                    options.AddPolicy("CanManageUsers", policy =>
                        policy.RequireClaim("permission", "users:manage"));
                });

                builder.Services
                    .AddGraphQLServer()
                    .AddQueryType()
                    .AddMutationType()
                    .AddTypes()
                    .AddAuthorization();

                var app = builder.Build();

                app.UseAuthentication();
                app.UseAuthorization();
                app.MapGraphQL();

                app.Run();
                ```

                ### Authenticated-Only Fields

                ```csharp
                [QueryType]
                public static class Query
                {
                    [Authorize]
                    public static async Task<User> GetMeAsync(
                        [GlobalState("UserId")] string userId,
                        IUserByIdDataLoader loader,
                        CancellationToken ct)
                    {
                        return (await loader.LoadAsync(int.Parse(userId), ct))!;
                    }
                }
                ```

                ### Policy-Based Authorization

                ```csharp
                [QueryType]
                public static class AdminQueries
                {
                    [Authorize(Policy = "IsAdmin")]
                    public static IQueryable<User> GetAllUsers(AppDbContext dbContext)
                        => dbContext.Users;
                }

                [MutationType]
                public static class UserMutations
                {
                    [Authorize(Policy = "CanManageUsers")]
                    public static async Task<User> CreateUserAsync(
                        CreateUserInput input,
                        AppDbContext dbContext,
                        CancellationToken ct)
                    {
                        var user = new User { Name = input.Name, Email = input.Email };
                        dbContext.Users.Add(user);
                        await dbContext.SaveChangesAsync(ct);
                        return user;
                    }
                }
                ```

                ### Type-Level Authorization

                ```csharp
                [Authorize(Policy = "IsAdmin")]
                public class AdminSettings
                {
                    public string SmtpServer { get; set; } = default!;
                    public int MaxConcurrentUsers { get; set; }
                    public bool MaintenanceMode { get; set; }
                }
                ```

                ### Field-Level Authorization on Type Extensions

                ```csharp
                [ObjectType<User>]
                public static partial class UserType
                {
                    // Public: anyone can see the name
                    public static string GetDisplayName([Parent] User user)
                        => user.Name;

                    // Restricted: only the user themselves or admins can see the email
                    [Authorize(Policy = "IsAdminOrSelf")]
                    public static string GetEmail([Parent] User user)
                        => user.Email;

                    // Restricted: only admins
                    [Authorize(Policy = "IsAdmin")]
                    public static DateTime GetLastLoginAt([Parent] User user)
                        => user.LastLoginAt;
                }
                ```

                ### Role-Based Authorization

                ```csharp
                [QueryType]
                public static class Query
                {
                    [Authorize(Roles = ["Admin", "Manager"])]
                    public static IQueryable<AuditLog> GetAuditLogs(AppDbContext db)
                        => db.AuditLogs;
                }
                ```

                ## Anti-patterns

                **Checking authorization manually in every resolver:**

                ```csharp
                // BAD: Manual auth checks are repetitive and error-prone
                public static async Task<User> GetUser(
                    int id,
                    IResolverContext context,
                    IUserByIdDataLoader loader,
                    CancellationToken ct)
                {
                    var user = context.GetUser();
                    if (!user.IsInRole("Admin"))
                        throw new UnauthorizedAccessException();

                    return await loader.LoadAsync(id, ct);
                }
                // Use [Authorize] attribute instead
                ```

                **Forgetting to call AddAuthorization on the GraphQL server:**

                ```csharp
                // BAD: [Authorize] attributes are silently ignored without AddAuthorization()
                builder.Services
                    .AddGraphQLServer()
                    .AddQueryType()
                    .AddTypes();
                // Missing: .AddAuthorization()
                ```

                **Over-restricting at the type level:**

                ```csharp
                // BAD: Type-level [Authorize] blocks ALL fields for non-admin users
                // even if some fields should be public
                [Authorize(Policy = "IsAdmin")]
                public class User
                {
                    public string Name { get; set; }  // Should be public!
                    public string Email { get; set; } // Should be restricted
                }
                // Use field-level authorization for granular control
                ```

                ## Key Points

                - Call `AddAuthorization()` on the GraphQL server builder to enable authorization
                - Use `[Authorize]` for simple authenticated-only access
                - Use `[Authorize(Policy = "...")]` for policy-based authorization
                - Use `[Authorize(Roles = [...])]` for role-based authorization
                - Apply authorization at the field level for granular control
                - Set up ASP.NET Core authentication and authorization middleware before `MapGraphQL()`
                - Authorization errors return null for the field and an error in the response

                ## Related Practices

                - [security-production-hardening] — For production security settings
                - [middleware-request] — For request-level auth middleware
                - [resolvers-global-state] — For passing auth state to resolvers
                """
            });

        docs.Add(
            new BestPracticeDocument
            {
                Id = "security-production-hardening",
                Title = "Production Security Hardening",
                Category = BestPracticeCategory.Security,
                Tags = ["hot-chocolate-16"],
                Styles = ["all"],
                Keywords = "security hardening production introspection depth complexity rate limit CORS headers protection",
                Abstract =
                    "How to configure Hot Chocolate for production: disable introspection, enable query depth/complexity limits, configure persisted queries, and restrict ad-hoc queries.",
                Body = """
                # Production Security Hardening

                ## When to Use

                Apply production security hardening before deploying your GraphQL API to production. Unlike REST APIs where endpoints are predefined, GraphQL allows clients to craft arbitrary queries, making it essential to limit what queries can do.

                These settings protect against:
                - Schema reconnaissance via introspection
                - Denial of service via deeply nested or overly complex queries
                - Resource exhaustion from unbounded queries
                - Unknown query execution when using persisted queries

                ## Implementation

                ### Disable Introspection

                ```csharp
                builder.Services
                    .AddGraphQLServer()
                    .AddQueryType()
                    .AddTypes()
                    .DisableIntrospection(!builder.Environment.IsDevelopment());
                ```

                Or use a more fine-grained approach:

                ```csharp
                builder.Services
                    .AddGraphQLServer()
                    .AddQueryType()
                    .AddTypes()
                    .ConfigureSchemaServices(s =>
                    {
                        if (!builder.Environment.IsDevelopment())
                        {
                            s.Configure<RequestExecutorOptions>(o =>
                            {
                                o.Introspection.Allowed = false;
                            });
                        }
                    });
                ```

                ### Query Depth and Complexity Limits

                ```csharp
                builder.Services
                    .AddGraphQLServer()
                    .AddQueryType()
                    .AddTypes()
                    .AddMaxExecutionDepthRule(15)
                    .SetRequestOptions(o =>
                    {
                        o.Complexity.Enable = true;
                        o.Complexity.MaximumAllowed = 1000;
                    });
                ```

                ### Persisted Queries

                Persisted queries allow only pre-registered queries to execute in production:

                ```csharp
                builder.Services
                    .AddGraphQLServer()
                    .AddQueryType()
                    .AddTypes()
                    .UsePersistedQueryPipeline()
                    .AddFileSystemQueryStorage("./persisted-queries");
                ```

                Block ad-hoc queries in production:

                ```csharp
                if (!builder.Environment.IsDevelopment())
                {
                    builder.Services
                        .AddGraphQLServer()
                        .AllowDocumentOnlyPersistedQueries();
                }
                ```

                ### Request Timeout

                ```csharp
                builder.Services
                    .AddGraphQLServer()
                    .AddQueryType()
                    .AddTypes()
                    .ModifyRequestOptions(o =>
                    {
                        o.ExecutionTimeout = TimeSpan.FromSeconds(30);
                    });
                ```

                ### Complete Production Configuration

                ```csharp
                var graphql = builder.Services
                    .AddGraphQLServer()
                    .AddQueryType()
                    .AddMutationType()
                    .AddTypes()
                    .AddFiltering()
                    .AddSorting()
                    .AddMutationConventions();

                if (builder.Environment.IsProduction())
                {
                    graphql
                        .DisableIntrospection()
                        .AddMaxExecutionDepthRule(15)
                        .SetRequestOptions(o =>
                        {
                            o.Complexity.Enable = true;
                            o.Complexity.MaximumAllowed = 1000;
                        })
                        .ModifyRequestOptions(o =>
                        {
                            o.ExecutionTimeout = TimeSpan.FromSeconds(30);
                        });
                }
                ```

                ## Anti-patterns

                **Leaving introspection enabled in production:**

                ```csharp
                // BAD: Allows anyone to discover your entire schema
                builder.Services
                    .AddGraphQLServer()
                    .AddQueryType()
                    .AddTypes();
                // No DisableIntrospection() — schema is fully discoverable
                ```

                **No query complexity limits:**

                ```csharp
                // BAD: A malicious client can send a deeply nested query
                // that causes exponential resolver execution
                // query { users { orders { items { product { reviews { author { orders { ... } } } } } } } }
                ```

                **Setting limits too high to be effective:**

                ```csharp
                // BAD: Limits that are too high provide no real protection
                .AddMaxExecutionDepthRule(100)  // 100 levels deep is effectively unlimited
                .SetRequestOptions(o =>
                {
                    o.Complexity.MaximumAllowed = 100000;  // Too high to be useful
                });
                ```

                ## Key Points

                - Disable introspection in production to prevent schema discovery
                - Set query depth limits (`AddMaxExecutionDepthRule`) to prevent deeply nested queries
                - Enable query complexity analysis to prevent expensive queries
                - Use persisted queries in production to restrict which queries can execute
                - Set execution timeouts to prevent long-running queries from consuming resources
                - Apply all security settings conditionally based on the environment

                ## Related Practices

                - [security-authorization] — For field-level authorization
                - [error-handling-error-filters] — For stripping internal error details
                - [configuration-performance] — For performance-related settings
                """
            });
    }
}
