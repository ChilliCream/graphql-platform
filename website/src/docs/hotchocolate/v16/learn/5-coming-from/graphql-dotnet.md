---
title: "GraphQL.NET"
description: "Translate GraphQL.NET schema types, resolvers, dependency injection, execution assumptions, and tests into Hot Chocolate v16 migration choices."
---

If you already know GraphQL and .NET, you’re well prepared for this migration. Hot Chocolate introduces new conventions for schema authoring, resolver signatures, dependency injection, middleware, and testing, but the core GraphQL contract (types, fields, arguments, variables, validation, execution, nullability, and errors) remains the same.

This page guides you through migrating from GraphQL.NET to Hot Chocolate v16. It’s not a compatibility matrix or a full API mapping. Instead, you’ll learn how to pick your first migration target, understand the key differences, and find links to the canonical Hot Chocolate docs for deeper dives.

This guide assumes:

- You have experience building or maintaining a GraphQL.NET server with schemas, graph types, resolvers, dependency injection, and tests.
- You understand GraphQL schemas, operations, variables, validation, execution, errors, and introspection.
- You might be evaluating Hot Chocolate for a new service, a rewrite, or a focused migration of part of your schema.
- You want to preserve client behavior unless you intentionally evolve your schema.

# Start with the GraphQL knowledge you already have

When you migrate, you’re still working with the same GraphQL contract, but the .NET server conventions change.

Here’s how familiar GraphQL.NET concepts map to Hot Chocolate v16, and where you’ll find guidance on this page:

