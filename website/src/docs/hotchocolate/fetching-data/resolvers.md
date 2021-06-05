---
title: "Resolvers"
---

import { ExampleTabs } from "../../../components/mdx/example-tabs"

When it comes to fetching data in a GraphQL server, it will always come down to a resolver.

**A resolver is a generic function that fetches data from an arbitrary data source for a particular field.**

We can think of each field in our query as a method of the previous type which returns the next type.

## Resolver Tree

A resolver tree is a projection of a GraphQL operation that is prepared for execution.

For better understanding, let's imagine we have a simple GraphQL query like the following, where we select some fields of the currently logged-in user.

```graphql
query {
  me {
    name
    company {
      id
      name
    }
  }
}
```

In Hot Chocolate this query results in the following resolver tree.

```mermaid
graph LR
  A(query: QueryType) --> B(me: UserType)
  B --> C(name: StringType)
  B --> D(company: CompanyType)
  D --> E(id: IdType)
  D --> F(name: StringType)
```

This tree will be traversed by the execution engine, starting with one or more root resolvers. In the above example the `me` field represents the only root resolver.

Field resolvers that are subselections of a field, can only be executed after a value has been resolved for their _parent_ field. In the case of the above example this means that the `name` and `company` resolvers can only run, after the `me` resolver has finished. Resolvers of field subselections can and will be executed in parallel.

**Because of this it is important that resolvers, with the exception of top level mutation field resolvers, do not contain side-effects, since their execution order may vary.**

The execution of a request finishes, once each resolver of the selected fields has produced a result.

_This is of course an oversimplification that differs from the actual implementation._

# Defining a Resolver

Resolvers can be defined in a way that should feel very familiar to C# developers, especially in the Annotation-based approach.

## Properties

Hot Chocolate automatically convertes properties with a public get accessor to default resolvers, returning the properties value.

Properties are also covered in detail by the [object type documentation](/docs/hotchocolate/defining-a-schema/object-types).

## Regular Resolver

A regular resolver is just a simple method, which returns a value.

<ExampleTabs>
<ExampleTabs.Annotation>

```csharp
public class Query
{
    public string Foo() => "Bar";
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

</ExampleTabs.Annotation>
<ExampleTabs.Code>

```csharp
public class Query
{
    public string Foo() => "Bar";
}

