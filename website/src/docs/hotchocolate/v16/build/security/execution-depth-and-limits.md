---
title: Execution depth and limits
---

GraphQL allows clients to define the shape of each request. While this flexibility is powerful, it also means that even a valid request can be resource-intensive before any resolver executes. For public endpoints, it is essential to set boundaries at every stage: request body handling, parsing, validation, execution, pagination, batching, and cancellation.

This page explains the Hot Chocolate controls that help keep request workloads predictable, focusing on limits and depth rules. For operation budgets, see [cost analysis](cost-analysis.md). If you can restrict production clients to known operations, see [trusted documents](../../performance/trusted-documents.md).

# Understanding the Request Lifecycle

Rejecting requests earlier is more efficient. A request that fails during HTTP body reading consumes fewer resources than one that builds a syntax tree, passes validation, opens database connections, and then times out.

```text
HTTP body
  -> maxAllowedRequestSize
GraphQL parser
  -> tokens, nodes, fields, directives, parser recursion depth
Validation
  -> execution depth, field cycle depth, introspection depth,
     fragment visits, field merge comparisons, validation errors
Execution
  -> execution timeout, cancellation, concurrent executions,
     Relay nodes batch size
Transport fan-out
  -> batching mode, max batch size
Pagination fan-out
  -> page size, required boundaries
```

| Stage      | Threat                           | Hot Chocolate control                                                                                         | Default                                             | Configure with                               |
| ---------- | -------------------------------- | ------------------------------------------------------------------------------------------------------------- | --------------------------------------------------- | -------------------------------------------- |
| HTTP body  | Oversized documents or variables | `maxAllowedRequestSize`                                                                                       | `20 * 1000 * 1024` bytes                            | `AddGraphQL(...)` or `AddGraphQLServer(...)` |
| Parser     | Large or deeply nested syntax    | `MaxAllowedFields`, `MaxAllowedDirectives`, `MaxAllowedRecursionDepth`, `MaxAllowedNodes`, `MaxAllowedTokens` | See parser table                                    | `ModifyParserOptions`                        |
| Validation | Long field paths                 | `AddMaxExecutionDepthRule(...)`                                                                               | No global execution depth rule by default           | GraphQL builder                              |
| Validation | Repeated recursive coordinates   | `AddMaxAllowedFieldCycleDepthRule(...)`                                                                       | Added by default security with limit `3`            | GraphQL builder                              |
| Validation | Fragment and merge amplification | `MaxAllowedFragmentVisits`, `SetMaxAllowedFieldMergeComparisons(...)`                                         | `1_000`, `100_000`                                  | `ConfigureValidation`, builder shortcut      |
| Validation | Large error payloads             | `SetMaxAllowedValidationErrors(...)`, `SetMaxAllowedLocationsPerValidationError(...)`                         | `5`, `5`                                            | GraphQL builder                              |
| Validation | Recursive introspection shape    | `SetIntrospectionAllowedDepth(...)`                                                                           | `16` for `ofType`, `1` for recursive list fields    | GraphQL builder                              |
| Execution  | Slow resolvers or capacity waits | `ExecutionTimeout`                                                                                            | `30` seconds, `30` minutes with a debugger attached | `ModifyRequestOptions`                       |
| Execution  | Too many concurrent executions   | `MaxConcurrentExecutions`                                                                                     | `64`                                                | `ModifyServerOptions`                        |
| Transport  | Many executions in one request   | `Batching`, `MaxBatchSize`                                                                                    | Batching `None`, max batch size `1024`              | `ModifyServerOptions`                        |
| Pagination | Large list slices                | `DefaultPageSize`, `MaxPageSize`, `RequirePagingBoundaries`                                                   | `10`, `50`, `false`                                 | `ModifyPagingOptions`, `[UsePaging]`         |

# Setting a Production Baseline

The following example provides a starting point for a public endpoint. The values are illustrative and not universally safe. Adjust them based on real traffic, schema structure, resolver performance, and client requirements.

