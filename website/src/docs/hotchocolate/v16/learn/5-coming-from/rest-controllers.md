---
title: "REST controllers"
description: "Translate ASP.NET Core controllers, routes, DTOs, status codes, and EF-backed endpoints into a Hot Chocolate v16 GraphQL schema without rewriting a working REST API."
---

If you already have working controllers and clients that depend on them, you don’t need to start over to adopt GraphQL. Your ASP.NET Core app likely uses middleware, dependency injection, authorization, logging, DTOs, and often EF Core behind your actions.

With Hot Chocolate, you can add GraphQL as an additional endpoint. Keep your existing REST routes for clients that need them, and migrate to GraphQL one feature at a time.

Use this guide if you’re familiar with ASP.NET Core REST controllers and want to design your first Hot Chocolate v16 schema: fields, operations, inputs, payloads, data boundaries, and error handling.

This page assumes:

- You understand controllers, route templates, action results, DTOs, model binding, status codes, dependency injection, and ASP.NET Core middleware.
- You’ve seen a basic GraphQL query or mutation.
- You may use EF Core or another data layer behind your controllers.
- You want migration guidance. For endpoint setup, see [Existing ASP.NET Core app](/docs/hotchocolate/v16/learn/4-installation-and-setup/existing-aspnet-core-app/).

# Keep Your REST API While Adding GraphQL

Begin by leaving your existing API untouched.

Your ASP.NET Core app can continue to serve routes like:

```text
GET  /api/products/{id}
GET  /api/products/{id}/reviews
POST /api/orders
```

At the same time, you can add a GraphQL endpoint:

```text
POST /graphql
GET  /graphql
```

Adding GraphQL changes how clients query your data, but it doesn’t remove ASP.NET Core routing, middleware, dependency injection, logging, authentication, authorization, health checks, downloads, webhooks, OpenAPI descriptions, or your existing REST contracts.

Aim for your first success by building a useful GraphQL slice on top of code you already trust. Reuse your application and data-access services through dependency injection. Avoid copying large controller action bodies into resolvers. Do not maintain two sets of business logic.

**Checkpoint:** Pick one screen, workflow, or endpoint cluster that can demonstrate value while all current REST clients continue to work.

# Translate the Mental Model Before the Code

A controller action can become a resolver, but in GraphQL, the schema field is the contract your clients see.

Before you start renaming files or copying methods, compare concepts side by side:

| REST controller concept | GraphQL counterpart | What changes | Common trap |
| --- | --- | --- | --- |
| Controller | A group of resolver methods or type extensions | The schema is not organized by controller routes. It is organized by `Query`, `Mutation`, object types, and fields. | Creating one GraphQL type per controller. |
| Action method | Possible resolver implementation | The resolver serves a schema field. Use domain language for field names and return types. | Copying every action into a root field. |
| Route template | Usually one `/graphql` HTTP endpoint plus schema fields | Clients select fields in an operation document, not by route templates. | Treating `/api/products/{id}` as the GraphQL operation name. |
| Route value or query string | Field argument or input object field | Arguments are typed, nullable or non-null, and validated by the schema. | Keeping REST parameter names that don’t fit the graph. |
| Request body DTO | Input object | Inputs should model the command or search shape clients need. | Reusing a persistence update model as a public input. |
| Response DTO | Object type or payload type | Response fields become selectable schema fields with names, nullability, and descriptions. | Publishing serialization quirks or internal fields. |
| `ActionResult<T>` | GraphQL data, payload data, top-level errors, and transport behavior | GraphQL can return partial `data` and `errors` in the same response. | Translating every `404` or `409` into the same error shape. |
| ASP.NET Core middleware | Application boundary around GraphQL requests | Authentication, CORS, logging, forwarded headers, and other middleware still apply to `/graphql`. | Moving transport concerns into every resolver. |

Keep these distinctions clear:

```text
/graphql is the HTTP endpoint.
productById(id:) is a schema field.
ProductDetails is a client operation name.
```

To learn more about the operation model, see [Operations](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/operations/). For field and resolver execution, see [How GraphQL executes](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/how-graphql-executes/) and [Resolvers](/docs/hotchocolate/v16/resolvers-and-data/resolvers/).

# Choose the First Controller Feature to Migrate

