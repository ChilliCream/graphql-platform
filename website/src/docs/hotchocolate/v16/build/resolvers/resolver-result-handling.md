---
title: "Resolver Result Handling"
---

Resolvers return C# values, which Hot Chocolate transforms into GraphQL field values, field errors, typed payload errors, or subscription event results.

Refer to this page when you know the resolver signature you want to use and need to understand what the client will receive. For parameter binding and general signature details, see [Resolver Signatures](./resolver-signature).

## Understanding the Result Lifecycle

Hot Chocolate processes resolver results in several stages:

```text
resolver returns value
  -> await Task<T> or ValueTask<T>
  -> normalize special results such as IError and FieldResult
  -> materialize list-like sources when the field completes as a list
  -> complete the GraphQL value using the field type and nullability
  -> serialize data and errors
```

This lifecycle leads to a practical guideline: select the return type that matches the result you want the client to observe.

| Return shape                             | Client-visible result                                                                |
| ---------------------------------------- | ------------------------------------------------------------------------------------ |
| `T`                                      | A completed field value.                                                             |
| `T?`                                     | A completed field value or `null` with no error when `null` is valid data.           |
| `IError` or `GraphQLException`           | The field becomes `null` and the response includes a top-level GraphQL error.        |
| `ReportError` plus `T`                   | The field can still return data and the response includes a top-level GraphQL error. |
| Mutation payload errors or `FieldResult` | The error appears in the mutation payload as typed data.                             |
| `IQueryable<T>` or `IExecutable<T>`      | Data middleware can compose work before the list is materialized.                    |
| `ValueTask<ISourceStream<T>>`            | A subscription source stream that can produce many GraphQL results.                  |

For details on middleware ordering and execution, see the [Field Middleware](/docs/hotchocolate/v16/build/execution-engine/field-middleware) and data middleware documentation. This page focuses on what your resolver returns and what the client receives.

## Returning Successful Values

Return a plain value when the data is already available.

```csharp
public sealed record Product([property: ID] int Id, string Name);

[QueryType]
public static partial class ProductQueries
{
    public static Product GetFeaturedProduct()
        => new(1, "Chai");
}
```

With nullable reference types enabled, a non-nullable `Product` return type results in a non-null GraphQL field:

```graphql
type Query {
  featuredProduct: Product!
}
```

A query retrieves the value under `data`:

```graphql
query {
  featuredProduct {
    id
    name
  }
}
```

```json
{
  "data": {
    "featuredProduct": {
      "id": "1",
      "name": "Chai"
    }
  }
}
```

Use `Task<T>` when the resolver performs asynchronous I/O. Use `ValueTask<T>` if the API you call already returns `ValueTask<T>` or if you have measured a performance benefit on a hot path.

```csharp
[QueryType]
public static partial class ProductQueries
{
    public static Task<Product> GetProductAsync(
        [ID] int id,
        ProductRepository products,
        CancellationToken cancellationToken)
        => products.GetRequiredProductAsync(id, cancellationToken);
}
```

Hot Chocolate awaits the task and completes the awaited value. If the request is canceled, completion may stop before the response is produced.

## Returning Nullable Values Intentionally

Use nullable return types to represent valid absence, such as when an entity is not found. Avoid using `null` to mask infrastructure failures or bugs.

```csharp
[QueryType]
public static partial class ProductQueries
{
    public static Task<Product?> GetProductByIdAsync(
        [ID] int id,
        ProductRepository products,
        CancellationToken cancellationToken)
        => products.FindProductAsync(id, cancellationToken);
}
```

The nullable return type produces a nullable field:

```graphql
type Query {
  productById(id: ID!): Product
}
```

When the repository returns `null`, the response contains `null` and no GraphQL error:

```json
{
  "data": {
    "productById": null
  }
}
```

### Aligning C# and GraphQL Nullability

When nullable reference types are enabled, Hot Chocolate maps C# nullability to GraphQL nullability as follows:

| C# return type    | Typical GraphQL field type | Meaning                                                 |
| ----------------- | -------------------------- | ------------------------------------------------------- |
| `Product`         | `Product!`                 | The resolver must produce a product.                    |
| `Product?`        | `Product`                  | The resolver may return `null`.                         |
| `List<Product>`   | `[Product!]!`              | The list and every item must be present.                |
| `List<Product>?`  | `[Product!]`               | The list may be `null`, but present items are non-null. |
| `List<Product?>`  | `[Product]!`               | The list is present, but items may be `null`.           |
| `List<Product?>?` | `[Product]`                | The list and its items may be `null`.                   |

For the complete mapping, see [Non-Null](/docs/hotchocolate/v16/build/schema-elements/lists-and-non-null) and [Lists](/docs/hotchocolate/v16/build/schema-elements/lists-and-non-null).

### Null Propagation for Non-Null Violations

If a resolver returns `null` for a non-null field, Hot Chocolate reports an execution error. GraphQL then propagates `null` to the nearest nullable parent.

```csharp
public sealed record Store(string Name, Product FeaturedProduct);

[QueryType]
public static partial class StoreQueries
{
    public static Store GetStore()
        => new("Downtown", null!);
}
```

```graphql
type Query {
  store: Store
}

type Store {
  name: String!
  featuredProduct: Product!
}
```

The `featuredProduct` field cannot be `null`, so the nullable `store` parent becomes `null`:

```json
{
  "errors": [
    {
      "message": "Cannot return null for non-nullable field.",
      "path": ["store", "featuredProduct"]
    }
  ],
  "data": {
    "store": null
  }
}
```

If a list item is non-null and an item completes as `null`, the same rule applies to the list item and then to the nearest nullable parent.

## Returning Collections and Deferred Data Sources

Select a collection shape based on who should control query execution.