```csharp
using HotChocolate.AspNetCore;
using HotChocolate.Language;

builder
    .AddGraphQL(maxAllowedRequestSize: 2 * 1000 * 1000)
    .ModifyParserOptions(o =>
    {
        o.MaxAllowedFields = 1_000;
        o.MaxAllowedDirectives = 4;
        o.MaxAllowedRecursionDepth = 100;
        o.MaxAllowedNodes = 5_000;
        o.MaxAllowedTokens = 20_000;
    })
    .AddMaxExecutionDepthRule(10)
    .AddMaxAllowedFieldCycleDepthRule(defaultCycleLimit: 3)
    .SetMaxAllowedValidationErrors(5)
    .SetMaxAllowedLocationsPerValidationError(5)
    .SetMaxAllowedFieldMergeComparisons(50_000)
    .ConfigureValidation((_, validation) =>
        validation.ModifyOptions(o => o.MaxAllowedFragmentVisits = 1_000))
    .SetIntrospectionAllowedDepth(
        maxAllowedOfTypeDepth: 8,
        maxAllowedListRecursiveDepth: 1)
    .ModifyRequestOptions(o =>
    {
        o.ExecutionTimeout = TimeSpan.FromSeconds(10);
    })
    .ModifyServerOptions(o =>
    {
        o.Batching = AllowedBatching.None;
        o.MaxBatchSize = 100;
        o.MaxConcurrentExecutions = 32;
    })
    .ModifyPagingOptions(o =>
    {
        o.DefaultPageSize = 25;
        o.MaxPageSize = 100;
        o.RequirePagingBoundaries = true;
    });
```

When `disableDefaultSecurity` is `false`, the standard server registration also adds the cost analyzer, disables introspection outside development, and adds the field cycle depth rule. Keep default security enabled unless you have intentionally replaced every default.

# Limiting Document Size Before Parsing

Set a request size limit to reject bodies before Hot Chocolate parses GraphQL text or variables.

```csharp
builder.AddGraphQL(maxAllowedRequestSize: 2 * 1000 * 1000);
```

If you configure through services, the same parameter is available:

```csharp
builder.Services.AddGraphQLServer(maxAllowedRequestSize: 2 * 1000 * 1000);
```

The default is `20 * 1000 * 1024` bytes. A request that exceeds the limit is rejected with a message like `Max GraphQL request size reached.`.

This limit is specific to GraphQL request parsing. Keep ASP.NET Core, reverse proxy, CDN, and file upload limits aligned with it.

# Limit parser work before validation

Parser limits protect CPU and memory before validation can inspect the document against your schema. They apply to invalid and valid documents.

```csharp
builder
    .AddGraphQL()
    .ModifyParserOptions(o =>
    {
        o.MaxAllowedFields = 1_000;
        o.MaxAllowedDirectives = 4;
        o.MaxAllowedRecursionDepth = 100;
        o.MaxAllowedNodes = 5_000;
        o.MaxAllowedTokens = 20_000;
    });
```

| Option                     | Default          | What it limits                                                            |
| -------------------------- | ---------------- | ------------------------------------------------------------------------- |
| `MaxAllowedFields`         | `2048`           | Field selections in the document. Repeated aliases still count as fields. |
| `MaxAllowedDirectives`     | `4` per location | Directives on a field, operation, or fragment definition.                 |
| `MaxAllowedRecursionDepth` | `200`            | Syntax nesting while parsing selection sets, values, and type references. |
| `MaxAllowedNodes`          | `int.MaxValue`   | Syntax tree nodes. Set a finite value for public APIs.                    |
| `MaxAllowedTokens`         | `int.MaxValue`   | Lexer tokens. Set a finite value for public APIs.                         |
| `IncludeLocations`         | `true`           | Whether syntax nodes preserve source locations for errors.                |

Parser recursion depth is not execution depth. Parser recursion protects the parser from deeply nested syntax. Execution depth measures field paths after the document is parsed and validated against the schema.

Example of a parser-level rejection:

```graphql
query {
  book @a @b @c @d @e {
    title
  }
}
```

With the default directive limit, the field has more than four directives at the same location and is rejected during parsing.

# Limit execution depth

Execution depth limits the longest field path that validation will accept. It is useful when clients can traverse relationships many levels deep.

```csharp
builder
    .AddGraphQL()
    .AddMaxExecutionDepthRule(10);
```

You can also configure introspection handling and request overrides:

```csharp
builder
    .AddGraphQL()
    .AddMaxExecutionDepthRule(
        maxAllowedExecutionDepth: 10,
        skipIntrospectionFields: true,
        allowRequestOverrides: false);
```

Hot Chocolate does not configure a global maximum execution depth by default. This is separate from field cycle depth, which is added by default security.

With a limit of `3`, this query is valid:

```graphql
query {
  viewer {
    profile {
      displayName
    }
  }
}
```

This query is rejected because the field path reaches a fourth level:

