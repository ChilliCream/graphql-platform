---
title: "Building a Public GraphQL API"
---

If you are building a GraphQL API that external developers will consume, this guide walks through the configuration and design decisions that matter most. A public API is one where you publish a schema and cannot control what operations clients send. Think of APIs like GitHub's GraphQL API, where thousands of third-party applications issue queries you never anticipated.

This guide is opinionated. It covers schema design, pagination, cost analysis, introspection, authorization, and request limits, then ties everything together in a complete `Program.cs` you can use as a starting point. Each section links to the relevant reference page for full details.

# Start with a Solid Schema Design

Your schema is a contract. Once external developers build against it, changing or removing fields is a breaking change. Invest time in naming and documentation before you ship.

**Name fields and types clearly.** Use domain language that makes sense without reading your source code. Avoid abbreviations and internal jargon. A field called `orgMemberships` is harder to discover than `organizationMemberships`.

**Add descriptions to every type, field, and argument.** Public API consumers rely on introspection and tooling like [Nitro](/products/nitro) to explore your schema. A field without a description is a field that generates support tickets.

```csharp
// Types/Organization.cs
[GraphQLDescription("A company or group that owns repositories.")]
public class Organization
{
    [GraphQLDescription("The unique login handle for this organization.")]
    public string Login { get; set; }

    [GraphQLDescription("The display name of the organization.")]
    public string? Name { get; set; }
}
```

**Plan for deprecation from day one.** Use `@deprecated` to phase out fields and `@requiresOptIn` to gate experimental features. Never remove a field without a deprecation period.

[Learn more about schema documentation](/docs/hotchocolate/v16/building-a-schema/documentation)

[Learn more about versioning and deprecation](/docs/hotchocolate/v16/building-a-schema/versioning)

# Use Cursor-Based Pagination for All Lists

Every list field that could grow beyond a handful of items should be a connection. Connections give clients a standardized way to page through results, and they give you control over how much data a single request can fetch.

```csharp
// Types/OrganizationQueries.cs
[QueryType]
public static partial class OrganizationQueries
{
    [UsePaging(MaxPageSize = 100, DefaultPageSize = 25)]
    public static IQueryable<Organization> GetOrganizations(AppDbContext db)
        => db.Organizations.OrderBy(o => o.Login);
}
```

Set `MaxPageSize` deliberately. This value is the upper bound on how many items a client can request in a single page, and it feeds directly into cost analysis. A `MaxPageSize` of 100 means cost analysis assumes up to 100 nodes per page when calculating query cost. Lower values give you tighter cost budgets.

For public APIs, require clients to specify how many items they want by enabling `RequirePagingBoundaries`. Without this, clients that omit `first` or `last` still get results, but cost analysis has to assume the worst case.

```csharp
// Program.cs
builder.Services
    .AddGraphQLServer()
    .ModifyPagingOptions(opt =>
    {
        opt.MaxPageSize = 100;
        opt.DefaultPageSize = 25;
        opt.RequirePagingBoundaries = true;
    });
```

[Learn more about pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination)

# Configure Cost Analysis

Cost analysis is the most important security layer for a public GraphQL API. It calculates the cost of every query before execution and rejects queries that exceed your budget. Without it, a single deeply nested query can consume unbounded server resources.

Hot Chocolate enables cost analysis by default. The default limits (`MaxFieldCost = 1000`, `MaxTypeCost = 1000`) work as a starting point, but you should tune them based on your schema and expected query patterns.

```csharp
// Program.cs
builder.Services
    .AddGraphQLServer()
    .ModifyCostOptions(options =>
    {
        options.MaxFieldCost = 5_000;
        options.MaxTypeCost = 5_000;
        options.EnforceCostLimits = true;
    });
```

## Default Weights

Hot Chocolate assigns default cost weights automatically:

- **Async resolvers** (fields that hit a database or service): weight `10`
- **Composite types** (object fields that resolve synchronously): weight `1`
- **Scalars**: weight `0`

For paginated fields, these weights multiply by the page size. A resolver with weight `10` inside a connection with `MaxPageSize = 50` contributes `10 x 50 = 500` to the field cost.

## Annotate Expensive Fields

If a resolver calls an external API, runs a complex computation, or triggers a database-heavy operation, increase its cost weight:

```csharp
// Types/ReportQueries.cs
[QueryType]
public static partial class ReportQueries
{
    [Cost(50)]
    public static async Task<SalesReport> GetSalesReportAsync(
        DateOnly from,
        DateOnly to,
        ReportService reports,
        CancellationToken ct)
        => await reports.GenerateAsync(from, to, ct);
}
```

For list fields where you know the typical size differs from the default, use `[ListSize]` to give the analyzer a more accurate estimate:

