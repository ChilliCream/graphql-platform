---
title: "First-Party API"
description: "Deploy a Hot Chocolate server as a locked-down first-party GraphQL API using trusted documents. Eliminate parser exploits, validation attacks, and complexity abuse by only executing pre-registered operations."
---

When your GraphQL API is only consumed by clients you control — mobile apps, websites, internal services — there is no reason for the server to accept arbitrary GraphQL Operations at runtime. You already know every operation your clients will ever send, because you wrote them. Trusted documents let you take advantage of that fact.

The idea is simple: rather than shipping GraphQL operation text inside your client and sending it to the server on every request, you extract all operations at build time and register them with the server ahead of deployment. At runtime, clients reference operations by a short identifier instead of sending the full operation text. The server looks up the pre-registered operation, executes it, and returns the result. It never parses or validates an operation it has not seen before.

This eliminates entire classes of attacks. The GraphQL parser and validator never process untrusted input, so parser exploits, validation-level denial of service, and complexity attacks simply cannot reach your server. But the benefits go well beyond security. Trusted documents give you something that REST and gRPC cannot: field-level visibility into which client, in which version, uses which operations and which fields. Because every operation is registered with its client identity and version, you can make informed schema evolution decisions at the individual field level. You know exactly who will be affected before you deprecate or remove a field, enabling safe, incremental API evolution at scale.

This is why Meta, Netflix, and X operate GraphQL in production with trusted documents.

This guide covers the server side of this workflow: how to configure a Hot Chocolate server to only execute trusted, pre-registered operations.

# How trusted documents work

Trusted documents (also called persisted operations) turn your GraphQL endpoint from an open operation processor into a closed, auditable contract. The workflow has three steps:

1. **Extract.** The client compiler (Relay, Strawberry Shake, or similar) extracts every GraphQL operation from the codebase at build time. Each operation is hashed to produce a stable, unique identifier.
2. **Publish.** The extracted operations are published to the Nitro client registry before the application is deployed.
3. **Execute.** At runtime, clients send the operation hash instead of the full operation text. The server looks up the operation by ID in the client registry, executes the pre-registered document, and returns the result.

Once this workflow is in place, the server never parses an operation it has not seen before.

# Configure the Hot Chocolate server

Hot Chocolate needs `UsePersistedOperationPipeline()` to replace the default request pipeline with one that resolves operations by ID. You also need to connect the server to Nitro and enable the security options that block ad-hoc operations and skip document body parsing.

First, add the `ChilliCream.Nitro` package to your project:

```bash
dotnet add package ChilliCream.Nitro
```

Then configure the server:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddNitro()
    .UsePersistedOperationPipeline()
    .ModifyRequestOptions(o =>
    {
        o.PersistedOperations.OnlyAllowPersistedDocuments = true;
        o.PersistedOperations.AllowDocumentBody = false;
    });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapGraphQL();
}

app.MapGraphQLPersistedOperations();

app.Run();
```

`MapGraphQL()` is only registered in development. It exposes the standard GraphQL endpoint that client tooling needs: ad-hoc operations, introspection, and the schema endpoint at `/graphql/sdl`. In production, only `MapGraphQLPersistedOperations()` is registered. The server exclusively accepts requests that reference a registered operation ID by URL, and the HTTP request parser skips reading the `query` field entirely.

# What the settings do

**`OnlyAllowPersistedDocuments = true`** means every executed operation must match a persisted document. Clients can still send the full operation text in the `query` field; the server will parse it, hash it, and verify that the hash matches a registered operation. This is useful during migration: legacy clients keep sending operation text while new clients send only the hash. Either way, only known operations execute.

**`AllowDocumentBody = false`** (this is the default) goes further. It configures the transport layer to skip parsing the `query` field entirely from JSON request bodies and GET query parameters. The GraphQL parser is never invoked for incoming request documents. This is the strict trusted documents mode.

**Together**, the server never parses untrusted GraphQL input. Operations are loaded from the client registry by ID, where they were pre-validated at publish time. The parser, validator, and cost analyzer are never exercised by external traffic.

# Client request format

With `MapGraphQLPersistedOperations()`, the operation hash and operation name are part of the URL path. Clients never send a `query` or `id` field. Only `variables` and `extensions` are specified, either in the POST body or as query parameters on GET:

**HTTP POST to `/graphql/persisted/{operationId}/{operationName}`:**

```json
{
  "variables": { "first": 10 }
}
```

**HTTP GET to `/graphql/persisted/{operationId}/{operationName}`:**

```http
GET /graphql/persisted/0c95d31ca29272475bf837f944f4e513/GetProducts?variables={"first":10}
```

The default base path is `/graphql/persisted`. You can customize it:

```csharp
app.MapGraphQLPersistedOperations("/api/operations");
```

You can require that clients always include an operation name in the URL:

```csharp
app.MapGraphQLPersistedOperations(requireOperationName: true);
```

The deterministic GET routes produce stable cache keys, which makes them ideal for CDN and HTTP response caching.

# Development workflow

During development, you need to iterate on operations without going through the full extract-publish-deploy cycle every time you change an operation. Tools like Nitro (the GraphQL IDE) need to send ad-hoc operations for exploration and testing.

The conditional `MapGraphQL()` in the configuration above already handles the endpoint side. For the execution pipeline, use an HTTP request interceptor to allow non-persisted operations in development:

```csharp
public class DevToolsInterceptor : DefaultHttpRequestInterceptor
{
    private readonly IHostEnvironment _env;