```graphql
query {
  viewer {
    friends {
      friends {
        friends {
          displayName
        }
      }
    }
  }
}
```

Fragments and inline fragments are followed by the depth rule:

```graphql
query {
  viewer {
    ...FriendFields
  }
}

fragment FriendFields on User {
  friends {
    friends {
      displayName
    }
  }
}
```

`skipIntrospectionFields` only affects this depth rule. Recursive introspection also has a dedicated introspection depth limit.

## Allow per-request depth overrides only for trusted callers

Per-request overrides are an escape hatch for internal tools or controlled support workflows. Enable them on the rule, then guard the override with authentication or another trusted signal.

```csharp
builder
    .AddGraphQL()
    .AddMaxExecutionDepthRule(10, allowRequestOverrides: true)
    .AddHttpRequestInterceptor(
        (context, executor, requestBuilder, ct) =>
        {
            if (context.User.IsInRole("GraphQLDiagnostics"))
            {
                requestBuilder.SetMaximumAllowedExecutionDepth(20);
            }

            return ValueTask.CompletedTask;
        });
```

`SkipExecutionDepthAnalysis()` is also available on `OperationRequestBuilder`, but reserve it for tightly controlled diagnostics. Do not expose it to regular clients.

# Keep recursive field cycles bounded

Some schemas contain self-referential fields such as `User.friends`, `Category.parent`, or `Comment.replies`. Field cycle depth counts repeated schema coordinates on a path.

```csharp
builder
    .AddGraphQL()
    .AddMaxAllowedFieldCycleDepthRule(defaultCycleLimit: 3);
```

With a limit of `3`, a fourth `relatives` cycle is rejected:

```graphql
query {
  human {
    relatives {
      relatives {
        relatives {
          relatives {
            name
          }
        }
      }
    }
  }
}
```

The error message is `Maximum allowed coordinate cycle depth was exceeded.`.

Allow a known-safe coordinate to go deeper while keeping the default low:

```csharp
builder
    .AddGraphQL()
    .AddMaxAllowedFieldCycleDepthRule(
        defaultCycleLimit: 3,
        coordinateCycleLimits:
        [
            (new SchemaCoordinate("Category", "parent"), 10),
        ]);
```

Execution depth and field cycle depth solve different problems. Execution depth limits every long path. Field cycle depth focuses on repeated coordinates, which is often the risky part of graph traversal. Use both for public APIs.

# Limit validation work and error volume

Invalid documents can be expensive even when they never execute. Keep validation work caps enabled and tune them from observed traffic.

```csharp
builder
    .AddGraphQL()
    .SetMaxAllowedValidationErrors(5)
    .SetMaxAllowedLocationsPerValidationError(5)
    .SetMaxAllowedFieldMergeComparisons(50_000)
    .ConfigureValidation((_, validation) =>
        validation.ModifyOptions(o => o.MaxAllowedFragmentVisits = 1_000))
    .SetIntrospectionAllowedDepth(
        maxAllowedOfTypeDepth: 8,
        maxAllowedListRecursiveDepth: 1);
```

| Control                                         | Default                                          | Purpose                                                                                        |
| ----------------------------------------------- | ------------------------------------------------ | ---------------------------------------------------------------------------------------------- |
| `MaxAllowedFragmentVisits`                      | `1_000`                                          | Limits repeated fragment traversal during validation. Configure through `ConfigureValidation`. |
| `SetMaxAllowedFieldMergeComparisons(...)`       | `100_000`                                        | Limits work in overlapping-fields-can-be-merged validation.                                    |
| `SetMaxAllowedValidationErrors(...)`            | `5`                                              | Stops validation after enough errors have been collected.                                      |
| `SetMaxAllowedLocationsPerValidationError(...)` | `5`                                              | Caps repeated source locations per validation error.                                           |
| `SetIntrospectionAllowedDepth(...)`             | `16` for `ofType`, `1` for recursive list fields | Bounds recursive introspection query shape.                                                    |

For introspection policy, see [Introspection](../../securing-your-api/introspection.md). Disabling introspection and limiting recursive introspection shape are related, but not the same control.

# Limit execution time and concurrency

After validation succeeds, resolvers can still wait on databases, external services, locks, or the Hot Chocolate concurrency gate. Set a finite timeout and pass cancellation tokens to I/O calls.

```csharp
builder
    .AddGraphQL()
    .ModifyRequestOptions(o =>
    {
        o.ExecutionTimeout = TimeSpan.FromSeconds(10);
    });
```