public class QueryType: ObjectType<Query>
{
    protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
    {
        descriptor
            .Field(f => f.Foo())
            .Type<NonNullType<StringType>>();
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

We can also provide a resolver delegate by using the `Resolve` method.

```csharp
descriptor
    .Field("foo")
    .Resolve(context =>
    {
        return "Bar";
    });
```

</ExampleTabs.Code>
<ExampleTabs.Schema>

```csharp
public class Query
{
    public string Foo() => "Bar";
}

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddGraphQLServer()
            .AddDocumentFromString(@"
                type Query {
                    foo: String!
                }
            ")
            .BindComplexType<Query>();
    }
}
```

We can also add a resolver, by calling `AddResolver()` on the `IRequestExecutorBuilder`.

```csharp
services
    .AddGraphQLServer()
    .AddDocumentFromString(@"
        type Query {
          foo: String!
        }
    ")
    .AddResolver("Query", "foo", (context) => "Bar");
```

</ExampleTabs.Schema>
</ExampleTabs>

## Async Resolver

Most data fetching operations, like calling a service or communicating with a database, will be asynchronous.

In Hot Chocolate we can simply mark our resolver methods and delegates as `async` or return a `Task<T>` and everything works out of the box.

We can also add a `CancellationToken` as argument to our resolver. Hot Chocolate will automatically cancel this token, if the request has been aborted.

```csharp
public class Query
{
    public async Task<string> Foo(CancellationToken ct)
    {
        // Omitted code for brevity
    }
}
```

When using a delegate resolver, the `CancellationToken` is passed as second argument to the delegate.

```csharp
descriptor
    .Field("foo")
    .Resolve((context, ct) =>
    {
        // Omitted code for brevity
    });
```

The `CancellationToken` can also be accessed through the `IResolverContext`.

```csharp
descriptor
    .Field("foo")
    .Resolve(context =>
    {
        CancellationToken ct = context.RequestAborted;

        // Omitted code for brevity
    });
```

## ResolveWith

Thus far we have looked at two ways to specify resolvers in Code-first:

- Add new methods to the CLR type, e.g. the `T` type of `ObjectType<T>`
- Add new fields to the schema type in the form of delegates
  ```csharp
  descriptor.Field("foo").Resolve(context => )
  ```

But there's a third way. We can describe our field using the `descriptor`, but instead of a resolver delegate, we can point to a method on another class, responsible for resolving this field.

```csharp
public class FooResolvers
{
    public string GetFoo(string arg, [Service] FooService service)
    {
        // Omitted code for brevity
    }
}

public class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor
            .Field("foo")
            .Argument("arg", a => a.Type<NonNullType<StringType>>())
            .ResolveWith<FooResolvers>(r => r.GetFoo(default, default));
    }
}
```

# Arguments

We can access the arguments we defined in the schema in our resolver, like regular arguments of a function.

There are also specific arguments that will be automatically populated by Hot Chocolate, when the resolver is executed. These include [Dependency injection services](#injecting-services), [DataLoaders](/docs/hotchocolate/fetching-data/dataloader), state, or even context like a [_parent_](#accessing-parent-values) value.

[Learn more about arguments](/docs/hotchocolate/defining-a-schema/arguments)

# Injecting Services

Resolvers integrate nicely with `Microsoft.Extensions.DependecyInjection`.
We can access all registered services in our resolvers.

Let's assume we have created a `UserService` and registered it as a service.

```csharp
public class UserService
{
    public List<User> GetUsers()
    {
        // Omitted code for brevity
    }
}

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddSingleton<UserService>()
            .AddGraphQLServer()
            .AddQueryType<Query>();
    }
}
```

We can then access the `UserService` in our resolvers like the following.

<ExampleTabs>
<ExampleTabs.Annotation>

```csharp
public class Query
{
    public List<User> GetUsers([Service] UserService userService)
        => userService.GetUsers();
}
```

</ExampleTabs.Annotation>
<ExampleTabs.Code>

```csharp
public class Query
{
    public List<User> GetUsers([Service] UserService userService)
        => userService.GetUsers();
}

public class QueryType: ObjectType<Query>
{
    protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
    {
        descriptor
            .Field(f => f.Foo(default))
            .Type<ListType<UserType>>();
    }
}
```

When using the `Resolve` method, we can access services through the `IResolverContext`.

```csharp
descriptor
    .Field("foo")
    .Resolve(context =>
    {
        var userService = context.Service<UserService>();

        return userService.GetUsers();
    });
```

</ExampleTabs.Code>
<ExampleTabs.Schema>

```csharp
public class Query
{
    public List<User> GetUsers([Service] UserService userService)
        => userService.GetUsers();
}
```

When using `AddResolver()`, we can access services through the `IResolverContext`.

```csharp
services
    .AddGraphQLServer()
    .AddDocumentFromString(@"
        type Query {
          users: [User!]!
        }
    ")
    .AddResolver("Query", "users", (context) =>
    {
        var userService = context.Service<UserService>();

        return userService.GetUsers();
    });
```

</ExampleTabs.Schema>
</ExampleTabs>

## Scoped Services

Scoped services can be injected in a similar fashion. The only difference is that we are now using the `[ScopedService]` instead of the `[Service]` attribute.

```csharp
public class Query
{
    public List<User> GetUsers([ScopedService] UserService userService)
        => userService.GetUsers();
}
```

TODO: How is this done in Code-first?

## IHttpContextAccessor

Like any other service we can also inject the `IHttpContextAccessor` into our resolver. This is useful, if we for example need to set a header or cookie.

```csharp
public string Foo(string id, [Service] IHttpContextAccessor httpContextAccessor)
{
    if (httpContextAccessor.HttpContext != null)
    {
        // Omitted code for brevity
    }
}
```

## IResolverContext

The `IResolverContext` is mainly used in delegate resolvers of the Code-first approach, but we can also access it in the Annotation-based approach, by simply injecting it.

```csharp
public class Query
{
    public string Foo(IResolverContext context)
    {
        // Omitted code for brevity
    }
}
```

# Accessing parent values

The resolver of each field on a type has access to the value that was resolved for said type.

Let's look at an example. We have the following schema.

```sdl
type Query {
  me: User!;
}

type User {
  id: ID!;
  friends: [User!]!;
}
```

The `User` schema type is represented by an `User` CLR type. The `id` field is an actual property on this CLR type.

```csharp
public class User
{
    public string Id { get; set; }
}
```

`friends` on the other hand is a resolver i.e. method we defined. It depends on the user's `Id` property to compute its result.
From the point of view of this `friends` resolver, the `User` CLR type is its _parent_.

We can access this so called _parent_ value like the following.

<ExampleTabs>
<ExampleTabs.Annotation>

TODO: Is the following discouraged?

In the Annotation-based approach we can just access the properties using the `this` keyword.

```csharp
public class User
{
    public string Id { get; set; }

    public List<User> GetFriends()
    {
        var currentUserId = this.Id;

        // Omitted code for brevity
    }
}
```

There's also a `[Parent]` attribute that injects the parent into the resolver.

```csharp
public class User
{
    public string Id { get; set; }

    public List<User> GetFriends([Parent] User parent)
    {
        // Omitted code for brevity
    }
}
```

This is especially useful when using [type extensions](/docs/hotchocolate/defining-a-schema/extending-types).

</ExampleTabs.Annotation>
<ExampleTabs.Code>

```csharp
public class User
{
    public string Id { get; set; }

    public List<User> GetFriends([Parent] User parent)
    {
        // Omitted code for brevity
    }
}
```

When using the `Resolve` method, we can access the parent through the `IResolverContext`.

```csharp
public class User
{
    public string Id { get; set; }
}

public class UserType : ObjectType<User>
{
    protected override void Configure(IObjectTypeDescriptor<User> descriptor)
    {
        descriptor
            .Field("friends")
            .Resolve(context =>
            {
                User parent = context.Parent<User>();

                // Omitted code for brevity
            });
    }
}
```

</ExampleTabs.Code>
<ExampleTabs.Schema>

TODO: Both bindcomplextype and addresolver

</ExampleTabs.Schema>
</ExampleTabs>

Due to how Hot Chocolate's execution engine works, we can not only access the _parent_ directly above us, but also _parents_ further up in the tree.