    public DevToolsInterceptor(IHostEnvironment env) => _env = env;

    public override ValueTask OnCreateAsync(
        HttpContext context,
        IRequestExecutor requestExecutor,
        OperationRequestBuilder requestBuilder,
        CancellationToken cancellationToken)
    {
        if (_env.IsDevelopment())
        {
            requestBuilder.AllowNonPersistedOperation();
        }

        return base.OnCreateAsync(context, requestExecutor, requestBuilder, cancellationToken);
    }
}
```

Register the interceptor:

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddNitro()
    .AddHttpRequestInterceptor<DevToolsInterceptor>()
    .UsePersistedOperationPipeline()
    .ModifyRequestOptions(o =>
    {
        o.PersistedOperations.OnlyAllowPersistedDocuments = true;
        o.PersistedOperations.AllowDocumentBody = false;
    });
```

In production, `IsDevelopment()` returns `false`, and all requests must reference a registered operation ID. In development, the interceptor calls `AllowNonPersistedOperation()` on the request builder, which bypasses the `OnlyAllowPersistedDocuments` check for that request.

Replace the environment check with an authorization policy, API key, or other mechanism that fits your requirements. The important thing is that production traffic always goes through the persisted operation path.

# Putting it together

Here is a complete `Program.cs` for a Hot Chocolate server deployed as a first-party API with trusted documents, authentication, and persisted operation routes:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddAuthentication()
    .AddJwtBearer(options =>
    {
        options.Authority = "https://your-identity-provider.com/realms/your-realm";
        options.Audience = "graphql-api";
    });

builder.Services.AddAuthorization();

builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddAuthorization()
    .AddNitro()
    .AddHttpRequestInterceptor<DevToolsInterceptor>()
    .UsePersistedOperationPipeline()
    .ModifyRequestOptions(o =>
    {
        o.PersistedOperations.OnlyAllowPersistedDocuments = true;
        o.PersistedOperations.AllowDocumentBody = false;
    });

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapGraphQL();
}

app.MapGraphQLPersistedOperations();

app.Run();
```

This configuration:

- Validates JWT tokens and enforces authorization policies.
- Rejects any request that does not reference a registered operation ID.
- Never parses the `query` field from incoming requests.
- Exposes deterministic GET routes at `/graphql/persisted/{operationId}/{operationName}` for CDN-friendly caching.
- Allows ad-hoc operations in development via the `DevToolsInterceptor`.

# Next Steps

- **"How do I extract and publish operations from my client?"** See [Client Registry](/docs/nitro/apis/client-registry) for extracting operations, publishing them to Nitro, and managing multiple client versions.
- **"I need authentication and authorization."** See [Security Overview](/docs/hotchocolate/v16/security) for authentication and authorization configuration.
- **"I need to protect against complexity attacks on a public API."** See [Public API guide](/docs/hotchocolate/v16/guides/public-api) for cost analysis, depth limits, and introspection controls.