| GraphQL.NET concept you know | Hot Chocolate v16 concept to learn | Where this page covers it |
| --- | --- | --- |
| `Schema`, `Query`, `Mutation`, and `Subscription` graph types | Root operation types registered through the Hot Chocolate builder | [Translate schema construction patterns](#translate-schema-construction-patterns) |
| `ObjectGraphType<T>` and field configuration | Implementation-first resolver methods, inferred object types, or `ObjectType<T>` descriptors | [Compare the defaults before mapping APIs](#compare-the-defaults-before-mapping-apis) |
| Resolver delegates with a field context | Resolver methods whose parameters declare arguments, services, parents, and cancellation | [Translate resolver signatures and field context usage](#translate-resolver-signatures-and-field-context-usage) |
| `context.GetArgument<T>()`, `context.Source`, and service access | Method parameters, `[Parent]`, service parameters, and `IResolverContext` when needed | [Translate resolver signatures and field context usage](#translate-resolver-signatures-and-field-context-usage) |
| GraphQL.NET data loader, middleware, validation, listeners, and tests | Hot Chocolate DataLoader, field and data middleware, interceptors, instrumentation, schema snapshots, and operation tests | [Verify behavior that does not migrate mechanically](#verify-behavior-that-does-not-migrate-mechanically) |

If you want to learn Hot Chocolate from scratch, the [tutorial](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/) is a great starting point. For production migrations, you can move incrementally: translate a small type cluster or a root field, then compare the resulting schema and operation behavior.

**Checkpoint:** Make sure you can distinguish which parts of your current server are pure GraphQL concepts and which are GraphQL.NET-specific conventions.

# Compare the defaults before mapping APIs

In GraphQL.NET, you often build your server around explicit schema and graph type classes. For example:

```csharp
public sealed class ProductQuery : ObjectGraphType
{
    public ProductQuery(IProductService products)
    {
        Field<ProductGraphType>("product")
            .Argument<NonNullGraphType<IdGraphType>>("id")
            .ResolveAsync(async context =>
            {
                var id = context.GetArgument<int>("id");
                return await products.GetByIdAsync(id, context.CancellationToken);
            });
    }
}
```

Hot Chocolate v16 encourages an implementation-first approach:

- C# records, classes, properties, and resolver methods become schema members.
- Use attributes like `[QueryType]`, `[MutationType]`, and `[SubscriptionType]` to mark root operation contributors.
- The source generator creates registrations, which you add with `AddTypes()`.
- The generated GraphQL schema is still the contract your clients see.

You can express the same query shape with a resolver method:

```csharp
// Types/ProductQueries.cs
using HotChocolate.Types;

namespace Store.Api.Types;

[QueryType]
public static partial class ProductQueries
{
    public static async Task<Product?> GetProductByIdAsync(
        int id,
        IProductService products,
        CancellationToken ct)
        => await products.GetByIdAsync(id, ct);
}

public sealed record Product(int Id, string Name);
```

With the analyzer package and `AddTypes()`, `GetProductByIdAsync` appears in your schema as:

```graphql
type Query {
  productById(id: Int!): Product
}
```

If you need more explicit control, code-first is still available:

```csharp
public sealed class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor.Name("CatalogProduct");

        descriptor
            .Field(p => p.Id)
            .Type<NonNullType<IdType>>();
    }
}
```

Here’s a good rule of thumb for choosing your approach:

| If your current GraphQL.NET code... | Start with... | Why |
| --- | --- | --- |
| Defines fields that match your C# models or service methods | Implementation-first | Resolver methods and inferred object types reduce repetitive configuration. |
| Needs precise field names, ignored members, descriptions, directives, deprecations, or custom type binding | Code-first descriptors or targeted attributes | You need explicit control over the schema contract. |
| Must preserve an SDL or introspection contract for clients | Schema output comparison first | Decide on authoring style after you know if you’re preserving or redesigning. |

Watch out for these common pitfalls:

- Not every `ObjectGraphType<T>` needs to become an `ObjectType<T>`. Many fields are better as resolver methods.
- You don’t have to use a constructor to configure every field. Hot Chocolate offers more flexible patterns.
- Use your existing SDL or introspection output as a comparison artifact, not necessarily as the new source format.

For a deeper decision guide, see [Implementation-first vs code-first](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/implementation-first-vs-code-first/).

**Checkpoint:** For one graph type, decide which fields can use implementation-first and which need explicit descriptor configuration.

# Translate schema construction patterns

Let the generated schema output guide your migration. If your clients depend on the current GraphQL.NET schema, export or capture that schema before you start. Compare field names, argument names, type names, nullability, default values, descriptions, deprecations, directives, and custom scalars between the old and new schemas.

Here’s how common GraphQL.NET items map to Hot Chocolate, and what to check in your schema output:

| Existing GraphQL.NET item | Likely Hot Chocolate shape | Verify in schema output | Canonical docs |
| --- | --- | --- | --- |
| Query graph type | `[QueryType]` resolver class with `AddTypes()`, or `AddQueryType<T>()` for code-first | Root field names, argument names, return types, and nullability | [Queries](/docs/hotchocolate/v16/building-a-schema/queries/) |
| Mutation graph type | `[MutationType]` resolver class, mutation conventions, or explicit mutation type | Payload shape, input names, error behavior, and serial execution assumptions | [Mutations](/docs/hotchocolate/v16/building-a-schema/mutations/) |
| Subscription graph type | `[SubscriptionType]` resolver class plus subscription provider and transport setup | Event field names, topic behavior, WebSocket behavior, and authorization | [Subscriptions](/docs/hotchocolate/v16/building-a-schema/subscriptions/) |
| `ObjectGraphType<Order>` | Inferred `Order` object, type extension, wrapper model, or `ObjectType<Order>` | Exposed fields, ignored members, descriptions, nullability, and field middleware | [Object types](/docs/hotchocolate/v16/building-a-schema/object-types/) |
| Field argument | Resolver method parameter or descriptor argument | Required vs. optional, default value, input type, and variable coercion | [Arguments](/docs/hotchocolate/v16/building-a-schema/arguments/) |
| Custom scalar | Built-in scalar package or custom scalar registration | Serialization, parsing, variable input, literals, and error messages | [Scalars](/docs/hotchocolate/v16/building-a-schema/scalars/) |

For implementation-first registration, make sure to include the analyzer package and module metadata as shown in the v16 examples.

Install the required packages:

```bash
dotnet add package HotChocolate.AspNetCore
dotnet add package HotChocolate.Types.Analyzers
```

Add the analyzer module metadata:

```csharp
// Properties/ModuleInfo.cs
using HotChocolate;

[assembly: Module("Types")]
```

Register GraphQL and map the endpoint:

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.AddGraphQL().AddTypes();

var app = builder.Build();

app.MapGraphQL();

app.Run();
```

`AddTypes()` picks up the generated type registrations from your module. If you skip the analyzer package or module metadata, `[QueryType]` examples might compile but not add the expected fields to your schema.

For the earlier `ProductQueries` example, you should see this schema output:

```graphql
type Query {
  productById(id: Int!): Product
}

type Product {
  id: Int!
  name: String!
}
```

Pay close attention to names and nullability. These are common sources of migration drift. Hot Chocolate removes `Get` and `Async` from method names and converts names to camel case. Nullable reference type annotations affect GraphQL nullability. Set names or nullability explicitly if you need compatibility.

For more setup details, see [ASP.NET Core setup](/docs/hotchocolate/v16/learn/4-installation-and-setup/aspnet-core/) and [Building a schema](/docs/hotchocolate/v16/building-a-schema/).

**Checkpoint:** For one existing graph type, write down for each field: the Hot Chocolate shape you’ll use, the schema output you want to preserve, and the docs page you’ll reference.

# Translate resolver signatures and field context usage

This is often the biggest day-to-day change you’ll notice.

In GraphQL.NET, resolvers typically receive a single context object and extract everything from it:

```csharp
.ResolveAsync(async context =>
{
    var id = context.GetArgument<int>("id");
    var viewer = context.Source as Viewer;
    var service = context.RequestServices.GetRequiredService<IProductService>();

    return await service.GetProductForViewerAsync(
        viewer!.Id,
        id,
        context.CancellationToken);
});
```

In Hot Chocolate, write resolver method signatures that declare exactly what each field needs:

```csharp
// Types/ViewerExtensions.cs
using HotChocolate;
using HotChocolate.Types;

namespace Store.Api.Types;

[ExtendObjectType<Viewer>]
public static partial class ViewerExtensions
{
    public static async Task<Product?> GetProductAsync(
        [Parent] Viewer viewer,
        int id,
        IProductService products,
        CancellationToken ct)
        => await products.GetProductForViewerAsync(viewer.Id, id, ct);
}
```

This signature makes each dependency clear:

- `[Parent] Viewer viewer` is the parent object.
- `int id` is the GraphQL field argument.
- `IProductService products` is a registered service.
- `CancellationToken ct` is canceled if the request is aborted.

`IResolverContext` is still available for advanced scenarios, but you rarely need to use it for arguments or services. Don’t mechanically translate every `context.GetArgument<T>()` to `IResolverContext.ArgumentValue<T>()`. Method parameters are usually clearer and easier to test.

You can also resolve fields with public properties:

```csharp
public sealed record Product(int Id, string Name);
```

When you expose `Product`, Hot Chocolate infers:

```graphql
type Product {
  id: Int!
  name: String!
}
```

Async data fetching works the same way. Pass `CancellationToken` into your database, HTTP, or queue calls. For relationship fields that would otherwise trigger N+1 queries, use [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader/) or batch resolver patterns.

For more, see [Resolvers](/docs/hotchocolate/v16/resolvers-and-data/resolvers/) and [Resolver and data middleware model](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/resolver-and-data-middleware-model/).

**Checkpoint:** Rewrite one of your current resolver signatures. List which values become arguments, parent values, services, cancellation tokens, or (rarely) `IResolverContext` access.

# Map service access and lifetimes deliberately

Hot Chocolate integrates with ASP.NET Core dependency injection, so you register your application services as usual:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IProductService, ProductService>();

builder.AddGraphQL().AddTypes();
```

Resolver methods can take registered services as parameters:

```csharp
[QueryType]
public static partial class ProductQueries
{
    public static async Task<Product?> GetProductByIdAsync(
        int id,
        IProductService products,
        CancellationToken ct)
        => await products.GetByIdAsync(id, ct);
}
```

Use this checklist to map your services:

1. List all services used by your current GraphQL.NET resolvers, graph type constructors, schema services, user-context patterns, and middleware.
2. Confirm each service’s registration and lifetime in ASP.NET Core DI.
3. Move business logic into application services where possible, so REST, GraphQL.NET, and Hot Chocolate don’t duplicate rules.
4. Inject services into Hot Chocolate resolver parameters when the dependency is field-specific.
5. Use constructor injection for your own application services, but be careful injecting into GraphQL type definitions. Schema types have framework-owned lifetimes.
6. Test scoped data access with representative nested queries.

Service lifetimes are still important. Query fields may execute independently, so a service that worked when called serially might fail if nested fields run in parallel or with different scopes. Pay special attention to EF Core `DbContext` usage. Follow Hot Chocolate’s [Dependency Injection](/docs/hotchocolate/v16/resolvers-and-data/dependency-injection/) and [Entity Framework](/docs/hotchocolate/v16/integrations/entity-framework) guidance instead of copying GraphQL.NET user-context or service-locator patterns directly.

If your GraphQL.NET server used a user-context dictionary, decide where that data belongs in Hot Chocolate: ASP.NET Core authentication, scoped services, request context, global state, interceptors, or a field argument. Don’t move an untyped dictionary into every resolver without reviewing its lifetime and responsibility.

**Checkpoint:** Pick one service dependency and decide if it belongs in a resolver parameter, an application service constructor, explicit type configuration, or request-level infrastructure.

# Verify behavior that does not migrate mechanically

Don’t stop at compiling code. Verify the runtime behavior, even if the schema looks similar. Here’s what to check, and why differences can appear:

| Behavior | What to verify | Why it can drift |
| --- | --- | --- |
| Schema shape | SDL, type names, field names, argument names, nullability, descriptions, directives, deprecations, and scalars | Hot Chocolate’s naming and nullability inference may differ from explicit GraphQL.NET configuration. |
| Successful operations | `data` shape, variables, fragments, selected nested fields, and no unexpected `errors` | Resolver binding, middleware, and result completion are framework-specific. |
| Validation failures | Error shape for invalid fields, bad variables, missing required arguments, and input coercion | Client-facing validation messages and extensions can differ. |
| Domain errors and exceptions | Error codes, messages, extensions, null propagation, and HTTP behavior | Error filtering and exception handling are server-specific. |
| Authorization | Policy mapping, directive or middleware placement, unauthenticated behavior, forbidden behavior, and partial data | The rule may move from graph type setup to attributes, middleware, or ASP.NET Core. |
| Data loading | Query counts, batching, caching, and nested relationship behavior | A field-by-field rewrite can recreate N+1 queries. |
| Field middleware | Paging, filtering, sorting, projections, authorization, and custom behavior | Hot Chocolate field and data middleware have their own order and provider behavior. |
| Instrumentation | Logs, metrics, traces, operation names, and request correlation | GraphQL.NET listeners and execution hooks do not map directly. |
| Subscriptions and transport | Schema shape, WebSocket protocol, pub/sub provider, authorization, and rollout behavior | Subscription transport and provider setup are not interchangeable. |
| Cancellation | Aborted request cancels downstream I/O where expected | Cancellation must be declared and passed through. |

Hot Chocolate’s DataLoader is the standard way to solve N+1 relationship loading, but use its own patterns and registration. Don’t assume a GraphQL.NET loader has the same lifecycle, cache boundary, or API.

For custom middleware, validation, listeners, and instrumentation, map intent to the right Hot Chocolate feature: field middleware, request middleware, authorization, error filters, interceptors, instrumentation, operation validation, request limits, or tests.

See [How GraphQL executes](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/how-graphql-executes/), [Errors](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/errors/), [Authorization](/docs/hotchocolate/v16/securing-your-api/authorization/), and [Testing GraphQL APIs](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/testing-graphql-apis/) to build your verification plan.

**Checkpoint:** Make a list of schema, operation, error, authorization, data-loading, and transport behaviors to test before you call the migration complete for a slice.

# Choose a migration strategy

Pick the approach that fits your risk and goals.

| If you want to... | Strategy | First success signal |
| --- | --- | --- |
| Learn Hot Chocolate before touching production code | Clean rebuild through the tutorial | You can build a small v16 server, add a field, write a resolver, add DataLoader, and test it. |
| Preserve an existing client contract | Schema-preserving slice migration | One root field or type cluster produces matching SDL and representative operation results. |
| Improve a schema with design debt | Client-task-preserving redesign | Clients can complete the same tasks through a reviewed schema evolution plan. |
| Keep the current GraphQL.NET server online during rollout | Side-by-side rollout | Old and new endpoints can be compared while clients move through a controlled release path. |

For a schema-preserving slice migration:

1. Capture your current GraphQL.NET schema SDL or introspection output.
2. Save key client operations and their expected responses.
3. Pick one root field or a small type cluster.
4. Choose implementation-first or code-first for that slice.
5. Implement the resolver and service injection using Hot Chocolate conventions.
6. Compare the schema output.
7. Run representative success, validation, error, authorization, and nested relationship operations.
8. Measure query counts or service calls for nested selections.
9. Decide if the next slice should follow the same pattern or adjust the design.

ASP.NET Core can host multiple endpoints if your architecture allows. Side-by-side operation is a rollout strategy, not a substitute for contract tests. Keep routes, authentication, CORS, logging, and dashboards clear so teams know which server handled each request.

For testing, use a mix of:

- schema snapshots or schema diffs
- golden operation tests
- resolver, service, or DataLoader tests for focused logic
- HTTP integration tests for endpoint behavior
- authorization and error tests for client-visible failure contracts

See the [Testing guide](/docs/hotchocolate/v16/guides/testing/) for Hot Chocolate test setup.

**Checkpoint:** Choose your migration strategy and your first slice before starting the migration branch.

# Troubleshoot common migration surprises

Here are some common issues you might encounter, why they happen, and how to address them:

| Symptom | Likely cause | Fix direction |
| --- | --- | --- |
| Every `ObjectGraphType<T>` became `ObjectType<T>` and the new code feels larger | Migration treated code-first as the only target | Re-evaluate which fields can use implementation-first resolver methods. Use descriptors only where explicit schema control is needed. |
| A field or argument name changed | Hot Chocolate naming conventions, `Get`/`Async` removal, parameter names, or descriptor config differ | Compare SDLs and set names explicitly where compatibility is required. |
| A nullable field became non-null, or vice versa | Hot Chocolate inferred nullability from C# types and nullable reference annotations differently | Review annotations and explicit type config. Add schema snapshots. |
| A service fails under nested queries | Lifetime, scoped service behavior, EF Core usage, or parallel field execution assumptions changed | Follow Hot Chocolate DI and EF guidance. Use DataLoader for keyed relationship loading. |
| The migrated resolver still pulls everything from `IResolverContext` | Mechanical translation from GraphQL.NET field context usage | Prefer method parameters for arguments, services, parent values, and cancellation. Use context only for context-specific features. |
| Clients see different error shapes | Error filtering, exception handling, validation messages, null propagation, or HTTP transport behavior differs | Define the client-facing error contract and add golden operation tests for failure cases. |
| Performance regressed for nested selections | Batching, DataLoader, projections, or query composition were not recreated | Identify fan-out fields, add DataLoader or data middleware, and measure query counts. |
| Unit tests pass but integration behavior differs | Tests covered methods without schema binding, middleware, DI, transport, or serialization | Add schema snapshots and operation tests through the Hot Chocolate executor or HTTP endpoint. |

# Where to go next

Pick your next step based on your current goal:

- **Build a clean Hot Chocolate slice:** Start with the [tutorial](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/) or see [ASP.NET Core setup](/docs/hotchocolate/v16/learn/4-installation-and-setup/aspnet-core/).
- **Choose an authoring style:** Read [Implementation-first vs code-first](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/implementation-first-vs-code-first/) and [Building a schema](/docs/hotchocolate/v16/building-a-schema/).
- **Rewrite resolvers:** Use [Resolvers](/docs/hotchocolate/v16/resolvers-and-data/resolvers/) and [Dependency Injection](/docs/hotchocolate/v16/resolvers-and-data/dependency-injection/).
- **Prevent N+1 queries:** Use [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader/) and [Resolver and data middleware model](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/resolver-and-data-middleware-model/).
- **Migrate data-shaped fields:** Review [Pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination/), [Filtering](/docs/hotchocolate/v16/resolvers-and-data/filtering/), [Sorting](/docs/hotchocolate/v16/resolvers-and-data/sorting/), and [Projections](/docs/hotchocolate/v16/resolvers-and-data/projections/).
- **Preserve the contract:** Read [Schema evolution](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/schema-evolution/), [Testing GraphQL APIs](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/testing-graphql-apis/), and [Errors](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/errors/).
- **Return to the migration router:** Use [Coming from another stack](/docs/hotchocolate/v16/learn/5-coming-from/) if you’re migrating from REST, OData, Apollo Server, EF-backed APIs, or upgrading Hot Chocolate versions.

**Final checkpoint:** You should now have a migration strategy, a first slice, and a verification plan that compares schema output and representative operations before clients depend on the new server.