```csharp
// Types/OrganizationNode.cs
[ObjectType<Organization>]
public static partial class OrganizationNode
{
    [UsePaging(MaxPageSize = 10)]
    [ListSize(AssumedSize = 10, SlicingArguments = ["first", "last"],
        SizedFields = ["edges", "nodes"])]
    public static IQueryable<Team> GetTeams(
        [Parent] Organization org, AppDbContext db)
        => db.Teams.Where(t => t.OrganizationId == org.Id);
}
```

## Test with the Cost Header

Use the `GraphQL-Cost: report` HTTP header to see the cost of any query without changing enforcement. Send your most complex expected queries and verify they fall within your limits before deploying.

[Learn more about cost analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis)

# Control Introspection

Introspection lets anyone discover every type, field, and argument in your schema. For a public API, you have two options:

**Option A: Keep introspection enabled.** If your API is meant to be discovered and you publish documentation, introspection is a feature, not a risk. Cost analysis already protects you from expensive introspection queries.

**Option B: Restrict introspection in production.** If you prefer to control schema discovery, disable introspection and allow it only for authorized requests:

```csharp
// Program.cs
builder.Services
    .AddGraphQLServer()
    .AllowIntrospection(builder.Environment.IsDevelopment());
```

For a more granular approach, use a request interceptor to allow introspection based on authentication or a specific header:

```csharp
// Interceptors/IntrospectionInterceptor.cs
public class IntrospectionInterceptor : DefaultHttpRequestInterceptor
{
    public override ValueTask OnCreateAsync(HttpContext context,
        IRequestExecutor requestExecutor, OperationRequestBuilder requestBuilder,
        CancellationToken cancellationToken)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            requestBuilder.AllowIntrospection();
        }

        return base.OnCreateAsync(context, requestExecutor, requestBuilder,
            cancellationToken);
    }
}
```

```csharp
// Program.cs
builder.Services
    .AddGraphQLServer()
    .AllowIntrospection(false)
    .AddHttpRequestInterceptor<IntrospectionInterceptor>();
```

[Learn more about introspection](/docs/hotchocolate/v16/securing-your-api/introspection)

# Set Up Authorization

Most public APIs have fields that require authentication or specific permissions. Use the `[Authorize]` attribute to protect sensitive types and fields.

```csharp
// Types/ViewerQueries.cs
[QueryType]
public static partial class ViewerQueries
{
    [Authorize]
    public static async Task<User?> GetViewerAsync(
        ClaimsPrincipal claimsPrincipal,
        UserService users,
        CancellationToken ct)
    {
        var userId = claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
        return userId is not null ? await users.GetByIdAsync(userId, ct) : null;
    }
}
```

For role-based access:

```csharp
// Types/AdminQueries.cs
[QueryType]
public static partial class AdminQueries
{
    [Authorize(Roles = ["Administrator"])]
    public static async Task<AuditLog[]> GetAuditLogsAsync(
        AuditService audits, CancellationToken ct)
        => await audits.GetRecentAsync(ct);
}
```

For policy-based access, define policies in your service configuration:

```csharp
// Program.cs
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanReadBilling", policy =>
        policy.RequireClaim("scope", "billing:read"));
});
```

Then apply them to fields:

```csharp
// Types/BillingNode.cs
[ObjectType<Organization>]
public static partial class BillingNode
{
    [Authorize(Policy = "CanReadBilling")]
    public static async Task<BillingInfo?> GetBillingAsync(
        [Parent] Organization org,
        BillingService billing,
        CancellationToken ct)
        => await billing.GetForOrgAsync(org.Id, ct);
}
```

Use `HotChocolate.Authorization.AuthorizeAttribute`, not the Microsoft one. The Microsoft attribute does not integrate with the Hot Chocolate authorization pipeline.

[Learn more about authorization](/docs/hotchocolate/v16/securing-your-api/authorization)

# Rate Limiting and Depth Limits

Cost analysis handles query complexity, but you also want to limit how many requests a client can send and how deeply nested a query can be.

## Max Execution Depth

Set a maximum query depth to reject pathologically nested queries before cost analysis even runs:

```csharp
// Program.cs
builder.Services
    .AddGraphQLServer()
    .AddMaxExecutionDepthRule(15);
```

Choose a depth that accommodates your deepest legitimate query path. For most APIs, a depth of 10 to 20 is reasonable.

## ASP.NET Core Rate Limiting

Combine Hot Chocolate's query-level protections with ASP.NET Core's rate limiting middleware to limit requests per client:

```csharp
// Program.cs
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("graphql", opt =>
    {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromMinutes(1);
    });
});

// ...

app.UseRateLimiter();

app.UseEndpoints(endpoints =>
{
    endpoints.MapGraphQL().RequireRateLimiting("graphql");
});
```

Rate limiting and cost analysis complement each other. Rate limiting caps the number of requests. Cost analysis caps the complexity of each request. Together, they bound the total work your server does for any client.

