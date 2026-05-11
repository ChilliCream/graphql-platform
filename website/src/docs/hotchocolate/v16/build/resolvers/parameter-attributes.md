---
title: "Resolver Parameter Attributes"
---

# Bind resolver parameters with attributes

Resolver parameters in Hot Chocolate can originate from several sources: the GraphQL client, the current parent object, dependency injection, request state, subscription events, or runtime helpers. When conventions are not sufficient, parameter attributes clarify the source of each value.

This page covers the resolver parameter binding attributes available in Hot Chocolate:

- `[Argument]`
- `[Parent]`
- `[Service]`
- `[GlobalState]`
- `[ScopedState]`
- `[LocalState]`
- `[EventMessage]`

Note: `[GraphQLName]` is used to rename an argument parameter, but it is a general schema naming attribute, not a binding-source attribute.

## Identify the source of each parameter

When reading a resolver signature, move from left to right and determine the source for each parameter:

```csharp
#nullable enable

using System.Security.Claims;

[ObjectType<Product>]
public static partial class ProductNode
{
    public static async Task<Brand?> GetBrandAsync(
        [Parent(requires: nameof(Product.BrandId))] Product product,
        [Argument("locale")] string? locale,
        [Service("catalog")] CatalogService catalog,
        IBrandByIdDataLoader brandById,
        ClaimsPrincipal? user,
        CancellationToken cancellationToken)
    {
        var brand = await brandById.LoadAsync(product.BrandId, cancellationToken);

        return brand is null
            ? null
            : catalog.Localize(brand, locale, user);
    }
}
```

Expected field shape:

```graphql
type Product {
  brand(locale: String): Brand
}
```

| Parameter           | Source               | Why                                               |
| ------------------- | -------------------- | ------------------------------------------------- |
| `product`           | Parent object        | `[Parent]` binds the current `Product`.           |
| `locale`            | GraphQL argument     | `[Argument("locale")]` binds the client argument. |
| `catalog`           | Keyed DI service     | `[Service("catalog")]` resolves a keyed service.  |
| `brandById`         | DataLoader           | DataLoader parameters bind by type.               |
| `user`              | Authenticated user   | `ClaimsPrincipal` binds by type.                  |
| `cancellationToken` | Request cancellation | `CancellationToken` binds by type.                |

Attributes are one way to control parameter binding. Hot Chocolate also recognizes registered services, DataLoaders, and built-in runtime types by their parameter type.

## Understand the binding order

If a parameter could match multiple sources, Hot Chocolate resolves bindings in the following order:

| Order | Binding                    | Trigger                                                                                |
| ----- | -------------------------- | -------------------------------------------------------------------------------------- |
| 1     | Parent value               | `[Parent]`                                                                             |
| 2     | Service                    | `[Service]`                                                                            |
| 3     | GraphQL argument           | `[Argument]`                                                                           |
| 4     | Global state               | `[GlobalState]`                                                                        |
| 5     | Scoped state               | `[ScopedState]`                                                                        |
| 6     | Local state                | `[LocalState]`                                                                         |
| 7     | Selection helper           | `[IsSelected]`                                                                         |
| 8     | Subscription event payload | `[EventMessage]`                                                                       |
| 9     | Inferred service           | Parameter type is registered in DI.                                                    |
| 10    | Built-in runtime type      | `CancellationToken`, `IResolverContext`, `ClaimsPrincipal`, and other framework types. |
| 11    | Argument fallback          | Any remaining parameter.                                                               |

> **Important:** If a parameter type is registered in dependency injection, Hot Chocolate will treat it as a service, not as a GraphQL argument. Use `[Argument]` if you want the value to come from the client.

Apply only one binding-source attribute per parameter. Using multiple binding attributes on a single parameter can make the code harder to read and may obscure the true source of the value.

## Select the appropriate binding attribute

