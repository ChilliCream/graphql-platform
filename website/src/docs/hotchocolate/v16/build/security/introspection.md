---
title: Introspection
---

Introspection is a core GraphQL feature that allows clients to query a server for schema metadata. Tools like Nitro, schema explorers, autocomplete, validation utilities, code generators, and schema registries all depend on this metadata.

In production environments, enabling introspection is a security and operational decision. Introspection can reveal type names, fields, arguments, deprecations, descriptions, and directive metadata. It may also be used in recursive query patterns that consume validation resources before resolvers execute.

Disabling introspection alone does not fully protect your schema. Schema details can still be exposed through client bundles, network traffic, error messages, documentation, schema registries, SDL endpoints, or source control. Treat introspection as one layer in a broader defense strategy, alongside authorization, request limits, cost analysis, trusted documents, endpoint configuration, and release management.

# Start with a Production Policy

Decide your introspection policy before publishing your endpoint.

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .DisableIntrospection();
```

This configuration disables runtime introspection validation for the request executor. Any request selecting `__schema`, `__type`, or Hot Chocolate semantic introspection fields will be rejected unless explicitly allowed for that request.

If you want introspection to remain available, such as for a public developer API where schema browsing is a feature, use the following:

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .DisableIntrospection(disable: false);
```

The `disable` parameter is literal:

| Value   | Result                                                                    |
| ------- | ------------------------------------------------------------------------- |
| `true`  | Runtime introspection is disabled unless the request carries an override. |
| `false` | Runtime introspection is allowed.                                         |

# What Introspection Exposes

GraphQL defines special fields for schema metadata and runtime type names.

| Field                          | What it returns                                                                                    | Disabled by `.DisableIntrospection()` |
| ------------------------------ | -------------------------------------------------------------------------------------------------- | ------------------------------------- |
| `__schema`                     | Schema-wide metadata, such as types, directives, query type, mutation type, and subscription type. | Yes                                   |
| `__type(name: ...)`            | Metadata for one named type, such as fields, arguments, enum values, interfaces, and input fields. | Yes                                   |
| `__typename`                   | The concrete object type name for the value being resolved.                                        | No                                    |
| `__search` and `__definitions` | Hot Chocolate semantic introspection fields when `EnableSemanticIntrospection` is enabled.         | Yes                                   |

For example, `__schema` can reveal all available types:

```graphql
query SchemaNames {
  __schema {
    types {
      name
    }
  }
}
```

The `__type` field can show the structure of a specific type:

```graphql
query ProductShape {
  __type(name: "Product") {
    fields {
      name
      args {
        name
      }
    }
  }
}
```

The `__typename` field remains available because many client operations require it for unions and interfaces:

```graphql
query Search {
  search(term: "coffee") {
    __typename
    ... on Product {
      name
    }
    ... on Store {
      address
    }
  }
}
```

When runtime introspection is disabled, the first two operations above are rejected. The `__typename` field remains valid.

Hot Chocolate also provides schema output options that affect what is visible when introspection is enabled:

| Option                         | Default | Effect                                                                                                                                    |
| ------------------------------ | ------- | ----------------------------------------------------------------------------------------------------------------------------------------- |
| `EnableSemanticIntrospection`  | `true`  | Adds semantic introspection fields such as `__search` and `__definitions`. These are also blocked when runtime introspection is disabled. |
| `EnableDirectiveIntrospection` | `false` | Controls whether custom directives appear in introspection output. This does not control whether introspection is allowed.                |

You can configure these options to adjust the level of schema detail:

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .ModifyOptions(o =>
    {
        o.EnableDirectiveIntrospection = true;
        o.EnableSemanticIntrospection = false;
    });
```

# Hot Chocolate v16 Defaults

The standard ASP.NET Core registration applies default security unless you opt out.

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>();
```

Calling `AddGraphQL()` on `IHostApplicationBuilder` delegates to `AddGraphQLServer(...)`. With default security enabled, Hot Chocolate v16:

- Adds cost analysis.
- Disables introspection when the host environment is not development.
- Adds a maximum field cycle depth rule.

