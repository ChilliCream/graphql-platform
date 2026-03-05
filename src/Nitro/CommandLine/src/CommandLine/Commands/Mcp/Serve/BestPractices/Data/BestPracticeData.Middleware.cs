using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.BestPractices.Models;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.BestPractices;

internal static partial class BestPracticeData
{
    private static void AddMiddlewareDocuments(List<BestPracticeDocument> docs)
    {
        docs.Add(
            new BestPracticeDocument
            {
                Id = "middleware-field",
                Title = "Field Middleware",
                Category = BestPracticeCategory.Middleware,
                Tags = ["hot-chocolate-16"],
                Styles = ["all"],
                Keywords = "field middleware intercept wrap pipeline resolver hook attribute decorator",
                Abstract =
                    "How to implement field middleware with Use and UseField. Explains the performance cost of broad field middleware and when field middleware is appropriate vs request middleware.",
                Body = """
                # Field Middleware

                ## When to Use

                Use field middleware when you need to intercept individual field resolutions. Field middleware wraps the resolver execution and can transform results, add validation, or implement cross-cutting concerns at the field level.

                Common use cases include:
                - Field-level authorization checks
                - Result transformation or formatting
                - Audit logging for specific fields
                - Custom caching per field

                Field middleware has a per-field performance cost, so it should be applied selectively. For request-level concerns that do not need per-field granularity, prefer request middleware.

                ## Implementation

                ### Attribute-Based Field Middleware

                Define a custom middleware attribute:

                ```csharp
                namespace MyApp.GraphQL.Middleware;

                public class LogFieldAttribute : ObjectFieldDescriptorAttribute
                {
                    protected override void OnConfigure(
                        IDescriptorContext context,
                        IObjectFieldDescriptor descriptor,
                        MemberInfo member)
                    {
                        descriptor.Use(next => async middlewareContext =>
                        {
                            var logger = middlewareContext.Services
                                .GetRequiredService<ILogger<LogFieldAttribute>>();

                            logger.LogInformation(
                                "Resolving field {TypeName}.{FieldName}",
                                middlewareContext.ObjectType.Name,
                                middlewareContext.Selection.Field.Name);

                            await next(middlewareContext);

                            logger.LogInformation(
                                "Resolved field {TypeName}.{FieldName}: {Result}",
                                middlewareContext.ObjectType.Name,
                                middlewareContext.Selection.Field.Name,
                                middlewareContext.Result);
                        });
                    }
                }
                ```

                Apply it to specific resolvers:

                ```csharp
                [QueryType]
                public static class Queries
                {
                    [LogField]
                    public static async Task<User?> GetUserAsync(
                        int id,
                        IUserByIdDataLoader loader,
                        CancellationToken ct)
                    {
                        return await loader.LoadAsync(id, ct);
                    }
                }
                ```

                ### Result Transformation Middleware

                ```csharp
                public class TrimStringAttribute : ObjectFieldDescriptorAttribute
                {
                    protected override void OnConfigure(
                        IDescriptorContext context,
                        IObjectFieldDescriptor descriptor,
                        MemberInfo member)
                    {
                        descriptor.Use(next => async middlewareContext =>
                        {
                            await next(middlewareContext);

                            if (middlewareContext.Result is string s)
                            {
                                middlewareContext.Result = s.Trim();
                            }
                        });
                    }
                }
                ```

                ### Global Field Middleware

                Apply middleware to all fields of a type using type interceptors:

                ```csharp
                public class AuditTypeInterceptor : TypeInterceptor
                {
                    public override void OnAfterCompleteType(
                        ITypeCompletionContext completionContext,
                        DefinitionBase definition)
                    {
                        if (definition is ObjectTypeDefinition objectDef &&
                            objectDef.Name == "Mutation")
                        {
                            foreach (var field in objectDef.Fields)
                            {
                                field.MiddlewareDefinitions.Insert(0,
                                    new FieldMiddlewareDefinition(next => async context =>
                                    {
                                        var logger = context.Services
                                            .GetRequiredService<ILogger<AuditTypeInterceptor>>();

                                        logger.LogInformation(
                                            "Mutation {Field} invoked by {User}",
                                            context.Selection.Field.Name,
                                            context.GetGlobalState<string>("UserId"));

                                        await next(context);
                                    }));
                            }
                        }
                    }
                }
                ```

                ## Anti-patterns

                **Applying field middleware to all fields:**

                ```csharp
                // BAD: Field middleware on every field adds overhead to every resolution
                // For 100 fields in a query, this runs 100 times
                builder.Services
                    .AddGraphQLServer()
                    .UseField(next => async context =>
                    {
                        Console.WriteLine($"Resolving {context.Selection.Field.Name}");
                        await next(context);
                    });
                ```

                **Heavy I/O in field middleware:**

                ```csharp
                // BAD: Database call in field middleware multiplies by every field
                descriptor.Use(next => async context =>
                {
                    // This runs for EVERY resolution of this field
                    var hasAccess = await dbContext.Permissions
                        .AnyAsync(p => p.FieldName == context.Selection.Field.Name);
                    if (!hasAccess) throw new UnauthorizedAccessException();
                    await next(context);
                });
                ```

                **Forgetting to call next:**

                ```csharp
                // BAD: Not calling next() stops the resolver from executing
                descriptor.Use(next => async context =>
                {
                    // Some logic here...
                    // Missing: await next(context);
                    // Result will be null!
                });
                ```

                ## Key Points

                - Field middleware wraps individual field resolutions using `descriptor.Use(next => ...)`
                - Apply field middleware selectively to specific fields — avoid global field middleware unless necessary
                - Field middleware has per-invocation cost — every time the field is resolved, the middleware runs
                - Always call `await next(context)` to execute the resolver and subsequent middleware
                - Access the result after `await next(context)` via `context.Result`
                - For request-level cross-cutting concerns, prefer request middleware over field middleware

                ## Related Practices

                - [middleware-request] — For request-level middleware
                - [security-authorization] — For authorization middleware
                - [resolvers-field] — For resolver implementations
                """
            });

        docs.Add(
            new BestPracticeDocument
            {
                Id = "middleware-request",
                Title = "Request Middleware Pipeline",
                Category = BestPracticeCategory.Middleware,
                Tags = ["hot-chocolate-16"],
                Styles = ["all"],
                Keywords = "request middleware pipeline HTTP context global interceptor logging tracing tenant",
                Abstract =
                    "How to implement and order request middleware using UseRequest, UseDefaultPipeline, and custom middleware components. Covers use cases and execution order.",
                Body = """
                # Request Middleware Pipeline

                ## When to Use

                Use request middleware when you need to intercept and process GraphQL requests at the request level, before or after the execution engine runs. Common use cases include:

                - Adding request-level logging and tracing
                - Setting global state (tenant ID, correlation ID) for resolvers
                - Implementing custom caching layers
                - Modifying the query document before execution
                - Adding custom error handling around the execution pipeline

                Request middleware operates on the entire GraphQL request, not individual fields. For field-level interception, use field middleware instead.

                ## Implementation

                ### Inline Request Middleware

                ```csharp
                builder.Services
                    .AddGraphQLServer()
                    .AddQueryType()
                    .AddTypes()
                    .UseRequest(next => async context =>
                    {
                        var stopwatch = Stopwatch.StartNew();

                        await next(context);

                        stopwatch.Stop();
                        context.Result = context.Result; // Result is available here

                        var logger = context.Services.GetRequiredService<ILogger<Program>>();
                        logger.LogInformation(
                            "GraphQL request completed in {ElapsedMs}ms",
                            stopwatch.ElapsedMilliseconds);
                    })
                    .UseDefaultPipeline();
                ```

                ### Class-Based Request Middleware

                For reusable middleware, create a class:

                ```csharp
                namespace MyApp.GraphQL.Middleware;

                public class CorrelationIdMiddleware
                {
                    private readonly RequestDelegate _next;

                    public CorrelationIdMiddleware(RequestDelegate next)
                    {
                        _next = next;
                    }

                    public async ValueTask InvokeAsync(IRequestContext context)
                    {
                        var httpContext = context.Services
                            .GetRequiredService<IHttpContextAccessor>()
                            .HttpContext;

                        var correlationId = httpContext?.Request.Headers["X-Correlation-Id"]
                            .FirstOrDefault() ?? Guid.NewGuid().ToString();

                        context.ContextData["CorrelationId"] = correlationId;

                        await _next(context);
                    }
                }
                ```

                Register the middleware:

                ```csharp
                builder.Services
                    .AddGraphQLServer()
                    .AddQueryType()
                    .AddTypes()
                    .UseRequest<CorrelationIdMiddleware>()
                    .UseDefaultPipeline();
                ```

                ### Middleware Ordering

                Middleware executes in registration order. Place custom middleware before `UseDefaultPipeline()`:

                ```csharp
                builder.Services
                    .AddGraphQLServer()
                    // Custom middleware runs first (outermost)
                    .UseRequest<CorrelationIdMiddleware>()
                    .UseRequest<TenantMiddleware>()
                    // Default pipeline includes parsing, validation, execution
                    .UseDefaultPipeline();
                ```

                ### Conditional Middleware

                ```csharp
                builder.Services
                    .AddGraphQLServer()
                    .UseRequest(next => async context =>
                    {
                        // Skip middleware for introspection queries
                        if (context.Document is not null &&
                            context.Document.Definitions.OfType<OperationDefinitionNode>()
                                .Any(o => o.SelectionSet.Selections
                                    .OfType<FieldNode>()
                                    .Any(f => f.Name.Value.StartsWith("__"))))
                        {
                            await next(context);
                            return;
                        }

                        // Your middleware logic for non-introspection queries
                        await next(context);
                    })
                    .UseDefaultPipeline();
                ```

                ## Anti-patterns

                **Forgetting UseDefaultPipeline:**

                ```csharp
                // BAD: Without UseDefaultPipeline, there is no parsing, validation, or execution
                builder.Services
                    .AddGraphQLServer()
                    .UseRequest<MyMiddleware>();
                // No UseDefaultPipeline() — nothing will execute!
                ```

                **Placing middleware after UseDefaultPipeline:**

                ```csharp
                // BAD: Middleware after UseDefaultPipeline runs after execution completes
                // and cannot modify the pipeline behavior
                builder.Services
                    .AddGraphQLServer()
                    .UseDefaultPipeline()
                    .UseRequest<TenantMiddleware>(); // Runs too late — tenant not set during execution
                ```

                **Heavy computation in middleware:**

                ```csharp
                // BAD: Middleware runs for every request — keep it lightweight
                public async ValueTask InvokeAsync(IRequestContext context)
                {
                    var data = await ExpensiveAnalysis(context); // Slow for every request
                    await _next(context);
                }
                ```

                ## Key Points

                - Register custom middleware with `UseRequest<T>()` or inline with `UseRequest(next => ...)`
                - Always call `UseDefaultPipeline()` after your custom middleware to include parsing, validation, and execution
                - Middleware order matters — earlier middleware wraps later middleware
                - Use `context.ContextData` to pass data from middleware to resolvers
                - Keep middleware lightweight — it runs for every GraphQL request
                - Request middleware is for cross-cutting concerns; use field middleware for per-field behavior

                ## Related Practices

                - [middleware-field] — For field-level middleware
                - [resolvers-global-state] — For accessing state set by middleware
                - [configuration-server-setup] — For server pipeline configuration
                """
            });
    }
}
