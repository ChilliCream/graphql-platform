---
title: Introspection
---

Introspection is the GraphQL feature that lets clients ask a server for schema metadata. Nitro, schema explorers, autocomplete, validation tools, code generators, and schema registries all rely on that metadata.

In production, introspection is a security and operations decision. It can reveal type names, fields, arguments, deprecations, descriptions, and directive metadata. It can also be used in recursive query shapes that spend validation resources before your resolvers run.

Disabling introspection is not a complete way to keep a schema secret. Schema details can also appear in client bundles, traffic, errors, documentation, schema registries, SDL endpoints, and source control. Treat introspection as one defense in depth control alongside authorization, request limits, cost analysis, trusted documents, endpoint configuration, and release processes.

# Start with the production policy

Most applications should decide the policy before publishing the endpoint.

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .DisableIntrospection();
```

This disables runtime introspection validation for the request executor. A request that selects `__schema`, `__type`, or Hot Chocolate semantic introspection fields is rejected unless the current request is explicitly allowed.

Use the opposite setting when you want introspection to stay available, for example for a public developer API where schema browsing is part of the product:

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>()
    .DisableIntrospection(disable: false);
```

The `disable` value is literal:

| Value   | Result                                                                    |
| ------- | ------------------------------------------------------------------------- |
| `true`  | Runtime introspection is disabled unless the request carries an override. |
| `false` | Runtime introspection is allowed.                                         |

# What introspection exposes

GraphQL defines special fields for schema metadata and runtime type names.

| Field                          | What it returns                                                                                    | Disabled by `.DisableIntrospection()` |
| ------------------------------ | -------------------------------------------------------------------------------------------------- | ------------------------------------- |
| `__schema`                     | Schema-wide metadata, such as types, directives, query type, mutation type, and subscription type. | Yes                                   |
| `__type(name: ...)`            | Metadata for one named type, such as fields, arguments, enum values, interfaces, and input fields. | Yes                                   |
| `__typename`                   | The concrete object type name for the value being resolved.                                        | No                                    |
| `__search` and `__definitions` | Hot Chocolate semantic introspection fields when `EnableSemanticIntrospection` is enabled.         | Yes                                   |

`__schema` can reveal the available types:

```graphql
query SchemaNames {
  __schema {
    types {
      name
    }
  }
}
```

`__type` can reveal the shape of a specific type:

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

`__typename` remains available because normal client operations often need it for unions and interfaces:

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

When runtime introspection is disabled, the first two operations are rejected. The `__typename` operation is still valid.

Hot Chocolate also has schema output options that affect what appears when introspection is allowed:

| Option                         | Default | Effect                                                                                                                                           |
| ------------------------------ | ------- | ------------------------------------------------------------------------------------------------------------------------------------------------ |
| `EnableSemanticIntrospection`  | `true`  | Adds semantic introspection fields such as `__search` and `__definitions`. These fields are also blocked when runtime introspection is disabled. |
| `EnableDirectiveIntrospection` | `false` | Controls whether custom directives appear in introspection output. This does not decide whether introspection is allowed.                        |

Configure these with schema options when you need to change the output detail:

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

# Hot Chocolate v16 defaults

The standard ASP.NET Core registration applies default security unless you opt out.

```csharp
builder
    .AddGraphQL()
    .AddQueryType<Query>();
```

`AddGraphQL()` on `IHostApplicationBuilder` delegates to `AddGraphQLServer(...)`. With default security enabled, Hot Chocolate v16:

- Adds cost analysis.
- Disables introspection when the host environment is not development.
- Adds the maximum field cycle depth rule.

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

Use that only when you add equivalent controls yourself. See the [security overview](index.md) for the broader default posture.

# Use environment-based rules

If you want the policy to be explicit in your application code, use the predicate overload. Return `true` to disable introspection for that executor configuration and `false` to allow it.

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

This is useful when you want to document the policy in the GraphQL registration even though the standard registration already disables introspection outside development.

# Allow selected requests

Some teams disable introspection by default but allow it for trusted developer requests, internal tooling, or schema monitoring. Use a request interceptor and call `OperationRequestBuilder.AllowIntrospection()` only after the request passes your own gate.

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

The role check is an example of where to connect your real authorization model. You can use endpoint authorization, authenticated internal users, network controls, or signed internal service credentials. Do not treat an unauthenticated header as a production security boundary.

