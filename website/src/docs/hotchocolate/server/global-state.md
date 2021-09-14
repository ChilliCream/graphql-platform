---
title: Global State
---

import { ExampleTabs } from "../../../components/mdx/example-tabs"

TODO

# Initializing Global State

We can add Global State using the `SetProperty` method on the `IQueryRequestBuilder`. This method takes a `key` and a `value` as an argument. WHile the `key` needs to be a `string` the value can be of any type.

Using an interceptor allows us to initialize the Global State on a per-request basis before the request is being executed.

```csharp
public class HttpRequestInterceptor : DefaultHttpRequestInterceptor
{
    public override ValueTask OnCreateAsync(HttpContext context,
        IRequestExecutor requestExecutor, IQueryRequestBuilder requestBuilder,
        CancellationToken cancellationToken)
    {
        string userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

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
<ExampleTabs.Annotation>

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

</ExampleTabs.Annotation>
<ExampleTabs.Code>

</ExampleTabs.Code>
<ExampleTabs.Schema>

</ExampleTabs.Schema>
</ExampleTabs>
