---
title: "Request Limits"
---

A Fusion gateway faces the same GraphQL query attacks as a standalone server: deep nesting, alias amplification, fragment expansion, and directive overloading. It also has gateway-specific risks like expensive query planning. Fusion enforces limits at every stage of the pipeline: parsing, validation, planning, and execution.

This page covers:

- Parser limits that reject payloads before the AST is fully constructed
- Validation limits that cap depth, cycles, and comparison work
- Planner guardrails that prevent expensive query plan generation
- Execution limits that bound time, request size, and transport features

## Parser Limits

Parser limits stop malicious payloads before the AST is fully constructed. They are the first line of defense because they run before any semantic analysis.

```csharp
builder
    .ModifyParserOptions(o =>
    {
        o.MaxAllowedFields = 1024;
        o.MaxAllowedDirectives = 4;
        o.MaxAllowedRecursionDepth = 100;
    });
```

| Option                     | Default        | Description                                                                                                  |
| -------------------------- | -------------- | ------------------------------------------------------------------------------------------------------------ |
| `MaxAllowedFields`         | 2048           | Maximum number of fields allowed in a query document.                                                        |
| `MaxAllowedDirectives`     | 4 per location | Maximum number of directives allowed on a single location (field, operation, or fragment definition).        |
| `MaxAllowedRecursionDepth` | 200            | Maximum nesting depth the parser allows for selection sets, list values, object values, and type references. |
| `MaxAllowedNodes`          | Unlimited      | Maximum number of AST nodes the parser produces from a document. Defaults to unlimited.                      |
| `MaxAllowedTokens`         | Unlimited      | Maximum number of tokens the lexer processes. Defaults to unlimited.                                         |

## Validation Limits

After parsing, the validation pipeline enforces semantic limits on the query structure. These limits protect against queries that are syntactically valid but computationally expensive to execute.

### Execution Depth

Caps how deeply nested a query can be. A depth of 10 covers most real-world queries while blocking deeply nested attacks:

```csharp
builder.AddMaxExecutionDepthRule(10);
```

### Fragment Visits

Each time a visitor enters a fragment spread counts as one visit. Queries with deeply nested or repeated fragment spreads can cause exponential visitor work. The total number of fragment visits per operation is capped at **1,000** by default.

### Field Merge Comparisons

The overlapping-fields-can-be-merged rule caps comparison work at 100,000 by default. No configuration is needed for most gateways. The default protects against fragment expansion bombs.

### Field Coordinate Cycles

Some schemas contain self-referential relationships. For example, a `User` type with a `friends` field that returns `[User]`. Without a limit, a client can nest this relationship arbitrarily deep, causing resolver fan-out that grows exponentially with each level.

The field cycle depth rule tracks how many times each schema coordinate (e.g., `User.friends`) appears on the query path:

```csharp
builder.AddMaxAllowedFieldCycleDepthRule(defaultCycleLimit: 3);
```

With a limit of 3, the following query is valid:

```graphql
{
  user {
    friends {
      # User.friends — cycle 1
      friends {
        # User.friends — cycle 2
        friends {
          # User.friends — cycle 3
          name
        }
      }
    }
  }
}
```

Adding a fourth level of `friends` would be rejected.

You can override the limit for specific coordinates:

```csharp
builder.AddMaxAllowedFieldCycleDepthRule(
    defaultCycleLimit: 3,
    coordinateCycleLimits:
    [
        (new SchemaCoordinate("Category", "parent"), 10),
    ]);
```

This rule is enabled by default in non-development environments as part of the default security policy. You can remove it with `RemoveMaxAllowedFieldCycleDepthRule()` if your schema does not contain self-referential relationships.

### Validation Errors

Caps the total number of validation errors reported per request. The default is 5. When the limit is reached, validation stops early instead of continuing to accumulate errors.

```csharp
builder.SetMaxAllowedValidationErrors(5);
```

### Introspection Depth

Introspection queries with recursive fields like `__Type.ofType` or `__Type.fields` can be used to construct expensive queries that consume significant server resources. The concern is not schema discovery (the schema is available at `/graphql/schema.graphql` by default when using `MapGraphQL`, computed once with no performance impact), but resource consumption from deeply recursive introspection operations.

```csharp
builder.SetIntrospectionAllowedDepth(
    maxAllowedOfTypeDepth: 8,
    maxAllowedListRecursiveDepth: 1);
```

### Disable Introspection

Introspection is disabled in non-development environments by default as part of the default security policy. This prevents clients from running expensive introspection queries against production systems. To disable it unconditionally (including in development):

```csharp
builder.DisableIntrospection();
```

## Operation Planner Guardrails

Before execution, the gateway plans how to distribute the query across subgraphs. Complex queries can cause expensive planning. These guardrails prevent planning from consuming excessive resources.

```csharp
builder
    .ModifyPlannerOptions(o =>
    {
        o.MaxPlanningTime = TimeSpan.FromSeconds(5);
        o.MaxExpandedNodes = 10_000;
        o.MaxQueueSize = 5_000;
    });
```

| Option                           | Default  | Description                                                                  |
| -------------------------------- | -------- | ---------------------------------------------------------------------------- |
| `MaxPlanningTime`                | Disabled | Maximum wall-clock time allowed for generating a query plan.                 |
| `MaxExpandedNodes`               | Disabled | Maximum number of planner nodes that may be expanded during plan generation. |
| `MaxQueueSize`                   | Disabled | Maximum size of the planner's internal work queue.                           |
| `MaxGeneratedOptionsPerWorkItem` | Disabled | Maximum number of options the planner generates per work item.               |

## Execution Limits

### Timeout

Requests are aborted after 30 seconds by default. The timeout covers the entire request including all subgraph calls. It is not enforced when a debugger is attached.

```csharp
builder
    .ModifyRequestOptions(o =>
    {
        o.ExecutionTimeout = TimeSpan.FromSeconds(10);
    });
```

### HTTP Request Size

The maximum HTTP request body size defaults to approximately 20 MB:

```csharp
services.AddGraphQLGatewayServer(maxAllowedRequestSize: 5 * 1000 * 1024); // 5 MB
```

### Server Options

Control which HTTP methods and features are available:

```csharp
builder
    .ModifyServerOptions(o =>
    {
        o.EnableMultipartRequests = false;
    });
```

## Next Steps

- **"I need to secure my gateway."** [Authentication and Authorization](/docs/fusion/v16/authentication-and-authorization) covers JWT validation, header propagation, and subgraph-level authorization.
- **"I need to tune transport performance."** [Performance Tuning](/docs/fusion/v16/performance-tuning) covers HTTP/2, request deduplication, and concurrency limiting.
- **"I need CDN and HTTP response caching behavior."** [Cache Control](/docs/fusion/v16/cache-control) covers `@cacheControl`, composition merge behavior, and gateway response headers.