| Registration                          | Development                                                         | Staging and production                                               | Opt out                              |
| ------------------------------------- | ------------------------------------------------------------------- | -------------------------------------------------------------------- | ------------------------------------ |
| `builder.AddGraphQL()`                | Runtime introspection is allowed by the default security predicate. | Runtime introspection is disabled by the default security predicate. | Pass `disableDefaultSecurity: true`. |
| `builder.Services.AddGraphQLServer()` | Runtime introspection is allowed by the default security predicate. | Runtime introspection is disabled by the default security predicate. | Pass `disableDefaultSecurity: true`. |

Opting out removes more than the introspection default:

```csharp
builder
    .AddGraphQL(disableDefaultSecurity: true)
    .AddQueryType<Query>();
```

Only use this if you are adding equivalent security controls yourself. For more on the default security posture, see the [security overview](index.md).

# Use Environment-Based Rules

If you want to make the policy explicit in your application code, use the predicate overload. Return `true` to disable introspection for that executor configuration, or `false` to allow it.

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .DisableIntrospection((services, _) =>
    {
        var environment = services.GetRequiredService<IHostEnvironment>();
        return !environment.IsDevelopment();
    });
```

This approach is helpful for documenting the policy in your GraphQL registration, even though the standard registration already disables introspection outside development.

# Allow Selected Requests

Some teams disable introspection by default but allow it for trusted developer requests, internal tooling, or schema monitoring. You can use a request interceptor and call `OperationRequestBuilder.AllowIntrospection()` only after the request passes your own checks.

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .DisableIntrospection()
    .AddHttpRequestInterceptor<IntrospectionInterceptor>();
```

```csharp
public sealed class IntrospectionInterceptor : DefaultHttpRequestInterceptor
{
    public override ValueTask OnCreateAsync(
        HttpContext context,
        IRequestExecutor requestExecutor,
        OperationRequestBuilder requestBuilder,
        CancellationToken cancellationToken)
    {
        if (context.User.IsInRole("SchemaExplorer"))
        {
            requestBuilder.AllowIntrospection();
        }

        return base.OnCreateAsync(
            context,
            requestExecutor,
            requestBuilder,
            cancellationToken);
    }
}
```

The role check above is an example. Connect this to your real authorization model, such as endpoint authorization, authenticated internal users, network controls, or signed internal service credentials. Do not treat an unauthenticated header as a production security boundary.

Learn more about request customization in [Interceptors](../../server/interceptors.md), and design data access rules with [Authorization](authorization.md).

# Customize the Rejection Message

A blocked introspection request returns a GraphQL validation error. The default message is `Introspection is not allowed for the current request.` and the error code is `HC0046`.

```json
{
  "errors": [
    {
      "message": "Introspection is not allowed for the current request.",
      "extensions": {
        "field": "__schema",
        "code": "HC0046"
      }
    }
  ]
}
```

Use `SetIntrospectionNotAllowedMessage(...)` to provide a product-specific message:

```csharp
public sealed class IntrospectionInterceptor : DefaultHttpRequestInterceptor
{
    public override ValueTask OnCreateAsync(
        HttpContext context,
        IRequestExecutor requestExecutor,
        OperationRequestBuilder requestBuilder,
        CancellationToken cancellationToken)
    {
        if (context.User.IsInRole("SchemaExplorer"))
        {
            requestBuilder.AllowIntrospection();
        }
        else
        {
            requestBuilder.SetIntrospectionNotAllowedMessage(
                "Schema introspection is not available for this request.");
        }

        return base.OnCreateAsync(
            context,
            requestExecutor,
            requestBuilder,
            cancellationToken);
    }
}
```

Keep production error messages neutral. Do not reveal required roles, internal endpoint names, network ranges, or credential formats in the error text.

# Limit Recursive Introspection Depth

Disabling introspection controls whether metadata queries are allowed. Depth limits control how much recursive introspection work is accepted when introspection is enabled.

Hot Chocolate v16 defaults:

| Limit                          | Default |
| ------------------------------ | ------- |
| `MaxAllowedOfTypeDepth`        | `16`    |
| `MaxAllowedListRecursiveDepth` | `1`     |

Adjust these limits only after understanding your tooling requirements:

