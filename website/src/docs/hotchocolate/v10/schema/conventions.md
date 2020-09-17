---
title: Conventions
---

When we build a GraphQL schema with Hot Chocolate we have a lot of conventions in place that lets the schema builder infer the type structure and more from existing .NET types. These conventions are provided through the `DefaultNamingConventions` class and the `DefaultTypeInspector` class.

`DefaultNamingConventions` handles how things are named (e.g. lower-camel-case) or where to fetch the description of member.

`DefaultTypeInspector` on the other hand inspects the .NET types and infers from them the structure of the GraphQL types.

If we wanted for example to introduce custom attributes instead of our GraphQL\* attributes than we could inherit from those two classes and overwrite what we want to change. In order to provide the schema builder with our new conventions class all we had to do is to register our convention instances with our dependency injection provider.

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddSingleton<INamingConventions, MyNamingConventions>();

    services.AddGraphQL(sp => SchemaBuilder.New()
        .AddQueryType<Foo>()
        .AddServices(sp)
        .Create());
}
```
