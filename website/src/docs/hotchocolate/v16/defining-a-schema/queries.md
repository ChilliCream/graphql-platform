---
title: "Queries"
---

The Query type is one of the three GraphQL operation types and the only one that is required. In GraphQL, the Query type is the entry point for fetching data from your server. Query fields are expected to perform side-effect-free read operations.

```sdl
type Query {
  books: [Book!]!
  author(id: Int!): Author
}
```

> Fields in GraphQL are like methods in C# and can have arguments. The key difference is that fields always return a value, so there is no `void` in GraphQL.

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

Since query fields are expected to be side‑effect-free, the executor is allowed to parallelize and reorder their execution.

> The term “query field” can be ambiguous in GraphQL. All fields in GraphQL are technically query fields, except for fields defined directly on the Mutation or Subscription operation types. This distinction is important because the executor may also parallelize and reorder nested fields as they are expected to be side‑effect free read operations.

# Usage

A query type can be defined like the following.

<ExampleTabs>
<Implementation>

```csharp
[QueryType]
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
</ExampleTabs>

When using our source generator with the implementation-first approach, the Query type must be decorated with the `[QueryType]` attribute.

```csharp
[QueryType]
public static partial class Query
{
    public Book GetBook()
    {
        return new Book { Title  = "C# in depth", Author = "Jon Skeet" };
    }
}
```

> The query type can also be defined as a non-static class in which case the source generator also registers it as a singleton service on the service collection. Types cannot be registered as scoped services.

When using the source generator, you can annotate multiple classes with the `[QueryType]` attribute. The source generator will merge all of these classes into a single GraphQL query type, since the GraphQL type system allows only a single query operation type in the GraphQL schema. This approach lets you structure semantic query classes in C#, grouping them by topic, entity, or any other organization that fits your domain.

```csharp
[QueryType]
public static partial class BookQueries
{
    public Book GetBook()
    {
        return new Book { Title  = "C# in depth" };
    }
}

[QueryType]
public static partial class AuthorQueries
{
    public Author GetAuthor()
    {
        return new Author { Name = "Jon Skeet" };
    }
}
```

These semantic operation types can also be split across multiple assemblies, when each of these assemblies uses the Hot Chocolate source generator.

While the GraphQL operation types have semantic importance in the schema, they are also standard GraphQL object types. You can [learn more about object types here.](/docs/hotchocolate/v16/defining-a-schema/object-types)
