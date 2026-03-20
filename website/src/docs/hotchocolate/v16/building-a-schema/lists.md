---
title: "Lists"
---

GraphQL lists represent ordered collections of elements. When a resolver returns any .NET collection type, Hot Chocolate exposes it as a list in the schema.

**GraphQL schema**

```graphql
type Query {
  users: [User!]!
}
```

**Client query**

```graphql
{
  users {
    id
    name
  }
}
```

The response contains an ordered array of objects matching the requested fields.

# Supported Collection Types

Hot Chocolate recognizes common .NET collection types and maps them to GraphQL lists.

| C# return type        | GraphQL type (NRT enabled) |
| --------------------- | -------------------------- |
| `List<User>`          | `[User!]!`                 |
| `User[]`              | `[User!]!`                 |
| `IEnumerable<User>`   | `[User!]!`                 |
| `IReadOnlyList<User>` | `[User!]!`                 |
| `IQueryable<User>`    | `[User!]!`                 |
| `List<User?>`         | `[User]!`                  |
| `List<User>?`         | `[User!]`                  |

Any type implementing `IEnumerable<T>` is treated as a list.

# Defining List Fields

<ExampleTabs>
<Implementation>

```csharp
// Types/UserQueries.cs
[QueryType]
public static partial class UserQueries
{
    public static List<User> GetUsers(CatalogContext db)
        => db.Users.ToList();
}
```

The return type `List<User>` is automatically mapped to `[User!]!` when NRT is enabled.

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
            .Field("users")
            .Type<ListType<UserType>>()
            .Resolve(context =>
            {
                // ...
            });
    }
}
```

Using `ListType<T>` makes the list type explicit in the descriptor.

</Code>
</ExampleTabs>

# List Nullability

Lists have two layers of nullability: the list itself and its items. With [nullable reference types](/docs/hotchocolate/v16/defining-a-schema/non-null) enabled, Hot Chocolate infers both layers from your C# types.

| C# type          | GraphQL type | Meaning                                     |
| ---------------- | ------------ | ------------------------------------------- |
| `List<string>`   | `[String!]!` | Non-null list of non-null items             |
| `List<string?>`  | `[String]!`  | Non-null list, items can be null            |
| `List<string>?`  | `[String!]`  | List itself can be null, items are non-null |
| `List<string?>?` | `[String]`   | Both list and items can be null             |

If you need to override the inferred nullability, use `[GraphQLType]` or the descriptor API:

```csharp
// Override to allow null items
[GraphQLType(typeof(ListType<StringType>))]
public List<string> Tags { get; set; }
```

# Nested Lists

Hot Chocolate supports nested lists (lists of lists). This pattern is useful for representing matrix-like data.

```csharp
// Types/GridQueries.cs
[QueryType]
public static partial class GridQueries
{
    public static List<List<int>> GetMatrix()
        => [[1, 2], [3, 4]];
}
```

This produces `matrix: [[Int!]!]!` in the schema.

# Next Steps

- **Need to control nullability?** See [Non-Null](/docs/hotchocolate/v16/defining-a-schema/non-null).
- **Need pagination instead of full lists?** See [Pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination).
- **Need to filter or sort lists?** See [Filtering](/docs/hotchocolate/v16/resolvers-and-data/filtering) and [Sorting](/docs/hotchocolate/v16/resolvers-and-data/sorting).