`ExecutionTimeout` defaults to `30` seconds. When a debugger is attached, the default is `30` minutes. Values below `100` milliseconds are raised to `100` milliseconds.

Limit concurrent executions per request executor:

```csharp
builder
    .AddGraphQL()
    .ModifyServerOptions(o =>
    {
        o.MaxConcurrentExecutions = 32;
    });
```

`MaxConcurrentExecutions` defaults to `64`. A value of `null` or a value less than or equal to `0` disables the gate. This is not a cluster-wide rate limit. Pair it with ASP.NET Core rate limiting and upstream controls when your threat model requires that.

Resolvers should accept `CancellationToken` and pass it to database and HTTP calls:

```csharp
[QueryType]
public static partial class ProductQueries
{
    public static async Task<Product?> GetProductAsync(
        int id,
        CatalogContext db,
        CancellationToken cancellationToken)
        => await db.Products.FindAsync([id], cancellationToken);
}
```

# Limit batching and operation fan-out

Batching can multiply executions behind one HTTP request. It is disabled by default.

```csharp
builder
    .AddGraphQL()
    .ModifyServerOptions(o =>
    {
        o.Batching = AllowedBatching.None;
        o.MaxBatchSize = 100;
    });
```

If a client requires batching, allow the narrowest mode and keep a finite batch size:

```csharp
builder
    .AddGraphQL()
    .ModifyServerOptions(o =>
    {
        o.Batching = AllowedBatching.VariableBatching;
        o.MaxBatchSize = 100;
    });
```

| Mode                               | Allows                                          | Use when                                                         |
| ---------------------------------- | ----------------------------------------------- | ---------------------------------------------------------------- |
| `AllowedBatching.None`             | No GraphQL batching                             | Default for public endpoints.                                    |
| `AllowedBatching.VariableBatching` | One operation with many variable payloads       | A trusted client needs repeated execution of the same operation. |
| `AllowedBatching.RequestBatching`  | A request array of independent GraphQL requests | Clients have a measured need to coalesce operations.             |
| `AllowedBatching.All`              | All batching modes                              | Reserved for controlled environments.                            |

`MaxBatchSize` defaults to `1024`. A value of `0` means unlimited and should be avoided for public APIs. For transport details, see [Batching](../../server/batching.md) and [HTTP transport](../../server/http-transport.md).

# Limit Relay node batch fetches

When global object identification is enabled, the `nodes(ids: [ID!]!)` field can fetch many objects in one field. Hot Chocolate limits this to `50` IDs by default.

```csharp
builder
    .AddGraphQL()
    .AddGlobalObjectIdentification(o => o.MaxAllowedNodeBatchSize = 25);
```

This limit does not replace authorization, DataLoader usage, paging, or cost analysis. Each fetched node still needs normal authorization and efficient resolver behavior.

# Limit paging fan-out

Depth limits do not control list cardinality. A shallow query can still request a large page, and nested pages multiply work.

```graphql
query {
  brands(first: 50) {
    nodes {
      products(first: 50) {
        nodes {
          reviews(first: 50) {
            nodes {
              body
            }
          }
        }
      }
    }
  }
}
```

Configure global paging defaults for public schemas:

```csharp
builder
    .AddGraphQL()
    .ModifyPagingOptions(o =>
    {
        o.DefaultPageSize = 25;
        o.MaxPageSize = 100;
        o.RequirePagingBoundaries = true;
    });
```

Built-in paging defaults are `DefaultPageSize = 10`, `MaxPageSize = 50`, and `RequirePagingBoundaries = false`. `MaxPageSize` also affects cost analysis because it is used as the assumed size for paged fields.

# How limits relate to cost analysis

Depth and parser limits describe the shape and size of a document. Cost analysis estimates the work a valid document can cause.

Use both:

| Control           | Catches                                                                        | Does not catch                                                    |
| ----------------- | ------------------------------------------------------------------------------ | ----------------------------------------------------------------- |
| Parser limits     | Large documents, token floods, syntax recursion, too many fields or directives | Resolver cost and list cardinality after validation               |
| Execution depth   | Long field paths                                                               | Wide selection sets, expensive resolvers, large pages             |
| Field cycle depth | Repeated self-referential coordinates                                          | Non-cyclic paths that are still expensive                         |
| Paging limits     | Large list slices                                                              | Expensive scalar fields or external service calls                 |
| Cost analysis     | Estimated field and type cost before execution                                 | Request body size, parser recursion, transport batching, timeouts |

