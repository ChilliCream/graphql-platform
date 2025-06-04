---
title: Dependency Injection
---

We are supporting dependency injection via the `IServiceProvider` interface. Since Hot Chocolate supports scoped services the service provider is passed in with the request. If you are using Hot Chocolate with ASP.NET core or ASP.NET classic then you do not have to think about how to setup dependency injection because we have already done that for you.

If you have a CLR-type representation of your schema type, then you can inject services as field resolver arguments. Injection of services as field resolver arguments should be your preferred choice since many of the services only have a request life time.

```csharp
public class Query
{
    public string Bar([Service]MyCustomService service)
    {
        return "foo";
    }
}
```

You are also able to inject parts from your field resolver context like the schema as field resolver argument.

```csharp
public class Query
{
    public string Bar(ISchema schema, [Service]MyCustomService service)
    {
        return "foo";
    }
}
```

Moreover, you have access to the `HttpContext` through field resolver argument injection. You should only inject `IHttpContextAccessor` as field resolver argument since the lifetime of `HttpContext` is bound to a single request.

```csharp
public class Query
{
    public string Bar([Service]IHttpContextAccessor contextAccessor)
    {
        return "foo";
    }
}
```
