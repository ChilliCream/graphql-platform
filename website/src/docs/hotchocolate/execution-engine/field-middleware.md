---
title: Field middleware
---

TODO introduce field middleware

# Execution order

TODO diagram, data middleware, why order matters

# Definition

Field middleware can be defined either as a delegate or as a separate type. In both cases we gain access to a `FieldDelegate` (`next`) and the `IMiddlewareContext` (`context`).

The `IMiddleware` context implements the `IResolverContext` interface so you can use all of the `IResolverContext` APIs in your middleware, similarly to how you would use them in your resolver code.

By awaiting the `FieldDelegate` we are waiting on all other field middleware that might come after the current middleware as well as the actual field resolver, computing the result of the field.

## Field middleware delegate

A field middleware delegate can be defined using Code-first APIs.

```csharp
public class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor
            .Field("example")
            .Use(next => async context =>
            {
                // code up here is executed before the following middleware
                // and the actual field resolver

                // this invokes the next middleware
                // or if we are at the last middleware the field resolver
                await next(context);

                // code down here is executed after all later middleware 
                // and the actual field resolver has finished executing
            })
            .Resolve(context =>
            {
                // Omitted for brevity
            });
    }
}
```

### Reusing the middleware delegate

As it's shown above the middleware is only applied to the `example` field on the `Query` type, but what if you want to use this middleware in multiple places?

You can simply create an extension method for the `IObjectFieldDescriptor`.

```csharp
public static class MyMiddlewareObjectFieldDescriptorExtension
{
    public static IObjectFieldDescriptor UseMyMiddleware(this IObjectFieldDescriptor descriptor)
    {
        descriptor
            .Use(next => async context =>
            {
                // Omitted code for brevity

                await next(context);

                // Omitted code for brevity
            });
    }
}
```

> Note: We recommend sticking to the convention of prepending `Use` to your extension method to indicate that it is applying a middleware.

You can now use this middleware in different places throughout your schema definition.

```csharp
public class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor
            .Field("example")
            .UseMyMiddleware()
            .Resolve(context =>
            {
                // Omitted for brevity
            });
    }
}
```

## Field middleware as a class

If you do not like using a delegate, you can also create a dedicated class for your middleware.

```csharp
public class MyMiddleware
{
    private readonly FieldDelegate _next;

    public MyMiddleware(FieldDelegate next)
    {
        _next = next;
    }

    // this method must be called InvokeAsync or Invoke
    public async Task InvokeAsync(IMiddlewareContext context)
    {
        // Code up here is executed before the following middleware
        // and the actual field resolver

        // This invokes the next middleware
        // or if we are at the last middleware the field resolver
        await _next(context);

        // Code down here is executed after all later middleware 
        // and the actual field resolver has finished executing
    }
}
```

If you need to access services you can either inject them via the constructor, if they are singleton, or as an argument of the `InvokeAsync` method, if they for example have a scoped lifetime.

```csharp
public class MyMiddleware
{
    private readonly FieldDelegate _next;
    private readonly IMySingletonService _singletonService;

    public MyMiddleware(FieldDelegate next, IMySingletonService singletonService)
    {
        _next = next;
        _singletonService = singletonService;
    }

    public async Task InvokeAsync(IMiddlewareContext context, IMyScopedService scopedService)
    {
        // Omitted code for brevity
    }
}
```

The ability to add additional arguments to the `InvokeAsync` is the reason why there isn't a contract like an interface or a base class for field middleware.

### Usage

Now that you've defined the middleware as a class we need to still apply it to a field.

```csharp
public class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor
            .Field("example")
            .Use<MyMiddleware>()
            .Resolve(context =>
            {
                // Omitted for brevity
            });
    }
}
```

If you need to pass an additional custom argument to the middleware you can do so using the factory overload of the `Use<T>`.

```csharp
descriptor
    .Field("example")
    .Use((services, next) => new MyMiddleware(next, "custom", 
        services.GetRequiredService<FooBar>()));
```

While an extension method like `UseMyMiddleware` doesn't make as much sense for `Use<MyMiddleware>` in contrast to the middleware delegate, we still recommend creating one as shown [here](#reusing-the-middleware-delegate). The reason being that you can make changes to this middleware more easily in the future without potentially having to change all places this middleware is being used.

# Usage as an attribute

Up until now we have only worked with Code-first APIs to create the field middleware. What if you want to apply your middleware to a field resolver defined using the Annotation-based approach?

You can create a new attribute inheriting from `ObjectFieldDescriptorAttribute` and call or create your middleware inside of the `OnConfigure` method.

```csharp
public class UseMyMiddlewareAttribute : ObjectFieldDescriptorAttribute
{
    public override void OnConfigure(IDescriptorContext context, 
        IObjectFieldDescriptor descriptor, MemberInfo member)
    {
        descriptor.UseMyMiddleware();
    }
}
```

The attribute can then be used like the following.

```csharp
public class Query
{
    [UseMyMiddleware]
    public string MyResolver()
    {
        // Omitted code for brevity
    }
}
```

# Accessing the resolver result

The `IMiddlewareContext` conveniently contains a `Result` property that can be used to access the field resolver result.

```csharp
descriptor
    .Use(next => async context =>
    {
        await next(context);

        // It only makes sense to access the result after calling
        // next(context), i.e. after the field resovler and any later
        // middleware has finished executing.
        object? result = context.Result;

        // If needed you can now narrow down the type of the result
        // using pattern matching and continue with the typed result
        if (result is string stringResult)
        {
            // Work with the stringResult
        }
    });
```

# Short-circuiting

In some cases we might want to short-circuit the execution of field middleware / the field resolver. For this we can simply not await the `FieldDelegate`.

```csharp
descriptor
    .Use(next => context =>
    {
        if(context.Parent<object>() is IDictionary<string, object> dict)
        {
            context.Result = dict[context.Field.Name];

            // We are not executing any of the later middleware or the field resolver
            return Task.CompletedTask;
        }
        else
        {
            return next(context);
        }
    })
```