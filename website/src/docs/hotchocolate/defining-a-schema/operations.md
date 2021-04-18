---
title: "GraphQL Operations"
---

> We are still working on the documentation for Hot Chocolate 11.1 so help us by finding typos, missing things or write some additional docs with us.

import { ExampleTabs } from "../../../components/mdx/example-tabs"

In GraphQL, we have three root types from which only the Query type has to be defined. Root types provide the entry points that lets us fetch data, mutate data, or subscribe to events. Root types themselves are object types and are commonly referred to as operations.

# Query

The query type is how we can read data. It is described as a way to access read-only data in a side-effect free way. This means that the GraphQL engine is allowed to parallelize data fetching.

A query type can be represented like the following:

> **Note:** Every single code example will be shown in three different approaches, annotation-based (previously known as pure code-first), code-first, and schema-first. However, they will always result in the same outcome on a GraphQL schema perspective and internally in Hot Chocolate. All three approaches have their pros and cons and can be combined when needed with each other. If you would like to learn more about the three approaches in Hot Chocolate, click on [Coding Approaches](/docs/hotchocolate/api-reference/coding-approaches).

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

# Mutation

The mutation type in GraphQL is used to mutate/change data. This means that when you are doing mutations you are causing side-effects to the system.

GraphQL defines mutations as top-level fields on the mutation type. Meaning only the fields on the mutation root type itself are mutations. Everything that is returned from a mutation field represents the changed state of the server.

```graphql
{
  mutation {
    # changeBookTitle is a mutation and is allowed to cause side-effects.
    changeBookTitle(input: { id: 1, title: "C# in depth, Fourth Edition" }) {
      # everything in this selection set and below is a query.
      # We essentially allow the user to query the effect that the mutation had
      # on our system.
      # In this case we are querying the changed book.
      book {
        title
      }
    }
  }
}
```

In one GraphQL request you can execute multiple mutations. Each of these mutations are executed serially one by one whereas their child selection sets are executed possibly in parallel since only the top-level mutations fields are allowed to have side-effects in GraphQL.

```graphql
{
  mutation {
    changeBookTitle(input: { id: 1, title: "C# in depth, Fourth Edition" }) {
      book {
        title
      }
    }
    publishBook(input: { id: 1 }) {
      book {
        isPublished
      }
    }
  }
}
```

A mutation type can be represented like the following:

<ExampleTabs>
<ExampleTabs.Annotation>

```csharp
// Mutation.cs
public class Mutation
{
    public async Task<Book> AddNewBook(Book book)
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
            .AddQueryType<Query>()
            .AddMutationType<Mutation>();
    }

    // Omitted code for brevity
}
```

</ExampleTabs.Annotation>
<ExampleTabs.Code>

```csharp
// Mutation.cs
public class Mutation
{
    public async Task<Book> AddNewBook(Book book)
    {
        // Omitted code for brevity
    }
}

// MutationType.cs
public class MutationType : ObjectType<Mutation>
{
    protected override void Configure(IObjectTypeDescriptor<Mutation> descriptor)
    {
        descriptor.Field(f => f.AddNewBook(default));
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
            .AddQueryType<QueryType>()
            .AddMutationType<MutationType>();
    }

    // Omitted code for brevity
}
```

</ExampleTabs.Code>
<ExampleTabs.Schema>

```csharp
// Mutation.cs
public class Mutation
{
    public async Task<Book> AddNewBook(Book book)
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
                  book: Book
                }

                type Mutation {
                  addNewBook(input: AddNewBookInput): AddNewBookPayload
                }

                type Book {
                  title: String
                  author: String
                }
            ")
            .BindComplexType<Query>()
            .BindComplexType<Mutation>();
    }

    // Omitted code for brevity
}
```

</ExampleTabs.Schema>
</ExampleTabs>

## Mutation Transactions

With multiple mutations executed serially in one request it sometimes would be great to put these into a transactions scope that we can control.

Hot Chocolate provides for this the `ITransactionScopeHandler` which is used by the operation execution middleware to create transaction scopes for mutation requests.

Hot Chocolate provides a default implementation based on the `System.Transactions.TransactionScope` which works with Microsoft ADO.NET data provider and hence can be used in combination with Entity Framework.

The default transaction scope handler can be added like the following:

```csharp
services
    .AddGraphQLServer()
    ...
    .AddDefaultTransactionScopeHandler();
```

This is how the default implementation looks like:

```csharp
/// <summary>
/// Represents the default mutation transaction scope handler implementation.
/// </summary>
public class DefaultTransactionScopeHandler : ITransactionScopeHandler
{
    /// <summary>
    /// Creates a new transaction scope for the current
    /// request represented by the <see cref="IRequestContext"/>.
    /// </summary>
    /// <param name="context">
    /// The GraphQL request context.
    /// </param>
    /// <returns>
    /// Returns a new <see cref="ITransactionScope"/>.
    /// </returns>
    public virtual ITransactionScope Create(IRequestContext context)
    {
        return new DefaultTransactionScope(
            context,
            new TransactionScope(
                TransactionScopeOption.Required,
                new TransactionOptions
                {
                    IsolationLevel = IsolationLevel.ReadCommitted
                }));
    }
}
```

If you implement a custom transaction scope handler or if you choose to extend upon the default transaction scope handler you can add it like the following.

```csharp
services
    .AddGraphQLServer()
    ...
    .AddTransactionScopeHandler<CustomTransactionScopeHandler>();
```

# Subscription

Documentation following ...
