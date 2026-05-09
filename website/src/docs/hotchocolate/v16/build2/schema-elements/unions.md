---
title: "Unions"
---

Use a union when one field can return one of several object types and those object types do not share a useful field contract.

```graphql
union SearchResult = Product | Brand | Category
```

Clients query union fields with `__typename` and fragments:

```graphql
query Search($term: String!) {
  search(term: $term) {
    __typename
    ... on Product {
      sku
      name
      price
    }
    ... on Brand {
      id
      name
    }
    ... on Category {
      slug
      displayName
    }
  }
}
```

If a field returns a union, clients select member fields through fragments. The union itself has no selectable fields other than `__typename`.

# Decide whether a union fits

Choose a union when a field can return one of several concrete output shapes and there is no useful shared field contract.

| Need                                    | Use                                        | Notes                                                  |
| --------------------------------------- | ------------------------------------------ | ------------------------------------------------------ | ----- | ---------- |
| Search across unrelated object types    | Union                                      | Example: `SearchResult = Product                       | Brand | Category`. |
| Expected domain outcomes                | Union                                      | Keep the result set small and meaningful.              |
| Shared selectable fields                | [Interface](./interfaces)                  | Interfaces declare fields clients can select directly. |
| One concrete shape with optional fields | [Object type](./object-types)              | Avoid a union when one object describes the result.    |
| Input alternatives                      | [Input object types](./input-object-types) | GraphQL unions are output-only.                        |

Use an object type when the result is one shape. Use an interface when the possible result types share fields that are part of the API contract. Use a union when the result types are related by context, not by fields.

## Compare unions and interfaces

| Question                                                                   | Union                   | Interface          |
| -------------------------------------------------------------------------- | ----------------------- | ------------------ | --------- | -------------------------- |
| Do member types need shared fields?                                        | No                      | Yes                |
| Can clients select fields directly on the abstract type?                   | Only `__typename`       | Yes, shared fields |
| Can members be object types?                                               | Yes                     | Yes                |
| Can members be scalars, enums, input objects, interfaces, or other unions? | No                      | No                 |
| Best example                                                               | `SearchResult = Product | Brand              | Category` | `Node`, `Message`, `Error` |

For shared-field polymorphism, see [Interfaces](./interfaces). This page focuses on unions.

# Define a union with implementation-first C#

Use an empty marker interface to group the CLR types that belong to the union. Keep the marker empty. If the marker needs shared fields, model a GraphQL interface instead.

```csharp
// Types/ISearchResult.cs
#nullable enable

using HotChocolate.Types;

[UnionType("SearchResult")]
public interface ISearchResult
{
}
```

Define the concrete result types:

```csharp
// Types/Product.cs
#nullable enable

public sealed record Product(
    string Sku,
    string Name,
    decimal Price) : ISearchResult;

// Types/Brand.cs
public sealed record Brand(
    string Id,
    string Name) : ISearchResult;

// Types/Category.cs
public sealed record Category(
    string Slug,
    string DisplayName) : ISearchResult;
```

Return the marker interface from the resolver and return concrete CLR objects at runtime:

```csharp
// Types/SearchQueries.cs
#nullable enable

[QueryType]
public static partial class SearchQueries
{
    public static IReadOnlyList<ISearchResult> Search(string term)
    {
        var results = new List<ISearchResult>
        {
            new Brand("brand-1", "Contoso"),
            new Category("running", "Running")
        };

        if (term.Contains("shoe", StringComparison.OrdinalIgnoreCase))
        {
            results.Insert(0, new Product("sku-1", "Trail Shoe", 129.95m));
        }

        return results;
    }
}
```

Register the union and every possible object member:

```csharp
// Program.cs
builder
    .AddGraphQL()
    .AddUnionType<ISearchResult>()
    .AddType<Product>()
    .AddType<Brand>()
    .AddType<Category>();
```

This produces a schema like this:

```graphql
type Query {
  search(term: String!): [SearchResult!]!
}

union SearchResult = Product | Brand | Category

type Product {
  sku: String!
  name: String!
  price: Decimal!
}

type Brand {
  id: String!
  name: String!
}

type Category {
  slug: String!
  displayName: String!
}
```

Hot Chocolate must know every concrete member type. Register the member classes directly, register their `ObjectType<T>` classes, or use the generated registration method your project already uses.

# Define a union with `UnionType`

Use `UnionType` when you prefer descriptor-based configuration or want the union definition in one type class.