| Need                     | Parameter shape                                               | Attribute needed? | Notes                                                         |
| ------------------------ | ------------------------------------------------------------- | ----------------- | ------------------------------------------------------------- |
| Client input             | `string search`                                               | Usually no        | Unrecognized parameters become GraphQL arguments.             |
| Explicit client input    | `[Argument("slug")] string value`                             | Yes               | Use when the C# name differs or service inference would win.  |
| Rename client input      | `[GraphQLName("slug")] string value`                          | Yes               | Renames the argument, but does not change the binding source. |
| Parent value             | `[Parent] Product product`                                    | Yes               | Use in static, external, and type-extension resolvers.        |
| Parent field requirement | `[Parent(requires: nameof(Product.BrandId))] Product product` | Yes               | Records which parent member the resolver needs.               |
| Unkeyed service          | `CatalogService catalog`                                      | No, if registered | Hot Chocolate infers registered services.                     |
| Keyed service            | `[Service("catalog")] CatalogService catalog`                 | Yes               | The key must match the DI registration.                       |
| Request state            | `[GlobalState("TenantId")] string tenantId`                   | Yes               | Reads request-level context data.                             |
| Propagated state         | `[ScopedState] string locale`                                 | Yes               | Without a key, the parameter name is used.                    |
| Field-local state        | `[LocalState("cacheKey")] string? cacheKey`                   | Yes               | Reads state local to the current field pipeline.              |
| Subscription payload     | `[EventMessage] Book book`                                    | Yes               | Use only in subscription event resolvers.                     |
| Authenticated user       | `ClaimsPrincipal? user`                                       | No                | Depends on authentication setup.                              |
| Resolver context         | `IResolverContext context`                                    | No                | Prefer specific parameters for common inputs.                 |
| Cancellation             | `CancellationToken cancellationToken`                         | No                | Pass it to downstream async APIs.                             |
| DataLoader               | `IBrandByIdDataLoader loader`                                 | No                | DataLoader setup belongs on the DataLoader page.              |

## Bind client input with arguments

A resolver parameter is treated as a GraphQL argument when Hot Chocolate does not recognize it as an injected value:

```csharp
[QueryType]
public static partial class ProductQueries
{
    public static async Task<IReadOnlyList<Product>> GetProductsAsync(
        string? search,
        int limit,
        ProductSearchService products,
        CancellationToken cancellationToken)
        => await products.SearchAsync(search, limit, cancellationToken);
}
```

Expected SDL:

```graphql
type Query {
  products(search: String, limit: Int!): [Product!]!
}
```

`ProductSearchService` and `CancellationToken` are runtime inputs and do not appear in the schema.

Use `[Argument]` to make argument binding explicit or to use a different public name:

```csharp
[QueryType]
public static partial class ProductQueries
{
    public static Task<Product?> GetProductBySlugAsync(
        [Argument("slug")] string productSlug,
        ProductSearchService products,
        CancellationToken cancellationToken)
        => products.FindBySlugAsync(productSlug, cancellationToken);
}
```

Expected SDL:

```graphql
type Query {
  productBySlug(slug: String!): Product
}
```

Alternatively, `[GraphQLName]` can rename an argument:

```csharp
[QueryType]
public static partial class ProductQueries
{
    public static Product? GetProduct(
        [GraphQLName("sku")] string stockKeepingUnit,
        ProductSearchService products)
        => products.FindBySku(stockKeepingUnit);
}
```

Expected SDL:

```graphql
type Query {
  product(sku: String!): Product
}
```

Choose `[Argument]` when you want to clarify the value's source. Use `[GraphQLName]` when you want to control the schema name.

Advanced argument shapes:

| Shape                        | Use                                                               |
| ---------------------------- | ----------------------------------------------------------------- |
| `Optional<T>`                | Distinguish omitted input from explicit `null`.                   |
| `IValueNode`                 | Read the raw GraphQL literal for advanced scenarios.              |
| `[Argument("name")] T value` | Force argument binding and optionally override the argument name. |

## Access parent values with `[Parent]`

Apply `[Parent]` when a field resolver needs access to the object that contains the field:

```csharp
[ObjectType<Product>]
public static partial class ProductNode
{
    public static async Task<Brand?> GetBrandAsync(
        [Parent] Product product,
        IBrandByIdDataLoader brandById,
        CancellationToken cancellationToken)
        => await brandById.LoadAsync(product.BrandId, cancellationToken);
}
```

Example client query:

```graphql
query GetProducts {
  products {
    name
    brand {
      name
    }
  }
}
```

The `[Parent] Product product` parameter is not a GraphQL argument. Hot Chocolate provides it from the current `Product` in the resolver tree.

If your resolver depends on a specific parent member, use `[Parent(requires: nameof(Product.BrandId))]`:

```csharp
public static Task<Brand?> GetBrandAsync(
    [Parent(requires: nameof(Product.BrandId))] Product product,
    IBrandByIdDataLoader brandById,
    CancellationToken cancellationToken)
    => brandById.LoadAsync(product.BrandId, cancellationToken);
```

For more on type extensions, instance members, `context.Parent<T>()`, and batch resolver parent patterns, see [Parent access](./parent-attribute).

## Inject services without confusing them with arguments

Hot Chocolate automatically infers registered services:

```csharp
[QueryType]
public static partial class ProductQueries
{
    public static Task<Product?> GetProductByIdAsync(
        [ID<Product>] int id,
        ProductService products,
        CancellationToken cancellationToken)
        => products.FindByIdAsync(id, cancellationToken);
}
```

Expected SDL:

```graphql
type Query {
  productById(id: ID!): Product
}
```

`ProductService` is not part of the schema because Hot Chocolate resolves it from dependency injection.

