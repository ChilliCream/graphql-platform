---
title: "Extending Types"
---

Type extensions let you add, remove, or replace fields on existing types without modifying the original class. This is particularly useful for types defined in other assemblies or NuGet packages, and for organizing a large schema into domain-focused modules.

Hot Chocolate merges type extensions with the original type definition at schema build time. The resulting schema contains a single type with all fields combined. Extensions do not produce the `extend type` syntax in the GraphQL schema.

# Adding Fields

The most common use case is adding resolver fields to an existing type.

<ExampleTabs>
<Implementation>

```csharp
// Types/Book.cs
public class Book
{
    public int Id { get; set; }
    public string Title { get; set; }
    public int AuthorId { get; set; }
}

// Types/BookExtensions.cs
[ExtendObjectType<Book>]
public static partial class BookExtensions
{
    public static IEnumerable<string> GetGenres([Parent] Book book)
    {
        // ...
    }
}
```

```csharp
// Program.cs
builder.Services
    .AddGraphQLServer()
    .AddTypeExtension<BookExtensions>();
```

The `genres` field appears on the `Book` type alongside the original fields.

</Implementation>
<Code>

```csharp
// Types/BookTypeExtensions.cs
public class BookTypeExtensions : ObjectTypeExtension<Book>
{
    protected override void Configure(IObjectTypeDescriptor<Book> descriptor)
    {
        descriptor
            .Field("genres")
            .Type<ListType<StringType>>()
            .Resolve(context =>
            {
                var parent = context.Parent<Book>();
                // ...
            });
    }
}
```

```csharp
// Program.cs
builder.Services
    .AddGraphQLServer()
    .AddTypeExtension<BookTypeExtensions>();
```

</Code>
</ExampleTabs>

# Extending Root Types

Type extensions are how you split root types across multiple classes. Each class adds its own set of fields to the Query, Mutation, or Subscription type.

<ExampleTabs>
<Implementation>

With the source generator, use `[QueryType]`, `[MutationType]`, or `[SubscriptionType]` on multiple classes. These are type extensions under the hood. See [Queries](/docs/hotchocolate/v16/defining-a-schema/queries) for details.

If you need to extend a root type without the source generator, use `[ExtendObjectType]`:

```csharp
// Types/BookQueries.cs
[ExtendObjectType(typeof(Query))]
public class BookQueries
{
    public IEnumerable<Book> GetBooks()
    {
        // ...
    }
}
```

```csharp
// Program.cs
builder.Services
    .AddGraphQLServer()
    .AddTypeExtension<BookQueries>();
```

</Implementation>
<Code>

```csharp
// Types/QueryBookResolvers.cs
public class QueryBookResolvers : ObjectTypeExtension<Query>
{
    protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
    {
        descriptor
            .Field("books")
            .Type<ListType<BookType>>()
            .Resolve(context =>
            {
                // ...
            });
    }
}
```

```csharp
// Program.cs
builder.Services
    .AddGraphQLServer()
    .AddTypeExtension<QueryBookResolvers>();
```

</Code>
</ExampleTabs>

# Removing Fields

You can exclude fields from the original type.

<ExampleTabs>
<Implementation>

```csharp
// Types/BookExtensions.cs
[ExtendObjectType(typeof(Book),
    IgnoreProperties = new[] { nameof(Book.AuthorId) })]
public class BookExtensions
{
}
```

```csharp
// Program.cs
builder.Services
    .AddGraphQLServer()
    .AddTypeExtension<BookExtensions>();
```

</Implementation>
<Code>

```csharp
// Types/BookTypeExtensions.cs
public class BookTypeExtensions : ObjectTypeExtension<Book>
{
    protected override void Configure(IObjectTypeDescriptor<Book> descriptor)
    {
        descriptor.Ignore(f => f.AuthorId);
    }
}
```

```csharp
// Program.cs
builder.Services
    .AddGraphQLServer()
    .AddTypeExtension<BookTypeExtensions>();
```

</Code>
</ExampleTabs>

# Replacing Fields

Replace a field by binding a new resolver to an existing property. This is useful for replacing foreign key IDs with resolved entities.

```csharp
// Types/BookExtensions.cs
[ExtendObjectType<Book>]
public static partial class BookExtensions
{
    [BindMember(nameof(Book.AuthorId))]
    public static Author GetAuthor([Parent] Book book, AuthorService authors)
        => authors.GetById(book.AuthorId);
}
```

This replaces the `authorId: Int!` field with an `author: Author!` field on the `Book` type.

# Extending by Name

When you cannot reference the target type directly, extend it by its GraphQL type name.

<ExampleTabs>
<Implementation>

```csharp
// Types/FooExtensions.cs
[ExtendObjectType("Foo")]
public class FooExtensions
{
    public string GetExtraField()
    {
        // ...
    }
}
```

For root types, use the constants in `OperationTypeNames` instead of string literals (for example, `OperationTypeNames.Query`).

</Implementation>
<Code>

```csharp
// Types/FooTypeExtensions.cs
public class FooTypeExtensions : ObjectTypeExtension
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name("Foo");

        descriptor
            .Field("extraField")
            .Type<StringType>()
            .Resolve(context =>
            {
                // ...
            });
    }
}
```

</Code>
</ExampleTabs>

# Extending by Base Type

You can extend every type that inherits from a base class or implements an interface.

```csharp
// Types/AuditExtensions.cs
[ExtendObjectType(typeof(object))]
public class AuditExtensions
{
    // Added to every object type
    public DateTime GetTimestamp() => DateTime.UtcNow;

    // Added only to Book (inferred from the [Parent] type)
    public Author GetAuthor([Parent] Book book, AuthorService authors)
        => authors.GetById(book.AuthorId);
}
```

You can also target a specific interface:

```csharp
// Types/PostExtensions.cs
[ExtendObjectType(typeof(IPost))]
public class PostExtensions
{
    public string GetSummary([Parent] IPost post)
    {
        // Applied to every type that implements IPost
        // ...
    }
}
```

The extension applies to every object type that implements `IPost`, not to the interface type itself.

# Troubleshooting

## Extension not applied

Verify you have registered the extension with `.AddTypeExtension<T>()`. Unlike regular types, extensions require explicit registration.

## Source generator extension not discovered

When using `[ExtendObjectType<T>]` with the source generator, the class must be `partial`. The source generator cannot extend non-partial classes.

## Conflicting field names

If an extension adds a field that already exists on the type, the schema fails to build. Either remove the duplicate field or use `[BindMember]` to replace the existing one.

## Extension across assemblies

Type extensions work across assembly boundaries. The assembly containing the extension must reference the assembly containing the original type and must use the Hot Chocolate source generator. Register the extension types in the main project's service configuration.

# Next Steps

- **Need to define root types?** See [Queries](/docs/hotchocolate/v16/defining-a-schema/queries) and [Mutations](/docs/hotchocolate/v16/defining-a-schema/mutations).
- **Need to define object types?** See [Object Types](/docs/hotchocolate/v16/defining-a-schema/object-types).
- **Need global object identification?** See [Relay](/docs/hotchocolate/v16/defining-a-schema/relay).