```csharp
// Types/SearchResultType.cs
#nullable enable

using HotChocolate.Types;

public sealed class SearchResultType : UnionType
{
    protected override void Configure(IUnionTypeDescriptor descriptor)
    {
        descriptor
            .Name("SearchResult")
            .Description("A result returned by catalog search.");

        descriptor.Type<ProductType>();
        descriptor.Type<BrandType>();
        descriptor.Type<CategoryType>();
    }
}

public sealed class ProductType : ObjectType<Product>
{
}

public sealed class BrandType : ObjectType<Brand>
{
}

public sealed class CategoryType : ObjectType<Category>
{
}
```

Register the union type class:

```csharp
builder
    .AddGraphQL()
    .AddType<SearchResultType>();
```

`descriptor.Type<TObjectType>()` adds object type members to the union. When you use explicit object type classes, the union definition makes those member types reachable from the schema. See [Object types](./object-types) for member object type configuration.

# Define a typed union with `UnionType<T>`

Use `UnionType<T>` when a C# interface or abstract base class is the resolver return type and you want the schema type class tied to that CLR abstraction.

```csharp
// Types/SearchResultType.cs
#nullable enable

using HotChocolate.Types;

public sealed class SearchResultType : UnionType<ISearchResult>
{
    protected override void Configure(IUnionTypeDescriptor descriptor)
    {
        descriptor.Name("SearchResult");

        descriptor.Type<ProductType>();
        descriptor.Type<BrandType>();
        descriptor.Type<CategoryType>();
    }
}
```

Concrete object members still need to be registered or reachable. `UnionType<T>` gives Hot Chocolate the CLR abstraction for the union, not a replacement for the member object types.

# Register a union in the schema

Use one registration style consistently in your application.

| Scenario                                    | Registration                                                                                            | Notes                                                          |
| ------------------------------------------- | ------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------- |
| Marker interface with `[UnionType]`         | `.AddUnionType<ISearchResult>().AddType<Product>().AddType<Brand>().AddType<Category>()`                | Clear for implementation-first examples.                       |
| Generated implementation-first registration | Your generated `.AddTypes()` setup                                                                      | Use this when your project already registers annotated types.  |
| Inline descriptor                           | `.AddUnionType(d => d.Name("SearchResult").Type<ProductType>().Type<BrandType>().Type<CategoryType>())` | Useful for compact schema setup.                               |
| `UnionType` class                           | `.AddType<SearchResultType>()`                                                                          | Member object type classes can be referenced from `Configure`. |
| `UnionType<T>` class                        | `.AddType<SearchResultType>()`                                                                          | Keeps the union tied to a CLR marker or abstract base.         |

Do not register Hot Chocolate schema type classes as runtime values. Resolver methods should return domain objects such as `Product`, `Brand`, and `Category`.

# Return concrete values from resolvers

Resolvers return concrete CLR objects. They do not return a GraphQL union wrapper.

```text
resolver returns Product, Brand, or Category
        |
        v
Hot Chocolate resolves the matching union member object type
        |
        v
client receives __typename and fragment-selected fields
```

For a non-null list with non-null items, return a non-null collection of non-null values:

```csharp
public static IReadOnlyList<ISearchResult> Search(string term)
{
    return new ISearchResult[]
    {
        new Product("sku-1", "Trail Shoe", 129.95m),
        new Brand("brand-1", "Contoso")
    };
}
```

With nullable reference types enabled, this maps to a field like this:

```graphql
type Query {
  search(term: String!): [SearchResult!]!
}
```

If the field can return no result, use a nullable return type:

```csharp
public static ISearchResult? FeaturedResult()
{
    return null;
}
```

For list and non-null wrapper details, see [Lists and Non-Null](./lists-and-non-null).

## Customize runtime matching when needed

Most schemas should use the default runtime type matching. Configure `ResolveAbstractType(...)` on the union descriptor when your resolver returns wrapper values, discriminator values, or another representation that Hot Chocolate cannot match to a registered object type by default.

Use these schema options for diagnostics and advanced matching behavior:

| Option                        | Purpose                                                             |
| ----------------------------- | ------------------------------------------------------------------- |
| `StrictRuntimeTypeValidation` | Enables stricter runtime validation for union and interface values. |
| `DefaultIsOfTypeCheck`        | Provides the fallback check used for abstract type matching.        |

See [Options](/docs/hotchocolate/v16/api-reference/options) for option names and defaults.

# Query a union field

Select `__typename` as the discriminator. Select member fields inside inline or named fragments.

```graphql
query Search($term: String!) {
  search(term: $term) {
    __typename
    ... on Product {
      sku
      name
      price
    }
    ... on Brand {
      id
      name
    }
    ... on Category {
      slug
      displayName
    }
  }
}
```

Example response:

```json
{
  "data": {
    "search": [
      {
        "__typename": "Product",
        "sku": "sku-1",
        "name": "Trail Shoe",
        "price": 129.95
      },
      {
        "__typename": "Brand",
        "id": "brand-1",
        "name": "Contoso"
      }
    ]
  }
}
```

