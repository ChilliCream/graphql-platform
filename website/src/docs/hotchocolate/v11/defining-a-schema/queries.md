---
title: "Queries"
---

The query type in GraphQL represents a read-only view of all of our entities and ways to retrieve them. A query type is required for every GraphQL server.

```sdl
type Query {
  books: [Book!]!
  author(id: Int!): Author
}
```

Clients can query one or more fields through the query type.

```graphql
query {
  books {
    title
    author
  }
  author(id: 1) {
    name
  }
}
```

Queries are expected to be side-effect free and are therefore parallelized by the execution engine.

# Usage

A query type can be defined like the following.

<ExampleTabs>
<Implementation>

```csharp
public class Query
{
    public Book GetBook()
    {
        return new Book { Title  = "C# in depth", Author = "Jon Skeet" };
    }
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

</Implementation>
<Code>

```csharp
public class Query
{
    public Book GetBook()
    {
        return new Book { Title  = "C# in depth", Author = "Jon Skeet" };
    }
}

public class QueryType : ObjectType<Query>
{
    protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
    {
        descriptor
            .Field(f => f.GetBook())
            .Type<BookType>();
    }
}

public class BookType : ObjectType<Book>
{
    protected override void Configure(IObjectTypeDescriptor<Book> descriptor)
    {
        descriptor
            .Field(f => f.Title)
            .Type<StringType>();

        descriptor
            .Field(f => f.Author)
            .Type<StringType>();
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

</Code>
<Schema>

```csharp
public class Query
{
    public Book GetBook()
    {
        return new Book { Title  = "C# in depth", Author = "Jon Skeet" };
    }
}

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
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
            .BindComplexType<Query>()
            .BindComplexType<Book>();
    }
}
```

</Schema>
</ExampleTabs>

A query type is just a regular [object type](/docs/hotchocolate/v11/defining-a-schema/object-types), so we can do everything we could do with an object type with the query type (this applies to all root types).
