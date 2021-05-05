---
title: "Inputs"
---

> We are still working on the documentation for Hot Chocolate 11.1 so help us by finding typos, missing things or write some additional docs with us.

In GraphQL we distinguish between input- and output-types. We already learned about object types which is the most prominent output-type and lets us consume data. Further, we used simple scalars like `String` to pass data into a field as an argument. In order to define complex structures of raw data that can be used as input data GraphQL defines input objects.

```sdl
input BookInput {
  title: String
  author: String
}
```

If we wanted for instance to create a new book with a mutation we could do that like the following.

<ExampleTabs>
<ExampleTabs.Annotation>

```csharp
// Query.cs
public class Query
{
    // Omitted code for brevity
}

// Query.cs
public class Mutation
{
    public async Task<Book> CreateBook(Book book)
    {

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
            .AddQueryType<Query>()
            .AddMutationType<Mutation>();
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
