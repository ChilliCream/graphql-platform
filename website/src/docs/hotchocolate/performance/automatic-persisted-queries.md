---
title: "Automatic persisted queries"
---

In this guide we will walk you through how automatic persisted queries work and how you can set them up with the Hot Chocolate GraphQL server.

# How it works

The automatic persisted queries protocol was originally specified by Apollo and represents an evolution of the persisted query feature that many GraphQL server implement. Instead of storing persisted queries ahead of time the client is able to store queries dynamically. This preserves the performance benefits of the original proposal but removes friction of setting up build processes that post process the client applications source code.

When the client makes a request to the server it will optimistically send a short cryptographic hash instead of the full query text.

## Optimized Path

Hot Chocolate server will inspect the incoming request for a query id or a full query. If the request has only a query id the execution engine will first try to resolve the full query from the query storage. If the query storage contains a query that matches the provided query id the request will be upgraded to a full valid GraphQL request and executed.

## New Query Path

If the query storage does not contain a query that matches ....

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

```bash
dotnet add package HotChocolate.PersistedQueries.InMemory
```

# Step 2: Configure automatic persisted queries

Next, we want to configure our GraphQL server to be able to handle automatic persisted query request. For this we need to register the in-memory query storage configure the request pipeline.

1. Configure GraphQL server to use the automatic persisted query pipeline.

```csharp
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

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services
        .AddRouting()
        .AddGraphQLServer()
        .AddQueryType<Query>()
        .UseAutomaticPersistedQueryPipeline()
        .AddInMemoryQueryStorage();
}
```

1. Last but not least we need to add the Microsoft Memory Cache which the in-memory query storage will use as the in-memory key value store.

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services
        // Global Services
        .AddRouting()
        .AddMemoryCache()

        // GraphQL server configuration
        .AddGraphQLServer()
        .AddQueryType<Query>()
        .UseAutomaticPersistedQueryPipeline()
        .AddInMemoryQueryStorage();
}
```

# Step 3: Verify our setup

```bash
curl -g 'http://localhost:5000/graphql/?extensions={"persistedQuery":{"version":1,"md5Hash":"71yeex4k3iYWQgg9TilDIg=="}}'
```

```json
{
  "errors": [
    { "message": "PersistedQueryNotFound", "extensions": { "code": "HC0020" } }
  ]
}
```

```bash
curl -g 'http://localhost:5000/graphql/?query={__typename}&extensions={"persistedQuery":{"version":1,"md5Hash":"71yeex4k3iYWQgg9TilDIg=="}}'
```

```json
{
  "data": { "__typename": "Query" },
  "extensions": {
    "persistedQuery": {
      "md5Hash": "71yeex4k3iYWQgg9TilDIg==",
      "persisted": true
    }
  }
}
```

```bash
curl -g 'http://localhost:5000/graphql/?extensions={"persistedQuery":{"version":1,"md5Hash":"71yeex4k3iYWQgg9TilDIg=="}}'
```

```json
{ "data": { "__typename": "Query" } }
```

# Step 4: Configure hashing algorithm

# Step 4: Use Redis as a query storage
