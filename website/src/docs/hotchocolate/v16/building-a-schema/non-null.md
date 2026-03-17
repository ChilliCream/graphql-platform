---
title: "Non-Null"
---

By default, every GraphQL field can return either its declared type or `null`. The non-null modifier (`!`) tells clients that a field will never be `null`. If a resolver returns `null` for a non-null field, the execution engine raises an error rather than sending unexpected null values to clients.

```graphql
type User {
  name: String!
  bio: String
}
```

In this schema, `name` always has a value. The `bio` field may be `null`.

# Implicit Nullability from C# Types

Hot Chocolate infers nullability from your C# types. When [nullable reference types](https://docs.microsoft.com/dotnet/csharp/nullable-references) (NRT) are enabled in your project, the mapping is straightforward.

## Value Types

Value types are non-null by default. Use `?` to make them nullable.

| C# type | GraphQL type |
| ------- | ------------ |
| `int`   | `Int!`       |
| `int?`  | `Int`        |
| `bool`  | `Boolean!`   |
| `bool?` | `Boolean`    |

## Reference Types (NRT Enabled)

With NRT enabled (recommended), non-nullable references map to non-null GraphQL types.

| C# type   | GraphQL type |
| --------- | ------------ |
| `string`  | `String!`    |
| `string?` | `String`     |
| `User`    | `User!`      |
| `User?`   | `User`       |

## Reference Types (NRT Disabled)

Without NRT, all reference types are nullable by default. Hot Chocolate cannot distinguish `string` from `string?` because the compiler treats them identically.

| C# type  | GraphQL type |
| -------- | ------------ |
| `string` | `String`     |
| `User`   | `User`       |

We strongly recommend enabling NRT. It provides accurate schema nullability without extra attributes and catches null-related bugs at compile time.

# Enabling Nullable Reference Types

Add the following to your `.csproj` file to enable NRT across the project:

```xml
<PropertyGroup>
    <Nullable>enable</Nullable>
</PropertyGroup>
```

You can also enable it per file with `#nullable enable` at the top of the file.

# Explicit Nullability

When you need to override the inferred nullability, use attributes or the descriptor API.

<ExampleTabs>
<Implementation>

```csharp
// Types/Book.cs
public class Book
{
    [GraphQLNonNullType]
    public string Title { get; set; }

    public string? Author { get; set; }
}
```

`[GraphQLNonNullType]` forces the field to be non-null in the schema regardless of the C# nullability.

</Implementation>
<Code>

```csharp
// Types/BookType.cs
public class BookType : ObjectType<Book>
{
    protected override void Configure(IObjectTypeDescriptor<Book> descriptor)
    {
        descriptor
            .Field(f => f.Title)
            .Type<NonNullType<StringType>>();

        descriptor
            .Field(f => f.Author)
            .Type<StringType>();
    }
}
```

</Code>
</ExampleTabs>

# Non-Null List Items

Lists have two layers of nullability: the list itself and its items. With NRT enabled:

| C# type          | GraphQL type |
| ---------------- | ------------ |
| `List<string>`   | `[String!]!` |
| `List<string>?`  | `[String!]`  |
| `List<string?>`  | `[String]!`  |
| `List<string?>?` | `[String]`   |

To override nullability on list items explicitly:

<ExampleTabs>
<Implementation>

```csharp
// Types/Book.cs
public class Book
{
    [GraphQLType(typeof(ListType<NonNullType<StringType>>))]
    public List<string> Genres { get; set; }
}
```

</Implementation>
<Code>

```csharp
// Types/BookType.cs
public class BookType : ObjectType<Book>
{
    protected override void Configure(IObjectTypeDescriptor<Book> descriptor)
    {
        descriptor
            .Field(f => f.Genres)
            .Type<ListType<NonNullType<StringType>>>();
    }
}
```

</Code>
</ExampleTabs>

Both produce `genres: [String!]` in the schema.

# Troubleshooting

## Field unexpectedly nullable

Verify that NRT is enabled in your project. Without NRT, all reference type fields default to nullable. Check your `.csproj` for `<Nullable>enable</Nullable>`.

## Resolver returns null for non-null field

When a resolver returns `null` for a non-null field, the execution engine propagates the null up to the nearest nullable parent field. If no parent is nullable, the entire `data` response becomes `null`. Either make the field nullable in the schema or ensure the resolver always returns a value.

## Input field nullability mismatch

Input fields follow the same NRT rules. If a client sends `null` for a non-null input field, the request fails validation. Ensure your input classes accurately reflect which fields are optional.

# Next Steps

- **Need to define lists?** See [Lists](/docs/hotchocolate/v16/defining-a-schema/lists).
- **Need to understand arguments?** See [Arguments](/docs/hotchocolate/v16/defining-a-schema/arguments).
- **Need input types?** See [Input Object Types](/docs/hotchocolate/v16/defining-a-schema/input-object-types).
- **Need to learn about scalars?** See [Scalars](/docs/hotchocolate/v16/defining-a-schema/scalars).
