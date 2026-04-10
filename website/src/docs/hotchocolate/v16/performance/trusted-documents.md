---
title: "Persisted operations"
description: "Learn how to use persisted operations (trusted documents) in GraphQL with Hot Chocolate."
---

Persisted operations (also known as trusted documents) let you pre-register all required operations of your clients. You extract the operations from your client applications at build time and place them in the server's operation document storage.

Extracting operations is supported by client libraries like [Relay](https://relay.dev/docs/guides/persisted-queries/) and in the case of [Strawberry Shake](/products/strawberryshake) no additional work is needed.

<Video videoId="ZZ5PF3_P_r4" />

# How It Works

- All operations that your clients execute are extracted during the client build process. Individual operation documents are hashed to generate a unique identifier for each operation.
- Before your server is deployed, the extracted operation documents are placed in the server's operation document storage.
- After the server has been deployed, clients execute persisted operations by specifying the operation ID (hash) in their requests.
- If Hot Chocolate finds an operation that matches the specified hash in the operation document storage, it executes the operation and returns the result to the client.

> Note: There are also [automatic persisted operations](/docs/hotchocolate/v16/performance/automatic-persisted-operations), which let clients persist operation documents at runtime. They might be a better fit if your API is used by many clients with different requirements.

# Benefits

There are two main benefits to using persisted operations:

**Performance**

- Only a hash and optionally variables need to be sent to the server, reducing network traffic.
- Operations no longer need to be embedded into the client code, reducing the bundle size in the case of websites.
- Hot Chocolate can optimize the execution of persisted operations because they are always the same.

**Security**

The server can be configured to [only execute persisted operations](#blocking-regular-operations) and refuse any other operation provided by a client. This eliminates a whole class of potential attack vectors because malicious actors can no longer craft and execute harmful operations against your GraphQL server.

# Usage

Instruct your server to handle persisted operations by calling `UsePersistedOperationPipeline()` on the `IRequestExecutorBuilder`:

```csharp
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .UsePersistedOperationPipeline();
```

# Production-Ready Persisted Operations

Setting up a persisted operation file is not sufficient for a robust production environment. A key aspect of managing persisted operations at scale involves version management and ensuring compatibility with your GraphQL schema. The client registry is the resource for this purpose.

The client registry simplifies the management of your GraphQL clients. It stores and retrieves persisted operation documents through their hashes and ensures that operations are validated against the current schema on publish, preventing runtime errors due to schema-operation mismatches. It also supports versioning of your clients, allowing updates and maintenance without disrupting existing operations.

Check out the [client registry documentation](/docs/nitro/apis/client-registry) for more information.

# Storage Mechanisms

Hot Chocolate supports several operation document storages for persisted operations.

## Filesystem

To load persisted operation documents from the filesystem, add the following package:

<PackageInstallation packageName="HotChocolate.PersistedOperations.FileSystem" />

Specify where the persisted operation documents are located. The argument to `AddFileSystemOperationDocumentStorage()` specifies the directory containing the operation documents.

```csharp
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .UsePersistedOperationPipeline()
    .AddFileSystemOperationDocumentStorage("./persisted_operations");
```

When presented with an operation document hash, Hot Chocolate checks the specified folder for a file in the format: `{Hash}.graphql`.

Example: `0c95d31ca29272475bf837f944f4e513.graphql`

This file is expected to contain the operation document that the hash was generated from.

> Warning: Ensure that the server has access to the directory.

## Redis

To load persisted operation documents from Redis, add the following package:

<PackageInstallation packageName="HotChocolate.PersistedOperations.Redis" />

Specify where the persisted operation documents are located. Using `AddRedisOperationDocumentStorage()`, point to a specific Redis database containing the operation documents.

```csharp
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .UsePersistedOperationPipeline()
    .AddRedisOperationDocumentStorage(services =>
        ConnectionMultiplexer.Connect("host:port").GetDatabase());
```

Keys in the specified Redis database are expected to be operation IDs (hashes) and contain the actual operation document as the value.

## Azure Blob Storage

To load persisted operation documents from Azure Blob Storage, add the following package:

<PackageInstallation packageName="HotChocolate.PersistedOperations.AzureBlobStorage" />

Specify where the persisted operation documents are located. Using `AddAzureBlobStorageOperationDocumentStorage()`, point to a specific Azure Blob Storage container. The blob's name is the hash of the query, and its content is the corresponding GraphQL query.

> Important: The Azure Blob Storage container must already exist when Hot Chocolate uses it for the first time.

```csharp
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .UsePersistedOperationPipeline()
    .AddAzureBlobStorageOperationDocumentStorage(services =>
        services.GetService<BlobServiceClient>().GetBlobContainerClient("hotchocolate"));
```

Unlike Redis, a Blob Storage client has no built-in way to set the expiration of files in Azure Blob Storage. However, you can define [a Lifecycle Management Policy](https://learn.microsoft.com/en-us/azure/storage/blobs/lifecycle-management-overview?tabs=azure-portal). The following sample policy instructs Azure to remove all files from the `hotchocolate` container when they have not been accessed for 10 days.

```json
{
  "rules": [
    {
      "enabled": true,
      "name": "remove-after-10d",
      "type": "Lifecycle",
      "definition": {
        "actions": {
          "baseBlob": {
            "delete": {
              "daysAfterLastAccessTimeGreaterThan": 10
            }
          }
        },
        "filters": {
          "blobTypes": ["blockBlob"],
          "prefixMatch": ["hotchocolate/"]
        }
      }
    }
  ]
}
```

# Hashing Algorithms

By default, Hot Chocolate uses the MD5 hashing algorithm. You can override this default by specifying a `DocumentHashProvider`:

```csharp
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    // choose one of the following providers
    .AddMD5DocumentHashProvider()
    .AddSha256DocumentHashProvider()
    .AddSha1DocumentHashProvider()
    .UsePersistedOperationPipeline()
    .AddFileSystemOperationDocumentStorage("./persisted_operations");
```

You can also configure how these hashes are encoded by specifying a `HashFormat` as argument:

```csharp
AddSha256DocumentHashProvider(HashFormat.Hex)
AddSha256DocumentHashProvider(HashFormat.Base64)
```

> Note: [Relay](https://relay.dev) uses the MD5 hashing algorithm. No additional Hot Chocolate configuration is required.

# Blocking Regular Operations

If you want to disallow any dynamic operations, enable `OnlyAllowPersistedDocuments`:

```csharp
builder.Services
    .AddGraphQLServer()
    // Omitted for brevity
    .ModifyRequestOptions(
        options => options
            .PersistedOperations
            .OnlyAllowPersistedDocuments = true);
```

This blocks any dynamic operations that do not contain the `id` of a persisted operation.

You might still want to allow the execution of dynamic operations in certain circumstances. Override the `OnlyAllowPersistedDocuments` rule on a per-request basis using the `AllowNonPersistedOperation` method on the `OperationRequestBuilder`. Implement a custom [IHttpRequestInterceptor](/docs/hotchocolate/v16/server/interceptors#ihttprequestinterceptor) and call `AllowNonPersistedOperation` if a certain condition is met:

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

In the above example, requests containing the `X-Developer` header can execute dynamic operations. In your production application, replace this check with an authorization policy, an API key, or whatever fits your requirements.

# Client Expectations

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

> Note: [Relay's persisted queries documentation](https://relay.dev/docs/guides/persisted-queries/#network-layer-changes) uses `doc_id` instead of `id`. Be sure to change it to `id`.

# Next Steps

- [Automatic Persisted Operations](/docs/hotchocolate/v16/performance/automatic-persisted-operations) for dynamically storing operations at runtime.
- [Interceptors](/docs/hotchocolate/v16/server/interceptors) for per-request customization.
- [Client Registry](/docs/nitro/apis/client-registry) for production-ready persisted operation management.