# Disable Request Batching

Request batching allows a client to send multiple GraphQL operations in a single HTTP request. For internal APIs where you trust the client, this can improve performance. For public APIs, batching lets a client bypass your per-request rate limits by packing many expensive operations into one request.

In Hot Chocolate v16, request batching is disabled by default. If you have explicitly enabled it, disable it for your public API:

```csharp
// Program.cs
builder.Services
    .AddGraphQLServer()
    .ModifyRequestOptions(opt => opt.AllowedBatchOperations = AllowedBatchOperations.None);
```

# Putting It All Together

Here is a complete `Program.cs` that combines all the configuration from this guide into one starting point:

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Authentication (configure for your identity provider)
builder.Services
    .AddAuthentication("Bearer")
    .AddJwtBearer();

// Authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanReadBilling", policy =>
        policy.RequireClaim("scope", "billing:read"));
});

// Rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("graphql", opt =>
    {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromMinutes(1);
    });
});

// GraphQL server
builder.Services
    .AddGraphQLServer()
    .AddAuthorization()
    .AddMaxExecutionDepthRule(15)
    .ModifyPagingOptions(opt =>
    {
        opt.MaxPageSize = 100;
        opt.DefaultPageSize = 25;
        opt.RequirePagingBoundaries = true;
    })
    .ModifyCostOptions(options =>
    {
        options.MaxFieldCost = 5_000;
        options.MaxTypeCost = 5_000;
        options.EnforceCostLimits = true;
    })
    .ModifyRequestOptions(opt =>
        opt.AllowedBatchOperations = AllowedBatchOperations.None)
    .AllowIntrospection(builder.Environment.IsDevelopment())
    .AddTypes();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.UseEndpoints(endpoints =>
{
    endpoints.MapGraphQL().RequireRateLimiting("graphql");
});

app.Run();
```

Adjust the specific values (`MaxPageSize`, `MaxFieldCost`, `MaxTypeCost`, depth limit, rate limit window) to match your schema and infrastructure. Use the `GraphQL-Cost: report` header to measure real query costs and tune from there.

# Troubleshooting

## Legitimate client queries are rejected by cost analysis

Send the query with the `GraphQL-Cost: report` header to inspect its field cost and type cost. Common causes: the query fans out across multiple paginated fields, or a resolver has a high default cost. Either increase `MaxFieldCost`/`MaxTypeCost` or reduce `MaxPageSize` on the specific fields that cause the fan-out.

## Clients receive "first or last argument required" errors

This happens when `RequirePagingBoundaries` is enabled and the client does not specify `first` or `last` on a paginated field. This is the intended behavior for public APIs. Document the requirement in your API guide and include `first` in your example queries.

## Authorization errors for authenticated users

Verify you are using `HotChocolate.Authorization.AuthorizeAttribute`, not `Microsoft.AspNetCore.Authorization.AuthorizeAttribute`. Also check that `AddAuthorization()` is called on both `IServiceCollection` and `IRequestExecutorBuilder`, and that `UseAuthentication()` comes before `UseAuthorization()` in the middleware pipeline.

## Introspection works in development but not in production

If you used `AllowIntrospection(builder.Environment.IsDevelopment())`, introspection is disabled in all non-development environments. This is typically what you want. If you need introspection in production for specific clients, use a request interceptor to allow it based on authentication.

## Rate limiting does not seem to apply

Ensure `app.UseRateLimiter()` is called before `app.UseEndpoints()` in the middleware pipeline, and that `RequireRateLimiting("graphql")` is chained onto `MapGraphQL()`. Also verify that the rate limiter policy name matches between `AddFixedWindowLimiter` and `RequireRateLimiting`.

# Next Steps

- **Cost analysis reference:** [Cost Analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis) covers all options, custom weights, filtering and sorting costs, and the tuning guide.
- **Authorization reference:** [Authorization](/docs/hotchocolate/v16/securing-your-api/authorization) covers roles, policies, global authorization, and accessing `IResolverContext` in handlers.
- **Pagination reference:** [Pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination) covers the `Connection<T>` type, total counts, extending connection types, and pagination providers.
- **Schema documentation:** [Documentation](/docs/hotchocolate/v16/building-a-schema/documentation) covers `[GraphQLDescription]`, XML docs, and priority order.
- **Schema versioning:** [Versioning](/docs/hotchocolate/v16/building-a-schema/versioning) covers `@deprecated`, `@requiresOptIn`, and feature stability.
- **Introspection:** [Introspection](/docs/hotchocolate/v16/securing-your-api/introspection) covers disabling, allowlisting, and custom error messages.
- **Trusted documents:** If you later add first-party clients that you control, [Trusted Documents](/docs/hotchocolate/v16/performance/trusted-documents) let you bypass cost analysis for pre-approved operations.
