---
title: "Extending Types"
---

Type extensions allow us to add, remove or replace fields on existing types, without necessarily needing access to these types.

Because of these capabilities, they also allow for better organization of our types. We could for example have classes that encapsulate part of our domain and extend our `Query` type with these functionalities.

Type extensions are especially useful if we want to modify third-party types, such as types that live in a separate assembly and are therefore not directly modifiable by us.

> Warning: Type extensions do not produce the [extend type syntax that GraphQL offers](https://spec.graphql.org/draft/#sec-Object-Extensions), since it would unnecessarily complicate the resulting schema. Instead, Hot Chocolate's type extensions are directly merged with the original type definition to create a single type at runtime.

# Object Types

Consider we have the following entity that we want to extend with functionality.

```csharp
public class Book
{
    public int Id { get; set; }

    public string Title { get; set; }

    public int AuthorId { get; set; }
}
```

## Adding fields

We can easily add new fields to our existing `Book` type.

<ExampleTabs>
<Implementation>

```csharp
[ExtendObjectType(typeof(Book))]
public class BookExtensions
{
    public IEnumerable<string> GetGenres([Parent] Book book)
    {
        // Omitted code for brevity
    }
}

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddGraphQLServer()
            .AddTypeExtension<BookExtensions>();
    }
}
```

One of the most common use-cases for this would be adding new resolvers to one of our root types.

```csharp
[ExtendObjectType(typeof(Query))]
public class QueryBookResolvers
{
    public IEnumerable<Book> GetBooks()
    {
        // Omitted code for brevity
    }
}

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddGraphQLServer()
            .AddTypeExtension<QueryBookResolvers>();
    }
}
```

</Implementation>
<Code>

```csharp
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

                // Omitted code for brevity
            });
    }
}

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddGraphQLServer()
            .AddTypeExtension<BookTypeExtensions>();
    }
}
```

One of the most common use-cases for this would be adding new resolvers to one of our root types.

```csharp
public class QueryTypeBookResolvers : ObjectTypeExtension<Query>
{
    protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
    {
        descriptor
            .Field("books")
            .Type<ListType<BookType>>()
            .Resolve(context =>
            {
                // Omitted code for brevity
            });
    }
}

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddGraphQLServer()
            .AddTypeExtension<QueryTypeBookResolvers>();
    }
}
```

</Code>
<Schema>

Simply add a new field to the existing type.

</Schema>
</ExampleTabs>

## Removing fields

We can also ignore fields of the type we are extending.

<ExampleTabs>
<Implementation>

```csharp
[ExtendObjectType(typeof(Book),
    IgnoreProperties = new[] { nameof(Book.AuthorId) })]
public class BookExtensions
{
}

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddGraphQLServer()
            .AddTypeExtension<BookExtensions>();
    }
}
```

</Implementation>
<Code>

**This is currently not working ([#3776](https://github.com/ChilliCream/graphql-platform/issues/3776))**

```csharp
public class BookTypeExtensions : ObjectTypeExtension<Book>
{
    protected override void Configure(IObjectTypeDescriptor<Book> descriptor)
    {
        descriptor.Ignore(f => f.AuthorId);
    }
}

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddGraphQLServer()
            .AddTypeExtension<BookTypeExtensions>();
    }
}
```

</Code>
<Schema>

Simply remove the field from the existing type.

</Schema>
</ExampleTabs>

## Replacing fields

We might have an `Id` field, which we want to replace with a field that resolves the actual type the `Id` is pointing to.

In this example we replace the `authorId` field with an `author` field.

<ExampleTabs>
<Implementation>

```csharp
[ExtendObjectType(typeof(Book))]
public class BookExtensions
{
    [BindMember(nameof(Book.AuthorId))]
    public Author GetAuthor([Parent] Book book)
    {
        // Omitted code for brevity
    }
}

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddGraphQLServer()
            .AddTypeExtension<BookExtensions>();
    }
}
```

</Implementation>
<Code>

**This is currently not working ([#3776](https://github.com/ChilliCream/graphql-platform/issues/3776))**

```csharp
public class BookTypeExtensions : ObjectTypeExtension<Book>
{
    protected override void Configure(IObjectTypeDescriptor<Book> descriptor)
    {
        descriptor
            .Field(f => f.AuthorId)
            .Type<AuthorType>()
            .Name("author")
            .Resolve(context =>
            {
                var parent = context.Parent<Book>();

                // Omitted code for brevity
            });
    }
}

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddGraphQLServer()
            .AddTypeExtension<BookTypeExtensions>();
    }
}
```

</Code>
<Schema>

Simply replace the field on the existing type.

</Schema>
</ExampleTabs>

## Extending by name

If we can not reference a type, we can still extend it by specifying its name.

<ExampleTabs>
<Implementation>

```csharp
[ExtendObjectType("Foo")]
public class FooExtensions
{
    // Omitted code for brevity
}
```

</Implementation>
<Code>

```csharp
public class FooTypeExtensions : ObjectTypeExtension
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name("Foo");

        // Omitted code for brevity
    }
}
```

</Code>
<Schema>

⚠️ Schema-first does currently not support extending types by their name

</Schema>
</ExampleTabs>

When extending root types, we can make use of the constants in `OperationTypeNames`. We can for example use `OperationTypeNames.Query` instead of writing `"Query"` everywhere.

## Extending base types

We can also extend multiple types at once, but still dedicate specific resolvers to specific types.

```csharp
// this extends every type that inherits from object (essentially every type)
[ExtendObjectType(typeof(object))]
public class ObjectExtensions
{
    // this field is added to every object type
    public string NewField()
    {
        // Omitted code for brevity
    }

    // this field is only added to the Book type
    public Author GetAuthor([Parent] Book book)
    {
        // Omitted code for brevity
    }

    // this field is only added to the Author type
    public IEnumerable<Book> GetBooks([Parent] Author author)
    {
        // Omitted code for brevity
    }
}
```

We can also modify all object types that are connected by a base type, like an interface.

```csharp
[InterfaceType]
public interface IPost
{
    string Title { get; set; }
}

// this extends every type that implements the IPost interface
// note: the interface itself is not extended in the schema
[ExtendObjectType(typeof(IPost))]
public class PostExtensions
{
    public string NewField([Parent] IPost post)
    {
        // Omitted code for brevity
    }
}
```

> Note: The `IPost` is annotated with `[InterfaceType]` to include it in the GraphQL schema, but that isn't necessary for the type extension to work.
> We can use any base type, like `object` or an `abstract` base class, as an extension point without necessarily exposing the base type in our GraphQL schema.
