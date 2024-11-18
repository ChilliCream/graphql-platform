---
title: "Persisted operations"
description: "In this section, you will learn how to use persisted operations in GraphQL with Hot Chocolate."
---

Persisted operations allow us to pre-register all required operations of our clients. This can be done by extracting the operations of our client applications at build time and placing them in the server's operation document storage.

Extracting operations is supported by client libraries like [Relay](https://relay.dev/docs/guides/persisted-queries/) and in the case of [Strawberry Shake](/products/strawberryshake) we do not have to do any additional work.

<Video videoId="ZZ5PF3_P_r4" />

# How it works

- All operations that our client(s) will execute are extracted during their build process. Individual operation documents are hashed to generate a unique identifier for each operation.
- Before our server is deployed, the extracted operation documents are placed in the server's operation document storage.
- After the server has been deployed, clients can execute persisted operations, by specifying the operation ID (hash) in their requests.
- If Hot Chocolate can find an operation that matches the specified hash in the operation document storage it will execute it and return the result to the client.

> Note: There are also [automatic persisted operations](/docs/hotchocolate/v15/performance/automatic-persisted-operations), which allow clients to persist operation documents at runtime. They might be a better fit, if our API is used by many clients with different requirements.

# Benefits

There are two main benefits to using persisted operations:

**Performance**

- Only a hash and optionally variables need to be sent to the server, reducing network traffic.
- Operations no longer need to be embedded into the client code, reducing the bundle size in the case of websites.
- Hot Chocolate can optimize the execution of persisted operations, as they will always be the same.

**Security**

The server can be tweaked to [only execute persisted operations](#blocking-regular-operations) and refuse any other operation provided by a client. This gets rid of a whole suite of potential attack vectors, since malicious actors can no longer craft and execute harmful operations against your GraphQL server.

# Usage

First we have to instruct our server to handle persisted operations. We can do so by calling `UsePersistedOperationPipeline()` on the `IRequestExecutorBuilder`.

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services
        .AddGraphQLServer()
        .AddQueryType<Query>()
        .UsePersistedOperationPipeline();
}
```

# Production Ready Persisted Operations

In transitioning your persisted operation setup to production, simply setting up a persisted operation file isn't sufficient for a robust production environment. A key aspect of managing persisted operations at scale involves version management and ensuring compatibility with your GraphQL schema. The client registry is your go-to resource for this purpose.

The client registry simplifies the management of your GraphQL clients. It allows for the storage and retrieval of persisted operation documents through their hashes but also ensures that these operations are validated against the current schema on publish, preventing runtime errors due to schema-operation mismatches. Additionally, it supports versioning of your clients, allowing seamless updates and maintenance without disrupting existing operations.

Check out the [client registry documentation](/docs/nitro/apis/client-registry) for
more information.

# Other Storage mechanisms

Hot Chocolate supports two operation document storages for regular persisted operations.

## Filesystem

To load persisted operation documents from the filesystem, we have to add the following package.

<PackageInstallation packageName="HotChocolate.PersistedOperations.FileSystem" />

After this we need to specify where the persisted operation documents are located. The argument of `AddFileSystemOperationDocumentStorage()` specifies the directory in which the operation documents are stored.

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services
        .AddGraphQLServer()
        .AddQueryType<Query>()
        .UsePersistedOperationPipeline()
        .AddFileSystemOperationDocumentStorage("./persisted_operations");
}
```

When presented with an operation document hash, Hot Chocolate will now check the specified folder for a file in the following format: `{Hash}.graphql`.

Example: `0c95d31ca29272475bf837f944f4e513.graphql`

This file is expected to contain the operation document that the hash was generated from.

> Warning: Do not forget to ensure that the server has access to the directory.

### Redis

To load persisted operation documents from Redis, we have to add the following package.

<PackageInstallation packageName="HotChocolate.PersistedOperations.Redis" />

After this we need to specify where the persisted operation documents are located. Using `AddRedisOperationDocumentStorage()` we can point to a specific Redis database in which the operation documents are stored.

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services
        .AddGraphQLServer()
        .AddQueryType<Query>()
        .UsePersistedOperationPipeline()
        .AddRedisOperationDocumentStorage(services =>
            ConnectionMultiplexer.Connect("host:port").GetDatabase());
}
```

Keys in the specified Redis database are expected to be operation IDs (hashes) and contain the actual operation document as the value.

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
        .UsePersistedOperationPipeline()
        .AddFileSystemOperationDocumentStorage("./persisted_operations");
}
```

We can also configure how these hashes are encoded, by specifying a `HashFormat` as argument:

```csharp
AddSha256DocumentHashProvider(HashFormat.Hex)
AddSha256DocumentHashProvider(HashFormat.Base64)
```

> Note: [Relay](https://relay.dev) uses the MD5 hashing algorithm - no additional Hot Chocolate configuration is required.

## Blocking regular operations

If you want to disallow any dynamic operations, you can enable `OnlyAllowPersistedDocuments`:

```csharp
builder.Services
    .AddGraphQLServer()
    // Omitted for brevity
    .ModifyRequestOptions(
        options => options
            .PersistedOperations
            .OnlyAllowPersistedDocuments = true);
```

This will block any dynamic operations that do not contain the `id` of a persisted operation.

You might still want to allow the execution of dynamic operations in certain circumstances. You can override the `OnlyAllowPersistedDocuments` rule on a per-request basis, using the `AllowNonPersistedOperation` method on the `OperationRequestBuilder`. Simply implement a custom [IHttpRequestInterceptor](/docs/hotchocolate/v15/server/interceptors#ihttprequestinterceptor) and call `AllowNonPersistedOperation` if a certain condition is met:

```csharp
builder.Services
    .AddGraphQLServer()
    // Omitted for brevity
    .AddHttpRequestInterceptor<CustomHttpRequestInterceptor>()
    .ModifyRequestOptions(
        options => options
            .PersistedOperations
            .OnlyAllowPersistedDocuments = true);

public class CustomHttpRequestInterceptor
    : DefaultHttpRequestInterceptor
{
    public override ValueTask OnCreateAsync(
        HttpContext context,
        IRequestExecutor requestExecutor,
        OperationRequestBuilder requestBuilder,
        CancellationToken cancellationToken)
    {
        if (context.Request.Headers.ContainsKey("X-Developer"))
        {
            requestBuilder.AllowNonPersistedOperation();
        }

        return base.OnCreateAsync(
            context,
            requestExecutor,
            requestBuilder,
            cancellationToken);
    }
}
```

In the above example we would allow requests containing the `X-Developer` header to execute dynamic operations. This isn't particularly secure, but in your production application you could replace this check with an authorization policy, an API key or whatever fits your requirement.

# Client expectations

A client is expected to send an `id` field containing the operation document hash instead of a `query` field.

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
