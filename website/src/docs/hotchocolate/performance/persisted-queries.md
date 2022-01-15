---
title: "Persisted queries"
---

Persisted queries allow us to pre-register all required queries of our clients. This can be done by extracting the queries of our client applications at build time and placing them in the server's query storage.

Extracting queries is supported by client libraries like [Relay](https://relay.dev/docs/guides/persisted-queries/) and in the case of [Strawberry Shake](/docs/strawberryshake) we do not have to do any additional work.

> Note: While this feature is called persisted _queries_ it works for all other GraphQL operations as well.

# How it works

- All queries our client(s) will execute are extracted during their build process. Individual queries are hashed to generate a unique identifier for each query.
- Before our server is deployed, the extracted queries are placed in the server's query storage.
- After the server has been deployed, clients can execute persisted queries, by specifying the query id (hash) in their requests.
- If Hot Chocolate can find a query that matches the specified hash in the query storage it will execute it and return the result to the client.

> Note: There are also [automatic persisted queries](/docs/hotchocolate/performance/automatic-persisted-queries), which allow clients to persist queries at runtime. They might be a better fit, if our API is used by many clients with different requirements.

# Benefits

<!-- There are two main benefits to using persisted queries: -->

**Performance**

- Only a hash and optionally variables need to be sent to the server, reducing network traffic.
- Queries no longer need to be embeded into the client code, reducing the bundle size in the case of websites.
- Hot Chocolate can optimize the execution of persisted queries, as they will always be the same.

<!-- **Security**

The server can be tweaked to [only accept persisted queries](#blocking-regular-queries) and refuse queries created by a client at runtime. This is useful mainly for public APIs. -->

# Usage

First we have to instruct our server to handle persisted queries. We can do so by calling `UsePersistedQueryPipeline()` on the `IRequestExecutorBuilder`.

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services
        .AddGraphQLServer()
        .AddQueryType<Query>()
        .UsePersistedQueryPipeline();
}
```

## Storage mechanisms

Hot Chocolate supports two query storages for regular persisted queries.

### Filesystem

To load persisted queries from the filesystem, we have to add the following package.

```bash
dotnet add package HotChocolate.PersistedQueries.FileSystem
```

After this we need to specify where the persisted queries are located. The argument of `AddReadOnlyFileSystemQueryStorage()` specifies the directory in which the persisted queries are stored.

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services
        .AddGraphQLServer()
        .AddQueryType<Query>()
        .UsePersistedQueryPipeline()
        .AddReadOnlyFileSystemQueryStorage("./persisted_queries");
}
```

When presented with a query hash, Hot Chocolate will now check the specified folder for a file in the following format: `{Hash}.graphql`.

Example: `0c95d31ca29272475bf837f944f4e513.graphql`

This file is expected to contain the query the hash was generated from.

> ⚠️ Note: Do not forget to ensure that the server has access to the directory.

### Redis

To load persisted queries from Redis, we have to add the following package.

```bash
dotnet add package HotChocolate.PersistedQueries.Redis
```

After this we need to specify where the persisted queries are located. Using `AddReadOnlyRedisQueryStorage()` we can point to a specific Redis database in which the persisted queries are stored.

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services
        .AddGraphQLServer()
        .AddQueryType<Query>()
        .UsePersistedQueryPipeline()
        .AddReadOnlyRedisQueryStorage(services =>
            ConnectionMultiplexer.Connect("host:port").GetDatabase());
}
```

Keys in the specified Redis database are expected to be a query id (hash) and contain the actual query as the value.

## Hashing algorithms

Per default Hot Chocolate uses the MD5 hashing algorithm, but we can override this default by specifying a `DocumentHashProvider`.

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services
        // choose one of the following providers
        .AddMD5DocumentHashProvider()
        .AddSha256DocumentHashProvider()
        .AddSha1DocumentHashProvider()

        // GraphQL server configuration
        .AddGraphQLServer()
        .AddQueryType<Query>()
        .UsePersistedQueryPipeline()
        .AddReadOnlyFileSystemQueryStorage("./persisted_queries");
}
```

We can also configure how these hashes are encoded, by specifying a `HashFormat` as argument:

```csharp
AddSha256DocumentHashProvider(HashFormat.Hex)
AddSha256DocumentHashProvider(HashFormat.Base64)
```

> Note: [Relay](https://relay.dev) uses the MD5 hashing algorithm - no additional Hot Chocolate configuration is required.

# Client expectations

A client is expected to send an `id` field containing the query hash instead of a `query` field.

**HTTP POST**

```json
{
  "id": "0c95d31ca29272475bf837f944f4e513",
  "variables": {
    // ...
  }
}
```

> Note: [Relay's persisted queries documentation](https://relay.dev/docs/guides/persisted-queries/#network-layer-changes) uses `doc_id` instead of `id`, be sure to change it to `id`.
