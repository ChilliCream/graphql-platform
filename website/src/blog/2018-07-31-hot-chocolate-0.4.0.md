---
path: "/blog/2018/07/31/hot-chocolate-0.4.0"
date: "2018-07-31"
title: "GraphQL - Hot Chocolate 0.4.0"
tags: ["hotchocolate", "graphql", "dotnet", "aspnetcore"]
author: Michael Staib
authorUrl: https://github.com/michaelstaib
authorImageUrl: https://avatars1.githubusercontent.com/u/9714350?s=100&v=4
---

With this version we introduce support for _DataLoaders_ and custom context objects.

## Data Loaders

Here is a short introduction to _DataLoaders_.

> A DataLoader is a generic utility to be used as part of your application's data fetching layer to
> provide a consistent API over various backends and reduce requests to those backends via batching
> and caching. -- facebook

If you want to read more about _DataLoaders_ in general, you can head over to Facebook's [GitHub repository](https://github.com/facebook/dataloader).

GraphQL is very flexible in the way you can request data. This flexibility also introduces new classes of problems called _n+1_ issues for the GraphQL server developer.

In order to depict the issue that DataLoaders solve in this context, let me introduce a little GraphQL schema:

```graphql
type Query {
  person(id: ID): Person
}

type Person {
  id: ID
  name: String
  friends: [Person]
}
```

The above schema allows to fetch a person by its internal identifier and each person has a list of friends that is represented by a list of persons.

Since GraphQL requests are not fixed requests like REST requests, the developer really defines what data he/she wants. This avoids over-fetching data that you do not need and also saves you unnecessary round-trips to the GraphQL backend.

So, a query against the above schema could look like the following:

```graphql
{
  a: person(id: "a") {
    name
  }

  b: person(id: "b") {
    name
  }
}
```

The above request fetches two persons in one go without the need to call the backend twice. The problem for the GraphQL backend is that field resolvers are atomic and do not have any knowledge about the query as a whole. So, a field resolver does not know that it will be called multiple times in parallel to fetch similar or equal data from the same data source.

This basically represents the first case where _DataLoaders_ help us by batching requests against our database or backend service. Currently, we allow _DataLoaders_ per request and globally.

So, let's look at some code in order to understand what they are doing. First, let's have a look at how we would write our field resolver without _DataLoaders_:

```csharp
public async Task<Person> GetPerson(string id, [Service]IPersonRepository repository)
{
    return await repository.GetPersonById(id);
}
```

The above example would result in two calls to the person repository that would than fetch the persons one by one from our data source.

If you think that through you can see that each GraphQL request would cause multiple requests to our data source resulting in sluggish performance and unnecessary round-trips to our data source.

This, means that we reduced the round-trips from our client to our server with GraphQL but multiplied the round-trips between the data sources and the service layer.

With _DataLoaders_ we can now centralize our person fetching and reduce the number of round trips to our data source.

First, we have to create a _DataLoader_ that now acts as intermediary between a field resolver and the data source.

```csharp
public class PersonDataLoader
    : DataLoaderBase<string, Person>
{
    private readonly IPersonRepository _repository;

    public PersonDataLoader(IPersonRepository repository)
        : base(new DataLoaderOptions<string>())
    {
        _repository = repository;
    }

    protected override Task<IReadOnlyList<Result<string>>> Fetch(
        IReadOnlyList<string> keys)
    {
        return _repository.GetPersonBatch(keys);
    }
}
```

The _DataLoader_ is now injected by the execution engine as a field resolver argument.

_DataLoaders_ have to be injected at field resolver argument level and **NOT** as constructor arguments since the lifetime of a _DataLoader_ is in many cases shorter than the class containing the field resolvers.

```csharp
public Task<Person> GetPerson(string id, [DataLoader]PersonDataLoader personLoader)
{
    return personLoader.LoadAsync(id);
}
```

Next, we have to register our _DataLoader_ with the schema. By default, _DataLoaders_ are registered as per-request meaning that the execution engine will create one instance of each _DataLoader_ per-request **if** a field resolver has requested a _DataLoader_. This ensures that, _DataLoaders_ that are not being requested are not instantiated unnecessarily.

```csharp
Schema.Create(c =>
{
    // your other code...

    c.RegisterDataLoader<PersonDataLoader>();
});
```

Now, person requests in a single execution batch will be batched to the data source.

