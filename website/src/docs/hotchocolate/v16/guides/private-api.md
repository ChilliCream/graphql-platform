---
title: "Building a private GraphQL API"
description: "An end-to-end guide for building a private, first-party GraphQL API using trusted documents in Hot Chocolate."
---

If you control every client that talks to your GraphQL API, you have a significant advantage: you know every operation at build time. You can extract those operations, register them with the server, and configure the server to reject anything it has not seen before. This is how Meta built and operates GraphQL internally, and it is the recommended path for teams building first-party APIs.

This guide walks you through the full workflow for building a private GraphQL API with Hot Chocolate. A private API serves your own applications: your website, your mobile app, your internal tools. You are not exposing a public endpoint for third-party developers. The key insight is that if you control the clients, you can lock down the server to only accept operations you have reviewed and approved.

# The trusted documents workflow

Trusted documents (also called persisted operations) turn your GraphQL API from an open query endpoint into a closed, auditable contract. The workflow has three steps:

1. **Extract** operations from your client applications at build time. Each operation gets a hash that serves as its unique identifier.
2. **Register** those operations with the server. You place the operation files in a storage location the server can read from, or you use the Nitro client registry.
3. **Configure** the server to only accept registered operations. Any request that does not reference a known operation ID is rejected.

Once this workflow is in place, the server never parses or executes an operation it has not seen before. Malicious queries, accidental expensive queries, and schema exploration by unauthorized clients are all blocked at the door.

# Extract operations from your client

The first step is to get the operations out of your client code and into files the server can consume. Both Relay and Strawberry Shake support this out of the box.

## Relay

Relay extracts operations during its compiler step. Configure `relay.config.js` to output persisted operations:

```js
// relay.config.js
module.exports = {
  src: "./src",
  schema: "./schema.graphql",
  language: "typescript",
  persistConfig: {
    file: "./persisted-operations/operations.json",
    algorithm: "MD5",
  },
};
```

After running the Relay compiler, the `operations.json` file contains a mapping of hashes to operation documents:

```json
{
  "913abc361487c481cf6015841c0eca22": "query GetUser { me { id name } }",
  "0e7cf2125e8eb711b470cc72c73ca77e": "query GetProducts { products { nodes { name price } } }"
}
```

