---
title: "Mutations"
---

import { ExampleTabs } from "../../../components/mdx/example-tabs"

The mutation type in GraphQL is used to mutate/change data. This means that when we are doing mutations, we are causing side-effects to the system.

GraphQL defines mutations as top-level fields on the mutation type. Meaning only the fields on the mutation root type itself are mutations. Everything that is returned from a mutation field represents the changed state of the server.

```graphql
{
  mutation {
    # changeBookTitle is a mutation and is allowed to cause side-effects.
    changeBookTitle(input: { id: 1, title: "C# in depth" }) {
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

In one GraphQL request we can execute multiple mutations. Each of these mutations are executed serially one by one whereas their child selection sets are executed possibly in parallel since only the top-level mutations fields are allowed to have side-effects in GraphQL.

```graphql
{
  mutation {
    addBook(input: { title: "C# in depth", author: "Jon Skeet" }) {
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

# Definition

A mutation type can be represented like the following:

<ExampleTabs>
<ExampleTabs.Annotation>

```csharp
public class Mutation
{
    public async Task<Book> AddBook(Book book)
    {
        // Omitted code for brevity
    }
}

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddGraphQLServer()
            // ...
            .AddMutationType<Mutation>();
    }

    // Omitted code for brevity
}
```

</ExampleTabs.Annotation>
<ExampleTabs.Code>

```csharp
public class Mutation
{
    public async Task<Book> AddBook(Book book)
    {
        // Omitted code for brevity
    }
}

public class MutationType : ObjectType<Mutation>
{
    protected override void Configure(IObjectTypeDescriptor<Mutation> descriptor)
    {
        descriptor.Field(f => f.AddBook(default));
    }
}

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddGraphQLServer()
            // ...
            .AddMutationType<MutationType>();
    }

    // Omitted code for brevity
}
```

</ExampleTabs.Code>
<ExampleTabs.Schema>

```csharp
public class Mutation
{
    public async Task<Book> AddBook(Book book)
    {
        // Omitted code for brevity
    }
}

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddGraphQLServer()
            .AddDocumentFromString(@"
                # ...

                type Mutation {
                  addBook(input: BookInput): Book
                }

                type BookInput {
                  title: String
                  author: String
                }

                type Book {
                  title: String
                  author: String
                }
            ")
            // ...
            .BindComplexType<Mutation>();
    }

    // Omitted code for brevity
}
```

</ExampleTabs.Schema>
</ExampleTabs>

# Transactions

With multiple mutations executed serially in one request it sometimes would be great to put these into a transactions scope that we can control.

Hot Chocolate provides for this the `ITransactionScopeHandler` which is used by the operation execution middleware to create transaction scopes for mutation requests.

Hot Chocolate provides a default implementation based on the `System.Transactions.TransactionScope` which works with Microsoft ADO.NET data provider and hence can be used in combination with Entity Framework.

The default transaction scope handler can be added like the following:

```csharp
services
    .AddGraphQLServer()
    // ...
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

If we implement a custom transaction scope handler or if we choose to extend upon the default transaction scope handler, we can add it like the following.

```csharp
services
    .AddGraphQLServer()
    // ...
    .AddTransactionScopeHandler<CustomTransactionScopeHandler>();
```
