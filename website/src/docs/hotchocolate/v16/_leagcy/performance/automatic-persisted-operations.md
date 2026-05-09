---
title: "Automatic persisted operations"
---

This guide walks you through how automatic persisted operations work and how to set them up with a Hot Chocolate GraphQL server.

# How It Works

The Automatic Persisted Queries (APQ) protocol was originally specified by Apollo and represents an evolution of the persisted operations feature that many GraphQL servers implement. Instead of storing operation documents ahead of time, the client stores operation documents dynamically. This preserves the performance benefits of persisted operations but removes the friction of setting up build processes that post-process client application source code.

When the client makes a request to the server, it optimistically sends a short cryptographic hash instead of the full operation document.

## Optimized Path

Hot Chocolate inspects the incoming request for an operation ID or a full GraphQL operation document. If the request has only an operation ID, the execution engine tries to resolve the full operation document from the operation document storage. If the storage contains an operation document that matches the provided operation ID, the request is upgraded to a fully valid GraphQL request and executed.

## New Operation Path

If the operation document storage does not contain an operation document that matches the sent operation ID, Hot Chocolate returns an error result indicating the operation document was not found (this only happens the first time a client asks for a certain operation document). The client application then sends a second request with the specified operation document ID and the complete GraphQL operation document. This triggers Hot Chocolate to store the new operation document in its operation document storage and execute the operation, returning the result.

# Setup

The following tutorial walks you through creating a Hot Chocolate GraphQL server configured to support automatic persisted operations.

## Step 1: Create a GraphQL Server Project

Open your preferred terminal and select a directory for this tutorial.

1. Install the Hot Chocolate templates.

```bash
dotnet new install HotChocolate.Templates
```

2. Create a new Hot Chocolate GraphQL server project.

```bash
dotnet new graphql
```

3. Add the in-memory persisted operations package to your project.

<PackageInstallation packageName="HotChocolate.PersistedOperations.InMemory" />

## Step 2: Configure Automatic Persisted Operations

Configure your GraphQL server to handle automatic persisted operation requests. Register the in-memory operation storage and configure the automatic persisted operation request pipeline.

1. Configure the GraphQL server to use the automatic persisted operation pipeline.

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .UseAutomaticPersistedOperationPipeline();
```

2. Register the in-memory operation document storage.

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .UseAutomaticPersistedOperationPipeline()
    .AddInMemoryOperationDocumentStorage();
```

3. Add the Microsoft Memory Cache, which the in-memory operation document storage uses as the key-value store.

```csharp
builder.Services.AddMemoryCache();

builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .UseAutomaticPersistedOperationPipeline()
    .AddInMemoryOperationDocumentStorage();
```

## Step 3: Verify Server Setup

Now that your server is set up with automatic persisted operations, verify that it works as expected. You can do this using your console and `curl`. For this example, you will use a dummy operation `{__typename}` with an MD5 hash serialized to base64 as an operation ID `71yeex4k3iYWQgg9TilDIg==`. The following steps walk you through the full automatic persisted operation flow.

1. Start the GraphQL server.

```bash
dotnet run
```

2. First, ask the GraphQL server to execute the operation with the optimized request containing only the operation hash. At this point, the server does not know this operation and returns an error.

**Request**

```bash
curl -g 'http://localhost:5000/graphql/?extensions={"persistedQuery":{"version":1,"md5Hash":"71yeex4k3iYWQgg9TilDIg=="}}'
```

**Response**

The response indicates, as expected, that this operation is unknown.

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

3. Next, store the dummy operation document on the server. Send the hash as before but now also provide the `query` parameter with the full GraphQL operation string.

**Request**

```bash
curl -g 'http://localhost:5000/graphql/?query={__typename}&extensions={"persistedQuery":{"version":1,"md5Hash":"71yeex4k3iYWQgg9TilDIg=="}}'
```

**Response**

The GraphQL server responds with the operation result and indicates that the operation was stored on the server (`"persisted": true`).

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

4. Verify that you can now use the optimized request by executing the initial request containing only the operation document hash.

**Request**

```bash
curl -g 'http://localhost:5000/graphql/?extensions={"persistedQuery":{"version":1,"md5Hash":"71yeex4k3iYWQgg9TilDIg=="}}'
```

**Response**

This time the server knows the operation and responds with the result.

```json
{ "data": { "__typename": "Query" } }
```

> In this example, you used GraphQL HTTP GET requests, which are also useful in caching scenarios with CDNs. The automatic persisted operation flow also works with GraphQL HTTP POST requests.

## Step 4: Configure the Hashing Algorithm

Hot Chocolate is configured to use the MD5 hashing algorithm by default, serialized to a base64 string. Hot Chocolate ships with support for MD5, SHA1, and SHA256 and can serialize the hash to base64 or hex. The following steps walk you through changing the hashing algorithm to SHA256 with hex serialization.

1. Add the SHA256 document hash provider to your GraphQL server configuration.

```csharp
builder.Services.AddMemoryCache();

builder
    .AddGraphQL()
    .AddSha256DocumentHashProvider(HashFormat.Hex)
    .AddQueryType<Query>()
    .UseAutomaticPersistedOperationPipeline()
    .AddInMemoryOperationDocumentStorage();
```

2. Start the GraphQL server.

```bash
dotnet run
```

3. Verify that the server now operates with the new hash provider and serialization format. Store an operation document on the server with the new SHA256 hash: `7f56e67dd21ab3f30d1ff8b7bed08893f0a0db86449836189b361dd1e56ddb4b`.

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

## Step 5: Use a Persisted Operation Document Storage

If you run multiple Hot Chocolate server instances and want to preserve stored operation documents after a server restart, use a persisted operation document storage. Hot Chocolate supports a file-system-based operation document storage, Azure Blob Storage, or a Redis cache.

1. Set up a Redis Docker container.

```bash
docker run --name redis-stitching -p 7000:6379 -d redis
```

2. Add the Redis persisted operations package to your server.

<PackageInstallation packageName="HotChocolate.PersistedOperations.Redis" />

3. Configure the server to use Redis as operation document storage.

```csharp
builder.Services.AddMemoryCache();

builder
    .AddGraphQL()
    .AddSha256DocumentHashProvider(HashFormat.Hex)
    .AddQueryType<Query>()
    .UseAutomaticPersistedOperationPipeline()
    .AddRedisOperationDocumentStorage(services =>
        ConnectionMultiplexer.Connect("localhost:7000").GetDatabase());
```

4. Start the GraphQL server.

```bash
dotnet run
```

5. Verify that the server works correctly by storing an operation document first.

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

8. Execute the optimized operation to verify the operation document was correctly stored in your Redis cache.

**Request**

```bash
curl -g 'http://localhost:5000/graphql/?extensions={"persistedQuery":{"version":1,"sha256Hash":"7f56e67dd21ab3f30d1ff8b7bed08893f0a0db86449836189b361dd1e56ddb4b"}}'
```

**Response**

```json
{ "data": { "__typename": "Query" } }
```

# Next Steps

- [Persisted Operations](/docs/hotchocolate/v16/performance/trusted-documents) for pre-registering operations ahead of deployment.
- [HTTP Transport](/docs/hotchocolate/v16/server/http-transport) for details on HTTP GET caching.

<!-- spell-checker:ignore yeex -->