This query is invalid because `name` is not a field on the union itself:

```graphql
query Search($term: String!) {
  search(term: $term) {
    name
  }
}
```

Move member fields into fragments:

```graphql
query Search($term: String!) {
  search(term: $term) {
    __typename
    ... on Product {
      name
    }
    ... on Brand {
      name
    }
  }
}
```

When you generate client models, keep a fallback branch for unknown `__typename` values. Adding a new union member can be a valid schema evolution step, and older clients should fail predictably.

# Use unions for typed domain outcomes sparingly

A union can model an expected business outcome that the client must handle.

```graphql
union UserByEmailResult = User | UserNotFoundError

type User {
  id: ID!
  email: String!
}

type UserNotFoundError {
  message: String!
}

type Query {
  userByEmail(email: String!): UserByEmailResult!
}
```

Implementation-first C#:

```csharp
#nullable enable

using HotChocolate.Types;

[UnionType("UserByEmailResult")]
public interface IUserByEmailResult
{
}

public sealed record User(
    string Id,
    string Email) : IUserByEmailResult;

public sealed record UserNotFoundError(
    string Message) : IUserByEmailResult;

[QueryType]
public static partial class UserQueries
{
    public static IUserByEmailResult GetUserByEmail(
        string email,
        UserService users)
    {
        var user = users.FindByEmail(email);

        if (user is null)
        {
            return new UserNotFoundError($"No user found for '{email}'.");
        }

        return user;
    }
}
```

Query the result with fragments:

```graphql
query GetUser($email: String!) {
  userByEmail(email: $email) {
    __typename
    ... on User {
      id
      email
    }
    ... on UserNotFoundError {
      message
    }
  }
}
```

Use this pattern for expected domain states, such as "not found" when the client must distinguish it from a successful result. Do not use result unions for unexpected system failures, infrastructure failures, programming errors, or transport errors.

Mutation conventions can generate similar unions for payload `errors` fields. See [Mutations](./operations-mutations) and [Error Handling](/docs/hotchocolate/v16/guides/error-handling) for those workflows. If several error types need shared fields such as `message`, use an [interface](./interfaces) for the shared error contract.

# Apply union rules

| Rule                                                                         | What it means                                                                    |
| ---------------------------------------------------------------------------- | -------------------------------------------------------------------------------- |
| Union members must be object types                                           | Use `Product`, `Brand`, `Category`, or their `ObjectType<T>` classes as members. |
| Members cannot be scalars, enums, input objects, interfaces, or other unions | Create object types that wrap the data you need to expose.                       |
| Unions are output-only                                                       | Do not use unions for arguments or input object fields.                          |
| A union declares no fields of its own                                        | Clients select `__typename` and use fragments for member fields.                 |
| Member object types do not need shared fields                                | Use an interface when shared fields are part of the contract.                    |
| One object type can be in more than one union                                | Do this when it keeps each field result meaningful.                              |
| Every returned runtime value must match a member                             | Register every concrete type that a resolver can return.                         |

# Troubleshoot union fields

| Symptom                                             | Likely cause                                                                    | Fix                                                                                                                                                      |
| --------------------------------------------------- | ------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `Cannot query field "name" on type "SearchResult"`  | The query selected a member field directly on the union                         | Use an inline or named fragment on `Product`, `Brand`, or another member type.                                                                           |
| A union member is missing from SDL or introspection | The member object type is not registered or reachable                           | Register the concrete class with `.AddType<T>()`, add the object type through `descriptor.Type<TObjectType>()`, or include it in generated registration. |
| Runtime execution cannot resolve the abstract type  | The resolver returned a CLR type that does not map to a registered union member | Return a valid member object, add the missing member, or configure `ResolveAbstractType(...)`.                                                           |
| A fragment never matches                            | The GraphQL object type name does not match the runtime value being returned    | Check registration, object type naming, and custom runtime matching logic.                                                                               |
| Client code fails after you add a member            | The client treated the union switch as closed                                   | Query `__typename` and keep a fallback branch for unknown member names.                                                                                  |
| A union was used in an input field                  | GraphQL unions are not input types                                              | Create a dedicated input object, or use `@oneOf` input objects for mutually exclusive input choices.                                                     |

# Next steps

- Define member shapes with [Object types](./object-types).
- Use shared output fields with [Interfaces](./interfaces).
- Model input alternatives with [Input object types](./input-object-types#use-oneof-for-mutually-exclusive-input-choices).
- Expose union-returning fields from [Queries](./operations-queries).
- Review nullable wrappers with [Lists and Non-Null](./lists-and-non-null).
- Learn typed mutation errors in [Mutations](./operations-mutations).
- Review abstract type options in [Options](/docs/hotchocolate/v16/api-reference/options).