Cost analysis is added by default security. Tune its budgets on the [Cost analysis](cost-analysis.md) page after you set basic parser, depth, paging, batching, and timeout limits.

# When trusted documents are stronger

If your clients are first-party and operations are known at build time, trusted documents are a stronger operation-shape control than dynamic limits. Unknown operation text can be rejected before validation.

```csharp
builder
    .AddGraphQL()
    .UsePersistedOperationPipeline()
    .ModifyRequestOptions(o =>
        o.PersistedOperations.OnlyAllowPersistedDocuments = true);
```

Automatic persisted queries are not the same security posture unless non-persisted operation text is blocked. Even with trusted documents, keep request size, parser, timeout, concurrency, batching, and paging limits. They protect the server from oversized payloads, misconfigured clients, and infrastructure pressure.

# Troubleshooting

| Symptom                                                            | Likely limit                                                     | Default                                                              | What to check first                                                                               |
| ------------------------------------------------------------------ | ---------------------------------------------------------------- | -------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------- |
| `Max GraphQL request size reached.`                                | `maxAllowedRequestSize`                                          | `20 * 1000 * 1024` bytes                                             | Inspect body size, variables, file upload path, proxy limits.                                     |
| Parser reports max fields, directives, recursion, nodes, or tokens | `ModifyParserOptions`                                            | See parser table                                                     | Inspect generated documents, aliases, fragments, and client codegen.                              |
| Maximum execution depth exceeded                                   | `AddMaxExecutionDepthRule(...)`                                  | No global rule                                                       | Inspect nested selections and fragments.                                                          |
| `Maximum allowed coordinate cycle depth was exceeded.`             | Field cycle depth                                                | Default security limit `3`                                           | Inspect recursive fields such as `friends`, `parent`, or `replies`.                               |
| Validation stops after a few errors                                | `SetMaxAllowedValidationErrors(...)`                             | `5`                                                                  | Fix the first errors instead of raising the cap.                                                  |
| Error locations are truncated                                      | `SetMaxAllowedLocationsPerValidationError(...)`                  | `5`                                                                  | Check repeated fragments and aliases.                                                             |
| Batch is rejected                                                  | `MaxBatchSize`                                                   | `1024` when batching is enabled                                      | Split client batches or reduce client-side coalescing.                                            |
| Request times out                                                  | `ExecutionTimeout`                                               | `30` seconds                                                         | Inspect resolvers, cancellation tokens, database calls, external services, and concurrency waits. |
| Introspection is rejected or too deep                              | `DisableIntrospection(...)`, `SetIntrospectionAllowedDepth(...)` | Disabled outside development by default security, depth `16` and `1` | Decide introspection policy separately from execution depth.                                      |
| A paged field returns more data than expected                      | Paging options                                                   | `DefaultPageSize = 10`, `MaxPageSize = 50`, boundaries not required  | Set `RequirePagingBoundaries`, lower `MaxPageSize`, and review nested pages.                      |

Exact HTTP status codes depend on transport and content negotiation. See [HTTP transport](../../server/http-transport.md) for response behavior.

# Production checklist

- Keep default security enabled unless equivalent controls are configured manually.
- Set `maxAllowedRequestSize` below the default if public clients do not need 20 MB requests.
- Set finite parser token and node limits for public endpoints.
- Add `AddMaxExecutionDepthRule(...)` from measured legitimate operations.
- Keep field cycle depth enabled. Add coordinate overrides only for known-safe relationships.
- Keep validation error, fragment visit, and field merge caps enabled.
- Keep `ExecutionTimeout` finite and pass cancellation tokens into resolver I/O.
- Size `MaxConcurrentExecutions` from capacity testing.
- Keep batching disabled unless required. If enabled, allow the narrowest mode and set `MaxBatchSize`.
- Set `MaxAllowedNodeBatchSize` when global object identification is enabled.
- Set paging boundaries and page-size limits before exposing large collections.
- Use DataLoader patterns for efficient resolver batching, but do not treat them as request limits.
- Prefer trusted documents for controlled clients.
- Keep exception details disabled in production.

# Next steps

- Continue with [Cost analysis](cost-analysis.md) to budget field and type cost.
- Continue with [Trusted documents](../../performance/trusted-documents.md) to reject unknown operation text.
- Review [Introspection](../../securing-your-api/introspection.md) for schema discovery and recursive introspection policy.
- Review [Batching](../../server/batching.md) if clients need batching.
- Review [Paging options](../pagination/paging-options.md) for field-level paging limits.
