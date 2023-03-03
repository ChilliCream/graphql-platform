---
title: Local state
---

Local state represents a key-value store that is scoped to the _resolver pipeline_. It can be used to share data between [field middleware](/docs/hotchocolate/v13/execution-engine/field-middleware) or between [field middleware](/docs/hotchocolate/v13/execution-engine/field-middleware) and the actual resolver of a field.

You could for example create a [field middleware](/docs/hotchocolate/v13/execution-engine/field-middleware) that creates or rents an object and puts it in the local state. This value can then be read from within a resolver the middleware has been applied to.

# Setting local state

You can set a local state value by calling `SetLocalValue` on either the `IMiddlewareContext` or the `IResolverContext`.

```csharp
public class TestObjectType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor
            .Field("field")
            .Use(next => async (context) =>
            {
                context.SetLocalValue("local-value", 123);
                await next(context);
            })
            .Resolve((context) =>
            {
                context.SetLocalValue("local-value-2", "my-local-value-2");
                // Omitted for brevity
            });
    }
}
```

The first argument is the _key_ under which to store the local state value. The second argument is the actual value you'd wish to store.

If you call `SetLocalValue` twice with the same _key_, the value associated with the _key_ will be that of the later call. So you would also use `SetLocalValue` to update a local state value.

# Reading local state

You can read a local state value by calling `GetLocalValue<T>` on either the `IMiddlewareContext` or the `IResolverContext`.

```csharp
public class TestObjectType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor
            .Field("field")
            .Use(next => async (context) =>
            {
                int localValue = context.GetLocalValue<int>("local-value");
                await next(context);
            })
            .Resolve(context =>
            {
                string? localValue2 = context
                    .GetLocalValue<string>("local-value-2");
                // Omitted for brevity
            });
    }
}
```

The first argument is the _key_ under which you stored the local state value. The type argument (`T`) must match the type under which you saved the local state value.

If the specified _key_ doesn't exist in the local state or the type of `T` is incompatible with the state, `null` will be returned.

You can also inject local state as an argument into your resolvers.

```csharp
public string? Field([LocalState] string myValue)
{
    // Omitted for brevity
}
```

The argument name will be used as _key_ when looking up the local state. In this case it would be `"myValue"`.

If you want to be more explicit, you can define the _key_ as an argument to the `LocalStateAttribute`.

```csharp
public string? Field([LocalState("local-state-value")] string someOtherName)
{
    // Omitted for brevity
}
```

If the specified _key_ can not be found you will get an exception. If the type of the state doesn't match the argument name and the value can not be coerced, you'll also get an error. You can of course inject the `IResolverContext` into your resolver and use the `GetLocalValue` method to try and retrieve the local state value, while correctly handling error cases yourself.

If you need to access the same local state in multiple resolves it can become quite tedious to have write out the `LocalStateAttribute` all the time. Fortunately you can use the [resolver compiler](/docs/hotchocolate/v13/execution-engine/resolver-compiler) to automatically inject local state into parameters of a certain type.

```csharp
builder.Services
    .AddSingleton<IParameterExpressionBuilder>(
        new CustomParameterExpressionBuilder<int?>(
            c => c.GetLocalValue<MyLocalState?>("local-value"),
            p => p.ParameterType == typeof(MyLocalState)));
builder.Services
    .AddGraphQLServer()
    // Omitted for brevity
    .AddQueryType<Test>();
public class Test
{
    [UseMiddlewareThatSetsMyLocalState]
    public string? Field(MyLocalState? someName)
    {
        // Omitted for brevity
    }
}
public record MyLocalState(string SomeValue, int SomeValue2);
```

The above would try to resolve the local state for the `"local-value"` _key_ for each argument of the `MyLocalState` type.

> ⚠️ Warning: Only try to bind complex types in this way, not primitives like `string`. Otherwise you will encounter problems where for example all `string` arguments will be omitted from the schema and treated as local state arguments.

# Removing local state

You can remove a local state value by calling `RemoveLocalValue` on either the `IMiddlewareContext` or the `IResolverContext`.

```csharp
public class TestObjectType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor
            .Field("field")
            .Use(next => async (context) =>
            {
                context.RemoveLocalValue("local-value");
                await next(context);
            })
            .Resolve(context =>
            {
                context.RemoveLocalValue("local-value-2");
                // Omitted for brevity
            });
    }
}
```

The first argument is the _key_ under which you stored the local state value

# Checking if a local state value exists

Through the `LocalContextData` on the `IMiddlewareContext` or the `IResolverContext` you have access to the underlying .NET dictionary, where the local state is being stored.

If you need to check if the local state contains a particular _key_, you can check it using the `ContainsKey` method on the dictionary.

```csharp
public class TestObjectType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor
            .Field("field")
            .Use(next => async (context) =>
            {
                if (context.LocalContextData.ContainsKey("local-value"))
                {
                    // The local state exists.
                }
                await next(context);
            })
            .Resolve(context =>
            {
                if (context.LocalContextData.ContainsKey("local-value-2"))
                {
                    // The local state exists.
                }
                // Omitted for brevity
            });
    }
}
```