Select a feature slice that reduces risk and demonstrates the benefits of GraphQL.

| Candidate | Why it fits | Why to defer | Read next |
| --- | --- | --- | --- |
| A read-heavy screen that calls several endpoints | One operation can fetch the selected graph for that screen. | If the screen has unclear ownership or unstable requirements, design can churn. | [Modeling entities vs operations](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/modeling-entities-vs-operations/) |
| A mobile or web view that over-fetches large DTOs | Selection sets let clients ask for fewer fields. | If the source query is unbounded, define paging first. | [Pagination styles](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/pagination-styles/) |
| A BFF-style aggregation over existing services | GraphQL can compose a client-facing graph while services stay behind DI. | If the aggregation has strict latency needs, plan batching and observability. | [Resolver and data middleware model](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/resolver-and-data-middleware-model/) |
| An EF-backed read endpoint with known clients | You can expose a reviewed, bounded data shape with projections or service-backed resolvers. | If the first design is `DbSet` exposure, pause and review the contract. | [Connecting to real data](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/connecting-to-real-data/) |
| File downloads, webhooks, callback protocols, and HTTP-specific flows | Keep them in REST when HTTP semantics are the product. | They rarely prove GraphQL value as a first slice. | [HTTP transport](/docs/hotchocolate/v16/server/http-transport/) |
| Public REST endpoints with strict compatibility promises | GraphQL can sit beside them while adoption is measured. | Removing or changing them early creates client risk. | [Public API guide](/docs/hotchocolate/v16/guides/public-api/) |

Before you start, define what success looks like:

1. A known client can fetch all required data in a single GraphQL operation.
2. Authorization and validation match the current feature.
3. Logs, metrics, and errors are clear and actionable.
4. REST remains available as a fallback during rollout.

**Checkpoint:** Write the expected GraphQL query shape for your chosen slice, using product language.

# Convert Read Actions into Query Fields and Object Fields

Don’t map every `GET` route directly to a root query field. Start by focusing on the client’s task.

**Before:**

```text
GET /api/products/{id}
GET /api/products/{id}/reviews
GET /api/products/{id}/availability
```

**After:**

```graphql
query ProductDetails($id: ID!) {
  productById(id: $id) {
    id
    name
    price
    availability {
      inStock
      shipsFrom
    }
    reviews(first: 3) {
      nodes {
        rating
        text
      }
    }
  }
}
```

**Example result:**

```json
{
  "data": {
    "productById": {
      "id": "123",
      "name": "GraphQL Workshop",
      "price": 99.0,
      "availability": {
        "inStock": true,
        "shipsFrom": "Berlin"
      },
      "reviews": {
        "nodes": [
          {
            "rating": 5,
            "text": "Practical and focused."
          }
        ]
      }
    }
  }
}
```

Your resolver can still call the same product service as your controller:

```csharp
// Types/ProductQueries.cs
using HotChocolate.Types;

namespace Store.Api.Types;

[QueryType]
public static partial class ProductQueries
{
    public static async Task<Product?> GetProductByIdAsync(
        string id,
        ProductService products,
        CancellationToken ct)
        => await products.GetProductByIdAsync(id, ct);
}
```

This approach keeps REST-like string identifiers such as `"123"`. The GraphQL `ID` scalar can carry that string value without requiring Relay global ID formatting.

The `[QueryType]` example uses the v16 analyzer registration path. Reference `HotChocolate.Types.Analyzers` and call `.AddTypes()` during schema setup, as described in [Existing ASP.NET Core app](/docs/hotchocolate/v16/learn/4-installation-and-setup/existing-aspnet-core-app/).

Use root `Query` fields for entry points like:

- `productById(id:)`
- `viewer`
- `searchProducts(text:, first:)`
- `orderReport(range:)`

Move repeated identity-scoped reads under object fields when they represent relationships or capabilities, such as:

- `Product.reviews`
- `Product.availability`
- `Customer.orders`
- `Order.shipments`

Map route values and query strings to field arguments with clear names, nullability, defaults, and paging boundaries. Use pagination for lists that can grow. Add filtering and sorting only where those features are part of the contract.

For EF-backed reads, decide whether a field should return a provider-backed query (so Hot Chocolate data middleware can shape it) or a DTO/read model from an application service. Both are valid choices when made deliberately.