Use `[Service]` when you need a keyed service:

```csharp
[QueryType]
public static partial class ProductQueries
{
    public static Task<Product?> GetArchivedProductAsync(
        [ID<Product>] int id,
        [Service("archive")] ProductService products,
        CancellationToken cancellationToken)
        => products.FindByIdAsync(id, cancellationToken);
}
```

| Parameter                                       | Behavior                                                                                 |
| ----------------------------------------------- | ---------------------------------------------------------------------------------------- |
| `ProductService products`                       | Required service. Resolution fails if the service is missing.                            |
| `[Service("archive")] ProductService products`  | Required keyed service. The key must be registered.                                      |
| `[Service] ProductService? products`            | Optional service. The parameter can receive `null`.                                      |
| `[Service("archive")] ProductService? products` | Optional keyed service. The parameter can receive `null` when the key is not registered. |

Do not use `[ScopedService]` for resolver parameters. Use an inferred service parameter or `[Service]`, and refer to the service injection documentation for scopes and lifetimes.

For more on service lifetimes, resolver scopes, constructor injection, and keyed service setup, see [Service Injection](./service-injection).

## Read request and resolver state

State attributes are useful when middleware, interceptors, or earlier resolver logic write values into the execution context.

```csharp
[QueryType]
public static partial class ProductQueries
{
    public static Task<IReadOnlyList<Product>> GetProductsAsync(
        [GlobalState("TenantId")] string tenantId,
        ProductService products,
        CancellationToken cancellationToken)
        => products.GetByTenantAsync(tenantId, cancellationToken);
}
```

| Attribute       | Reads from           | Key behavior                    | Missing value behavior                                                                            |
| --------------- | -------------------- | ------------------------------- | ------------------------------------------------------------------------------------------------- |
| `[GlobalState]` | Request context data | Attribute key or parameter name | Nullable or defaulted parameters receive a default value. Non-nullable required parameters throw. |
| `[ScopedState]` | Scoped context data  | Attribute key or parameter name | Nullable or defaulted parameters receive a default value. Non-nullable required parameters throw. |
| `[LocalState]`  | Local context data   | Attribute key or parameter name | Nullable or defaulted parameters receive a default value. Non-nullable required parameters throw. |

If you do not specify a key, Hot Chocolate uses the parameter name:

```csharp
public static string GetLocalizedName(
    [Parent] Product product,
    [ScopedState] string locale)
    => product.GetName(locale);
```

This reads scoped state using the key `locale`.

Choose parameter shapes that match your contract:

```csharp
public static Task<IReadOnlyList<Product>> GetProductsAsync(
    [GlobalState("TenantId")] string tenantId,
    [ScopedState("Locale")] string locale = "en",
    [LocalState("cacheKey")] string? cacheKey = null,
    ProductService products,
    CancellationToken cancellationToken)
    => products.GetProductsAsync(tenantId, locale, cacheKey, cancellationToken);
```

### Create a custom state attribute

You can derive from a state attribute to centralize a key:

```csharp
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class TenantIdAttribute : GlobalStateAttribute
{
    public TenantIdAttribute()
        : base("TenantId")
    {
    }
}

[QueryType]
public static partial class ProductQueries
{
    public static Task<IReadOnlyList<Product>> GetProductsAsync(
        [TenantId] string tenantId,
        ProductService products,
        CancellationToken cancellationToken)
        => products.GetByTenantAsync(tenantId, cancellationToken);
}
```

This pattern also works with `[ScopedState]` and `[LocalState]` when a repeated key belongs to that state source.

### Write state with `SetState<T>`

Use `SetState<T>` when a resolver needs a typed setter for a state entry:

```csharp
[QueryType]
public static partial class LocaleQueries
{
    public static string SetLocale(
        [Argument] string locale,
        [ScopedState("Locale")] SetState<string> setLocale)
    {
        setLocale(locale);
        return locale;
    }
}
```

This approach is best for advanced resolver pipelines. For request setup, prefer interceptors or field middleware that write state before dependent resolvers run.

## Handle subscription payloads with `[EventMessage]`

Apply `[EventMessage]` to the payload parameter in a subscription event resolver:

```csharp
[SubscriptionType]
public static partial class BookSubscriptions
{
    [Subscribe]
    public static Book OnBookAdded([EventMessage] Book book)
        => book;
}
```

Expected SDL:

```graphql
type Subscription {
  bookAdded: Book!
}
```

Example client operation:

```graphql
subscription WatchBooks {
  bookAdded {
    id
    title
  }
}
```

`[EventMessage]` is only relevant when Hot Chocolate is resolving a subscription event. For more on topics, transports, and publishing events, see [Subscriptions](/docs/hotchocolate/v16/build/schema-elements/operations-subscriptions).

## Use runtime helper parameters without attributes

