---
title: "Fetching from REST"
---

import { ExampleTabs } from "../../../components/mdx/example-tabs"

In this section, we will cover how you can easily integrate a REST API into your GraphQL API.

GraphQL has a strongly-typed type system and therefore also has to know the dotnet runtime types of the data it returns in advance. 
The easiest way to integrate a REST API is, to define an OpenAPI specification for it. 
OpenAPI describes what data a REST endpoint returns. 
You can automatically generate a dotnet client for this API and integrate it into your schema.

# OpenAPI in .NET
If you do not have an OpenAPI specification for your REST endpoint yet, you can easily add it to your API.
There are two major OpenAPI implementations in dotnet: [NSwag](http://nswag.org) and [Swashbuckle](https://github.com/domaindrivendev/Swashbuckle.AspNetCore).
Head over to the [official ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/tutorials/web-api-help-pages-using-swagger?view=aspnetcore-5.0) documentation to see how it is done.
 
In this example, we will use [the official example of Swashbuckle](https://github.com/dotnet/AspNetCore.Docs/tree/main/aspnetcore/tutorials/web-api-help-pages-using-swagger/samples/3.0/TodoApi.Swashbuckle).
When you start this project, you can navigate to the [Swagger UI](http://localhost:5000/swagger). 

This REST API covers a simple Todo app. 
We will expose `todos` and `todoById` in our GraphQL API.
# Generating a client
Every REST endpoint that supports OpenAPI, can easily be wrapped with a fully typed client. 
Again, you have several options on how you generate your client.
You can generate your client from the OpenAPI specification of your endpoint, during build or even with external tools with GUI.
Have a look here and see what fits your use case the best: 
- [NSwag Code Generation](https://docs.microsoft.com/en-us/aspnet/core/tutorials/getting-started-with-nswag?view=aspnetcore-5.0&tabs=visual-studio#code-generation)

In this example, we will use the NSwag dotnet tool. 
First, we need to create a tool manifest.
Switch to your GraphQL project and execute 

```bash
dotnet new tool-manifest
```

Then we install the NSwag tool

```bash
dotnet tool install NSwag.ConsoleCore --version 13.10.9
```

You then have to get the `swagger.json` from your REST endpoint

```bash
curl http://localhost:5000/swagger/v1/swagger.json > swagger.json
```

Now you can generate the client from the `swagger.json`.

```bash
dotnet nswag swagger2csclient /input:swagger.json /classname:TodoService /namespace:TodoReader /output:TodoService.cs
```

The code generator generated a new file called `TodoService.cs`. 
In this file, you will find the client for your REST API.

The generated needs `Newtonsoft.Json`. 
Make sure to also add this package by executing:

```bash
dotnet add package Newtonsoft.Json
```

# Exposing the API
You will have to register the client in the dependency injection of your GraphQL service. 
To expose the API you can inject the generated client into your resolvers.

<ExampleTabs>
<ExampleTabs.Annotation>

```csharp
// Query.cs
public class Query
{
    public Task<ICollection<TodoItem>> GetTodosAsync(
        [Service]TodoService service,
        CancellationToken cancellationToken)
    {
        return service.GetAllAsync(cancellationToken);
    }
    
    public Task<TodoItem> GetTodoByIdAsync(
        [Service]TodoService service,
        long id,
        CancellationToken cancellationToken)
    {
        return service.GetByIdAsync(id, cancellationToken);
    }
}

// Startup.cs
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddHttpClient<TodoService>();
        services
            .AddRouting()
            .AddGraphQLServer()
            .AddQueryType<Query>();
    }

    // Omitted code for brevity
}
```

</ExampleTabs.Annotation>
<ExampleTabs.Code>

```csharp
// Query.cs
public class Query
{
    public Task<ICollection<TodoItem>> GetTodosAsync(
        [Service]TodoService service,
        CancellationToken cancellationToken)
    {
        return service.GetAllAsync(cancellationToken);
    }
    
    public Task<TodoItem> GetTodoByIdAsync(
        [Service]TodoService service,
        long id,
        CancellationToken cancellationToken)
    {
        return service.GetByIdAsync(id, cancellationToken);
    }
}

// QueryType.cs
public class QueryType : ObjectType<Query>
{
    protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
    {
        descriptor
            .Field(f => f.GetTodoByIdAsync(default!, default!, default!))
            .Type<TodoType>();
        
        descriptor
            .Field(f => f.GetTodosAsync(default!, default!))
            .Type<ListType<TodoType>>();
    }
}

// TodoType.cs
public class TodoType : ObjectType<Todo>
{
    protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
    {
        descriptor
            .Field(f => f.Id)
            .Type<LongType>();

        descriptor
            .Field(f => f.Name)
            .Type<StringType>();

        descriptor
            .Field(f => f.IsComplete)
            .Type<BooleanType>();
    }
}


// Startup.cs
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddRouting()
            .AddGraphQLServer()
            .AddQueryType<QueryType>();
    }

    // Omitted code for brevity
}
```

</ExampleTabs.Code>
<ExampleTabs.Schema>

```csharp
// Query.cs
public class Query
{
    public Task<ICollection<TodoItem>> GetTodosAsync(
        [Service]TodoService service,
        CancellationToken cancellationToken)
    {
        return service.GetAllAsync(cancellationToken);
    }
    
    public Task<TodoItem> GetTodoByIdAsync(
        [Service]TodoService service,
        long id,
        CancellationToken cancellationToken)
    {
        return service.GetByIdAsync(id, cancellationToken);
    }
}

// Startup.cs
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddRouting()
            .AddGraphQLServer()
            .AddDocumentFromString(@"
                type Query {
                  todos: [TodoItem!]!
                  todoById(id: Uuid): TodoItem
                }

                type TodoItem {
                  id: Long
                  name: String
                  isCompleted: Boolean
                }
            ")
            .BindComplexType<Query>();
    }

    // Omitted code for brevity
}
```

</ExampleTabs.Schema>
</ExampleTabs>

You can now head over to your Banana Cake Pop on your GraphQL Server (/graphql) and query todos:
```graphql
{
  todoById(id:1){
    id
    isComplete
    name
  }
  todos {
    id
    isComplete
    name
  }
}
```
