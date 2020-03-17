---
path: "/blog/2019/12/26/hot-chocolate-10.3.0"
date: "2020-03-18"
title: "Get started with EF Core in a Hot Chocolate GraphQL Server"
author: Michael Staib
authorURL: https://github.com/michaelstaib
authorImageURL: https://avatars1.githubusercontent.com/u/9714350?s=100&v=4
---

![Hot Chocolate](/img/blog/hotchocolate-banner.svg)

This article shows how to use _Entity Framework Core_ in an _Hot Chocolate_ GraphQL server.

With the release of version 10.4 of _Hot Chocolate_ we started to support _Entity Framework_ out of the box. _Entity Framework_ for a long time is one of the most requested features and we are happy to finally help the community along with this.

<!--truncate-->

## Introduction

In this article we will mainly discuss _Entity Framework Core_ also we also support _Entity Framework 6_. _Entity Framework_ is an OR-mapper from Microsoft that implements the unit-of-work pattern. This basically means that with _Entity Framework_ you work against your `DBContext` and once in a while commit the changes to the database by invoking `SaveChanges` on the context.

While this makes _Entity Framework_ nice to use it also introduces some issues with it for GraphQL. With GraphQL the default execution strategy is to parallelize the execution of fields. This means that we potentially access the same scoped `DBContext` with two different threads.

Since now two threads modify the local state of the `DBContext` we are getting into error states.

## Serial Execution

With version 10.4 we are now allowing to opt into a fully serial execution strategy. While this potentially slows down the processing of the query we can guarantee with this that the `DBContext` is only accessed by a single thread. With version 11 we will improve on this and allow to use pooling for the `DBContext` so that a resolver can rent a pool and return it.

In order to force serial execution for the entire query graph you now can set a the new `ForceSerialExecution` option for the query execution.

```csharp
services
    .AddGraphQL(sp =>
        SchemaBuilder.New()
            ...
            .Create(),
        new QueryExecutionOptions { ForceSerialExecution = true });
```

As I said with the upcoming version 11 we will integrate with the `DBContext` pooling feature and allow for renting multiple context for one query execution.

## Projections

With the serial execution feature you basically can just use entity framework to pull in data without worrying to run into thread exceptions.

But when we started talked about integrating _Entity Framework_ we immediately started talking about rewriting the whole query graph into one native query on top of _Entity Framework_. With 10.4 we are introducing the first step on this road with our new projections.

The new _Hot Chocolate_ projections allows us to annotate on the root field that we want to use the selections middleware and _Hot Chocolate_ will then take the query graph from the annotation and rewrite it into a native query.

```csharp
[UseSelection]
public IQueryable<Person> GetPeople(
    [Service]ChatDbContext dbContext) =>
    dbContext.People;
```

Whenever I know write a GraphQL query like: 

```graphql
{
    people {
        name
    }
}
```

It translates into:

```SQL
SELECT [Name] FROM [People]
```

But we did not stop here. We already have those nice middleware that you can use for filtering and like always you can combine these. So, lets take our initial example and improve upon this:

```csharp
[UseSelection]
[UseFiltering]
[UseSorting]
public IQueryable<Person> GetPeople(
    [Service]ChatDbContext dbContext) =>
    dbContext.People;
```

Whenever I know write a GraphQL query like:

```graphql
{
    people(where: { name: "foo" }) {
        name
    }
}
```

It translates into:

```SQL
SELECT [Name] FROM [People] WHERE [Name] = 'foo'
```

The selection middleware is not only effecting level on which we annotated it but will take the whole sub-graph into account. This means that if our `Person` for instance has a collection of addresses then we can just dig in.

```graphql
{
    people(where: { name: "foo" }) {
        name
        addresses {
            street
        }
    }
}
```


There were two main issues that made using _Entity Framework_ with _Hot Chocolate_ difficult. The first issue is that the `DBContext` is not thread-safe. _Entity Framework_ implements the unit-of-work pattern and basically to work against a context that holts in memory instances of your entities. You can change those entities in memory and when you have done all you needed to do you invoke `SaveChangesAsync` and all is good.

With ASP.NET Core the `DBContext` is added to the dependency injection as scoped reference by default or in newer version you now can pool them. You basically get one instance per request, you do what you need to do with the context and the

With GraphQL the default execution algorithm for queries executes fields potentially in parallel.

BTW, head over to our _pure code-first_ [Star Wars example](https://github.com/ChilliCream/hotchocolate-examples/tree/master/PureCodeFirst).

If you want to get into contact with us head over to our [slack channel](https://join.slack.com/t/hotchocolategraphql/shared_invite/enQtNTA4NjA0ODYwOTQ0LTViMzA2MTM4OWYwYjIxYzViYmM0YmZhYjdiNzBjOTg2ZmU1YmMwNDZiYjUyZWZlMzNiMTk1OWUxNWZhMzQwY2Q) and join our community.

| [HotChocolate Slack Channel](https://join.slack.com/t/hotchocolategraphql/shared_invite/enQtNTA4NjA0ODYwOTQ0LTViMzA2MTM4OWYwYjIxYzViYmM0YmZhYjdiNzBjOTg2ZmU1YmMwNDZiYjUyZWZlMzNiMTk1OWUxNWZhMzQwY2Q) | [Hot Chocolate Documentation](https://hotchocolate.io) | [Hot Chocolate on GitHub](https://github.com/ChilliCream/hotchocolate) |
| ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------ | ---------------------------------------------------------------------- |


[hot chocolate]: https://hotchocolate.io
[hot chocolate source code]: https://github.com/ChilliCream/hotchocolate