```csharp
builder
    .AddGraphQL()
    .SetIntrospectionAllowedDepth(
        maxAllowedOfTypeDepth: 8,
        maxAllowedListRecursiveDepth: 1);
```

See [Execution depth and limits](execution-depth-and-limits.md) for the full request limit model.

# Separate Introspection from SDL and Nitro

Runtime introspection, SDL download, Nitro hosting, and schema export are separate controls. This distinction is a common source of production surprises.

`MapGraphQL()` can serve the GraphQL endpoint, Nitro, and SDL downloads under the same path. Disabling runtime introspection does not automatically disable `GET /graphql?sdl`, schema file support, or Nitro hosting.

| Goal                                                                | Control                                               |
| ------------------------------------------------------------------- | ----------------------------------------------------- |
| Block runtime introspection fields such as `__schema` and `__type`. | `.DisableIntrospection(...)`                          |
| Block live SDL downloads with `?sdl`.                               | `EnableSchemaRequests = false`                        |
| Block schema SDL file support.                                      | `EnableSchemaFileSupport = false`                     |
| Disable the browser tool on an endpoint.                            | `Tool.Enable = false`                                 |
| Expose Nitro on a separate route.                                   | `MapNitroApp(...)`                                    |
| Expose SDL on a separate route.                                     | `MapGraphQLSchema(...)`                               |
| Produce SDL for CI or client builds.                                | `dotnet run -- schema export --output schema.graphql` |

Configure endpoint schema requests separately from runtime introspection:

```csharp
app.MapGraphQL()
    .WithOptions(o =>
    {
        o.EnableSchemaRequests = false;
        o.EnableSchemaFileSupport = false;
    });
```

Configure Nitro separately as well:

```csharp
app.MapGraphQL()
    .WithOptions(o => o.Tool.Enable = app.Environment.IsDevelopment());
```

If production Nitro requires schema metadata, provide an allowed request path, an authenticated non-production endpoint, or a schema artifact or registry workflow. Do not assume that disabling Nitro blocks introspection, or that disabling introspection blocks Nitro hosting.

See [Endpoints](../server-configuration/endpoints.md) for complete endpoint setup.

# Provide Schema Access Without Production Introspection

Private and restricted APIs often need schema metadata for development, CI, and client generation, but should not expose live production introspection.

A typical workflow is:

1. Export the schema during CI.
2. Publish the SDL artifact to client builds, documentation, or a schema registry.
3. Generate clients and validate operations against that artifact.
4. Deploy production with runtime introspection restricted.
5. Allow developer access through authenticated non-production endpoints or explicit request overrides.

```bash
dotnet run -- schema export --output schema.graphql
```

See [Command Line](../server-configuration/command-line.md) for schema export details.

# Introspection and Trusted Documents

Trusted documents and introspection address different concerns.

| Control           | Question it answers                                                                       |
| ----------------- | ----------------------------------------------------------------------------------------- |
| Trusted documents | Can this client execute arbitrary operation text, or only registered operation documents? |
| Introspection     | Can this request query runtime schema metadata?                                           |

For private first-party clients, combine both approaches:

- Use trusted documents or persisted operation routes for known client operations.
- Disable runtime introspection in production unless there is an intentional exception path.
- Export schema artifacts for CI and client generation.
- Maintain field and endpoint authorization for protected data.

Continue with [Trusted documents](../../performance/trusted-documents.md) and [Automatic persisted operations](../../performance/automatic-persisted-operations.md).

# Choose a Strategy

| Scenario                                  | Runtime introspection                                | SDL downloads                        | Nitro                                   | CI schema source    | Common failure mode                                         |
| ----------------------------------------- | ---------------------------------------------------- | ------------------------------------ | --------------------------------------- | ------------------- | ----------------------------------------------------------- |
| Public developer API with a public schema | Allow intentionally. Keep limits and cost analysis.  | Allow if SDL is part of the product. | Allow where the tool is supported.      | Export or registry. | Limits are too strict for tooling.                          |
| Public API without public schema browsing | Disable.                                             | Disable public downloads.            | Disable public Nitro.                   | Export or registry. | A separate SDL route still exposes schema text.             |
| Private first-party API                   | Disable by default.                                  | Restrict or disable.                 | Non-production or authenticated access. | Export or registry. | Client generation depends on live production introspection. |
| Internal API                              | Allow only for trusted developers or internal tools. | Restrict to the same audience.       | Internal or non-production.             | Export or registry. | Network location is treated as the only control.            |