[Learn more about Relay persisted queries](https://relay.dev/docs/guides/persisted-queries/)

## Strawberry Shake

Strawberry Shake extracts operations as part of the normal .NET build. Add the `GraphQLPersistedOperationOutput` property to your client project:

```xml
<!-- MyClient.csproj -->
<PropertyGroup>
    <GraphQLPersistedOperationOutput>./persisted-operations</GraphQLPersistedOperationOutput>
</PropertyGroup>
```

When you build the project, Strawberry Shake writes one `.graphql` file per operation to the output directory, using the operation hash as the filename:

```text
persisted-operations/
  913abc361487c481cf6015841c0eca22.graphql
  0e7cf2125e8eb711b470cc72c73ca77e.graphql
```

If your server expects the Relay JSON format instead, set the output format:

```xml
<!-- MyClient.csproj -->
<PropertyGroup>
    <GraphQLPersistedOperationOutput>./persisted-operations</GraphQLPersistedOperationOutput>
    <GraphQLPersistedOperationFormat>relay</GraphQLPersistedOperationFormat>
</PropertyGroup>
```

[Learn more about Strawberry Shake persisted operations](/docs/strawberryshake/v16/performance/persisted-operations)

# Register operations with the server

Once you have the extracted operation files, the server needs access to them. You have two options.

## Filesystem storage

For straightforward setups, place the operation files on disk where the server can read them. Install the filesystem storage package:

<PackageInstallation packageName="HotChocolate.PersistedOperations.FileSystem" />

Point the server at the directory containing your operation files:

```csharp
// Program.cs
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .UsePersistedOperationPipeline()
    .AddFileSystemOperationDocumentStorage("./persisted-operations");
```

Hot Chocolate looks for files named `{hash}.graphql` in the specified directory. Make sure the hashing algorithm matches between client and server. Hot Chocolate defaults to MD5, which matches Relay's default.

## Client registry (Nitro)

For production deployments with multiple client versions, CI/CD validation, and schema compatibility checks, use the [Nitro client registry](/docs/nitro/apis/client-registry). The client registry validates operations against your schema on publish and distributes them to your server at runtime.

```csharp
// Program.cs
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddNitro(x =>
    {
        x.ApiKey = "<<your-api-key>>";
        x.ApiId = "<<your-api-id>>";
        x.Stage = "production";
    })
    .UsePersistedOperationPipeline();
```

The client registry is the recommended approach for teams running multiple services or deploying frequently. It catches operation-schema mismatches in your CI pipeline before they reach production.

[Learn more about the client registry](/docs/nitro/apis/client-registry)

# Configure the server

The `UsePersistedOperationPipeline()` method replaces the default request pipeline with one that resolves operations by ID. Instead of parsing a raw GraphQL document from the request body, the server looks up the operation in its document storage using the hash provided by the client.

```csharp
// Program.cs
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .UsePersistedOperationPipeline()
    .AddFileSystemOperationDocumentStorage("./persisted-operations");
```

With this pipeline active, clients send requests with an `id` field instead of a `query` field:

```json
{
  "id": "913abc361487c481cf6015841c0eca22",
  "variables": {
    "userId": "abc123"
  }
}
```

> Note: Relay uses `doc_id` instead of `id` by default. Update your Relay network layer to send the hash as `id`.

# Block ad-hoc operations

The persisted operation pipeline still accepts regular operations alongside persisted ones unless you explicitly block them. To close this gap, enable `OnlyAllowPersistedDocuments`:

```csharp
// Program.cs
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .UsePersistedOperationPipeline()
    .AddFileSystemOperationDocumentStorage("./persisted-operations")
    .ModifyRequestOptions(options =>
        options.PersistedOperations.OnlyAllowPersistedDocuments = true);
```

This is the key security boundary of a private API. With this option enabled, any request that does not reference a registered operation ID is rejected with an error. No ad-hoc queries, no introspection queries, no malicious payloads.

You can customize the error message returned to clients:

```csharp
// Program.cs
.ModifyRequestOptions(options =>
{
    options.PersistedOperations.OnlyAllowPersistedDocuments = true;
    options.PersistedOperations.OperationNotAllowedError = ErrorBuilder.New()
        .SetMessage("This API only accepts pre-registered operations.")
        .Build();
})
```

# Development workflow

During development, you need to iterate on operations without going through the full extract-register-deploy cycle every time you change a query. There are two approaches.

## Automatic persisted operations for development

Use `UseAutomaticPersistedOperationPipeline()` in your development environment. This lets clients persist operations at runtime: the first request with a new operation stores it, and subsequent requests use the hash.

```csharp
// Program.cs
if (app.Environment.IsDevelopment())
{
    builder.Services
        .AddMemoryCache()
        .AddGraphQLServer()
        .AddQueryType<Query>()
        .UseAutomaticPersistedOperationPipeline()
        .AddInMemoryOperationDocumentStorage();
}
else
{
    builder.Services
        .AddGraphQLServer()
        .AddQueryType<Query>()
        .UsePersistedOperationPipeline()
        .AddFileSystemOperationDocumentStorage("./persisted-operations")
        .ModifyRequestOptions(options =>
            options.PersistedOperations.OnlyAllowPersistedDocuments = true);
}
```

## Developer bypass with an HTTP interceptor

If you prefer to keep the persisted operation pipeline active during development but allow specific developers to send ad-hoc operations, use a custom HTTP request interceptor:

```csharp
// Interceptors/DevelopmentRequestInterceptor.cs
public class DevelopmentRequestInterceptor : DefaultHttpRequestInterceptor
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
            context, requestExecutor, requestBuilder, cancellationToken);
    }
}
```

Register the interceptor:

```csharp
// Program.cs
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddHttpRequestInterceptor<DevelopmentRequestInterceptor>()
    .UsePersistedOperationPipeline()
    .AddFileSystemOperationDocumentStorage("./persisted-operations")
    .ModifyRequestOptions(options =>
        options.PersistedOperations.OnlyAllowPersistedDocuments = true);
```

In production, remove the interceptor or gate the bypass behind a proper authorization check.

# What you can skip

A private API with trusted documents eliminates entire categories of attack vectors. Because you control every operation that reaches the server, several security measures that public APIs require become unnecessary:

**Cost analysis.** Cost analysis protects against expensive queries from unknown clients. With trusted documents, every operation has already been reviewed by your team. You know the cost of each operation before it reaches production.

**Introspection restrictions.** Clients in a trusted documents workflow do not use introspection at runtime. Operations are compiled against the schema at build time using schema files or code generation. Disabling introspection in production is still reasonable as a defense-in-depth measure, but it is no longer a primary security control.

**Depth limits.** Query depth limits prevent deeply nested queries from consuming excessive resources. With trusted documents, you have reviewed every operation and know its structure. Depth limits add no value when the operation set is fixed.

**Operation complexity limits.** Similar to cost analysis, complexity limits guard against operations you have not seen before. With trusted documents, there are no surprise operations.

This is a significant operational win. Your server configuration becomes lighter, your security model becomes simpler, and your team spends less time tuning limits and thresholds.

# Putting it all together

Here is a complete `Program.cs` for a private API with trusted documents:

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddAuthorization()
    .UsePersistedOperationPipeline()
    .AddFileSystemOperationDocumentStorage("./persisted-operations")
    .ModifyRequestOptions(options =>
        options.PersistedOperations.OnlyAllowPersistedDocuments = true);

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGraphQL();

app.Run();
```

Compare this with a public API that needs cost analysis, depth limits, and introspection controls:

```csharp
// Program.cs (public API for comparison)
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddAuthorization()
    .AddMaxExecutionDepthRule(15)
    .ModifyRequestOptions(o =>
    {
        o.ExecutionTimeout = TimeSpan.FromSeconds(30);
    })
    .ModifyCostOptions(o =>
    {
        o.MaxFieldCost = 2000;
        o.MaxTypeCost = 2000;
        o.EnforceCostLimits = true;
    });

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGraphQL();

app.Run();
```

The private API setup is shorter, has fewer knobs to tune, and provides a stronger security guarantee. The tradeoff is the build-time extraction step, which your client tooling handles automatically.

# Next Steps

- **Need to set up persisted operations storage?** See [Persisted Operations](/docs/hotchocolate/v16/performance/trusted-documents) for filesystem, Redis, and Azure Blob Storage options.
- **Want runtime operation persistence for development?** See [Automatic Persisted Operations](/docs/hotchocolate/v16/performance/automatic-persisted-operations).
- **Need authentication and authorization?** See [Security Overview](/docs/hotchocolate/v16/security).
- **Managing multiple clients in production?** See [Client Registry](/docs/nitro/apis/client-registry) for versioning, validation, and distribution.
- **Using Strawberry Shake?** See [Strawberry Shake Persisted Operations](/docs/strawberryshake/v16/performance/persisted-operations) for client-side configuration.
