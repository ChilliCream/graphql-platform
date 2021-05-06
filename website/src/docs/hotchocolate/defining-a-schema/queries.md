---
title: "Queries"
---

import { ExampleTabs } from "../../../components/mdx/example-tabs"

The query type is how we can read data. It is described as a way to access read-only data in a side-effect free way. This means that the GraphQL engine is allowed to parallelize data fetching.

```graphql
query {
  book(id: 1) {
    title
    author
  }
}
```

# Definition

A query type can be represented like the following:

<ExampleTabs>
<ExampleTabs.Annotation>

```csharp
public class Query
{
    public Book GetBook() => new Book { Title  = "C# in depth", Author = "Jon Skeet" };
}

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddGraphQLServer()
            .AddQueryType<Query>();
    }

    // Omitted code for brevity
}
```

</ExampleTabs.Annotation>
<ExampleTabs.Code>

```csharp
public class Query
{
    public Book GetBook() => new Book { Title  = "C# in depth", Author = "Jon Skeet" };
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

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddGraphQLServer()
            .AddQueryType<QueryType>();
    }

    // Omitted code for brevity
}
```

</ExampleTabs.Code>
<ExampleTabs.Schema>

```csharp
public class Query
{
    public Book GetBook() => new Book { Title  = "C# in depth", Author = "Jon Skeet" };
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

    // Omitted code for brevity
}
```

</ExampleTabs.Schema>
</ExampleTabs>
