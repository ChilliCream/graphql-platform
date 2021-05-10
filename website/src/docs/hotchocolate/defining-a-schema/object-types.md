---
title: "Object Types"
---

import { ExampleTabs } from "../../../components/mdx/example-tabs"

The most important type in a GraphQL schema is the object type. It contains fields that can return simple scalars like `String`, `Int`, or again object types.

```sdl
type Author {
  name: String
}

type Book {
  title: String
  author: Author
}
```

Learn more about object types [here](https://graphql.org/learn/schema/#object-types-and-fields).

# Usage

Object types can be defined like the following.

<ExampleTabs>
<ExampleTabs.Annotation>

In the Annotation-based approach we are essentially just creating regular C# classes.

```csharp
public class Author
{
    public string Name { get; set; }
}

public class Book
{
    public string Title { get; set; }

    public Author Author { get; set; }
}

public class Query
{
    public Book GetBook()
        => new Book
        {
            Title = "C# in depth",
            Author = new Author
            {
                Name = "Jon Skeet"
            }
        };
}

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddGraphQLServer()
            .AddQueryType<Query>();
    }
}
```

</ExampleTabs.Annotation>
<ExampleTabs.Code>

In the Code-first approach we are also starting out with our POCOs `Author` and `Book`, but here we are creating new classes inheriting from `ObjectType<T>` to map our POCOs to schema object types.

```csharp
public class Author
{
    public string Name { get; set; }
}

public class Book
{
    public string Title { get; set; }

    public Author Author { get; set; }
}

public class AuthorType : ObjectType<Author>
{

}

public class BookType : ObjectType<Book>
{

}

public class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("book")
            .Type<BookType>()
            .Resolve(context =>
            {
                return new Book
                {
                    Title = "C# in depth",
                    Author = new Author
                    {
                        Name = "Jon Skeet"
                    }
                };
            });
    }
}

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddGraphQLServer()
            .AddQueryType<QueryType>();
    }
}
```

</ExampleTabs.Code>
<ExampleTabs.Schema>

```csharp
// Query.cs
public class Query
{
    public Book GetBook() => new Book { Title  = "C# in depth", Author = "Jon Skeet" };
}

// Startup.cs
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddRouting()
            .AddGraphQLServer()
            .AddDocumentFromString(@"
                type Query {
                  book: Book
                }

                type Book {
                  title: String
                  author: String
                }
            ")
            .BindComplexType<Query>();
    }

    // Omitted code for brevity
}
```

</ExampleTabs.Schema>
</ExampleTabs>

## Fields

Fields of object types can be compared to methods in C# and allow us to pass in arguments.

```sdl
type Query {
  book(id: String): Book
}

type Book {
  title: String
  author: String
}
```

```graphql
{
  book(id: "abc") {
    title
  }
}
```

<ExampleTabs>
<ExampleTabs.Annotation>

```csharp
// Query.cs
public class Query
{
    public Task<Book> GetBookAsync(string id)
    {
        // Omitted code for brevity
    }
}

// Book.cs
public class Book
{
    public string Title { get; set; }

    public string Author { get; set; }
}

// Startup.cs
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddRouting()
            .AddGraphQLServer()
            .AddQueryType<Query>();
    }

    // Omitted code for brevity
}
```

</ExampleTabs.Annotation>
<ExampleTabs.Code>

```csharp
// Query.cs
public class Query
{
    public Task<Book> GetBookAsync(string id)
    {
        // Omitted code for brevity
    }
}

// QueryType.cs
public class QueryType : ObjectType<Query>
{
    protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
    {
        descriptor
            .Field(f => f.GetBook(default))
            .Type<BookType>();
    }
}

// Book.cs
public class Book
{
    public string Title { get; set; }

    public string Author { get; set; }
}

// BookType.cs
public class BookType : ObjectType<Book>
{
    protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
    {
        descriptor
            .Field(f => f.Title)
            .Type<StringType>();

        descriptor
            .Field(f => f.Author)
            .Type<StringType>();
    }
}


// Startup.cs
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddRouting()
            .AddGraphQLServer()
            .AddQueryType<QueryType>();
    }

    // Omitted code for brevity
}
```

</ExampleTabs.Code>
<ExampleTabs.Schema>

```csharp
// Query.cs
public class Query
{
    public Task<Book> GetBookAsync(string id)
    {
        // Omitted code for brevity
    }
}

// Startup.cs
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddRouting()
            .AddGraphQLServer()
            .AddDocumentFromString(@"
                type Query {
                  book(id: String): Book
                }

                type Book {
                  title: String
                  author: String
                }
            ")
            .BindComplexType<Query>();
    }

    // Omitted code for brevity
}
```

</ExampleTabs.Schema>
</ExampleTabs>

# Binding behavior

TODO

## Ignoring fields

TODO

- adding new fields without modifying the CLR type
- accessing parent values
