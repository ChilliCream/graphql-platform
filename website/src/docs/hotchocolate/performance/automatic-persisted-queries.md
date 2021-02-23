---
title: "Automatic persisted queries"
---

This guide will walk you through how automatic persisted queries work and how you can set them up with the Hot Chocolate GraphQL server.

# How it works

The automatic persisted queries protocol was originally specified by Apollo and represent an evolution of the persisted query feature that many GraphQL servers implement. Instead of storing persisted queries ahead of time, the client can store queries dynamically. This preserves the original proposal's performance benefits but removes the friction of setting up build processes that post-process the client applications source code.

When the client makes a request to the server, it will optimistically send a short cryptographic hash instead of the full query text.

## Optimized Path

Hot Chocolate server will inspect the incoming request for a query id or a full GraphQL query. If the request has only a query id the execution engine will first try to resolve the full query from the query storage. If the query storage contains a query that matches the provided query id, the request will be upgraded to a fully valid GraphQL request and will be executed.

## New Query Path

If the query storage does not contain a query that matches the sent query id, the Hot Chocolate server will return an error result that indicates that the query was not found (this will only happen the first time a client asks for a certain query). The client application will then send in a second request with the specified query id and the complete GraphQL query. This will trigger Hot Chocolate server to store this new query in its query storage and, at the same time, execute the query and returning the result.

# Setup

In the following tutorial, we will walk you through creating a Hot Chocolate GraphQL server and configuring it to support automatic persisted queries.

## Step 1: Create a GraphQL server project

Open your preferred terminal and select a directory where you want to add the code of this tutorial.

1. Install the Hot Chocolate GraphQL server template.

```bash
dotnet new -i HotChocolate.Templates.Server
```

2. Create a new Hot Chocolate GraphQL server project.

```bash
dotnet new graphql
```

3. Add the in-memory query storage to your project.

```bash
dotnet add package HotChocolate.PersistedQueries.InMemory
```

## Step 2: Configure automatic persisted queries

Next, we want to configure our GraphQL server to be able to handle automatic persisted query requests. For this, we need to register the in-memory query storage and configure the automatic persisted query request pipeline.

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

2. Next, register the in-memory query storage.

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

3. Last but not least, we need to add the Microsoft Memory Cache, which the in-memory query storage will use as the in-memory key-value store.

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

## Step 3: Verify server setup

Now that our server is set up with automatic persisted queries, let us verify that it works as expected. We can do that by just using our console and a tool called `curl`. For our example, we will use a dummy query `{__typename}` with an MD5 hash serialized to base64 as a query id `71yeex4k3iYWQgg9TilDIg==`. We will test the full automatic persisted query flow and walk you through the responses.

1. Start the GraphQL server.

```bash
dotnet run
```

2. First, we will ask our GraphQL server to execute our query with the optimized request containing only the query hash. At this point, the server will not know this query and hast to return an error indicating this.

**Request**

```bash
curl -g 'http://localhost:5000/graphql/?extensions={"persistedQuery":{"version":1,"md5Hash":"71yeex4k3iYWQgg9TilDIg=="}}'
```

**Response**

The response indicates, as expected, that this query is unknown so far.

```json
{
  "errors": [
    {
      "message": "PersistedQueryNotFound",
      "extensions": { "code": "HC0020" }
    }
  ]
}
```

3. Next, we want to store our dummy query on the server. We will send in the hash as before but now also provide the query parameter with the full GraphQL query string.

**Request**

```bash
curl -g 'http://localhost:5000/graphql/?query={__typename}&extensions={"persistedQuery":{"version":1,"md5Hash":"71yeex4k3iYWQgg9TilDIg=="}}'
```

**Response**

Our GraphQL server will respond with the query result and indicate that the query was stored on the server `"persisted": true`.

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

4. Last but not least, we will verify that we can now use our optimized request by executing our initial request containing only the query hash.

**Request**

```bash
curl -g 'http://localhost:5000/graphql/?extensions={"persistedQuery":{"version":1,"md5Hash":"71yeex4k3iYWQgg9TilDIg=="}}'
```

**Response**

This time the server knows the query and will respond with the simple result of this query.

