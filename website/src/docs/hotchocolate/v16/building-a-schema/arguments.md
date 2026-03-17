---
title: "Arguments"
---

GraphQL arguments let clients pass values to individual fields. In Hot Chocolate, each parameter on a resolver method becomes a field argument in the schema, unless it is a recognized service type (like `CancellationToken` or a registered service).

**GraphQL schema**

```graphql
type Query {
  user(id: ID!): User
  users(role: UserRole, limit: Int = 10): [User!]!
}
```

**Client query**

```graphql
{
  user(id: "UHJvZHVjdAppMQ==") {
    name
  }
}
```

Arguments are frequently provided through variables, which separate the static query structure from the dynamic runtime values:

```graphql
query ($userId: ID!) {
  user(id: $userId) {
    name
  }
}
```

# Defining Arguments

Method parameters on a resolver become GraphQL arguments.

<ExampleTabs>
<Implementation>

```csharp
// Types/UserQueries.cs
[QueryType]
public static partial class UserQueries
{
    public static User? GetUser(string username, UserService users)
        => users.FindByName(username);
}
```

The `username` parameter becomes a `username: String!` argument. The `UserService` parameter is recognized as a service and is not exposed in the schema.

</Implementation>
<Code>

```csharp
// Types/UserQueriesType.cs
public class UserQueriesType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("user")
            .Argument("username", a => a.Type<NonNullType<StringType>>())
            .Resolve(context =>
            {
                var username = context.ArgumentValue<string>("username");
                // ...
            });
    }
}
```

</Code>
</ExampleTabs>

# Renaming Arguments

Use `[GraphQLName]` to change the argument name in the schema while keeping the C# parameter name unchanged.

```csharp
// Types/UserQueries.cs
[QueryType]
public static partial class UserQueries
{
    public static User? GetUser(
        [GraphQLName("name")] string username,
        UserService users)
        => users.FindByName(username);
}
```

This produces `user(name: String!): User` in the schema.

# Optional Arguments

An argument is required when its C# type is non-nullable. Make an argument optional by using a nullable type.

```csharp
// Types/ProductQueries.cs
[QueryType]
public static partial class ProductQueries
{
    public static List<Product> GetProducts(string? category, int? limit)
    {
        // Both arguments are optional
        // ...
    }
}
```

This produces:

```graphql
type Query {
  products(category: String, limit: Int): [Product!]!
}
```

When using nullable reference types (recommended), `string` maps to `String!` and `string?` maps to `String`. See [Non-Null](/docs/hotchocolate/v16/defining-a-schema/non-null) for details.

# Default Values

Use `[DefaultValue]` to assign a default to an argument. The default appears in the schema and is used when the client omits the argument.

```csharp
// Types/ProductQueries.cs
[QueryType]
public static partial class ProductQueries
{
    public static List<Product> GetProducts(
        [DefaultValue(10)] int limit)
    {
        // ...
    }
}
```

This produces `products(limit: Int! = 10): [Product!]!`.

C# default parameter values also work:

```csharp
public static List<Product> GetProducts(int limit = 10)
```

# The ID Attribute

The `[ID]` attribute marks a parameter as a GraphQL `ID` scalar. When combined with [global object identification](/docs/hotchocolate/v16/defining-a-schema/relay), it also deserializes the opaque global ID back to the underlying value.

```csharp
// Types/ProductQueries.cs
[QueryType]
public static partial class ProductQueries
{
    public static Product? GetProduct([ID] int id, CatalogContext db)
        => db.Products.Find(id);
}
```

To restrict the ID to a specific type (ensuring only IDs serialized for `Product` are accepted):

```csharp
public static Product? GetProduct(
    [ID(nameof(Product))] int id,
    CatalogContext db)
    => db.Products.Find(id);
```

In v16, you can also use the generic form `[ID<Product>]` which infers the type name automatically.

# Complex Arguments

When an argument needs multiple fields, use an [input object type](/docs/hotchocolate/v16/defining-a-schema/input-object-types) instead of multiple scalar arguments.

```csharp
// Types/BookFilterInput.cs
public record BookFilterInput(string? Title, string? Author, int? Year);

// Types/BookQueries.cs
[QueryType]
public static partial class BookQueries
{
    public static List<Book> GetBooks(BookFilterInput filter, CatalogContext db)
    {
        // ...
    }
}
```

This produces:

```graphql
input BookFilterInput {
  title: String
  author: String
  year: Int
}

type Query {
  books(filter: BookFilterInput!): [Book!]!
}
```

# Troubleshooting

## Argument not appearing in schema

Verify the parameter is `public` and is not a type that Hot Chocolate recognizes as a service (like `CancellationToken`, `ClaimsPrincipal`, or types registered in DI). Service types are injected but not exposed as arguments.

## Wrong nullability on argument

Check whether nullable reference types are enabled in your project. With NRT enabled, `string` is non-null and `string?` is nullable. Without NRT, all reference types default to nullable. See [Non-Null](/docs/hotchocolate/v16/defining-a-schema/non-null).

## ID deserialization fails

If a global ID cannot be deserialized, the error message indicates a type mismatch. Verify that the `[ID]` type name matches the type that serialized the ID. If using `[ID(nameof(Product))]`, the ID must have been serialized for the `Product` type.

# Next Steps

- **Need structured input?** See [Input Object Types](/docs/hotchocolate/v16/defining-a-schema/input-object-types).
- **Need to understand nullability?** See [Non-Null](/docs/hotchocolate/v16/defining-a-schema/non-null).
- **Need global IDs?** See [Relay](/docs/hotchocolate/v16/defining-a-schema/relay).
- **Need to set up resolvers?** See [Resolvers](/docs/hotchocolate/v16/fetching-data/resolvers).