**Checkpoint:** Classify each former `GET` action as a root entry point, nested object field, service-backed read, queryable list, or REST-only endpoint.

# Convert Write Actions into Mutations with Payloads

GraphQL mutations are top-level fields that perform side effects. Mutation fields execute in the order they appear in the request.

Don’t translate HTTP verbs directly. Instead, ask what domain command the mutation represents:

| REST action | Better GraphQL question |
| --- | --- |
| `POST /api/orders` | What domain command creates the order? |
| `PUT /api/products/{id}` | Is the command replacing the product, publishing it, renaming it, or changing a price? |
| `PATCH /api/products/{id}/price` | Which input values does the price-change command need? |
| `DELETE /api/subscriptions/{id}` | Is the domain command `deleteSubscription`, `cancelSubscription`, or `closeSubscription`? |

Name mutations after the domain action they perform:

```graphql
type Mutation {
  changeProductPrice(input: ChangeProductPriceInput!): ChangeProductPricePayload!
}

input ChangeProductPriceInput {
  productId: ID!
  price: Decimal!
  expectedVersion: Int
}

type ChangeProductPricePayload {
  product: Product
  errors: [ChangeProductPriceError!]
}

union ChangeProductPriceError =
    ProductNotFoundError
  | ProductPriceLockedError
  | ProductVersionConflictError
```

Clients can select the data and domain errors they need:

```graphql
mutation ChangePrice($input: ChangeProductPriceInput!) {
  changeProductPrice(input: $input) {
    product {
      id
      price
    }
    errors {
      __typename
      ... on ProductVersionConflictError {
        currentVersion
      }
    }
  }
}
```

Hot Chocolate v16 can generate input and payload types for you if you enable `.AddMutationConventions(applyToAllMutations: true)`. See the [Mutations](/docs/hotchocolate/v16/building-a-schema/mutations/) page for implementation details.

Preserve important semantics from your controller design:

- Idempotency rules
- Validation rules
- Concurrency checks
- Transaction boundaries
- Authorization rules
- Domain error names and codes your clients already use

Keep HTTP-specific commands, callbacks, and file upload flows in REST when the HTTP boundary is the better contract.

**Checkpoint:** For one write action, write the mutation name, input shape, payload fields, and known domain errors.

# Shift from Route-Based to Operation-Based Client Requests

In REST, a client chooses a route:

```http
GET /api/products/123?includeReviews=true
```

In GraphQL, a client sends an operation document to the GraphQL endpoint:

```json
{
  "query": "query ProductDetails($id: ID!) { productById(id: $id) { name reviews(first: 3) { nodes { rating } } } }",
  "operationName": "ProductDetails",
  "variables": {
    "id": "123"
  }
}
```

The HTTP path is always `/graphql`. The operation name, variables, and selection set define the client’s request.

HTTP `GET` and `POST` are transport choices for GraphQL requests. They are not REST resource verbs. The operation type (`query` or `mutation`) tells you whether the request reads data or performs side effects.

When your schema exposes relationships, a single GraphQL operation can fetch data that would have required several REST calls:

```graphql
query CustomerSupportView($id: ID!) {
  customerById(id: $id) {
    name
    email
    orders(first: 5) {
      nodes {
        number
        status
      }
    }
    supportTickets(first: 3) {
      nodes {
        title
        status
      }
    }
  }
}
```

Later, you can introduce persisted or trusted documents to give first-party clients an operation catalog, stable IDs, and stronger production controls. Treat this as a rollout step after your first schema slice is working. See [First-Party API](/docs/hotchocolate/v16/guides/private-api/) and [Trusted documents](/docs/hotchocolate/v16/performance/trusted-documents/).

**Checkpoint:** For your first client request, separate the endpoint path, operation name, variables, and selected fields.

# Translate DTOs into Schema Types, Inputs, and Payloads

Existing DTOs are valuable evidence. They show what clients needed, which names they know, and which persistence details you already hid.

However, DTOs are not automatically schema types. Review each DTO field with these options:

| DTO field decision | Use when | GraphQL outcome |
| --- | --- | --- |
| Expose | The field is client-safe domain data. | Add an object field with a reviewed name and nullability. |
| Rename | The DTO name came from a route, serializer, database, or legacy abbreviation. | Add a schema field in domain language. |
| Move to input | The field is supplied by clients for a command or search. | Add it to an input object with a clear required or optional contract. |
| Move to payload | The field describes the result of a command. | Add it to the mutation payload. |
| Hide | The field is internal, persistence-specific, privileged, or a REST serialization workaround. | Keep it out of the schema. |
| Keep REST-only | The field exists for a legacy client or HTTP-specific response. | Leave it on the REST DTO while GraphQL exposes a different shape. |

**Example split:**

```text
UpdateProductRequestDto
  productId
  price
  rowVersion
  changedByUserId
```

could become:

```graphql
input ChangeProductPriceInput {
  productId: ID!
  price: Decimal!
  expectedVersion: Int
}
```

The `changedByUserId` field should usually come from the authenticated user or application service, not from client input.

For output DTOs, separate read models from mutation payloads:

```graphql
type Product {
  id: ID!
  name: String!
  price: Decimal!
}

type ChangeProductPricePayload {
  product: Product
  errors: [ChangeProductPriceError!]
}
```

Use descriptions, naming, nullability, and deprecation deliberately when the schema is shared beyond one team. See [Schema design principles](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/schema-design-principles/), [Nullability](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/nullability/), [Schema evolution](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/schema-evolution/), and [Public API guide](/docs/hotchocolate/v16/guides/public-api/).

**Checkpoint:** Mark each DTO field as expose, rename, input, payload, hide, or REST-only.

# Handle EF-Backed Endpoints Without Exposing Tables

EF entities are persistence models. They are not automatically the right GraphQL object types.

Avoid turning this:

```csharp
public DbSet<Product> Products => Set<Product>();
public DbSet<Order> Orders => Set<Order>();
public DbSet<OrderLine> OrderLines => Set<OrderLine>();
```

into this by default:

```graphql
type Query {
  products: [Product!]!
  orders: [Order!]!
  orderLines: [OrderLine!]!
}
```

Don’t let GraphQL become an EF table browser unless that’s your explicit goal.

Choose the data boundary for each field:

| Former EF-backed action | Possible GraphQL shape | Data boundary | Migration check |
| --- | --- | --- | --- |
| `GET /api/products` backed by `DbSet<Product>` | `products(first:, where:, order:)` | Return `IQueryable<Product>` when paging, projections, filtering, and sorting are reviewed and provider-translatable. | Set page limits, allowed filters, allowed sorts, and inspect generated SQL. |
| `GET /api/products/{id}` with visibility rules | `productById(id:)` | Use an application service or query object when authorization and business rules shape the result. | Test success, missing, and forbidden cases. |
| `GET /api/customers/{id}/orders` | `customerById(id:) { orders(first:) }` | Use projection, a DataLoader, batch resolver, or service method based on the relationship shape. | Confirm query count for realistic lists. |
| `GET /api/dashboard` with aggregation | `salesDashboard(range:)` | Return a DTO/read model from a service. | Keep aggregation and caching decisions outside generic filters. |
| `POST /api/orders/{id}/cancel` | `cancelOrder(input:)` | Use a mutation and application service for transaction and workflow boundaries. | Return changed data and typed domain errors. |

Hot Chocolate data middleware is helpful when a schema field is a bounded provider-backed query. The typical attribute order is:

```csharp
[UsePaging(MaxPageSize = 50)]
[UseProjection]
[UseFiltering]
[UseSorting]
public static IQueryable<Product> GetProducts(CatalogContext db)
    => db.Products.Where(p => p.IsPublished);
```

Use projections when selected fields match a provider-translatable `IQueryable` shape. Use DataLoader or batch resolvers for relationship fan-out and keyed lookups that projections can’t cover.

Don’t materialize data too early if you want projections, filtering, or sorting to become SQL:

```csharp
// Avoid this on a field that should use provider-backed projection.
public static async Task<List<Product>> GetProductsAsync(CatalogContext db)
    => await db.Products.ToListAsync();
```

