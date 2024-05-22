---
path: "/blog/2019/04/11/integration-tests"
date: "2019-04-11"
title: "GraphQL - How to write integration tests against Hot Chocolate"
tags: ["hotchocolate", "graphql", "dotnet", "aspnetcore", "testing"]
author: Michael Staib
authorUrl: https://github.com/michaelstaib
authorImageUrl: https://avatars1.githubusercontent.com/u/9714350?s=100&v=4
---

**This post is outdated. If you are looking to do tests for Hot Chocolate 12 or newer watch our YouTube episode on testing.**

<Video videoId="Nf7nX2H_iiM" />

Today I was asked in our slack channel how one could write an integration test against Hot Chocolate without setting up an ASP.NET Core _TestServer_.
Though the ASP.NET Core _TestServer_ API is quite nice, it is much more cumbersome to test a schema this way.

For full integration tests through all the layers we could in fact setup a test GraphQL endpoint with the complete ASP.net core pipeline by using the ASP.NET core _TestServer_ API.

With this approach we could ensure that the GraphQL endpoint is correctly configured and works well within our service. In many cases this seems too much since we only want to test parts of the schema.

> If you want to read more about the ASP.NET Core _TestServer_ API there is a nice article on the [Visual Studio Magazine](https://visualstudiomagazine.com/articles/2017/07/01/testserver.aspx).

## Setup

Before we get started, assume we have a simple query class representing our GraphQL `Query` type:

```csharp
public class Query
{
    public string SayHello() => "Hello";
}
```

In order to create a schema from that simple type we could just do the following:

```csharp
ISchema schema = Schema.Create(c => c.RegisterQueryType<Query>());
```

OK, now we have a schema against which we can write our tests.

Let\`s take a step back and let us think about what we want to actually test before we go into the how.

Most of the times we want to write tests that ensure that our internal services are correctly hooked up with the GraphQL layer. Basically, we want to test that our business logic works well in the context of GraphQL and that all data is passed correctly. This means that we want to write queries and assert the results of our query.

The second thing that might be worth to ensure is that our schema is correctly expressed, so that all the default values are ,correct and no unexpected field is exposed.

Last but not least we might want to test a query- or field-middleware in various situations.

## Integration Tests

All right, let us get started with the integration tests first. In order to write queries against our schema we need to create a query executor:

```csharp
IQueryExecutor executor = schema.MakeExecutable();
```

The next thing that is important when testing the query engine in isolation is dependency injection.

Dependency injection is provided through `IServiceProvider`, this makes it really easy to provide the services to the execution engine that we might need like our data layer or so on.

The easiest way ist to create a service collection and setup whatever we need.

```csharp
IServiceProvider serviceProvider =
    new ServiceCollection()
        .AddSingleton<Foo, Bar>()
        .BuildServiceProvider();
```

The second thing we have to ensure is that we did not use `HttpContext` in our resolver- or middleware-logic.

**Wait a minute, but how are we able to access properties from `HttpContext` when we are not allowed to access it?**

Agreed, in some cases we really need to have access to properties on the `HttpContext` like the current `HttpContext.User` or some header value. In these cases, we need to access some parts of the `HttpContext` and copy those parts we need to our context data. The context data dictionary is thread-safe and can be accessed in query-, field-middleware and the field-resolver. This makes it easy to abstract the user context from ASP.NET Core dependencies like `HttpContext`. By doing this we will make our schema more testable and less dependant on the service layer.

We can do this by writing a query middleware that copies these properties to our context or by using our `OnCreateRequestAsync` hook. I will show how this can be done at the end of this post.

For now, let us assume we have done that already, then the only thing that we would need to do is to set the context data when we create our request. So, lets put a simple test together to see how we can write a test:

```csharp
[Fact]
public async Task SayHello_HelloIsReturned()
{
    // arrange
    IServiceProvider serviceProvider =
        new ServiceCollection()
            .AddSingleton<IDataLayer, MyDataLayer>()
            .BuildServiceProvider();

    IQueryExecutor executor = Schema.Create(c =>
    {
        c.RegisterQueryType<Query>();
    })
    .MakeExecutable();

    IReadOnlyQueryRequest request =
        OperationRequestBuilder.New()
            .SetQuery("{ sayHello }")
            .SetServices(serviceProvider)
            .AddProperty("Key", "value")
            .Create();

    // act
    IExecutionResult result = await executor.ExecuteAsync(request);

    // assert
    // so how do we assert this thing???
}
```

That does look good already, but how do we assert the result and what is the result.

The query executor will return an execution result, depending on the type of operation it could be a `IResponseStream` or a `IReadOnlyQueryResult`.

An `IReadOnlyQueryResult` contains basically the result graph of the query, but asserting this could be very tiresome.

My good friend [Normen](https://github.com/nscheibe) who works at Swiss Life created a snapshot testing library that basically works like [jestjs](https://jestjs.io). We use _Snapshooter_ internally to test the Hot Chocolate core.

[Snapshooter](https://github.com/SwissLife-OSS/snapshooter) will create a snapshot at the first execution of the test. The snapshots are saved in a folder `__snapshot__` that is co-located with our test class. Every consecutive test run will be validated against that first snapshot. If the snapshots do not match the test will fail and tell us what part did not match.

So, let us have a look how our test would look like with this assertion in place.

```csharp
[Fact]
public async Task SayHello_HelloIsReturned()
{
    // arrange
    IServiceProvider serviceProvider =
        new ServiceCollection()
            .AddSingleton<IDataLayer, MyDataLayer>()
            .BuildServiceProvider();

    IQueryExecutor executor = Schema.Create(c =>
    {
        c.RegisterQueryType<Query>();
    })
    .MakeExecutable();

    IReadOnlyQueryRequest request =
        OperationRequestBuilder.New()
            .SetQuery("{ sayHello }")
            .SetServices(serviceProvider)
            .AddProperty("Key", "value")
            .Create();

    // act
    IExecutionResult result = await executor.ExecuteAsync(request);

    // assert
    result.MatchSnapshot();
}
```

This test looks very clean now, the snapshots are serializing to json which makes them easy to read.

```json
{
  "Data": {
    "sayHello": "hello"
  },
  "Extensions": {},
  "Errors": []
}
```

The awesome thing with snapshooter is that we can ignore parts of our result-graph or validate one property of the result-graph in a special way.

```csharp
result.MatchSnapshot(o =>
    o.IgnoreField("Extensions.SomeProperty"));
```

For more information about how snapshooter works head over to their repository:

<https://github.com/SwissLife-OSS/snapshooter>

## Schema Tests

Ok, lets have a look at our second category. This I think is the simplest test we will write and probably we will just have one or two of those tests.

Hot Chocolate lets us print our schema as GraphQL SDL, this means that we can create a simple SDL representation like the following:

```graphql
type Query {
  sayHello: String
}
```

In order to get this representation we just have to do the following:

```csharp
Schema.Create(c => c.RegisterQueryType<Query>()).ToString();
```

That\`s quite simple, just calling `ToString()` on the schema will return the schema SDL representation.

The good thing with _Snapshooter_ is that we also can create snapshots of scalar values like a string. _Snapshooter_ will than just save the raw scalar as snapshot, so our SDL will **NOT** be polluted with JSON escape characters.

Our test could look like the following:

```csharp
[Fact]
public async Task Ensure_Schema_IsCorrect()
{
    // arrange
    ISchema schema = Schema.Create(c =>
    {
        c.RegisterQueryType<Query>();
    });

    // act
    string schemaSDL = schema.ToString();

    // assert
    schemaSDL.MatchSnapshot();
}
```

## Middleware/Resolver Tests

The last category concerns our middleware logic. I would strongly suggest testing a middleware with a unit test and not by firing a query against the query engine. You can use [Moq](https://github.com/Moq/moq4/wiki/Quickstart) to create a `IResolverContext` mock.

In cases that you want to test a resolver or middleware pipeline of a field you can retrieve those from that type like the following:

```csharp
[Fact]
public async Task SayHello_HelloIsReturned()
{
    // arrange
    IServiceProvider serviceProvider =
        new ServiceCollection()
            .AddSingleton<IDataLayer, MyDataLayer>()
            .BuildServiceProvider();

    ISchema schema = Schema.Create(c =>
    {
        c.RegisterQueryType<Query>();
    });

    ObjectType type = schema.GetType<ObjectType>("Query");
    ObjectField field = type.Fields["sayHello"];

    Mock<IResolverContext> contextMock = new Mock<IResolverContext>();
    // note that depending on what you are using in your resolver you will
    // have to setup properties for your mock.

    // act
    object result = await field.Resolver(contextMock.Object)

    // assert
    result.MatchSnapshot();
}
```

The resolver-property will just have the isolated resolver logic. In order to access the middleware pipeline, use the `Middleware` property on the field. The middleware represents the compiled middleware pipeline including the resolver.

## HttpContext Abstraction

So, lets come back the question about the `HttpContext`. In order to copy properties from the `HttpContext` to your GraphQL request I said that we can use `OnCreateRequestAsync`. This is actually the simplest way to do it.

Let us grab the user from the `HttpContext` and copy it to our context data dictionary as an example.

```csharp
app.UseGraphQL(new QueryMiddlewareOptions
{
    OnCreateRequest = (context, builder, ct) =>
    {
        builder.SetProperty("user", context.User);
        return Task.CompletedTask;
    }
})
```

The second way is a little bit more complicated but easier to test and feels cleaner.

We could write a little query middleware. The middleware could be provided as delegate like the upper example or we could take the extra effort to make a class.

```csharp
public class CopyUserMiddleware
{
    private readonly QueryDelegate _next;

    public CopyVariablesToResolverContextMiddleware(QueryDelegate next)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
    }

    public Task InvokeAsync(IQueryContext context)
    {
        IHttpContextAccessor accessor = context.Services.GetService<IHttpContextAccessor>();
        context.ContextData["user"] = accessor.HttpContext.User;
        return _next.Invoke(context);
    }
}
```

So, this code does the same as our first example but is now easily testable and can be integrated like the following to our GraphQL execution pipeline:

```csharp
services.AddGraphQL(Schema.Create(c =>
    {
        c.RegisterQueryType<Query>();
    })
    .MakeExecutable(b => b.Use<CopyUserMiddleware>().UseDefaultPipeline()));
```

I hope this little post will help when you start writing tests for your schema. If you run into any issues or if you have further questions/suggestions head over to our slack channel and we will be happy to help you.
