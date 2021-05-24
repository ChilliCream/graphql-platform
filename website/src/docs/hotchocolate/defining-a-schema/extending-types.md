---
title: "Extending Types"
---

import { ExampleTabs } from "../../../components/mdx/example-tabs"

GraphQL types tend to become pretty large, especially root types like the query type.

Type extensions allow us to extend to existing types. A type can have one or more type extensions, which will form a combined schema type at runtime.

# Usage

Given is the following entity that we want to extend with functionality.

```csharp
public class Book
{
    public int Id { get; set; }

    public string Title { get; set; }

    public int AuthorId { get; set; }
}

// This is only relevant for the Code-first approach
public class BookType : ObjectType<Book>
{
}
```

## Adding fields

We can easily add new fields to an existing type.

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

</ExampleTabs.Annotation>
<ExampleTabs.Code>

TODO

</ExampleTabs.Code>
<ExampleTabs.Schema>

TODO

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddGraphQLServer()
            .AddDocumentFromString(@"
                type Book {
                  id: Int!
                  title: String!
                  authorId: Int!
                }

                extend type Book {
                  genres: [String!]!
                }
            ");
    }
}
```

</ExampleTabs.Schema>
</ExampleTabs>

One of the most common use-cases for this would be adding new resolvers to one of our root types.

<!-- todo: maybe with example tabs -->

```csharp
[ExtendObjectType(typeof(Query))]
public class QueryBookResolvers
{
    public IEnumerable<Book> GetBooks()
    {
        // Omitted code for brevity
    }
}
```

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

TODO

</ExampleTabs.Code>
<ExampleTabs.Schema>

Simply remove the field from the original type.

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

TODO

</ExampleTabs.Code>
<ExampleTabs.Schema>

Simply replace the field on the existing type.

</ExampleTabs.Schema>
</ExampleTabs>

<!-- todo: example tabs for the section below? -->

# Extending multiple types

We can extend multiple types at once by extending upon base types or interfaces.

```csharp
// this extends every type that inherits from object (essentially every type)
[ExtendObjectType(typeof(object))]
public class ObjectExtensions
{
    public string NewField()
    {
        // Omitted code for brevity
    }
}

[InterfaceType]
public interface IPost
{
    string Title { get; set; }
}

// this extends every type that implements the IPost interface
[ExtendObjectType(typeof(IPost))]
public class PostExtensions
{
    public string NewField([Parent] IPost post)
    {
        // Omitted code for brevity
    }
}
```

> Note: The `newField` property is only added to types implementing the `IPost` interface, not the interface itself.

## Specific resolvers

We can also extend multiple types at once, but dedicate specific resolvers to specific types.

```csharp
// this extends every type that inherits from object (essentially every type)
[ExtendObjectType(typeof(object))]
public class ObjectExtensions
{
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
