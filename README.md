![HotChocolate](https://cdn.rawgit.com/ChilliCream/hotchocolate-logo/acacc5b353f4a21bc03591d9910232c3c748d552/img/hotchocolate-banner-light.svg)

[![GitHub release](https://img.shields.io/github/release/chillicream/HotChocolate.svg)](https://github.com/ChilliCream/hotchocolate/releases) [![NuGet Package](https://img.shields.io/nuget/v/hotchocolate.svg)](https://www.nuget.org/packages/HotChocolate/) [![License](https://img.shields.io/github/license/ChilliCream/hotchocolate.svg)](https://github.com/ChilliCream/hotchocolate/releases) [![AppVeyor](https://ci.appveyor.com/api/projects/status/uf8xnbyo32bh7ge1/branch/master?svg=true)](https://ci.appveyor.com/project/rstaib/zeus) [![Travis](https://travis-ci.org/ChilliCream/hotchocolate.svg?branch=master)](https://travis-ci.org/ChilliCream/hotchocolate) [![Tests](https://img.shields.io/appveyor/tests/rstaib/zeus/master.svg)](https://ci.appveyor.com/project/rstaib/zeus) [![Coverage Status](https://sonarcloud.io/api/project_badges/measure?project=HotChocolate&metric=coverage)](https://sonarcloud.io/dashboard?id=HotChocolate) [![Quality](https://sonarcloud.io/api/project_badges/measure?project=HotChocolate&metric=alert_status)](https://sonarcloud.io/dashboard?id=HotChocolate) [![BCH compliance](https://bettercodehub.com/edge/badge/ChilliCream/hotchocolate?branch=master)](https://bettercodehub.com/)

---

**Hot Chocolate is a GraphQL server for _.NET Core_ and _.NET Classic_**

_Hot Chocolate_ is a GraphQL server and parser implementation based on the current GraphQL [June 2018 specification](http://facebook.github.io/graphql/June2018/) defined by Facebook.

We are currently in the process of closing some gaps and hope to finalise Version 1 by September. We have listed the implemented specification parts at the bottom of this readme.

## Getting Started

If you are just getting started with GraphQL a good way to learn is visiting [GraphQL.org](https://graphql.org).
We have implemented the Star Wars example with the Hot Chocolate API and you can use our example implementation to follow along.

To generate the example project, head over to your console and fire up the following commands:

```bash
mkdir starwars
cd starwars
dotnet new -i HotChocolate.Templates.StarWars
dotnet new starwars
```

The GraphQL specification and more is available on the [Facebook GraphQL repository](https://github.com/facebook/graphql).

### Using Hot Chocolate

The easiest way to get a feel for the API is to walk through our README example. If you need additional information, you can also have a look at our [documentation](http://hotchocolate.io).

_Hot Chocolate_ can build a GraphQL schema, serve queries against that schema and host that schema for web requests.

_For our examples we use .net core and the dotnet CLI which you can download [here](https://dot.net)._

Let’s get started by setting up a new console application that we will use to showcase how to set up a GraphQL schema and execute queries against it.

```bash
mkdir graphql-demo
cd graphql-demo
dotnet new console -n graphql-console
```

Now add the query engine package to your project with the following command.

```bash
dotnet add package HotChocolate
```

The GraphQL schema describes the capabilities of a GraphQL API. _Hot Chocolate_ allows you to do that code-first by defining .net classes describing that schema or schema-first by defining the schema in the GraphQL syntax and binding resolvers to it. Our README walkthrough shows you the code-first approach.

The following example shows the code-first approach.

```csharp
public class Program
{
    public static void Main(string[] args)
    {
        var schema = Schema.Create(c => c.RegisterQueryType<Query>());
    }
}

public class Query
{
    public string Hello() => "world";
}
```

The code above defines a simple schema with one type `Query` and one field `hello` that returns a string.

If you would write that schema down in the GraphQL syntax it would look as follows:

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

In order to set up a GraphQL HTTP endpoint, Hot Chocolate comes with an ASP.net core middleware.

Create a new project with the web template that comes with your dotnet CLI.

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

The above example adds the GraphQL schema and the execution engine to the dependency injection.

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

This will set up all the necessary endpoints to query the GraphQL schema via HTTP GET or HTTP POST. In order to run a query against your schema, start your web host and get [GraphiQL](https://github.com/graphql/graphiql).

By default, the middleware will be configured to listen on the service root for GraphQL requests. If you want to use a different endpoint route you can pass the desired route into the UseGraphQL instruction.

```csharp
protected override void Configure(IApplicationBuilder app, IHostingEnvironment env)
{
    if (env.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }
    app.UseGraphQL("Foo/Bar");
}
```

_We are also currently working on a middleware for ASP.net classic which is planned for Version 0.6.0._

### Templates

Apart from the Star Wars template, we also have a GraphQL server template that generates a project with everything hooked up so that you can start building your API quickly.

To install the GraphQL server template, run the following command:

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

We currently support the following parts of the current [June 2018 specification](http://facebook.github.io/graphql/June2018/) of GraphQL.

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
- [x] Deprecated

### Validation

- [x] [Validation](https://github.com/ChilliCream/hotchocolate/projects/3)

### Execution

- [x] Query
- [x] Mutation
- [x] Subscription

### Introspection

- Fields

  - [x] \_\_typename
  - [x] \_\_type
  - [x] \_\_schema

- \_\_Schema

  - [x] types
  - [x] queryType
  - [x] mutationType
  - [x] subscriptionType
  - [x] directives

- \_\_Type
  - [x] kind
  - [x] name
  - [x] fields
  - [x] interfaces
  - [x] possibleTypes
  - [x] enumValues
  - [x] inputFields
  - [x] ofType

Moreover, we are working on the following parts that are not defined in the spec.

### Draft Features

We are currently working on the following features that are proposed for the next GraphQL specification.

- [ ] [Limit directive uniqueness to explicitly marked directives](https://github.com/facebook/graphql/pull/472) (#291 in development - 0.7.0)
- [ ] [Add rules for how circular references in Input Objects are handled](https://github.com/facebook/graphql/pull/445) (in development - 0.10.0)
- [ ] [Add description to Schema](https://github.com/facebook/graphql/pull/466) (in development - 0.9.0)
- [ ] ["Directive order is significant" section](https://github.com/facebook/graphql/pull/470) (in development - 0.7.0)

### Additional Scalar Types

- [x] DateTime
- [x] Date
- [x] URL
- [x] UUID
- [x] Decimal
- [x] Short (Int16)
- [x] Long (Int64)
- [x] Custom Scalars

### Additional Directives

- [ ] Schema Stitching (in development - 0.8.0)
- [ ] HTTP Directives (in development - 0.8.0)
- [x] Custom Schema Directives
- [x] Custom Query Directives

### Execution Engine

- [x] Custom Context Objects
- [x] Data Loader Integration / Batched Operations

### Schema Creation

- [x] Schema-First approach
- [x] Code-First approach

## Supported Frameworks

- [ ] ASP.NET Classic
  - [ ] _Get_ (in development - 0.7.0)
  - [ ] _Post_ (in development - 0.7.0)
  - [ ] _WebSockets_ (in development - 0.8.0)
  - [ ] Schema Builder (in development - 1.0.0)

- [x] ASP.NET Core
  - [x] Get
  - [x] Post
  - [x] WebSockets
  - [ ] Schema Builder (in development - 0.11.0)

## Documentation

For more examples and detailed documentation, click [here](http://hotchocolate.io).

For documentation about our _DataLoader_ implementation click [here](https://github.com/ChilliCream/greendonut).
