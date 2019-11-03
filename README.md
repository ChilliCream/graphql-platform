![HotChocolate](https://chillicream.com/img/projects/hotchocolate-banner.svg)

[![GitHub release](https://img.shields.io/github/release/chillicream/HotChocolate.svg)](https://github.com/ChilliCream/hotchocolate/releases) [![NuGet Package](https://img.shields.io/nuget/v/hotchocolate.svg)](https://www.nuget.org/packages/HotChocolate/) [![License](https://img.shields.io/github/license/ChilliCream/hotchocolate.svg)](https://github.com/ChilliCream/hotchocolate/releases) [![Azure DevOps builds](https://img.shields.io/azure-devops/build/chillicream/414ff59c-4852-4687-b04d-6973125e7de2/48.svg)](https://chillicream.visualstudio.com/HotChocolate/_build?definitionId=48) [![Azure DevOps tests](https://img.shields.io/azure-devops/tests/chillicream/HotChocolate/48.svg)](https://chillicream.visualstudio.com/HotChocolate/_build?definitionId=48) [![Coverage Status](https://sonarcloud.io/api/project_badges/measure?project=HotChocolate&metric=coverage)](https://sonarcloud.io/dashboard?id=HotChocolate) [![Quality](https://sonarcloud.io/api/project_badges/measure?project=HotChocolate&metric=alert_status)](https://sonarcloud.io/dashboard?id=HotChocolate)
[![Slack channel](https://img.shields.io/badge/join%20the%20community-on%20slack-blue.svg)](https://join.slack.com/t/hotchocolategraphql/shared_invite/enQtNTA4NjA0ODYwOTQ0LTViMzA2MTM4OWYwYjIxYzViYmM0YmZhYjdiNzBjOTg2ZmU1YmMwNDZiYjUyZWZlMzNiMTk1OWUxNWZhMzQwY2Q) [![Twitter](https://img.shields.io/badge/join%20us-on%20twitter-green.svg)](https://twitter.com/chilli_cream)

---

**Hot Chocolate is a GraphQL server for _.NET Core_ and _.NET Classic_**

_Hot Chocolate_ is a GraphQL server implementation based on the current GraphQL [June 2018 specification](https://graphql.github.io/graphql-spec/June2018/).

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

The GraphQL specification and more is available on the [Facebook GraphQL repository](https://github.com/graphql/graphql-spec).

If you want to get in touch with us you can do so by joining our [slack group](https://join.slack.com/t/hotchocolategraphql/shared_invite/enQtNTA4NjA0ODYwOTQ0LTViMzA2MTM4OWYwYjIxYzViYmM0YmZhYjdiNzBjOTg2ZmU1YmMwNDZiYjUyZWZlMzNiMTk1OWUxNWZhMzQwY2Q).

> This readme only provides a simple quickstart, in order to learn more about advanced features like schema stitching head over to our [documentation](http://hotchocolate.io).

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

If you would write that schema down in the GraphQL syntax it would look as follows:

```graphql
type Query {
  hello: String
}
```

Moreover, we bound a resolver to the field that returns a fixed value _world_. A resolver is basically a function that resolves the data for the specified field.

In order to serve up queries against our schema lets make it executable:

```csharp
var executor = schema.MakeExecutable();
```

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
protected override void ConfigureServices(IServiceCollection services)
{
    services.AddGraphQL(sp => SchemaBuilder.New()
      .AddQueryType<Query>()
      .AddServices(sp)
      .Create());
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

> We also have a ASP.Net Framework middleware available.

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

## Features and Roadmap

We have moved the roadmap into the [ROADMAP.md](ROADMAP.md)

## Documentation

For more examples and detailed documentation, click [here](http://hotchocolate.io).

For documentation about our _DataLoader_ implementation click [here](https://github.com/ChilliCream/hotchocolate/tree/master/src/DataLoader).