# Troubleshooting

| Symptom                                                                   | What to check                                                                                                                                                           |
| ------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Introspection works locally but fails in staging or production.           | Default security disables introspection outside development. Check `IHostEnvironment`, `disableDefaultSecurity: true`, and explicit `.DisableIntrospection(...)` calls. |
| `/graphql?sdl` still returns the schema after introspection was disabled. | SDL downloads use `EnableSchemaRequests` and endpoint options. Configure them separately.                                                                               |
| Nitro is disabled but clients can still introspect.                       | Nitro hosting and runtime introspection are independent controls. Disable introspection on the request executor.                                                        |
| Nitro does not show schema reference or autocomplete in production.       | Check introspection policy, endpoint authorization, SDL settings, and whether the tool should use a schema artifact or registry.                                        |
| Client code generation or schema checks fail in CI.                       | Use schema export or a registry workflow instead of live production introspection.                                                                                      |
| `__typename` still appears in responses.                                  | This is expected. `__typename` remains valid for normal operations.                                                                                                     |
| A custom directive is missing from introspection output.                  | Check `EnableDirectiveIntrospection`. It controls output detail, not whether introspection is allowed.                                                                  |
| Recursive introspection fails with a depth error.                         | Tune `.SetIntrospectionAllowedDepth(...)` if the tool requires deeper shapes, then keep cost and request limits in place.                                               |

# When Disabling Introspection Is Not Enough

Do not rely on introspection settings as your only protection for sensitive data or operations. Add controls that match your actual threat model:

- Authentication and authorization for users, tenants, fields, mutations, and ownership checks.
- Cost analysis and depth limits for expensive operation shapes.
- Trusted documents when clients should not send arbitrary operation text.
- Endpoint controls for Nitro, SDL downloads, GET, multipart uploads, batching, CORS, cookies, and WebSockets.
- Neutral error messages and server-side logging for incidents.
- Schema release processes for client generation, schema checks, and documentation.

# API Quick Reference

| API                                                                      | Purpose                                                                       |
| ------------------------------------------------------------------------ | ----------------------------------------------------------------------------- |
| `.DisableIntrospection()`                                                | Disable runtime introspection globally for the executor.                      |
| `.DisableIntrospection(disable: bool)`                                   | Explicitly disable or allow runtime introspection.                            |
| `.DisableIntrospection(Func<IServiceProvider, ValidationOptions, bool>)` | Decide the runtime introspection policy from services and validation options. |
| `OperationRequestBuilder.AllowIntrospection()`                           | Allow introspection for the current request.                                  |
| `OperationRequestBuilder.SetIntrospectionNotAllowedMessage(...)`         | Set the rejection message for the current request.                            |
| `.SetIntrospectionAllowedDepth(ushort, ushort)`                          | Tune recursive introspection depth limits.                                    |
| `GraphQLServerOptions.EnableSchemaRequests`                              | Control `?sdl` schema downloads.                                              |
| `GraphQLServerOptions.EnableSchemaFileSupport`                           | Control schema SDL file support.                                              |
| `GraphQLServerOptions.Tool.Enable`                                       | Control Nitro on the mapped endpoint.                                         |
| `MapGraphQLSchema(...)`                                                  | Map a separate SDL endpoint.                                                  |
| `MapNitroApp(...)`                                                       | Map Nitro separately from the GraphQL endpoint.                               |

# Next Steps

- [Security overview](index.md)
- [Execution depth and limits](execution-depth-and-limits.md)
- [Cost analysis](cost-analysis.md)
- [Authentication](authentication.md)
- [Authorization](authorization.md)
- [Endpoints](../server-configuration/endpoints.md)
- [Interceptors](../../server/interceptors.md)
- [Command Line](../server-configuration/command-line.md)
