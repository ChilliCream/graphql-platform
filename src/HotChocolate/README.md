![HotChocolate](https://chillicream.com/resources/hotchocolate-banner.svg)

[![GitHub release](https://img.shields.io/github/release/chillicream/HotChocolate.svg)](https://github.com/ChilliCream/hotchocolate/releases) [![NuGet Package](https://img.shields.io/nuget/v/hotchocolate.svg)](https://www.nuget.org/packages/HotChocolate/) [![License](https://img.shields.io/github/license/ChilliCream/hotchocolate.svg)](https://github.com/ChilliCream/hotchocolate/releases) [![Slack channel](https://img.shields.io/badge/join%20the%20community-on%20slack-blue.svg)](https://join.slack.com/t/hotchocolategraphql/shared_invite/enQtNTA4NjA0ODYwOTQ0LTViMzA2MTM4OWYwYjIxYzViYmM0YmZhYjdiNzBjOTg2ZmU1YmMwNDZiYjUyZWZlMzNiMTk1OWUxNWZhMzQwY2Q) [![Twitter](https://img.shields.io/badge/join%20us-on%20twitter-green.svg)](https://twitter.com/chilli_cream)

---

**Hot Chocolate is a GraphQL server for _.NET Core_ and _.NET Classic_**

_Hot Chocolate_ is a GraphQL server implementation based on the current GraphQL [June 2018 specification](https://graphql.github.io/graphql-spec/June2018/).

## Getting Started

If you are just getting started with GraphQL a good way to learn is visiting [GraphQL.org](https://graphql.org).
We have implemented the Star Wars example used on [GraphQL.org](https://graphql.org) with the Hot Chocolate API and you can use our example implementation to follow along.

To generate the example project, head over to your console and fire up the following commands:

```bash
mkdir starwars
cd starwars
dotnet new -i HotChocolate.Templates.StarWars
dotnet new starwars
```

The GraphQL specification and more is available on the [Facebook GraphQL repository](https://github.com/graphql/graphql-spec).

If you want to get in touch with us you can do so by joining our [slack group](https://join.slack.com/t/hotchocolategraphql/shared_invite/enQtNTA4NjA0ODYwOTQ0LTViMzA2MTM4OWYwYjIxYzViYmM0YmZhYjdiNzBjOTg2ZmU1YmMwNDZiYjUyZWZlMzNiMTk1OWUxNWZhMzQwY2Q).

> This readme only provides a simple quickstart, in order to learn more about advanced features like schema stitching head over to our [documentation](http://hotchocolate.io).

### Using Hot Chocolate

The easiest way to get a feel for the API is to walk through our README example. If you need additional information, you can also have a look at our [documentation](https://chillicream.com/docs/hotchocolate/).

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

The GraphQL schema describes the capabilities of a GraphQL API. _Hot Chocolate_ allows you to describe schemas in three variants.

- SDL-First: Describe your schema with the GraphQL schema definition language and bind .NET types to it.
- Code-First: Define GraphQL types with C#. This gives you compile safety.
- Pure Code-First: Just describe the schema with clean .NET types without any GraphQL types and the Hot Chocolate will infer the schema types for you.

The following example shows the pure code-first approach.

> Make sure to add the following usings to your code in order to get access to the extension methods used in the examples:
> using HotChocolate;
> using HotChocolate.Execution;

```csharp
public class Program
{
    public static void Main(string[] args)
    {
        var schema = SchemaBuilder.New()
          .AddQueryType<Query>()
          .Create();
    }
}

public class Query
{
    public string Hello() => "world";
}
```

The code above defines a simple schema with one type `Query` and one field `hello` that returns a string.

If you would write that schema down in the GraphQL SDL it would look as follows:

```graphql
type Query {
  hello: String
}
```

Moreover, we bound a resolver to the field that returns a fixed value _world_. A resolver is basically a method that resolves the data for the specified field.

In order to serve up queries against our schema lets make it executable:

```csharp
var executor = schema.MakeExecutable();
```

MakeExecutable will create a `IQueryExecutor` instance that can execute queries against our schema.

Now that the schema is setup and executable we can serve up a query against it.

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
Console.WriteLine(executor.Execute("{ hello }").ToJson());
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
Console.WriteLine(executor.Execute("{ foo }").ToJson());
```

In order to set up a GraphQL HTTP endpoint, Hot Chocolate comes with an ASP .Net core middleware.

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
public void ConfigureServices(IServiceCollection services)
{
    services
        .AddRouting()
        .AddGraphQLServer()
        .AddQueryType<Query>();
}
```

The above example adds the GraphQL schema and the execution engine to the dependency injection.

```csharp
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    if (env.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }

    app.UseRouting();

    app.UseEndpoints(endpoints =>
    {
        endpoints.MapGraphQL();
    });
}
```

This will set up all the necessary endpoints to query the GraphQL schema via HTTP GET or HTTP POST.

By default, the middleware will be configured to listen on `/graphql` for GraphQL requests. If you want to use a different endpoint route you can pass the desired route into the UseGraphQL instruction.

```csharp
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    if (env.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }

    app.UseRouting();

    app.UseEndpoints(endpoints =>
    {
        endpoints.MapGraphQL("Foo/Bar");
    });
}
```

After setting up the endpoint and starting the web host, you can explore your schema using our GraphQL IDE [Banana Cake Pop](https://chillicream.com/docs/bananacakepop/). Just open your configured GraphQL endpoint in your browser and start querying!

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
dotnet new graphql
```

## Documentation

For more examples and detailed documentation, click [here](https://chillicream.com/docs/hotchocolate/).

## Components

Component | Description
---------|----------
 Core | Core consists of the execution engine, the query validation and the type system.
 Language | Language consists of the UTF-8 high-performance GraphQL parser, the abstract syntax tree and the syntax visitor API.
 Filters | Filters contains the basic filter rewriter, the filter middleware for `IQueryable` and other database related logic.
 Persisted Queries | Persisted queries contains the persisted queries storage implementations for the file system, redis cache and others.
 Stitching | Stitching represents the stitching layer version 1.
 Utilities | Utilities contains helpers like the 2-phase introspection that are used by multiple components.
 AspNetCore | AspNetCore contains the server implementation for AspNetCore servers.
 AzureFunctions | AzureFunctions contains the server implementation for AzureFunctions.