For guidance on EF Core resolver scopes, `DbContext` factories, projections, and concurrency, see [Entity Framework Core](/docs/hotchocolate/v16/integrations/entity-framework/), [Fetching from databases](/docs/hotchocolate/v16/resolvers-and-data/fetching-from-databases/), [Projections](/docs/hotchocolate/v16/resolvers-and-data/projections/), [Filtering](/docs/hotchocolate/v16/resolvers-and-data/filtering/), [Sorting](/docs/hotchocolate/v16/resolvers-and-data/sorting/), [Pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination/), and [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader/).

**Checkpoint:** Classify each EF-backed action as a queryable read, service-backed read, relationship field, aggregate read, mutation boundary, or REST-only endpoint.

# Map Status Codes to Data, Domain Errors, and Transport Errors

This is the biggest difference between REST and GraphQL.

Start by asking: can the client recover in the domain, authenticate, retry, or report a server fault?

| REST result | Likely GraphQL representation | When HTTP status still matters | Follow-up |
| --- | --- | --- | --- |
| `200 OK` with data | `data` contains the selected fields. | The HTTP response still communicates transport success. | [Operations](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/operations/) |
| `201 Created` | Mutation payload contains changed data, identifiers, workflow state, or links as fields. | HTTP status is not the main domain contract for the mutation result. | [Mutations](/docs/hotchocolate/v16/building-a-schema/mutations/) |
| `400 Bad Request` for invalid GraphQL syntax or variable shape | Top-level `errors` before execution. | Malformed HTTP requests, unsupported media types, and transport parsing failures can use HTTP status codes. | [HTTP transport](/docs/hotchocolate/v16/server/http-transport/) |
| `400 Bad Request` for validation failure | Typed payload errors or field-level domain data when the operation was well-formed. | Use HTTP only when the request cannot enter GraphQL execution. | [Errors](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/errors/) |
| `401 Unauthorized` | Transport authentication failure when no valid identity is present. | HTTP status remains appropriate at the ASP.NET Core boundary. | [Authorization](/docs/hotchocolate/v16/securing-your-api/authorization/) |
| `403 Forbidden` | Field authorization error, nullable field, or domain payload outcome based on client needs. | Endpoint-level denial can still be HTTP `403`. | [Security and API boundaries](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/security-and-api-boundaries/) |
| `404 Not Found` | Nullable data, typed domain error, or top-level error depending on meaning. | Unknown HTTP path remains transport `404`. | [Nullability](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/nullability/) |
| `409 Conflict` | Typed domain error such as version conflict or workflow state conflict. | Use HTTP only for transport-level conflict outside GraphQL execution. | [Errors](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/errors/) |
| `500 Internal Server Error` | Top-level GraphQL error with sanitized message and server-side logging. | Server availability and non-GraphQL failures still use HTTP status codes. | [Error handling guide](/docs/hotchocolate/v16/guides/error-handling/) |

A successful GraphQL HTTP response can contain `data`, `errors`, or both:

```json
{
  "data": {
    "productById": {
      "name": "GraphQL Workshop",
      "availability": null
    }
  },
  "errors": [
    {
      "message": "Availability could not be loaded.",
      "path": ["productById", "availability"],
      "extensions": {
        "code": "AVAILABILITY_UNAVAILABLE"
      }
    }
  ]
}
```

Expose expected business outcomes in the schema when clients must handle them. Log, sanitize, and report unexpected exceptions through the GraphQL error pipeline. Never leak exception details, SQL text, stack traces, downstream URLs, or infrastructure names in production responses.

**Checkpoint:** Classify each existing `ActionResult` branch as payload data, typed domain error, top-level GraphQL error, transport-level failure, or REST-only behavior.

# Adopt Incrementally with a Safety Checklist

Use a migration loop you can verify at every step:

- Choose one feature slice and one client.
- Keep the REST endpoint as a fallback while piloting the client.
- Design schema fields, inputs, payloads, nullability, and errors before writing resolvers.
- Reuse application services where controller and GraphQL behavior must match.
- Add tests that compare important REST and GraphQL outcomes for your selected slice.
- Keep authorization, validation, logging, metrics, and error redaction equivalent before switching clients.
- For EF-backed fields, compare SQL, query counts, and page sizes before and after adding projections, DataLoader, or batch resolvers.
- Document which API owns the client contract during the transition.
- Only deprecate or remove the REST endpoint after clients move and usage confirms it.
- Review production controls: request limits, cost analysis, paging limits, introspection policy, trusted documents, and observability.

