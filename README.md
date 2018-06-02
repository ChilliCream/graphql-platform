![HotChocolate](https://cdn.rawgit.com/ChilliCream/hotchocolate-logo/master/img/hotchocolate-banner-light.svg)

[![GitHub release](https://img.shields.io/github/release/chillicream/HotChocolate.svg)](https://github.com/ChilliCream/hotchocolate/releases) [![NuGet Package](https://img.shields.io/nuget/v/hotchocolate.svg)](https://www.nuget.org/packages/HotChocolate/) [![License](https://img.shields.io/github/license/ChilliCream/hotchocolate.svg)](https://github.com/ChilliCream/hotchocolate/releases) [![Build](https://img.shields.io/appveyor/ci/rstaib/prometheus/master.svg)](https://ci.appveyor.com/project/rstaib/prometheus) [![Tests](https://img.shields.io/appveyor/tests/rstaib/prometheus/master.svg)](https://ci.appveyor.com/project/rstaib/prometheus) [![coverage](https://img.shields.io/coveralls/ChilliCream/prometheus.svg)](https://coveralls.io/github/ChilliCream/prometheus?branch=master)

---

**Hot Chocolate is a GraphQL Server for _.net core_ and _.net classic_**

_Hot Chocolate_ is a GraphQL server and parser implementation based on the current GraphQL [draft specification](http://facebook.github.io/graphql/draft/) defined by facebook.

## Getting Started

If you are just getting started with GraphQL a good way to learn is visiting [GraphQL.org](https://graphql.org).
The GraphQL specification and more is available in the [Facebook GraphQL repository](https://github.com/facebook/graphql).

## Using Hot Chocolate

The easiest way to get a feel for the API is to walk through our README example. But you can also visit our [documentation](http://hotchocolate.io) for a deep dive.

_We use for our examples .net core which you can download [here](https://dot.net)._

```bash
mkdir graphql-demo
cd graphql-demo
dotnet new web
```

Hot Chocoloate provides two important capabilities: building a type schema, and serving queries against that type schema.

First, we will setup a GraphQL type schema which maps to your code base.
You can do that code-first meaning you define the GraohQL types as .net classes.

```csharp
public class Query
    : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor<IType> descriptor)
    {
        descriptor.Field("hello")
          .Resolver(() => "world");
    }
}

public class Startup 
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        var schema = Schema.Create(c => c.RegisterQuery<Query>()); 
    }

}
```

Or, you can do that schema-first. This means you specify the schema in GraphQL and bind .net types to it.

```csharp
public class Query 
{
    public string GetHello() => "World";
}

public class Startup 
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        var schema = Schema.Create(
          "type Query { hello: String }",
          c => c.BindType<Query>());
    }

}
```

## Documentation

Click [here](http://hotchocolate.io) for the documentation.
