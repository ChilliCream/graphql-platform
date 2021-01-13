---
title: "Automatic persisted queries"
---

In this guide we will walk you through how automatic persisted queries work and how you can set them up with Hot Chocolate.

# Step 1: Create a GraphQL server project

Open your preferred terminal and select a directory where you want to add the code of this tutorial.

1. Install the Hot Chocolate GraphQL server template.

```bash
dotnet new -i HotChocolate.Templates.Server
```

1. Create a new Hot Chocolate GraphQL server project.

```bash
dotnet new graphql
```

1. Add the in-memory query storage to your project.

```
dotnet add package HotChocolate.PersistedQueries.InMemory
```

# Step 2: Configure Automatic persisted queries

Next, we want to configure our GraphQL server to be able to handle automatic persisted query request. For this we need to register the in-memory query storage configure the request pipeline.

1. Configure GraphQL server to use the automatic persisted query pipeline.

```
public void ConfigureServices(IServiceCollection services)
{
    services
        .AddRouting()
        .AddGraphQLServer()
        .AddQueryType<Query>()
        .UseAutomaticPersistedQueryPipeline();
}
```

1. Next, register the in-memory query storage.

```
public void ConfigureServices(IServiceCollection services)
{
    services
        .AddRouting()
        .AddGraphQLServer()
        .AddQueryType<Query>()
        .UseActivePersistedQueryPipeline()
        .AddInMemoryQueryStorage();
}
```

1. Last but not least we need to add the Microsoft Memory Cache which the in-memory query storage will use as the in-memory key value store.

```
public void ConfigureServices(IServiceCollection services)
{
    services
        // Global Services
        .AddRouting()
        .AddMemoryCache()

        // GraphQL server configuration
        .AddGraphQLServer()
        .AddQueryType<Query>()
        .UseActivePersistedQueryPipeline()
        .AddInMemoryQueryStorage();
}
```
