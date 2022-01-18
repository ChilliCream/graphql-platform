---
title: Global State
---

import { ExampleTabs, Annotation, Code, Schema } from "../../../components/mdx/example-tabs"

Global State allows us to define properties on a per-request basis to be made available to all resolvers and middleware.

# Initializing Global State

We can add Global State using the `SetProperty` method on the `IQueryRequestBuilder`. This method takes a `key` and a `value` as an argument. While the `key` needs to be a `string` the value can be of any type.

Using an interceptor allows us to initialize the Global State before the request is being executed.

```csharp
public class HttpRequestInterceptor : DefaultHttpRequestInterceptor
{
    public override ValueTask OnCreateAsync(HttpContext context,
        IRequestExecutor requestExecutor, IQueryRequestBuilder requestBuilder,
        CancellationToken cancellationToken)
    {
        string userId =
            context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        requestBuilder.SetProperty("UserId", userId);
        // requestBuilder.SetProperty("IntegerValue", int.Parse(userId));
        // requestBuilder.SetProperty("ObjectValue", new User { Id = userId });

        return base.OnCreateAsync(context, requestExecutor, requestBuilder,
            cancellationToken);
    }
}
```

[Learn more about interceptors](/docs/hotchocolate/server/interceptors)

# Accessing Global State

We can access the Global State in our resolvers like the following.

<ExampleTabs>
<Annotation>

```csharp
public class Query
{
    public string Example1([GlobalState("UserId")] string userId)
    {
        // Omitted code for brevity
    }

    public string Example2([GlobalState("ObjectValue")] User user)
    {
        // Omitted code for brevity
    }
}
```

The `GlobalStateAttribute` accepts the `key` of the Global State `value` as an argument. An exception is thrown if no Global State value exists for the specified `key` or if the `value` can not be coerced to the type of the argument.

It's a good practice to create a new attribute inheriting from `GlobalStateAttribute`.

```csharp
public class UserIdAttribute : GlobalStateAttribute
{
    public UserIdAttribute() : base("UserId")
    {

    }
}

public class Query
{
    public string Example([UserId] string userId)
    {
        // Omitted code for brevity
    }
}
```

</Annotation>
<Code>

```csharp
public class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor
            .Field("example")
            .Resolve(context =>
            {
                var userId = context.GetGlobalValue<string>("UserId");

                // Omitted code for brevity
            });
    }
}
```

> ⚠️ Note: If no value exists for the specified `key` a default value is returned an no exception is thrown.

We can also access the Global State through the `ContextData` dictionary on the `IResolverContext`.

```csharp
descriptor
    .Field("example")
    .Resolve(context =>
    {
        if (!context.ContextData.TryGetValue("UserId", out var value)
            || value is not string userId)
        {
            // handle failed assertion
        }

        // Omitted code for brevity
    });
```

</Code>
<Schema>

Take a look at the Annotation-based or Code-first example.

</Schema>
</ExampleTabs>
