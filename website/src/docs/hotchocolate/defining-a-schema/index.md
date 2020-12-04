---
title: "Schema basics"
---

The schema in GraphQL represents the type system and exposes your business model in a strong and rich way. The schema fully describes the shape of your data and how you can interact with it.

The most important type in GraphQL is the object type which lets you consume data. Every object type has to have at least one field which holds the data of an object. Fields can return simple scalars like String, Int, or again object types.

```SDL
type Book {
  title: String
  author: String
}
```

In GraphQL, we have three root types from which only the Query type has to be defined. Root types provide the entry points that let you fetch data, mutate data, or subscribe to events. Root types themself are object types.

```SDL
schema {
  query: Query
}

type Query {
  book: Book
}

type Book {
  title: String
  author: String
}
```

In Hot Chocolate, there are three ways to define an object type.

> **Note:** Every single code example will be shown in three different approaches, annotation-based (previously known as pure code-first), code-first, and schema-first. However, they will always result in the same outcome on a GraphQL schema perspective and internally in Hot Chocolate. All three approaches have their pros and cons and can be combined when needed with each other. If you would like to learn more about the three approaches in Hot Chocolate, click on [Coding Approaches](/docs/hotchocolate/api-reference/coding-approaches).

**Annotation-based approach**

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

**Code-first approach**

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

**Schema-first approach**

```csharp
// Query.cs
public class Query
{
    public string Say() => "Hello World!";
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