But there are still some more issues ahead that _DataLoaders_ will help us with. For that we should amend our query a little bit.

```graphql
{
  a: person(id: "a") {
    name
    friends {
      name
    }
  }

  b: person(id: "b") {
    name
    friends {
      name
    }
  }
}
```

The above query now drills down into the friends property, which again yields persons.

Let's, say our person object is located in a mongo database and the document would look something like the following:

```json
{
  "id":"a"
  "name":"Foo"
  "friends": [
    "b",
    "c",
    "d"
  ]
}

{
  "id":"b"
  "name":"Bar"
  "friends": [
    "a",
    "c",
    "e"
  ]
}
```

The person with ID `a` is also friends with person `b`. Moreover, `a` is also friends with `c` and `d`. Furthermore, `b` is friends with `a` and also friends with `c` and `e`.
The best case now would be that we only fetch `c`, `d` and `e` since we have already fetched `a` and `b`.

This is the second problem class the _DataLoader_ utility helps us with since the _DataLoader_ contains a cache and holds the resolved instances by default for the duration of your request.

For more information about our _DataLoader_ implementation head over to our _DataLoader_ [GitHub repository](https://github.com/ChilliCream/greendonut).

As a side note, you are not bound to our _DataLoader_ implementation. If you want to create your own implementation of _DataLoaders_ or if you already have a _DataLoader_ implementation then you can hook this up to our execution engine as well. I will explain this in the _DataLoader_ documentation once I have finalized it.

## Custom Context Objects

Custom context objects are basically custom .net objects that you can declare with the GraphQL engine and access throughout your request execution. Custom context objects can use dependency injection and have the same scoping as the _DataLoaders_.

For example you could declare a class that handles authorization for your service like an IPrincipal and access this in each resolver.

```csharp
public Task<ResolverResult<Person>> GetPerson(string id, [State]MyPrincipal principal)
{
    if(principal.IsInRole("foo"))
    {
      return new ResolverResult<Person>(personLoader.LoadAsync(id));
    }
    return new ResolverResult<Person>(
      "You do not have the access role to access this person.");
}
```

Moreover, you can use this custom context to store states in or caches during execution time. This will become especially useful with our next version when we allow the writing of custom schema directives and field resolver middlewares.

Custom context objects are registered like _DataLoaders_:

```csharp
Schema.Create(c =>
{
    // your other code...

    c.RegisterCustomContext<MyPrincipal>();
});
```

Like with _DataLoaders_ we have multiple `RegisterCustomContext` overloads that allow for more control over how the object is created.

## Query Validation

With this release we have also implemented the following query validation rules:

- [All Variables Used](http://facebook.github.io/graphql/June2018/#sec-All-Variables-Used)
- [All Variable Uses Defined](http://facebook.github.io/graphql/June2018/#sec-All-Variable-Uses-Defined)
- [Directives Are In Valid Locations](http://facebook.github.io/graphql/June2018/#sec-Directives-Are-In-Valid-Locations)
- [Directives Are Unique Per Location](http://facebook.github.io/graphql/June2018/#sec-Directives-Are-Unique-Per-Location)
- [Variables Are Input Types](http://facebook.github.io/graphql/June2018/#sec-Variables-Are-Input-Types)
- [Field Selection Merging](http://facebook.github.io/graphql/June2018/#sec-Field-Selection-Merging)

You can follow our progress on which rule is implemented [here](https://github.com/ChilliCream/graphql-platform/projects/3).

We plan for full compliance with the June 2018 spec version with version 0.6.0.

## Dependency Injection

We reworked out dependency injection approach and have now integrated the request services during request execution. Meaning you are now able to access HttpContext directly as a field resolver argument.

This was already possible with the old version through the accessor as a constructor injection.

Generally speaking, you can now let the execution engine inject any service as a field resolver argument.

```csharp
public async Task<Person> Example1(string id, [Service]IPersonRepository repository)
{
    return await repository.GetPersonById(id);
}

public async Task<Person> Example2(string id, [Service]HttpContext context)
{
    return await repository.GetPersonById(id);
}
```

It is important to know that http related services are only available if the execution engine runs integrated into ASP.net core. So, basically if you are using our middleware.

From a design standpoint you should avoid accessing this directly and think about a custom context object which would provide some abstraction.

I will write some more on dependency injection sometime later this week.
