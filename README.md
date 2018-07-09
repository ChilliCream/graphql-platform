![HotChocolate](https://cdn.rawgit.com/ChilliCream/hotchocolate-logo/master/img/hotchocolate-banner-light.svg)

[![GitHub release](https://img.shields.io/github/release/chillicream/HotChocolate.svg)](https://github.com/ChilliCream/hotchocolate/releases) [![NuGet Package](https://img.shields.io/nuget/v/hotchocolate.svg)](https://www.nuget.org/packages/HotChocolate/) [![License](https://img.shields.io/github/license/ChilliCream/hotchocolate.svg)](https://github.com/ChilliCream/hotchocolate/releases) [![Build](https://ci.appveyor.com/api/projects/status/uf8xnbyo32bh7ge1/branch/master?svg=true)](https://ci.appveyor.com/project/rstaib/zeus) [![Tests](https://img.shields.io/appveyor/tests/rstaib/zeus/master.svg)](https://ci.appveyor.com/project/rstaib/zeus) [![Coverage Status](https://coveralls.io/repos/github/ChilliCream/hotchocolate/badge.svg?branch=master)](https://coveralls.io/github/ChilliCream/hotchocolate?branch=master) [![BCH compliance](https://bettercodehub.com/edge/badge/ChilliCream/hotchocolate?branch=master)](https://bettercodehub.com/)

---

**Hot Chocolate is a GraphQL Server for _.NET Core_ and _.NET Classic_**

_Hot Chocolate_ is a GraphQL server and parser implementation based on the current GraphQL [June 2018 specification](http://facebook.github.io/graphql/June2018/) defined by facebook.
Currently we are still closing some gaps and hope to finalise Version 1 by September. We have listed the implemented specification parts at the bottom of this readme.

## Getting Started

If you are just getting started with GraphQL a good way to learn is visiting [GraphQL.org](https://graphql.org).
We have implemented the Star Wars example with the Hot Chocolate API and you can use our example implementation to follow along.

In order to get generate the example project head over to your console and fire up the following commands.

```bash
mkdir starwars
cd starwars
dotnet new -i HotChocolate.Templates.StarWars
dotnet new starwars
```

The GraphQL specification and more is available on the [Facebook GraphQL repository](https://github.com/facebook/graphql).

### Using Hot Chocolate

The easiest way to get a feel for the API is to walk through our README example. But you can also visit our [documentation](http://hotchocolate.io) for a deep dive.

_Hot Chocolate_ can build a GraphQL schema, serve queries against that schema and host that schema for web requests.

_For our examples we use .net core and the dotnet CLI which you can download [here](https://dot.net)._

Lets get started by setting up a new console application that we will use to showcase how to setup a GraphQL schema and execute queries against it.

```bash
mkdir graphql-demo
cd graphql-demo
dotnet new console -n graphql-console
```

Now add the query engine package to the project with the following command.

```bash
dotnet add package HotChocolate
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

In order to setup a GraphQL HTTP endpoint hot chocolate comes with a asp.net core middleware. In order to set it up create a new empty web project with the dotnet CLI.

```bash
dotnet new web -n graphql-web
```

Now add our middleware package to the project with the following command.

```bash
dotnet add package HotChocolate.AspNetCore
```

Open the Startup.cs and add the following code.

```csharp
protected override void ConfigureServices(IServiceCollection services)
{
    services.AddGraphQL(c => c.RegisterQueryType<ObjectType<Query>>());
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

_We are also currently working on a middleware for ASP.net classic._

### Templates

Apart from the Star Wars template we also have a GraphQL server template that just generates a project with everything hooked up so that you can start building your API quickliy.

So, to install the GraphQL Server template run the following command.

```bash
dotnet new -i HotChocolate.Templates.Server
```

Now that you have implemented this you can generate a new server project by running the following commands.

```bash
mkdir myserver
cd myserver
dotnet new graphql-server
```

## Features

We currently support the following parts of the current [draft spec](http://facebook.github.io/graphql/draft/) of GraphQL.

### Types

- [x] Object Type
- [x] Interface Type
- [x] Union Type
- [x] Enum Type
- [x] Input Object Type

### Scalar Types

- [x] Int
- [x] Float
- [x] String
- [x] Boolean
- [x] ID

### Directives

- [x] Skip
- [x] Continue
- [ ] Depricated

### Validation

- [ ] Validation

  For a detailed view which validation rule currently is implemented have a look at our issues

### Execution

- [x] Query
- [x] Mutation
- [ ] Subscription

### Introspection

- Fields
  - [x] __typename
  - [x] __type
  - [x] __schema

- __Schema
  - [x] types
  - [x] queryType
  - [x] mutationType
  - [x] subscriptionType
  - [ ] directives

- __Type
  - [x] kind
  - [x] name
  - [x] fields
  - [x] interfaces
  - [x] possibleTypes
  - [x] enumValues
  - [x] inputFields
  - [x] ofType

Moreoreover, we are working on the following parts that are not defined in the spec.

### Additional Scalar Types

- [x] DateTime
- [x] Date
- [ ] Time
- [ ] URL
- [x] Decimal
- [x] Short (Int16)
- [x] Long (Int64)
- [x] Custom Scalars

### Additional Directives

- [ ] Export
- [ ] Defer
- [ ] Stream
- [ ] Custom Schema Directives
- [ ] Custom Execution Directives

### Schema Creation

- [x] Schema-First approach
- [x] Code-First approach

## Supported Frameworks

- [ ] ASP.NET Classic
  - [ ] Get
  - [ ] Post

- [x] ASP.NET Core
  - [x] Get
  - [x] Post

## Documentation

For more examples and a detailed documentation click [here](http://hotchocolate.io).
