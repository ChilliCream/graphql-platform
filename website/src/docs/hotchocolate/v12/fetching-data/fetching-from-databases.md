---
title: "Fetching from Databases"
---

In this section, you find a simple example on how you can fetch data from a database and expose it as a GraphQL API.

**Hot Chocolate is not bound to a specific database, pattern or architecture.**
[We do have a few integrations](/docs/hotchocolate/v12/integrations), that help with a variety of databases, though these are just additions on top of HotChocolate.
You can couple your business logic close to the GraphQL server, or cleanly decouple your domain layer from the GraphQL layer over abstractions.
The GraphQL server only knows its schema, types and resolvers, what you do in these resolvers and what types you expose, is up to you.

In this example, we will directly fetch data from MongoDB in a resolver.

# Setting up the Query

The query type in a GraphQL schema is the root type. Each field defined on this type is available at the root of a query.
If a field is requested, the resolver of the field is called.
The data of this resolver is used for further execution.
If you return a scalar, value (e.g. `string`, `int` ...) the value is serialized and added to the response.
If you return an object, this object is the parent of the resolver in the subtree.

<ExampleTabs>
<Implementation>

```csharp
// Query.cs
public class Query
{
    public Task<Book?> GetBookById(
        [Service] IMongoCollection<Book> collection,
        Guid id)
    {
        return collection.Find(x => x.Id == id).FirstOrDefaultAsync();
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
            .AddGraphQLServer()
            .AddQueryType<Query>();
    }

    // Omitted code for brevity
}
```

</Implementation>
<Code>

```csharp
// Query.cs
public class Query
{
    public Task<Book?> GetBookById(
        [Service] IMongoCollection<Book> collection,
        Guid id)
    {
        return collection.Find(x => x.Id == id).FirstOrDefaultAsync();
    }
}

// QueryType.cs
public class QueryType : ObjectType<Query>
{
    protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
    {
        descriptor
            .Field(f => f.GetBookById(default!, default!))
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
            .AddGraphQLServer()
            .AddQueryType<QueryType>();
    }

    // Omitted code for brevity
}
```

</Code>
<Schema>

```csharp
// Query.cs
public class Query
{
    public Task<Book?> GetBookById(
        [Service] IMongoCollection<Book> collection,
        Guid id)
    {
        return collection.Find(x => x.Id == id).FirstOrDefaultAsync();
    }
}

// Startup.cs
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddGraphQLServer()
            .AddDocumentFromString(@"
                type Query {
                  bookById(id: Uuid): Book
                }

                type Book {
                  title: String
                  author: String
                }
            ")
            .BindRuntimeType<Query>();
    }

    // Omitted code for brevity
}
```

</Schema>
</ExampleTabs>
