![HotChocolate](https://cdn.rawgit.com/ChilliCream/hotchocolate-logo/master/img/hotchocolate-banner-light.svg)

[![GitHub release](https://img.shields.io/github/release/chillicream/HotChocolate.svg)](https://github.com/ChilliCream/hotchocolate/releases) [![NuGet Package](https://img.shields.io/nuget/v/hotchocolate.svg)](https://www.nuget.org/packages/HotChocolate/) [![License](https://img.shields.io/github/license/ChilliCream/hotchocolate.svg)](https://github.com/ChilliCream/hotchocolate/releases) [![Build](https://ci.appveyor.com/api/projects/status/uf8xnbyo32bh7ge1/branch/master?svg=true)](https://ci.appveyor.com/project/rstaib/zeus) [![Tests](https://img.shields.io/appveyor/tests/rstaib/zeus/master.svg)](https://ci.appveyor.com/project/rstaib/zeus) [![Coverage Status](https://coveralls.io/repos/github/ChilliCream/hotchocolate/badge.svg?branch=master)](https://coveralls.io/github/ChilliCream/hotchocolate?branch=master)

---

**Hot Chocolate is a GraphQL Server for _.net core_ and _.net classic_**

_Hot Chocolate_ is a GraphQL server and parser implementation based on the current GraphQL [draft specification](http://facebook.github.io/graphql/draft/) defined by facebook.

# Getting Started

If you are just getting started with GraphQL a good way to learn is visiting [GraphQL.org](https://graphql.org).
The GraphQL specification and more is available in the [Facebook GraphQL repository](https://github.com/facebook/graphql).

## Using Hot Chocolate

The easiest way to get a feel for the API is to walk through our README example. But you can also visit our [documentation](http://hotchocolate.io) for a deep dive.

_Hot Chocolate_ can build a GraphQL schema, serve queries against that schema and host that schema for web requests.

_For our examples we use .net core and the dotnet CLI which you can download [here](https://dot.net)._

Lets get started by setting up a new console application that we will use to showcase how to setup a GraphQL schema and execute queries against it.

```bash
mkdir graphql-demo
cd graphql-demo
dotnet new console -n graphql-console
```

The GraphQL schema describes the capabilities of a GraphQL API. _Hot Chocolate_ allows you to do that code-first by defining .net classes describing that schema or schema-first by defining the schema in the GraphQL syntax and binding resolvers to it. Our README walkthrough shows you the code-first approache.

The following example shows the code-first approach.

```csharp
public class Program
{
    public static void Main(string[] args)
    {
        var schema = Schema.Create(c => c.RegisterType<ObjectType<Query>>());
    }
}

public class Query
{
    public string Hello() => "world";
}
```

The code above defines a simple schema with one type `Query` and one field `hello` that returns a string.

If you would write that schema down in the GraphQL syntax it would look like the following.

```graphql
type Query {
  hello: String
}
```

Moreover, we bound a resolver to the field that returns a fixed value _world_. A resolver is basically a function that resolves the data for the specified field.

Now that the schema is setup we can serve up a query against it.

```graphql
{
  hello
}
```

```csharp
// Prints
// {
//   data: { hello: "world" }
// }
Console.WriteLine(schema.Execute("{ hello }"));
```

This runs a query fetching the one field defined. The graphql function will first ensure the query is syntactically and semantically valid before executing it, reporting errors otherwise.

```csharp
// {
//   "errors": [
//     {
//       "FieldName": "foo",
//       "Locations": [
//         {
//           "Line": 1,
//           "Column": 3
//         }
//       ],
//       "Message": "Could not resolve the specified field."
//     }
//   ]
// }
Console.WriteLine(schema.Execute("{ foo }"));
```

In order to setup a GraphQL HTTP endpoint that can be used by a web application or other application we have to first create an empty web project with the dotnet CLI.

```bash
dotnet new web -n graphql-web
```

Open the Startup.cs and add the following code.

```csharp
protected override void ConfigureServices(IServiceCollection services)
{
    services.AddGraphQL(c => c.RegisterQuery<ObjectType<Query>>());
}
```

```csharp
protected override void Configure(IApplicationBuilder app, IHostingEnvironment env)
{
    if (env.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }
    app.UseGraphQL();
}
```

This will setup all the necessary endpoints to query the GraphQL schema via HTTP GET or HTTP POST.
In order to run a query against your schema startup your web host and get [GraphiQL](https://github.com/graphql/graphiql).


## Documentation

For more examples and a detailed documentation click [here](http://hotchocolate.io).