```json
{ "data": { "__typename": "Query" } }
```

> In this example, we used GraphQL HTTP GET requests, which are also useful in caching scenarios with CDNs. But the automatic persisted query flow can also be used with GraphQL HTTP POST requests.

## Step 4: Configure the hashing algorithm

Hot Chocolate server is configured to use by default the MD5 hashing algorithm, which is serialized to a base64 string. Hot Chocolate server comes out of the box with support for MD5, SHA1, and SHA256 and can serialize the hash to base64 or hex. In this step, we will walk you through changing the hashing algorithm to SHA256 with a hex serialization.

1. Add the SHA256 document hash provider to your Hot Chocolate GraphQL server's global services.

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services
        // Global Services
        .AddRouting()
        .AddMemoryCache()
        .AddSha256DocumentHashProvider(HashFormat.Hex)

        // GraphQL server configuration
        .AddGraphQLServer()
        .AddQueryType<Query>()
        .UseAutomaticPersistedQueryPipeline()
        .AddInMemoryQueryStorage();
}
```

2. Start the GraphQL server.

```bash
dotnet run
```

3. Next, let us verify that our server now operates with the new hash provider and the new hash serialization format. For this we will store again a query on the server, but this time our hash string will look like the following: `7f56e67dd21ab3f30d1ff8b7bed08893f0a0db86449836189b361dd1e56ddb4b`.

**Request**

```bash
curl -g 'http://localhost:5000/graphql/?query={__typename}&extensions={"persistedQuery":{"version":1,"sha256Hash":"7f56e67dd21ab3f30d1ff8b7bed08893f0a0db86449836189b361dd1e56ddb4b"}}'
```

**Response**

```json
{
  "data": { "__typename": "Query" },
  "extensions": {
    "persistedQuery": {
      "sha256Hash": "7f56e67dd21ab3f30d1ff8b7bed08893f0a0db86449836189b361dd1e56ddb4b",
      "persisted": true
    }
  }
}
```

## Step 4: Use Redis as a query storage

If you run multiple Hot Chocolate server instances and want to preserve stored queries after a server restart, you can opt to use a file system based query storage or opt to use a Redis cache. Hot Chocolate server supports both.

1. Setup a Redis docker container.

```bash
docker run --name redis-stitching -p 7000:6379 -d redis
```

2. Add the Redis persisted query storage package to your server.

```bash
dotnet add package HotChocolate.PersistedQueries.Redis
```

3. Next, we need to configure the server to use Redis as query storage.

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services
        // Global Services
        .AddRouting()
        .AddSha256DocumentHashProvider(HashFormat.Hex)

        // GraphQL server configuration
        .AddGraphQLServer()
        .AddQueryType<Query>()
        .UseAutomaticPersistedQueryPipeline()
        .AddRedisQueryStorage(services => ConnectionMultiplexer.Connect("localhost:7000").GetDatabase());
}
```

4. Start the GraphQL server.

```bash
dotnet run
```

5. Now, let us verify again if our server works correctly by storing our query first.

**Request**

```bash
curl -g 'http://localhost:5000/graphql/?query={__typename}&extensions={"persistedQuery":{"version":1,"sha256Hash":"7f56e67dd21ab3f30d1ff8b7bed08893f0a0db86449836189b361dd1e56ddb4b"}}'
```

**Response**

```json
{
  "data": { "__typename": "Query" },
  "extensions": {
    "persistedQuery": {
      "sha256Hash": "7f56e67dd21ab3f30d1ff8b7bed08893f0a0db86449836189b361dd1e56ddb4b",
      "persisted": true
    }
  }
}
```

6. Stop your GraphQL server.

7. Start your GraphQL server again.

```bash
dotnet run
```

8. Let us execute the optimized query to see if our query was correctly stored on our Redis cache.

**Request**

```bash
curl -g 'http://localhost:5000/graphql/?extensions={"persistedQuery":{"version":1,"sha256Hash":"7f56e67dd21ab3f30d1ff8b7bed08893f0a0db86449836189b361dd1e56ddb4b"}}'
```

**Response**

```json
{ "data": { "__typename": "Query" } }
```