| Return shape                                           | Use when                                                             | Execution behavior                                                                                               | Details                                                                                                            |
| ------------------------------------------------------ | -------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------ |
| `IEnumerable<T>`, `IReadOnlyList<T>`, `List<T>`, array | Items are already in memory.                                         | Hot Chocolate completes the value as a GraphQL list.                                                             | [Lists](/docs/hotchocolate/v16/build/schema-elements/lists-and-non-null)                                           |
| `IQueryable<T>`                                        | EF Core or another LINQ provider should compose database operations. | Data middleware can add paging, projection, filtering, and sorting before materialization.                       | [Projections](/docs/hotchocolate/v16/build/filtering-sorting-projections/projection-options)                       |
| `IExecutable<T>`                                       | A Hot Chocolate data provider exposes provider-neutral execution.    | Hot Chocolate recognizes it as list-like and calls executable operations such as `ToListAsync`.                  | [Executable](/docs/hotchocolate/v16/build/execution-internals/executable)                                          |
| `Connection<T>`                                        | The resolver owns cursor paging.                                     | The returned value is already a connection payload.                                                              | [Pagination](/docs/hotchocolate/v16/build/pagination)                                                              |
| `QueryContext<T>`                                      | You use the integrated projection, filtering, and sorting pattern.   | Execution applies the query context behavior.                                                                    | [Projections](/docs/hotchocolate/v16/build/filtering-sorting-projections/projection-options#querycontextt-pattern) |
| `IAsyncEnumerable<T>`                                  | Items are produced asynchronously.                                   | Ordinary field completion materializes the sequence for a list result. Use subscriptions for live event streams. | [Subscriptions](/docs/hotchocolate/v16/build/schema-elements/operations-subscriptions)                             |

### Let Data Middleware Compose `IQueryable<T>`

Return `IQueryable<T>` when paging, projection, filtering, or sorting should be handled by the data provider.

```csharp
[QueryType]
public static partial class ProductQueries
{
    [UsePaging]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public static IQueryable<Product> GetProducts(CatalogContext db)
        => db.Products;
}
```

A client receives a paged result:

```graphql
query {
  products(first: 2) {
    nodes {
      id
      name
    }
  }
}
```

```json
{
  "data": {
    "products": {
      "nodes": [
        { "id": "1", "name": "Chai" },
        { "id": "2", "name": "Chang" }
      ]
    }
  }
}
```

Do not call `ToList`, `ToArray`, or `AsEnumerable` in the resolver when middleware should still compose the query. If you need a single value from a queryable source, `[UseFirstOrDefault]` and `[UseSingleOrDefault]` can rewrite a list-like resolver into a nullable single field.

### Return `IExecutable<T>` from provider abstractions

`IExecutable<T>` keeps database-specific details out of the GraphQL layer.

```csharp
public interface IUserRepository
{
    IExecutable<User> FindAll();
}

[QueryType]
public static partial class UserQueries
{
    public static IExecutable<User> GetUsers(IUserRepository repository)
        => repository.FindAll();
}
```

```graphql
type Query {
  users: [User!]!
}
```

Filtering, sorting, and projection providers can work with the executable before Hot Chocolate materializes it.

### Return `Connection<T>` when the resolver owns paging

Use `Connection<T>` when your application or external API performs cursor paging and returns a completed connection shape.

```csharp
[QueryType]
public static partial class ProductQueries
{
    public static async Task<Connection<Product>> GetProductsAsync(
        PagingArguments paging,
        ProductSearchService search,
        CancellationToken cancellationToken)
        => await search.GetProductsAsync(paging, cancellationToken);
}
```

Clients query it like any other connection:

```graphql
query {
  products(first: 2) {
    edges {
      cursor
      node {
        id
        name
      }
    }
    pageInfo {
      hasNextPage
    }
  }
}
```

The resolver returns a connection object. Hot Chocolate completes the connection fields rather than applying offset or cursor logic to an `IQueryable<T>` result.

## Choosing Between `null`, GraphQL Errors, and Domain Results

Result handling is most predictable when each outcome has a distinct meaning.

| Scenario                                            | Prefer                                                | Client sees                                  |
| --------------------------------------------------- | ----------------------------------------------------- | -------------------------------------------- |
| Optional entity is not found.                       | `T?` and a nullable field.                            | `data.field: null` with no error.            |
| Unexpected bug or infrastructure failure occurs.    | Throw or let the exception bubble.                    | `data.field: null` plus top-level `errors`.  |
| Field has partial data and a warning.               | `IResolverContext.ReportError` plus a returned value. | `data.field` plus top-level `errors`.        |
| Whole field should fail with a known GraphQL error. | Return `IError` or throw `GraphQLException`.          | `data.field: null` plus top-level `errors`.  |
| Mutation business rule fails.                       | Mutation payload error or `FieldResult`.              | Typed error data under the mutation payload. |
| Query has expected business variants.               | Union result.                                         | Client selects fields with inline fragments. |

Top-level GraphQL errors serve as execution diagnostics. They are useful for failed fields and partial responses, but are not the ideal default for expected business outcomes.

## Reporting Field Errors Without Domain Payloads

Use field errors when a GraphQL field fails, or when you want to include a non-terminating diagnostic alongside partial data.

### Report a non-terminating error and still return data

`IResolverContext.ReportError` adds an error to the response without replacing the field value.

```csharp
[QueryType]
public static partial class ProductQueries
{
    public static Product GetFeaturedProduct(IResolverContext context)
    {
        context.ReportError(
            ErrorBuilder.New()
                .SetMessage("Featured product inventory is stale.")
                .SetCode("INVENTORY_STALE")
                .Build());

        return new Product(1, "Chai");
    }
}
```

```json
{
  "errors": [
    {
      "message": "Featured product inventory is stale.",
      "code": "INVENTORY_STALE",
      "path": ["featuredProduct"]
    }
  ],
  "data": {
    "featuredProduct": {
      "id": "1",
      "name": "Chai"
    }
  }
}
```

### Return or throw a terminating field error

Return `IError` when the field should become `null` and the response should contain a known GraphQL error. When you use descriptor-based configuration, keep the GraphQL field type on the descriptor and return the error from the resolver delegate.

```csharp
public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor
            .Field("discontinuedProduct")
            .Type<ProductType>()
            .Resolve(_ => ErrorBuilder.New()
                .SetMessage("This product is no longer available.")
                .SetCode("PRODUCT_DISCONTINUED")
                .Build());
    }
}
```

```json
{
  "errors": [
    {
      "message": "This product is no longer available.",
      "code": "PRODUCT_DISCONTINUED",
      "path": ["discontinuedProduct"]
    }
  ],
  "data": {
    "discontinuedProduct": null
  }
}
```

Throw `GraphQLException` when exception flow fits the code path better:

```csharp
[QueryType]
public static partial class ProductQueries
{
    public static Product GetInternalProduct()
        => throw new GraphQLException(
            ErrorBuilder.New()
                .SetMessage("You cannot access this product.")
                .SetCode("PRODUCT_FORBIDDEN")
                .Build());
}
```

Unhandled exceptions are also converted into GraphQL errors. By default, the serialized message is `Unexpected Execution Error`, and exception details are hidden. See [Errors](/docs/hotchocolate/v16/build/errors) and [Error Handling](/docs/hotchocolate/v16/_leagcy/guides/error-handling) for filters, error construction, and exception detail options.

## Modeling Mutation Outcomes with Payload Errors and `FieldResult`

Expected mutation failures should be represented as typed data, allowing clients to handle them without parsing top-level error messages.

With mutation conventions, declare domain exceptions using `[Error]`:

```csharp
public sealed class UserNameTakenException(string username) : Exception
{
    public string Username { get; } = username;
}

[MutationType]
public static partial class UserMutations
{
    [Error(typeof(UserNameTakenException))]
    public static async Task<User?> UpdateUserNameAsync(
        [ID] Guid userId,
        string username,
        UserService users,
        CancellationToken cancellationToken)
        => await users.UpdateNameAsync(userId, username, cancellationToken);
}
```

Hot Chocolate adds an `errors` field to the generated payload:

```graphql
type Mutation {
  updateUserName(input: UpdateUserNameInput!): UpdateUserNamePayload!
}

type UpdateUserNamePayload {
  user: User
  errors: [UpdateUserNameError!]
}

union UpdateUserNameError = UserNameTakenError
```

A business-rule failure appears in the payload, not in top-level `errors`:

```json
{
  "data": {
    "updateUserName": {
      "user": null,
      "errors": [
        {
          "__typename": "UserNameTakenError",
          "message": "The username is already taken."
        }
      ]
    }
  }
}
```

`FieldResult<TResult, TError>` is the current name for mutation-style success-or-error return values. It replaced the older `MutationResult` naming. Use it when your resolver can return either the success value or a typed error object.

```csharp
public sealed record QuantityCannotBeNegativeError(int Quantity)
{
    public string Message => "Quantity cannot be negative.";
}

[MutationType]
public static partial class BasketMutations
{
    public static async Task<FieldResult<ShoppingBasket, QuantityCannotBeNegativeError>>
        AddToBasketAsync(
            [ID<Product>] int productId,
            int quantity,
            BasketService baskets,
            CancellationToken cancellationToken)
    {
        if (quantity < 0)
        {
            return new QuantityCannotBeNegativeError(quantity);
        }

        return await baskets.AddAsync(productId, quantity, cancellationToken);
    }
}
```

The implicit conversions create either the success result or the error result. Keep detailed mutation convention setup in the [Mutations](/docs/hotchocolate/v16/build/schema-elements/operations-mutations) page.

## Modeling Query Outcomes with Unions

Use a union when a query or subscription can return multiple expected types. This approach keeps business variants in `data` and allows clients to use inline fragments.

```csharp
public sealed record UserNotFoundError(string Email)
{
    public string Message => $"No user with email {Email} was found.";
}

[UnionType("UserByEmailResult")]
public interface IUserByEmailResult
{
}

public sealed record UserResult(User User) : IUserByEmailResult;

public sealed record UserNotFoundResult(UserNotFoundError Error) : IUserByEmailResult;

[QueryType]
public static partial class UserQueries
{
    public static async Task<IUserByEmailResult> GetUserByEmailAsync(
        string email,
        UserService users,
        CancellationToken cancellationToken)
    {
        var user = await users.FindByEmailAsync(email, cancellationToken);

        return user is null
            ? new UserNotFoundResult(new UserNotFoundError(email))
            : new UserResult(user);
    }
}
```

A client can handle each case explicitly:

```graphql
query {
  userByEmail(email: "ada@example.com") {
    __typename
    ... on UserResult {
      user {
        id
        name
      }
    }
    ... on UserNotFoundResult {
      error {
        message
      }
    }
  }
}
```

Use [Unions](/docs/hotchocolate/v16/build/schema-elements/unions) for detailed union declaration patterns. Use `IError`, `GraphQLException`, or `ReportError` for GraphQL execution diagnostics instead.

## Returning Subscription Event Payloads

A subscription operation can emit multiple GraphQL results. Each event triggers the payload resolver, and Hot Chocolate completes that return value as it would for any other field.

```csharp
[SubscriptionType]
public static partial class ProductSubscriptions
{
    [Subscribe]
    public static Product OnProductUpdated([EventMessage] Product product)
        => product;
}
```

Each published product produces a result with the same shape as a query response:

```json
{
  "data": {
    "productUpdated": {
      "id": "1",
      "name": "Chai"
    }
  }
}
```

Use a custom subscribe resolver when you need to control the source stream:

```csharp
[SubscriptionType]
public static partial class ProductSubscriptions
{
    public static ValueTask<ISourceStream<Product>> SubscribeToProducts(
        ITopicEventReceiver receiver)
        => receiver.SubscribeAsync<Product>("Products");

    [Subscribe(With = nameof(SubscribeToProducts))]
    public static Product OnProductUpdated([EventMessage] Product product)
        => product;
}
```

`ISourceStream<T>` is a subscription source stream. It is not the return shape for ordinary query fields. If a query resolver returns `IAsyncEnumerable<T>`, ordinary field completion materializes it as a list unless you use a verified incremental delivery feature. For subscription setup and transports, see [Subscriptions](/docs/hotchocolate/v16/build/schema-elements/operations-subscriptions) and [HTTP Transport](/docs/hotchocolate/v16/build/server-configuration/http-transport).

## Handling DataLoader and Batch Resolver Item Results

DataLoader calls typically return `Task<T?>`, `ValueTask<T?>`, or another awaitable result. After the DataLoader result is awaited, Hot Chocolate applies the same nullability and completion rules as for any other field.

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

Batch resolvers return one result per parent. The output count and order must match the parent list. Use `ResolverResult.Ok(value)` and `ResolverResult.Fail(error)` when one item should fail without failing the entire batch.

```csharp
[ObjectType<User>]
public static partial class UserNode
{
    [BatchResolver]
    public static List<ResolverResult> GetVerificationStatus([Parent] List<User> users)
    {
        return users.Select<User, ResolverResult>(user =>
        {
            if (user.Email is null)
            {
                return ResolverResult.Fail(
                    ErrorBuilder.New()
                        .SetMessage("User has no email address.")
                        .SetCode("EMAIL_MISSING")
                        .Build());
            }

            return ResolverResult.Ok(user.IsVerified ? "verified" : "pending");
        }).ToList();
    }
}
```

If the second user has no email, the response can contain data for the first item and an error path for the second item:

```json
{
  "errors": [
    {
      "message": "User has no email address.",
      "code": "EMAIL_MISSING",
      "path": ["users", 1, "verificationStatus"]
    }
  ],
  "data": {
    "users": [
      { "verificationStatus": "verified" },
      { "verificationStatus": null }
    ]
  }
}
```

`ResolverResult` is for batch resolver items. It is not the same as a regular resolver returning a single `IError`. For batching and caching details, see [DataLoader](/docs/hotchocolate/v16/build/dataloader).

## Troubleshooting Result Handling

| Problem                                                       | What to check                                                                                                                                          |
| ------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------ |
| A not-found result appears as an error.                       | Check whether the field is non-null in the schema or inferred from a non-nullable C# type. Return `T?` or change the field type when absence is valid. |
| A parent object disappeared after a child returned `null`.    | A non-null child field likely completed as `null`. GraphQL propagated `null` to the nearest nullable parent.                                           |
| An exception message is hidden.                               | Hot Chocolate hides exception details by default. Use error filters for safe messages. Enable `IncludeExceptionDetails` only for development.          |
| A business error appears in top-level `errors`.               | Model expected outcomes as mutation payload errors, `FieldResult`, or unions.                                                                          |
| `IAsyncEnumerable<T>` did not stream each item to the client. | Ordinary field completion materializes list-like results. Use subscriptions for live events or a verified incremental delivery feature.                |
| `IQueryable<T>` ran before paging or projection.              | Avoid materializing the query in the resolver. Return `IQueryable<T>` or `IExecutable<T>` and let middleware compose the operation.                    |
| A batch resolver error affected the wrong item.               | Verify the returned list has the same count and order as the parent list.                                                                              |
| Returning `IError` produced `null` data.                      | That is expected for a terminating field error. Use `ReportError` when you want to keep a field value.                                                 |

## Result Shape Quick Reference

| Resolver return value          | Typical schema shape                             | Client-visible behavior                                               | Best use case                                         |
| ------------------------------ | ------------------------------------------------ | --------------------------------------------------------------------- | ----------------------------------------------------- |
| `T`                            | `T!` for non-nullable references and value types | Field completes with a value.                                         | Required data.                                        |
| `T?`                           | `T`                                              | Field completes with a value or `null`.                               | Optional data.                                        |
| `Task<T>` or `ValueTask<T>`    | Shape of awaited `T`                             | Hot Chocolate awaits and completes the value.                         | Asynchronous I/O.                                     |
| `IEnumerable<T>` or `List<T>`  | `[T!]!` when list and items are non-null         | List is completed from in-memory items.                               | Already materialized collections.                     |
| `IQueryable<T>`                | `[T!]!` or connection with paging                | Middleware can compose provider operations before materialization.    | EF Core and LINQ providers.                           |
| `IExecutable<T>`               | `[T!]!`                                          | Executable operations materialize the result.                         | Provider-neutral data access.                         |
| `Connection<T>`                | `TConnection`                                    | Resolver supplies the connection payload.                             | Custom cursor paging.                                 |
| `QueryContext<T>`              | `[T!]!` or middleware-defined shape              | Query context carries projection, filtering, and sorting information. | Integrated data operations.                           |
| `IAsyncEnumerable<T>`          | `[T!]!`                                          | Ordinary field completion materializes a list.                        | Asynchronous list production, not live subscriptions. |
| `IError`                       | Any nullable field                               | Field becomes `null` and a top-level error is reported.               | Terminating field errors.                             |
| `IEnumerable<IError>`          | Any nullable field                               | Reports multiple GraphQL errors according to the error API.           | Multiple terminating field errors.                    |
| `GraphQLException`             | Any nullable field                               | Field becomes `null` and one or more top-level errors are reported.   | Exception-based field failure.                        |
| `FieldResult<TResult, TError>` | Mutation payload                                 | Success value or typed payload error.                                 | Mutation business outcomes.                           |
| `ResolverResult`               | Batch resolver item field                        | Per-parent success or per-parent error.                               | Batch resolver item failures.                         |
| `ValueTask<ISourceStream<T>>`  | Subscription field                               | Source stream can emit many events.                                   | Custom subscription sources.                          |

## Next Steps

- Learn how to design resolver parameters in [Resolver Signatures](./resolver-signature).
- Configure nullability with [Non-Null](/docs/hotchocolate/v16/build/schema-elements/lists-and-non-null).
- Model mutation payloads in [Mutations](/docs/hotchocolate/v16/build/schema-elements/operations-mutations).
- Explore field error construction in [Errors](/docs/hotchocolate/v16/build/errors).
- Add batching with [DataLoader](/docs/hotchocolate/v16/build/dataloader).
