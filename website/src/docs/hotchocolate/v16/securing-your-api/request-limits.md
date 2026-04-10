---
title: "Request Limits"
---

Unlike REST, where each endpoint has a predictable cost, a single GraphQL request can trigger unbounded work through deep nesting, alias amplification, or fragment expansion. Hot Chocolate enforces limits at every stage of request processing (parsing, validation, and execution) to keep resource consumption bounded even under adversarial workloads.

# Parser Limits

Parser limits stop malicious payloads before the AST is fully constructed. Because parsing happens before validation, these limits are your first line of defense.

Configure parser limits with `ModifyParserOptions`:

```csharp
builder.Services
    .AddGraphQLServer()
    .ModifyParserOptions(o =>
    {
        o.MaxAllowedFields = 1024;
        o.MaxAllowedDirectives = 4;
        o.MaxAllowedRecursionDepth = 100;
    });
```

| Option                     | Default          | Description                                                                                                  |
| -------------------------- | ---------------- | ------------------------------------------------------------------------------------------------------------ |
| `MaxAllowedFields`         | `2048`           | Maximum number of fields allowed in a query document.                                                        |
| `MaxAllowedDirectives`     | `4` per location | Maximum number of directives allowed on a single location (field, operation, or fragment definition).        |
| `MaxAllowedRecursionDepth` | `200`            | Maximum nesting depth the parser allows for selection sets, list values, object values, and type references. |
| `MaxAllowedNodes`          | Unlimited        | Maximum number of AST nodes the parser produces from a document. Defaults to unlimited.                      |
| `MaxAllowedTokens`         | Unlimited        | Maximum number of tokens the lexer processes. Defaults to unlimited.                                         |

# Validation Limits

After parsing, the validation layer checks the document against your schema. Some validation rules can become expensive on adversarial inputs.

## Execution Depth

Limits how deeply nested a query can be:

```csharp
builder.Services
    .AddGraphQLServer()
    .AddMaxExecutionDepthRule(10);
```

This prevents queries that traverse deep relationship chains (e.g., `user.friends.friends.friends...`). Unlike the parser recursion depth (which prevents stack overflows), execution depth measures the logical nesting of field selections against your schema.

You can skip introspection fields from the depth count and allow per-request overrides:

```csharp
builder.Services
    .AddGraphQLServer()
    .AddMaxExecutionDepthRule(
        maxAllowedExecutionDepth: 10,
        skipIntrospectionFields: true,
        allowRequestOverrides: true);
```

## Fragment Visits

Each time a visitor enters a fragment spread counts as one visit. Queries with deeply nested or repeated fragment spreads can cause exponential visitor work. Hot Chocolate caps the total number of fragment visits per operation at **1,000** by default:

```csharp
builder.Services
    .AddGraphQLServer()
    .ModifyValidationOptions(o =>
    {
        o.MaxAllowedFragmentVisits = 1_000;
    });
```

## Field Merge Comparisons

The "overlapping fields can be merged" validation rule checks that fields with the same response name have compatible types and arguments. On adversarial inputs with deeply nested fragments, this check can become expensive.

Hot Chocolate caps the comparison budget at **100,000** by default. Queries exceeding this budget are rejected:

```csharp
builder.Services
    .AddGraphQLServer()
    .SetMaxAllowedFieldMergeComparisons(50_000);
```

## Field Coordinate Cycles

Some schemas contain self-referential relationships. For example, a `User` type with a `friends` field that returns `[User]`. Without a limit, a client can nest this relationship arbitrarily deep, causing resolver fan-out that grows exponentially with each level.

The field cycle depth rule tracks how many times each schema coordinate (e.g., `User.friends`) appears on the query path:

```csharp
builder.Services
    .AddGraphQLServer()
    .AddMaxAllowedFieldCycleDepthRule(defaultCycleLimit: 3);
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

You can override the limit for specific coordinates if some relationships are safe to traverse more deeply than others:

```csharp
builder.Services
    .AddGraphQLServer()
    .AddMaxAllowedFieldCycleDepthRule(
        defaultCycleLimit: 3,
        coordinateCycleLimits:
        [
            (new SchemaCoordinate("Category", "parent"), 10),
        ]);
```

This rule is enabled by default in non-development environments as part of the default security policy. You can remove it with `RemoveMaxAllowedFieldCycleDepthRule()` if your schema does not contain self-referential relationships.

## Validation Errors

Documents designed to generate excessive validation errors can consume memory accumulating error objects. The default limit is **5**. When the limit is reached, validation stops early instead of continuing to accumulate errors.

```csharp
builder.Services
    .AddGraphQLServer()
    .SetMaxAllowedValidationErrors(5);
```

## Introspection Depth

Introspection queries with recursive fields like `__Type.ofType` or `__Type.fields` can be used to construct expensive queries that consume significant server resources. The concern is not schema discovery (the schema is available at `/graphql/schema.graphql` by default when using `MapGraphQL`, computed once with no performance impact), but resource consumption from deeply recursive introspection operations.

Recursive introspection queries are limited by default:

- **`ofType` chain:** 16 levels
- **List fields** (`fields`, `inputFields`, `interfaces`, `possibleTypes`): 1 level of recursion

```csharp
builder.Services
    .AddGraphQLServer()
    .SetIntrospectionAllowedDepth(
        maxAllowedOfTypeDepth: 8,
        maxAllowedListRecursiveDepth: 1);
```

# Execution Limits

## Timeout

Requests are aborted after 30 seconds by default. The timeout is not enforced when a debugger is attached.

```csharp
builder.Services
    .AddGraphQLServer()
    .ModifyRequestOptions(o =>
    {
        o.ExecutionTimeout = TimeSpan.FromSeconds(10);
    });
```

## Nodes Batch Size

The `nodes(ids: [ID!]!)` field allows fetching multiple entities at once. The default batch limit is **50**:

```csharp
builder.Services
    .AddGraphQLServer()
    .AddGlobalObjectIdentification(o => o.MaxAllowedNodeBatchSize = 25);
```

# Next Steps

- **Need cost analysis?** See [Cost Analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis).
- **Need trusted documents?** See [Trusted Documents](/docs/hotchocolate/v16/performance/trusted-documents).
- **Need to control introspection?** See [Introspection](/docs/hotchocolate/v16/securing-your-api/introspection).
- **Back to overview?** See [Securing Your API](/docs/hotchocolate/v16/securing-your-api).
