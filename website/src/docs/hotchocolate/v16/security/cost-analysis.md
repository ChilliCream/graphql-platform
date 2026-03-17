---
title: Cost Analysis
---

import { List, Panel, Tab, Tabs } from "../../../../components/mdx/tabs";

If you expose a GraphQL API to the public internet, you cannot predict what queries clients will send. A single deeply nested query requesting thousands of nodes can bring your server to its knees. Cost analysis prevents this by calculating the cost of a query before executing it and rejecting queries that exceed your budget.

Hot Chocolate implements static cost analysis based on the draft [IBM Cost Analysis specification](https://ibm.github.io/graphql-specs/cost-spec.html). It assigns weights to fields and estimates list sizes, then computes two metrics: **field cost** (execution impact) and **type cost** (data impact). Queries that exceed either limit are rejected before any resolver runs.

# Why This Matters for Public APIs

With REST, each endpoint has a predictable cost. You know that `GET /users` returns a page of users and takes a roughly constant amount of server time. With GraphQL, a client can construct a query that fans out across relationships:

```graphql
query {
  users(first: 50) {
    edges {
      node {
        orders(first: 50) {
          edges {
            node {
              items(first: 50) {
                edges {
                  node {
                    product {
                      reviews(first: 50) {
                        edges {
                          node {
                            author {
                              name
                            }
                          }
                        }
                      }
                    }
                  }
                }
              }
            }
          }
        }
      }
    }
  }
}
```

This query requests up to 50 x 50 x 50 x 50 = 6,250,000 nodes. Without cost analysis, the server would attempt to resolve all of them.

Cost analysis catches this at validation time and rejects the query before it consumes resources.

# How Cost Is Calculated

Hot Chocolate assigns default weights and computes two metrics:

- **Field cost** represents the execution impact on the server. Async resolvers default to `10`, composite types to `1`, and scalars to `0`.
- **Type cost** represents the number of objects the server instantiates.

## Field Cost Example

```graphql
query {
  book {
    # 10 (async resolver)
    title # 0  (scalar)
    author {
      # 1  (composite type)
      name # 0  (scalar)
    }
  }
}
# Field cost: 11
```

For paginated fields, costs multiply by the page size:

```graphql
query {
  books(first: 50) {
    # 10 (async resolver)
    edges {
      # 1  (composite type)
      node {
        # 50 (1 x 50 items)
        title # 0  (scalar)
        author {
          # 50 (1 x 50 items)
          name # 0  (scalar)
        }
      }
    }
  }
}
# Field cost: 111
```

## Type Cost Example

```graphql
query {
  # 1 Query
  books(first: 50) {
    # 50 BooksConnections
    edges {
      # 1  BooksEdge
      node {
        # 50 Books
        title
        author {
          # 50 Authors
          name
        }
      }
    }
  }
}
# Type cost: 152
```

# Defaults for Paginated Fields

Hot Chocolate automatically annotates paginated fields with cost and list size directives. For connection-based pagination:

```graphql
books(first: Int, after: String, last: Int, before: String): BooksConnection
  @listSize(
    assumedSize: 50
    slicingArguments: ["first", "last"]
    sizedFields: ["edges", "nodes"]
  )
  @cost(weight: "10")
```

The `assumedSize` defaults to the `MaxPageSize` from your pagination options.

# Applying a Cost Weight

Override the default cost for a specific field:

<ExampleTabs>
<Implementation>

```csharp
// Types/BookQueries.cs
[QueryType]
public static partial class BookQueries
{
    [Cost(100)]
    public static async Task<Book> GetBookAsync(int id, CatalogContext db, CancellationToken ct)
        => await db.Books.FindAsync([id], ct);
}
```

</Implementation>
<Code>

```csharp
// Types/BookQueriesType.cs
public class BookQueriesType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("book")
            .Resolve(_ => new Book("C# in depth", new Author("Jon Skeet")))
            .Cost(100);
    }
}
```

</Code>
</ExampleTabs>

# Applying List Size Settings

For fields that return lists, control how cost analysis estimates the list size:

<ExampleTabs>
<Implementation>

```csharp
// Types/BookQueries.cs
[QueryType]
public static partial class BookQueries
{
    [ListSize(
        AssumedSize = 100,
        SlicingArguments = ["first", "last"],
        SizedFields = ["edges", "nodes"],
        RequireOneSlicingArgument = false)]
    public static IEnumerable<Book> GetBooks()
        => [new Book("C# in depth", new Author("Jon Skeet"))];
}
```

</Implementation>
<Code>

```csharp
// Types/BookQueriesType.cs
public class BookQueriesType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("books")
            .Resolve<IEnumerable<Book>>(
                _ => [new Book("C# in depth", new Author("Jon Skeet"))])
            .ListSize(
                assumedSize: 100,
                slicingArguments: ["first", "last"],
                sizedFields: ["edges", "nodes"],
                requireOneSlicingArgument: false);
    }
}
```

</Code>
</ExampleTabs>

# Inspecting Cost Metrics

To see the cost of a query without changing enforcement, set the `GraphQL-Cost` HTTP header:

| Header Value | Behavior                                                        |
| ------------ | --------------------------------------------------------------- |
| `report`     | Executes the request and includes cost metrics in the response. |
| `validate`   | Returns cost metrics without executing the request.             |

This is invaluable when tuning your cost configuration. Send representative queries from your client applications and review their costs before deploying changes.

## Accessing Costs in Code

Read cost metrics from `IResolverContext` or `IMiddlewareContext`:

```csharp
// Types/BookQueries.cs
public static Book GetBook(IResolverContext context)
{
    var costMetrics = (CostMetrics)context.ContextData[WellKnownContextData.CostMetrics]!;

    double fieldCost = costMetrics.FieldCost;
    double typeCost = costMetrics.TypeCost;

    // Use for logging, monitoring, etc.
}
```

# Tuning Guide

## Start with Defaults

The defaults (`MaxFieldCost = 1000`, `MaxTypeCost = 1000`) work for many schemas. Deploy with defaults first and observe which queries are rejected.

## Measure Real Queries

Use the `GraphQL-Cost: report` header to measure the cost of your actual client queries. This gives you a baseline to tune from.

## Adjust MaxFieldCost and MaxTypeCost

Increase the limits if legitimate queries are rejected. Decrease them if you want tighter protection. The right values depend on your infrastructure and acceptable load.

```csharp
// Program.cs
builder.Services
    .AddGraphQLServer()
    .ModifyCostOptions(options =>
    {
        options.MaxFieldCost = 5_000;
        options.MaxTypeCost = 5_000;
    });
```

## Assign Custom Weights to Expensive Fields

If a resolver calls an external API or runs an expensive query, increase its cost weight:

```csharp
[Cost(50)]
public static async Task<Report> GetReportAsync(/* ... */)
```

## Use RequirePagingBoundaries

Force clients to specify `first` or `last` on paginated fields. Without this, the cost analyzer uses `MaxPageSize` as the assumed list size, which may overestimate the cost of well-behaved queries:

```csharp
builder.Services
    .AddGraphQLServer()
    .ModifyPagingOptions(opt => opt.RequirePagingBoundaries = true);
```

# Real-World Example

Consider a product catalog API with this schema:

```graphql
type Query {
  products(first: Int, after: String): ProductsConnection
}

type Product {
  name: String
  reviews(first: Int, after: String): ReviewsConnection
}

type Review {
  text: String
  author: User
}
```

With `MaxPageSize = 50` and default costs, a query requesting `products(first: 50) { ... reviews(first: 50) { ... } }` has:

- Field cost: 10 (products resolver) + 1 (edges) + 50 (node) + 500 (reviews resolver, 10 x 50) + 50 (reviews edges) + 2500 (review node, 50 x 50) + 2500 (author, 50 x 50) = ~5,611
- Type cost: 1 (Query) + 50 (Products) + 50 (ProductEdges) + 2500 (Reviews) + 2500 (ReviewEdges) + 2500 (Authors) = ~7,601

With default limits of 1,000, this query is rejected. You can either increase the limits or reduce `MaxPageSize` for the `reviews` field:

```csharp
[UsePaging(MaxPageSize = 10)]
public IQueryable<Review> GetReviews([Parent] Product product, CatalogContext db)
    => db.Reviews.Where(r => r.ProductId == product.Id);
```

Now the cost drops to a level within the default budget.

# Options Reference

## Cost Options

| Option                | Default | Description                                          |
| --------------------- | ------- | ---------------------------------------------------- |
| `MaxFieldCost`        | `1_000` | Maximum allowed field cost.                          |
| `MaxTypeCost`         | `1_000` | Maximum allowed type cost.                           |
| `EnforceCostLimits`   | `true`  | Whether to reject queries that exceed cost limits.   |
| `ApplyCostDefaults`   | `true`  | Whether to apply default cost weights to the schema. |
| `DefaultResolverCost` | `10.0`  | Default cost for an async resolver.                  |

```csharp
// Program.cs
builder.Services
    .AddGraphQLServer()
    .ModifyCostOptions(options =>
    {
        options.MaxFieldCost = 5_000;
        options.MaxTypeCost = 5_000;
        options.EnforceCostLimits = true;
        options.ApplyCostDefaults = true;
        options.DefaultResolverCost = 10.0;
    });
```

## Filtering Cost Options

| Option                                | Default | Description                                                 |
| ------------------------------------- | ------- | ----------------------------------------------------------- |
| `DefaultFilterArgumentCost`           | `10.0`  | Cost for a filter argument.                                 |
| `DefaultFilterOperationCost`          | `10.0`  | Cost for a filter operation.                                |
| `DefaultExpensiveFilterOperationCost` | `20.0`  | Cost for an expensive filter operation.                     |
| `VariableMultiplier`                  | `5`     | Multiplier when a variable is used for the filter argument. |

```csharp
options.Filtering.DefaultFilterArgumentCost = 10.0;
options.Filtering.DefaultFilterOperationCost = 10.0;
```

## Sorting Cost Options

| Option                     | Default | Description                                               |
| -------------------------- | ------- | --------------------------------------------------------- |
| `DefaultSortArgumentCost`  | `10.0`  | Cost for a sort argument.                                 |
| `DefaultSortOperationCost` | `10.0`  | Cost for a sort operation.                                |
| `VariableMultiplier`       | `5`     | Multiplier when a variable is used for the sort argument. |

```csharp
options.Sorting.DefaultSortArgumentCost = 10.0;
options.Sorting.DefaultSortOperationCost = 10.0;
```

# Disabling Cost Enforcement

If you protect your API through other means (such as trusted documents), you can disable cost enforcement. The analyzer still computes costs for reporting, but does not reject queries:

```csharp
// Program.cs
builder.Services
    .AddGraphQLServer()
    .ModifyCostOptions(o => o.EnforceCostLimits = false);
```

# Troubleshooting

## Legitimate queries are rejected

Use the `GraphQL-Cost: report` header to inspect the cost of the rejected query. Common causes: the query fans out across multiple paginated fields, or a field has an unexpectedly high default cost. Increase `MaxFieldCost`/`MaxTypeCost` or reduce `MaxPageSize` on specific fields.

## Cost seems too high for a small query

Check whether the query includes paginated fields without specifying `first` or `last`. Without a slicing argument, cost analysis uses `MaxPageSize` (default 50) as the assumed list size. Enable `RequirePagingBoundaries` to force clients to specify the page size.

## Cost metrics differ between environments

Cost calculation depends on the schema and cost configuration. If different environments have different `MaxPageSize` or cost weights, the metrics will differ. Keep cost configuration consistent across environments.

## Variable-based filters have unexpectedly high cost

When a filter argument is provided as a variable (rather than inline), the analyzer cannot inspect its structure at validation time. It applies a `VariableMultiplier` (default 5) to account for worst-case complexity. This is intentional. Adjust `VariableMultiplier` if the default is too conservative.

# Next Steps

- **Need to restrict access to fields?** See [Authorization](/docs/hotchocolate/v16/security/authorization).
- **Building a private API?** See [Trusted Documents](/docs/hotchocolate/v16/performance/persisted-operations).
- **Need to limit query depth?** See [Query Depth](/docs/hotchocolate/v16/security/query-depth).
- **Need an overview of security options?** See [Security Overview](/docs/hotchocolate/v16/security).
