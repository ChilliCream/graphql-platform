---
title: "Resolver"
---

Here we will learn what resolvers are, how they are defined, and what else we could do with them in Hot Chocolate.

# Introduction

When it comes to fetching data in a GraphQL server, you will always end up with a resolver.
**A resolver is a generic function that fetches data from an arbitrary data source for a particular field.**
It means every field has its specific resolver function to fetch or select data. Even if there isn't a resolver defined for one field, Hot Chocolate will create a default resolver for this particular field behind the scenes.

```mermaid
graph TD
  A(field) --> B{has resolver?}
  B --> |yes|C(return resolver)
  B --> |no|D(create default resolver)
  D --> C
```

In Hot Chocolate, a default resolver is a compiled function for a specific field that accesses a property of its parent value, which matches with the field name. For example, if we have a parent value of type `User`, which has a field called `name`, the compiled default resolver for the field `name` would look like the following.

```csharp
var resolver = (User parent) => parent.Name;
```

It's not exactly how it's implemented in Hot Chocolate, but it serves here basically as a simplified illustration. The key takeaway is that there is always a resolver for every field in place.

> **Note:** The parent value represents the parent resolver's inner value, or in the case of a root resolver, the root value, which means the root type's value (query, mutation, or subscription). It has nothing to do with the result type of a resolver and is specific to the business logic.

## Resolver Tree

A resolver tree is a projection of a GraphQL operation that is prepared for execution. The execution engine takes the resolver tree and follows the path of resolvers from top to down. For better understanding, let's imagine we have a simple GraphQL query like the following, where we select the currently logged-in user's name.

```graphql
query {
  me {
    name
  }
}
```

In Hot Chocolate, this query results in the following resolver tree.

```mermaid
graph TD
  A(query: QueryType) --> B("me() => [UserType]")
  B --> C("name() => StringType")
```

A resolver tree is, in the end, nothing else than a resolver chain where each branch can be executed in parallel.

```mermaid
graph LR
  A("me()") --> B("name()")
```

Okay, let's dissect a little further here. A resolver chain always starts with one or many root resolver, which is in our case `me()` and then follows the path along. In this scenario, the next resolver would be `name()`, which is also the last resolver in our chain. As soon as `me` has fetched the user profile of the currently logged-in user, Hot Chocolate will immediately start executing the next resolver and feeding in the previous object value, also called a parent or parent value in spec language. Let's say the parent value looks like this.

```csharp
var parent = new User
{
  Id = "user-1",
  Name = "ChilliCream",
  ...
}
```

Then the `name()` resolver can just access the `Name` property of the parent value and simply return it. As soon as all resolvers have been completed, the execution engine would return the following GraphQL result, provided that everything went successful.

```json
{
  "data": {
    "me": {
      "name": "ChilliCream"
    }
  }
}
```

Excellent, now that we know what resolvers are and how they work in a bigger picture, how can we start writing one. Let's jump to the next section and find out.

# Defining a resolver

A resolver is a function that takes zero or many arguments and returns one value. The simplest resolver to write is a resolver that takes zero arguments and returns a simple value type (e.g., a string). For simplicity, we will do precisely that in our first example. Creating a resolver named `Say` with no arguments, which returns just a static string value `Hello World!`.

> **Note:** Every single code example will be shown in three different approaches, annotation-based (previously known as pure code-first), code-first, and schema-first. However, they will always result in the same outcome on a GraphQL schema perspective and internally in Hot Chocolate. All three approaches have their pros and cons and can be combined when needed with each other. If you would like to learn more about the three approaches in Hot Chocolate, click on [Coding Approaches](/docs/hotchocolate/api-reference/coding-approaches).

## Basic resolver example

**Annotation-based approach**

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
    public string Say() => "Hello World!";
}

// QueryType.cs
public class QueryType
    : ObjectType<Query>
{
    protected override void Configure(
        IObjectTypeDescriptor<Query> descriptor)
    {
        descriptor
            .Field(f => f.Say())
            .Type<NonNullType<StringType>>();
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
                    say: String!
                }
            ")
            .BindComplexType<Query>();
    }

    // Omitted code for brevity
}
```

When comparing all three approaches side-by-side, we can see very quickly that they all look nearly the same. They all have the `Query` type in common, which is identical in all three approaches. Regardless, the `Query` type contains a method named `Say`, which is our resolver, in fact, the most significant bit here. The `Say` method will be translated into the `say` field on the schema side as soon as Hot Chocolate is initialized. As a small side note here, all three approaches will result in the same `SDL`.

```sdl
type Query {
  say: String!
}
```

Let's get back to where the approaches differentiate—the `Startup` class, which contains the service configuration that slightly differs in each approach. In the **annotation-based** approach, we bind the `Query` type to the GraphQL schema. Easy, quick, and without writing any GraphQL specific binding code. Hot Chocolate will do the hard part and infer everything from the type itself. In the **code-first** approach, we bind a meta-type, the `QueryType` type, which contains the GraphQL configuration for the `Query` type, to the GraphQL schema. Instead of inferring the GraphQL type, Hot Chocolate will take our specific GraphQL configuration and creates the GraphQL schema out of it. In the **schema-first** approach, we provide Hot Chocolate the `SDL` directly, and Hot Chocolate will match that to our resolver. Now that we know how to define a resolver in all three approaches, it's time to learn how to pass arguments into a resolver. Let's head to the next section.

# Resolver Arguments

A resolver argument, not to be confused with a field argument in GraphQL, can be a field argument value, a DataLoader, a DI service, state, or even context like a parent value.

# Naming Rules

- How should we name things
- How is a method name translated

# Best Practices

# Resolver Pipeline

# Error Handling
