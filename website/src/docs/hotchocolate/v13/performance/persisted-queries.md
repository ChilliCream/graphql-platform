---
title: "Persisted queries"
description: "In this section, you will learn how to use persisted queries in GraphQL with Hot Chocolate."
---

Persisted queries allow us to pre-register all required queries of our clients. This can be done by extracting the queries of our client applications at build time and placing them in the server's query storage.

Extracting queries is supported by client libraries like [Relay](https://relay.dev/docs/guides/persisted-queries/) and in the case of [Strawberry Shake](/products/strawberryshake) we do not have to do any additional work.

> Note: While this feature is called persisted _queries_ it works for all other GraphQL operations as well.

<Video videoId="ZZ5PF3_P_r4" />

# How it works

- All queries our client(s) will execute are extracted during their build process. Individual queries are hashed to generate a unique identifier for each query.
- Before our server is deployed, the extracted queries are placed in the server's query storage.
- After the server has been deployed, clients can execute persisted queries, by specifying the query id (hash) in their requests.
- If Hot Chocolate can find a query that matches the specified hash in the query storage it will execute it and return the result to the client.

> Note: There are also [automatic persisted queries](/docs/hotchocolate/v13/performance/automatic-persisted-queries), which allow clients to persist queries at runtime. They might be a better fit, if our API is used by many clients with different requirements.

# Benefits

There are two main benefits to using persisted queries:

**Performance**

- Only a hash and optionally variables need to be sent to the server, reducing network traffic.
- Queries no longer need to be embedded into the client code, reducing the bundle size in the case of websites.
- Hot Chocolate can optimize the execution of persisted queries, as they will always be the same.

**Security**

The server can be tweaked to [only execute persisted queries](#blocking-regular-queries) and refuse any other queries provided by a client. This gets rid of a whole suite of potential attack vectors, since malicious actors can no longer craft and execute harmful queries against your GraphQL server.

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

# Production Ready Persisted Queries

In transitioning your persisted query setup to production, simply setting up a persisted query file
isn't sufficient for a robust production environment. A key aspect of managing persisted queries at
scale involves version management and ensuring compatibility with your GraphQL schema. The client
registry is your go-to resource for this purpose.

The client registry simplifies the management of your GraphQL clients and their queries.
It allows for the storage and retrieval of persisted queries through their hashes but also ensures
that these queries are validated against the current schema on publish, preventing runtime errors
due to schema-query mismatches. Additionally, it supports versioning of your clients, allowing
seamless updates and maintenance without disrupting existing operations

Check out the [client registry documentation](/docs/bananacakepop/v2/apis/client-registry) for
more information.

# Other Storage mechanisms

Hot Chocolate supports two query storages for regular persisted queries.

## Filesystem

To load persisted queries from the filesystem, we have to add the following package.

<PackageInstallation packageName="HotChocolate.PersistedQueries.FileSystem" />

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

> Warning: Do not forget to ensure that the server has access to the directory.

### Redis

To load persisted queries from Redis, we have to add the following package.

<PackageInstallation packageName="HotChocolate.PersistedQueries.Redis" />

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

## Blocking regular queries

If you want to disallow any dynamic queries, you can enable `OnlyAllowPersistedQueries`:

```csharp
builder.Services
    .AddGraphQLServer()
    // Omitted for brevity
    .ModifyRequestOptions(o => o.OnlyAllowPersistedQueries = true);
```

This will block any dynamic queries that do not contain the `id` of a persisted query.

You might still want to allow the execution of dynamic queries in certain circumstances. You can override the `OnlyAllowPersistedQueries` rule on a per-request basis, using the `AllowNonPersistedQuery` method on the `IQueryRequestBuilder`. Simply implement a custom [IHttpRequestInterceptor](/docs/hotchocolate/v13/server/interceptors#ihttprequestinterceptor) and call `AllowNonPersistedQuery` if a certain condition is met:

```csharp
builder.Services
    .AddGraphQLServer()
    // Omitted for brevity
    .AddHttpRequestInterceptor<CustomHttpRequestInterceptor>()
    .ModifyRequestOptions(o => o.OnlyAllowPersistedQueries = true);

public class CustomHttpRequestInterceptor : DefaultHttpRequestInterceptor
{
    public override ValueTask OnCreateAsync(HttpContext context,
        IRequestExecutor requestExecutor, IQueryRequestBuilder requestBuilder,
        CancellationToken cancellationToken)
    {
        if (context.Request.Headers.ContainsKey("X-Developer"))
        {
            requestBuilder.AllowNonPersistedQuery();
        }

        return base.OnCreateAsync(context, requestExecutor, requestBuilder,
            cancellationToken);
    }
}
```

In the above example we would allow requests containing the `X-Developer` header to execute dynamic queries. This isn't particularly secure, but in your production application you could replace this check with an authorization policy, an API key or whatever fits your requirement.

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