Some resolver parameters are bound by type. Do not add parameter attributes for these values.

| Parameter type                          | Use                                                                                                | Note                                                                                       |
| --------------------------------------- | -------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------ |
| `CancellationToken`                     | Cancel async work when the request stops.                                                          | Pass it to EF Core, HTTP clients, services, and DataLoaders.                               |
| `IResolverContext`                      | Access advanced APIs for arguments, parent values, services, state, selections, paths, and errors. | Prefer typed parameters for common inputs.                                                 |
| `ClaimsPrincipal` or `ClaimsPrincipal?` | Read the authenticated user.                                                                       | Configure authentication and authorization. Use nullable when anonymous access is allowed. |
| DataLoader interface or `IDataLoader`   | Batch and cache data loading within a request.                                                     | Register or generate the DataLoader before injecting it.                                   |

Example:

```csharp
[QueryType]
public static partial class UserQueries
{
    public static async Task<User?> GetMeAsync(
        ClaimsPrincipal? user,
        UserService users,
        CancellationToken cancellationToken)
    {
        var userId = user?.FindFirst("sub")?.Value;

        return userId is null
            ? null
            : await users.GetByIdAsync(userId, cancellationToken);
    }
}
```

Expected SDL:

```graphql
type Query {
  me: User
}
```

`ClaimsPrincipal`, `UserService`, and `CancellationToken` are not GraphQL arguments. For HTTP-specific data access, see the HTTP context documentation.

## Troubleshoot ambiguous resolver signatures

| Symptom                                                | Likely cause                                                                | Solution                                                                                  |
| ------------------------------------------------------ | --------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------- |
| Parameter does not appear as a GraphQL argument.       | The type is registered in DI or matched by a built-in binding.              | Add `[Argument]` if the value should come from the client.                                |
| Service parameter appears as a GraphQL argument.       | The service type is not registered or cannot be inferred.                   | Register the service or use `[Service]` for keyed services.                               |
| Keyed service fails to resolve.                        | The DI key does not match `[Service("key")]`, or the service is missing.    | Check the keyed registration and parameter nullability.                                   |
| State parameter throws or receives `null`.             | The key is wrong, the state was not written, or nullability does not match. | Check the key, writer middleware or interceptor, and default value.                       |
| `ClaimsPrincipal` is missing.                          | Authentication, authorization, or request setup is incomplete.              | Configure authentication and call `.AddAuthorization()` on the GraphQL builder if needed. |
| `[Parent]` parameter fails validation.                 | The parent parameter type does not match the object type context.           | Use the correct parent CLR type or move the resolver to the matching type.                |
| Subscription payload fails to bind.                    | `[EventMessage]` is used outside a subscription event resolver.             | Use it only on subscription event resolvers.                                              |
| Multiple binding attributes are used on one parameter. | The parameter has conflicting binding intent.                               | Use only one binding-source attribute per parameter.                                      |

Decision checklist:

1. Does the client supply it? Use a normal parameter or `[Argument]`.
2. Is it the current object in the resolver tree? Use `[Parent]`.
3. Is it an application service? Register it in DI, then use an inferred service parameter or `[Service]` for keyed services.
4. Is it request, scoped, or local state? Use the appropriate state attribute.
5. Is it a subscription event payload? Use `[EventMessage]`.
6. Is it a supported runtime helper type? Use the type directly without an attribute.

## Go next

| Goal                               | Page                                                                                                                                            |
| ---------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------- |
| Design GraphQL arguments           | [Arguments](/docs/hotchocolate/v16/build/schema-elements/arguments)                                                                             |
| Review resolver signatures         | [Resolver Signatures](./resolver-signature)                                                                                                     |
| Access parent values               | [Parent access](./parent-attribute)                                                                                                             |
| Inject services and keyed services | [Service Injection](./service-injection)                                                                                                        |
| Read and write request state       | [Global State](/docs/hotchocolate/v16/build/server-configuration/global-state)                                                                  |
| Configure request interceptors     | [Interceptors](/docs/hotchocolate/v16/build/server-configuration/interceptors)                                                                  |
| Add subscriptions                  | [Subscriptions](/docs/hotchocolate/v16/build/schema-elements/operations-subscriptions)                                                          |
| Access authenticated users         | [Authentication](/docs/hotchocolate/v16/build/security/authentication) and [Authorization](/docs/hotchocolate/v16/build/security/authorization) |
| Batch related data                 | [DataLoader](/docs/hotchocolate/v16/build/dataloader)                                                                                           |
| Access HTTP-specific data          | [HTTP Context and State](./ihttpcontextaccessor-and-context)                                                                                    |
| Review built-in attributes         | [Attribute Reference](/docs/hotchocolate/v16/build/attributes/custom-descriptor-attributes)                                                     |
