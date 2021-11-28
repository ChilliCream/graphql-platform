---
title: Field middleware
---

TODO

# Definition

Field middleware can be defined either as a delegate or as a separate type.

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
> 
> For the namespace of this extension class we recommend `HotChocolate.Types`.

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


## Field middleware type

TODO

While an extension method like `UseMyMiddleware` doesn't make as much sense for the `Use<MyMiddleware>` in contrast to the middleware delegate, we still recommend creating on as shown [here](#reusing-the-middleware-delegate). The reason being that you can make easier changes to this middleware in the future without potentially affecting all places you've used this middleware in.

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

> Note: You do not have to create an extension method for your middleware first. Of course you can also just create your middleware directly in the `OnConfigure` method using `descriptor.Use`.

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

# Execution order