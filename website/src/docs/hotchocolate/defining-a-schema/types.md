---
title: "Types"
---

> We are still working on the documentation for Hot Chocolate 11.1 so help us by finding typos, missing things or write some additional docs with us.

TODO: Use the one from api reference

The most important type in a GraphQL schema is the object type which lets you consume data. Every object type has to have at least one field which holds the data of an object. Fields can return simple scalars like String, Int, or again object types.

```sdl
type Book {
  title: String
  author: String
}
```

<ExampleTabs>
<ExampleTabs.Annotation>

```csharp
// Query.cs
public class Query
{
    public Book GetBook() => new Book { Title  = "C# in depth", Author = "Jon Skeet" };
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
    public Book GetBook() => new Book { Title  = "C# in depth", Author = "Jon Skeet" };
}

// QueryType.cs
public class QueryType : ObjectType<Query>
{
    protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
    {
        descriptor
            .Field(f => f.GetBook())
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