**Troubleshooting:**

| Symptom | Likely cause | Fix |
| --- | --- | --- |
| `Query` has one field for every controller action. | Route templates were copied into the schema. | Redesign around entry points, object relationships, and domain commands. |
| Clients still make several GraphQL operations for one screen. | Former endpoints became isolated fields without relationships. | Add nested fields or reshape the entry point so the client can select related data in one operation. |
| Resolver logic drifts from controller logic. | Business rules were copied instead of shared. | Move reusable behavior behind services and call those from both boundaries during migration. |
| The schema exposes every EF entity and column. | The database model became the public contract. | Review fields, hide persistence details, use domain names, and introduce API-facing types where needed. |
| Projection does not reduce selected columns. | The resolver materializes data too early or returns a non-queryable result. | Keep queryable fields queryable where projections are desired. Move custom work to explicit fields. |
| A mutation returns only `true` or a status string. | REST status thinking stayed in the payload. | Return a payload with changed data and expected domain errors. |
| A not-found case confuses clients. | Every `404` was translated the same way. | Decide whether absence is nullable data, a typed domain error, an authorization outcome, or a transport failure. |
| Nested fields trigger many HTTP or database calls. | Controller endpoints were reused without batching or provider translation. | Plan DataLoader, projections, batch resolvers, or a dedicated service for the graph shape. |
| EF Core reports concurrent operation or disposed context errors. | `DbContext` lifetime does not match GraphQL execution or batching behavior. | Follow the v16 EF Core integration and DataLoader guidance. |
| Existing authorization works for REST but not GraphQL. | Endpoint-level and field-level authorization were conflated. | Keep transport authentication in ASP.NET Core middleware and add schema authorization where access differs by field or operation. |
| Development tooling or introspection is exposed without review. | GraphQL was added beside controllers without production endpoint checks. | Review setup, introspection, request limits, trusted documents, Nitro, and schema download behavior. |
| A REST endpoint was removed too early. | Migration success was measured by server implementation only. | Track clients, monitor usage, and deprecate only after adoption is verified. |

**Checkpoint:** Leave this page with a written first-slice migration plan and a verification checklist.

# What to Read Next

Choose the next page that matches your current task:

- Add GraphQL beside controllers: [Existing ASP.NET Core app](/docs/hotchocolate/v16/learn/4-installation-and-setup/existing-aspnet-core-app/) and [Endpoints](/docs/hotchocolate/v16/server/endpoints/)
- Decide field shapes: [Modeling entities vs operations](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/modeling-entities-vs-operations/) and [Schema design principles](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/schema-design-principles/)
- Learn operation syntax, variables, and selections: [Operations](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/operations/)
- Get resolver implementation details: [Resolvers](/docs/hotchocolate/v16/resolvers-and-data/resolvers/) and [Dependency Injection](/docs/hotchocolate/v16/resolvers-and-data/dependency-injection/)
- Data access guidance: [Connecting to real data](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/connecting-to-real-data/), [Fetching from databases](/docs/hotchocolate/v16/resolvers-and-data/fetching-from-databases/), and [Fetching from REST](/docs/hotchocolate/v16/resolvers-and-data/fetching-from-rest/)
- EF Core and list middleware: [Entity Framework Core](/docs/hotchocolate/v16/integrations/entity-framework/), [Projections](/docs/hotchocolate/v16/resolvers-and-data/projections/), [Filtering](/docs/hotchocolate/v16/resolvers-and-data/filtering/), [Sorting](/docs/hotchocolate/v16/resolvers-and-data/sorting/), and [Pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination/)
- Avoid N+1 calls: [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader/)
- Error design: [Errors](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/errors/), [Error handling guide](/docs/hotchocolate/v16/guides/error-handling/), and [Mutations](/docs/hotchocolate/v16/building-a-schema/mutations/)
- Security and rollout guidance: [Authorization](/docs/hotchocolate/v16/securing-your-api/authorization/), [Security and API boundaries](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/security-and-api-boundaries/), [Public API guide](/docs/hotchocolate/v16/guides/public-api/), and [First-Party API](/docs/hotchocolate/v16/guides/private-api/)
- Broader migration gateway: [Coming from another stack](/docs/hotchocolate/v16/learn/5-coming-from/)
