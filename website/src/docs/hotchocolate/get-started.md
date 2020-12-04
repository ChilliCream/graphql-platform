---
title: "Get started with Hot Chocolate"
---

In this tutorial, we will walk you through the basics of creating a GraphQL server with Hot Chocolate. If you want to dig deeper into Hot Chocolate, we have our GraphQL workshop, which touches on topics like schema design, DataLoader, and many more things.

In this tutorial, we will teach you:

- To set up a GraphQL Server.
- To define a GraphQL schema.
- To query your GraphQL server.

# Step 1: Create a GraphQL server project

Open your preferred terminal and select a directory where you want to add the code of this tutorial.

1. Create an empty ASP.NET Core server project.

```bash
dotnet new web -n Demo
dotnet new sln -n Demo
dotnet sln add ./Demo
```

2. Add the `HotChocolate.AspNetCore` package.

```bash
dotnet add ./Demo package HotChocolate.AspNetCore --version 11.0.0-rc.0
```

# Step 2: Create a GraphQL schema

Next, we want to create a GraphQL schema. The GraphQL schema defines how we expose data to our consumers. To define the schema, open your favorite C# editor and let us get started.

1. Add a new class `Author`.

```csharp
namespace Demo
{
    public class Author
    {
        public string Name { get; set; }
    }
}
```

2. Add a new class `Book`.

```csharp
namespace Demo
{
    public class Book
    {
        public string Title { get; set; }

        public Author Author { get; set; }
    }
}
```

We have a nice and simple model with these two classes that we can use to build our GraphQL schema. We now need to define a query root type. The query root type exposes all the possible queries that a user can drill into. A query root type can be defined as our models just with C#.

3. Add a new class `Query`.

```csharp
namespace Demo
{
    public class Query
    {
        public Book GetBook() =>
            new Book
            {
                Title = "C# in depth.",
                Author = new Author
                {
                    Name = "Jon Skeet"
                }
            };
    }
}
```

Now, we have all the parts to create a valid GraphQL schema. Let us now head over to the `Startup.cs` and configure our GraphQL schema.

4. Add the GraphQL schema to the service configuration by adding the following code to the `ConfigureServices` method in the `Startup.cs`.

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services
        .AddGraphQLServer()
        .AddQueryType<Query>();
}
```

5. Lastly, we need something to execute our code; for this, we will head over to the `Configure` method of our `Startup.cs` and add `MapGraphQL` to `UseEndpoints`.

```csharp
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    app
        .UseRouting()
        .UseEndpoints(endpoints =>
        {
            endpoints.MapGraphQL();
        });
}
```

# Step 3: Execute a GraphQL query

Now that your server is finished let us try it out by executing a simple GraphQL query.

1. Start your GraphQL server.

```bash
dotnet run --project ./Demo
```

2. Open Chrome, Edge or Firefox and head over to `http://localhost:5000/graphql` to open the built-in GraphQL IDE Banana Cake Pop.

![GraphQL IDE](../../images/get-started-bcp.png)

3. Next, click on the `Book` icon on the left-hand navigation bar to explore the server GraphQL schema. In the schema explorer, we can see that we have one query root field exposed. By clicking on the field, we can drill into the schema structure.

![GraphQL IDE Schema Explorer](../../images/get-started-bcp-schema-explorer.png)

4. Head back to the query tab and execute your first GraphQL query by clicking the play button.

```graphql
{
  book {
    title
    author {
      name
    }
  }
}
```

![GraphQL IDE Execute Query](../../images/get-started-bcp-query.png)

# Summary

In this guide we have learned how to set up a simple GraphQL server project. We have looked at how we can define a GraphQL schema and last but not least we explored our GraphQL schema with our GraphQL IDE Banana Cake Pop and executed a simple query with it.

If you want to dive deeper into Hot Chocolate you can start with our GraphQL tutorial [here](https://github.com/ChilliCream/graphql-workshop) which will dive deeper into several topics around GraphQL and Hot Chocolate.
