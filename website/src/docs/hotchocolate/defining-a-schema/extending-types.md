---
title: "Extending Types"
---

import { ExampleTabs } from "../../../components/mdx/example-tabs"

GraphQL allows us to extend existing types with new functionality.

```sdl
type Query {
  foo: String
}

extend type Query {
  bar: String
}
```

In Hot Chocolate type extensions work a little different. Instead of generating the `extend` syntax in the schema, the original type definition and its type extensions are merged at runtime into a combined type.

Type extensions make most sense, if

- we want to split up types into separate classes.

- we can not modify the original type.

> Note: If we just want to organize the fields of one of our types in different files, we can use [partial classes](https://docs.microsoft.com/dotnet/csharp/programming-guide/classes-and-structs/partial-classes-and-methods) in the Annotation-based approach.

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
<ExampleTabs.Annotation>

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

</ExampleTabs.Annotation>
<ExampleTabs.Code>

```csharp
public class BookTypeExtensions : ObjectTypeExtension<Book>
{
    protected override void Configure(IObjectTypeDescriptor<Book> descriptor)
    {
        // this needs to match the name of the actual object type
        descriptor.Name("Book");

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
public class QueryTypeBookResolvers : ObjectTypeExtension
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

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

> Note: We can use `OperationTypeNames.Query` instead of `"Query"`. `OperationTypeNames` contain the names of the three root types.

</ExampleTabs.Code>
<ExampleTabs.Schema>

Simply add a new field to the existing type.

</ExampleTabs.Schema>
</ExampleTabs>

## Removing fields

We can also ignore fields of the type we are extending.

<ExampleTabs>
<ExampleTabs.Annotation>

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

</ExampleTabs.Annotation>
<ExampleTabs.Code>

**This is currently not working ([#3776](https://github.com/ChilliCream/hotchocolate/issues/3776))**

```csharp
public class BookTypeExtensions : ObjectTypeExtension<Book>
{
    protected override void Configure(IObjectTypeDescriptor<Book> descriptor)
    {
        // this needs to match the name of the actual object type
        descriptor.Name("Book");

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

</ExampleTabs.Code>
<ExampleTabs.Schema>

Simply remove the field from the existing type.

</ExampleTabs.Schema>
</ExampleTabs>

## Replacing fields

We might have an `Id` field, which we want to replace with a field that resolves the actual type the `Id` is pointing to.

In this example we replace the `authorId` field with an `author` field.

<ExampleTabs>
<ExampleTabs.Annotation>

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

</ExampleTabs.Annotation>
<ExampleTabs.Code>

**This is currently not working ([#3776](https://github.com/ChilliCream/hotchocolate/issues/3776))**

```csharp
public class BookTypeExtensions : ObjectTypeExtension<Book>
{
    protected override void Configure(IObjectTypeDescriptor<Book> descriptor)
    {
        // this needs to match the name of the actual object type
        descriptor.Name("Book");

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

</ExampleTabs.Code>
<ExampleTabs.Schema>

Simply replace the field on the existing type.

</ExampleTabs.Schema>
</ExampleTabs>

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
