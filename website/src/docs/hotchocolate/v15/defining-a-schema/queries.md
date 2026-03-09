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
```

```csharp
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>();
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
```

```csharp
builder.Services
    .AddGraphQLServer()
    .AddQueryType<QueryType>();
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
```

```csharp
builder.Services
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
    .BindRuntimeType<Query>()
    .BindRuntimeType<Book>();
```

</Schema>
</ExampleTabs>

> Warning: Only **one** query type can be registered using `AddQueryType()`. If we want to split up our query type into multiple classes, we can do so using type extensions.
>
> [Learn more about extending types](/docs/hotchocolate/v15/defining-a-schema/extending-types)

A query type is just a regular object type, so everything that applies to an object type also applies to the query type (this is true for all root types).

[Learn more about object types](/docs/hotchocolate/v15/defining-a-schema/object-types)