Learn more about request customization in [Interceptors](../../server/interceptors.md), and design data access rules with [Authorization](authorization.md).

# Customize the rejection message

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

Use `SetIntrospectionNotAllowedMessage(...)` when you need a product-specific message:

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

Keep production messages neutral. Do not reveal required roles, internal endpoint names, network ranges, or credential formats in the error text.

# Limit recursive introspection depth

Disabling introspection decides whether metadata queries are allowed. Depth limits decide how much recursive introspection work is accepted when introspection is allowed.

Hot Chocolate v16 defaults are:

| Limit                          | Default |
| ------------------------------ | ------- |
| `MaxAllowedOfTypeDepth`        | `16`    |
| `MaxAllowedListRecursiveDepth` | `1`     |

Tune these limits only after you understand your tooling requirements:

```csharp
builder
    .AddGraphQL()
    .SetIntrospectionAllowedDepth(
        maxAllowedOfTypeDepth: 8,
        maxAllowedListRecursiveDepth: 1);
```

Use [Execution depth and limits](execution-depth-and-limits.md) for the full request limit model.

# Separate introspection from SDL and Nitro

Runtime introspection, SDL download, Nitro hosting, and schema export are separate controls. This distinction is the most common source of production surprises.

`MapGraphQL()` can serve the GraphQL endpoint, Nitro, and SDL downloads below the same path. Disabling runtime introspection does not automatically disable `GET /graphql?sdl`, schema file support, or Nitro hosting.

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

Configure Nitro separately too:

```csharp
app.MapGraphQL()
    .WithOptions(o => o.Tool.Enable = app.Environment.IsDevelopment());
```

If production Nitro needs schema metadata, provide an allowed request path, an authenticated non-production endpoint, or a schema artifact or registry workflow. Do not assume that disabling Nitro blocks introspection, and do not assume that disabling introspection blocks Nitro hosting.

See [Endpoints](../server-configuration/endpoints.md) for complete endpoint setup.

# Provide schema access without production introspection

Private and restricted APIs often need schema metadata for development, CI, and client generation without exposing live production introspection.

A common workflow is:

1. Export the schema during CI.
2. Publish the SDL artifact to client builds, documentation, or a schema registry.
3. Generate clients and validate operations against that artifact.
4. Deploy production with runtime introspection restricted.
5. Allow developer access through authenticated non-production endpoints or explicit request overrides.

```bash
dotnet run -- schema export --output schema.graphql
```

Use [Command Line](../server-configuration/command-line.md) for schema export details.

# Introspection and trusted documents

Trusted documents and introspection solve different problems.

| Control           | Question it answers                                                                       |
| ----------------- | ----------------------------------------------------------------------------------------- |
| Trusted documents | Can this client execute arbitrary operation text, or only registered operation documents? |
| Introspection     | Can this request query runtime schema metadata?                                           |

For private first-party clients, combine them:

- Use trusted documents or persisted operation routes for known client operations.
- Disable runtime introspection in production unless there is an intentional exception path.
- Export schema artifacts for CI and client generation.
- Keep field and endpoint authorization in place for protected data.

Continue with [Trusted documents](../../performance/trusted-documents.md) and [Automatic persisted operations](../../performance/automatic-persisted-operations.md).

# Choose a strategy

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

# When disabling introspection is not enough

Do not use introspection settings as your only protection for sensitive data or operations. Add the controls that match the actual threat:

- Authentication and authorization for users, tenants, fields, mutations, and ownership checks.
- Cost analysis and depth limits for expensive operation shapes.
- Trusted documents when clients should not send arbitrary operation text.
- Endpoint controls for Nitro, SDL downloads, GET, multipart uploads, batching, CORS, cookies, and WebSockets.
- Neutral error messages and server-side logging for incidents.
- Schema release processes for client generation, schema checks, and documentation.

# API quick reference

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

# Next steps

- [Security overview](index.md)
- [Execution depth and limits](execution-depth-and-limits.md)
- [Cost analysis](cost-analysis.md)
- [Authentication](authentication.md)
- [Authorization](authorization.md)
- [Endpoints](../server-configuration/endpoints.md)
- [Interceptors](../../server/interceptors.md)
- [Command Line](../server-configuration/command-line.md)
