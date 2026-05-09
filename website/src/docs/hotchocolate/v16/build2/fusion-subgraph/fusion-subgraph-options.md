---
title: Fusion subgraph options
---

A Fusion subgraph is a normal Hot Chocolate GraphQL server that exports source schema artifacts for Fusion composition. There is no single "subgraph options" object to configure. A production-ready subgraph uses options in three places:

1. `Program.cs`, where the Hot Chocolate server, endpoint, request behavior, and schema behavior are configured.
2. `schema.graphqls` and `schema-settings.json`, which are exported from the subgraph and consumed by composition.
3. Composition or gateway configuration, where source schemas are merged and the gateway later creates clients for those sources.

This page focuses on the first two places and calls out the boundary when a setting belongs to composition or the gateway.

## Start with the subgraph service

Install the packages used by a typical v16 attribute-based subgraph:

| Package                               | Purpose                                                          |
| ------------------------------------- | ---------------------------------------------------------------- |
| `HotChocolate.AspNetCore`             | Hosts the GraphQL endpoint.                                      |
| `HotChocolate.AspNetCore.CommandLine` | Adds `schema export`, `schema print`, and `schema list`.         |
| `HotChocolate.Types.Analyzers`        | Generates `AddTypes()` registration for attribute-based schemas. |

Configure the service with a stable source schema name.

```csharp
var builder = WebApplication.CreateBuilder(args);

builder
    .AddGraphQL("Products")
    .AddTypes();

var app = builder.Build();

app.MapGraphQL();

return await app.RunWithGraphQLCommandsAsync(args);
```

The important calls are:

| API                                 | What it configures                                                                                                                                        |
| ----------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `AddGraphQL("Products")`            | Names the Hot Chocolate schema. The exported source schema settings use this name.                                                                        |
| `AddTypes()`                        | Registers generated types and generated module code. When Fusion source-schema metadata is detected, the generated module applies source schema defaults. |
| `MapGraphQL()`                      | Exposes the HTTP endpoint. The default path is `/graphql`.                                                                                                |
| `RunWithGraphQLCommandsAsync(args)` | Enables command-line schema commands and returns their exit code.                                                                                         |

If you use `builder.Services.AddGraphQLServer("Products")` in an older or service-collection based setup, pass the same stable name there. For new ASP.NET Core examples, prefer `builder.AddGraphQL("Products")`.

Do not rely on the unnamed schema for a Fusion subgraph. The source schema name is part of the composition contract, so choose one service name, keep it unique, and keep it stable across releases.

## Add source schema defaults when registering types manually

Attribute-based projects that call `AddTypes()` get generated registration code. When the generator detects source-schema root type metadata, it calls `AddSourceSchemaDefaults()` for you.

If you configure a request executor manually and do not use the generated module, apply the defaults yourself:

```csharp
builder
    .AddGraphQL("Products")
    .AddSourceSchemaDefaults()
    .AddQueryType<ProductQueries>();
```

`AddSourceSchemaDefaults()` registers the schema as a source schema. It also applies defaults such as shareable connection, `PageInfo`, node field, and scalar serialization metadata. It is server-side setup, not a replacement for lookup attributes or exported settings.

## Configure endpoint and request behavior in `Program.cs`

The gateway calls your subgraph endpoint at runtime. Server options control what that endpoint accepts. Source settings describe those capabilities to composition and the gateway.

```csharp
builder
    .AddGraphQL("Products")
    .AddTypes()
    .ModifyRequestOptions(o =>
    {
        o.IncludeExceptionDetails = builder.Environment.IsDevelopment();
    })
    .ModifyParserOptions(o =>
    {
        o.MaxAllowedFields = 2048;
    });
```

| API                    | Subgraph impact                                                                                                                                                                                                 |
| ---------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `ModifyRequestOptions` | Controls execution behavior, for example exception details and execution timeout. Gate exception details by environment.                                                                                        |
| `ModifyServerOptions`  | Controls endpoint behavior such as schema requests, GET requests, multipart requests, and batching. Source schema defaults enable variable and request batching, so use this for deliberate endpoint overrides. |
| `ModifyParserOptions`  | Controls parser limits for direct client traffic and gateway-to-subgraph traffic.                                                                                                                               |
| `ModifyOptions`        | Controls schema construction. Root type names, visibility, validation, and unreachable type removal can change exported SDL.                                                                                    |

If you map a non-default endpoint, the exported settings URL must use the same path:

```csharp
app.MapGraphQL("/api/graphql");
```

```json
{
  "name": "Products",
  "transports": {
    "http": {
      "url": "https://products.internal/api/graphql"
    }
  }
}
```

## Export the source schema and settings

Run `schema export` from your repository or solution root:

```bash
dotnet run --project ./Products -- schema export
```

The default output is:

```text
schema.graphqls
schema-settings.json
```

Use `--output` in scripts so paths are predictable:

```bash
dotnet run --project ./Products -- schema export --output ./Products/schema.graphqls
```

Output path rules:

| `--output` value                             | Result                                                     |
| -------------------------------------------- | ---------------------------------------------------------- |
| Not specified                                | Writes `schema.graphqls` in the current working directory. |
| Existing directory, for example `./Products` | Writes `schema.graphqls` inside that directory.            |
| File ending in `.graphql` or `.graphqls`     | Uses that file name.                                       |
| Other file name                              | Appends `.graphqls`.                                       |

The settings file uses the schema file base name. For example, `./Products/products.graphqls` creates `./Products/products-settings.json`.

Create parent directories before exporting. The export command should not be treated as a directory creator.

Use `--schema-name` when the app registers more than one schema:

```bash
dotnet run --project ./Products -- schema export --schema-name Products --output ./Products/schema.graphqls
```

`--schema-name` selects the Hot Chocolate schema to export. It is not an environment name.

Use `--semantic-non-null` only when a downstream tool expects SDL with `@semanticNonNull` annotations:

```bash
dotnet run --project ./Products -- schema export --semantic-non-null
```

## Understand generated defaults

A new settings file contains the schema name and an HTTP transport URL:

```json
{
  "name": "Products",
  "transports": {
    "http": {
      "url": "http://localhost:5000/graphql"
    }
  }
}
```

The generated URL always starts as `http://localhost:5000/graphql`. Change it to the endpoint the gateway can reach for the environment you compose for.

Verified defaults for v16:

| Area                                     | Default                                                                                  |
| ---------------------------------------- | ---------------------------------------------------------------------------------------- |
| Unnamed Hot Chocolate schema             | Uses the Hot Chocolate default schema name. Name Fusion subgraphs explicitly.            |
| Exported schema file                     | `schema.graphqls` in the current working directory.                                      |
| Exported settings file                   | `<schema-file-base>-settings.json`, for example `schema-settings.json`.                  |
| Generated HTTP URL                       | `http://localhost:5000/graphql`.                                                         |
| Gateway HTTP client name                 | `fusion` when `clientName` is omitted.                                                   |
| Gateway HTTP capabilities                | Variable batching and request batching are advertised when not disabled.                 |
| Gateway supported operations             | Queries, mutations, and subscriptions are supported when subscriptions are not disabled. |
| `capabilities.onError`                   | Not set when omitted. Supported values are `Null` and `Propagate`.                       |
| Source schema parser validation          | Enabled when not disabled in source schema settings.                                     |
| Source schema preprocessor validation    | Enabled when not disabled in source schema settings.                                     |
| Infer keys from lookups                  | Enabled by default.                                                                      |
| Inherit interface keys                   | Enabled by default.                                                                      |
| Composition global object identification | Disabled unless enabled during composition.                                              |
| Satisfiability paths                     | Disabled unless enabled during composition.                                              |

## Maintain `schema-settings.json`

`schema-settings.json` is composition input. Composition moves each source settings object under `sourceSchemas.<name>` in gateway settings and removes source-only fields such as `name` and `environments`. The gateway then uses the settings for each source schema to create transport clients.

The default gateway parser understands the `transports.http` shape.

| Setting                                                  | Required                        | Guidance                                                                                                   |
| -------------------------------------------------------- | ------------------------------- | ---------------------------------------------------------------------------------------------------------- |
| `name`                                                   | Yes                             | Unique, non-empty source schema name. Keep it aligned with `AddGraphQL("Products")`.                       |
| `transports.http.url`                                    | Yes for the default HTTP parser | GraphQL endpoint URL the gateway can reach after composition.                                              |
| `transports.http.clientName`                             | No                              | Named `HttpClient` for the gateway. Omit it to use `fusion`.                                               |
| `transports.http.capabilities.standard.formats`          | No                              | Accept header media types for single non-subscription operations. Omit to use built-in defaults.           |
| `transports.http.capabilities.batching.variableBatching` | No                              | Set `false` only when the endpoint does not support variable batching.                                     |
| `transports.http.capabilities.batching.requestBatching`  | No                              | Set `false` only when the endpoint does not support request batching.                                      |
| `transports.http.capabilities.batching.formats`          | No                              | Accept header media types for batched responses. Omit to use built-in defaults.                            |
| `transports.http.capabilities.subscriptions.supported`   | No                              | Set `false` when this HTTP transport does not support subscriptions.                                       |
| `transports.http.capabilities.subscriptions.formats`     | No                              | Accept header media types for subscriptions. Omit to use built-in defaults.                                |
| `transports.http.capabilities.onError`                   | No                              | `Null` or `Propagate`. Parsing is case-insensitive.                                                        |
| `environments`                                           | No                              | Values for composition-time interpolation. This section is not emitted to gateway settings.                |
| `extensions`                                             | No                              | Custom metadata preserved through composition. Use only for tools that document their own extension shape. |

Other transport blocks can be preserved in settings, but the built-in fallback parser claims source schemas only when `transports.http` is present. A different transport needs gateway code or tooling that understands that transport block.

### Local settings

```json
{
  "name": "Products",
  "transports": {
    "http": {
      "url": "http://localhost:5001/graphql"
    }
  }
}
```

Use the port and path your service actually listens on.

### Environment interpolation

Use `{{VARIABLE_NAME}}` in settings and provide values under `environments`:

```json
{
  "name": "Products",
  "transports": {
    "http": {
      "url": "{{PRODUCTS_URL}}/graphql"
    }
  },
  "environments": {
    "development": {
      "PRODUCTS_URL": "http://localhost:5001"
    },
    "production": {
      "PRODUCTS_URL": "https://products.internal"
    }
  }
}
```

Compose with the matching environment:

```bash
nitro fusion compose \
  --source-schema-file ./Products/schema.graphqls \
  --archive ./gateway.far \
  --environment production
```

If a value is a pure variable reference, composition preserves the JSON value type. That means booleans and numbers can remain booleans and numbers after interpolation.

### Capability overrides

Keep the advertised capabilities in sync with the endpoint behavior. For example, disable request batching, variable batching, and subscriptions when the endpoint does not support them:

```json
{
  "name": "Products",
  "transports": {
    "http": {
      "url": "https://products.internal/graphql",
      "clientName": "products-client",
      "capabilities": {
        "batching": {
          "requestBatching": false,
          "variableBatching": false
        },
        "subscriptions": {
          "supported": false
        },
        "onError": "Propagate"
      }
    }
  }
}
```

Remember the split:

| Need                                                 | Configure                                                                                                                             |
| ---------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------- |
| Make the subgraph endpoint accept batching           | Usually `AddSourceSchemaDefaults()` through generated `AddTypes()`. Use `ModifyServerOptions` only for deliberate endpoint overrides. |
| Tell the gateway not to use batching for that source | `transports.http.capabilities.batching` in `schema-settings.json`.                                                                    |

## Entity lookup behavior

Lookup behavior is mostly schema metadata, not an option. Add lookup metadata to resolvers and let composition infer keys from that metadata.

```csharp
using HotChocolate.Types.Composite;

namespace Products.Types;

public sealed class Product
{
    public int Id { get; init; }

    public required string Name { get; init; }
}

[QueryType]
public static partial class ProductQueries
{
    [Lookup]
    public static Task<Product?> GetProductByIdAsync(
        int id,
        IProductByIdDataLoader productById,
        CancellationToken cancellationToken)
        => productById.LoadAsync(id, cancellationToken);
}
```

Lookup rules:

| Rule                                                                     | Why it matters                                                                                |
| ------------------------------------------------------------------------ | --------------------------------------------------------------------------------------------- |
| Return a nullable single entity, such as `Product?` or `Task<Product?>`. | Missing keys can return `null` without failing the whole request.                             |
| Do not return a list or array.                                           | A lookup resolves one entity for one key shape.                                               |
| Use `[Is]` when argument names or field paths differ.                    | Composition needs to map lookup arguments to entity key fields.                               |
| Use `[Internal]` for gateway-only lookups.                               | Internal lookups stay available for planning but are hidden from the public composite schema. |

```csharp
using HotChocolate.Types.Composite;

public sealed record Product(int Id);

[QueryType]
public static partial class ReviewProductLookups
{
    [Lookup, Internal]
    public static Product? GetProductById(int id)
        => new(id);
}
```

Two advanced source-schema settings affect lookup preprocessing during composition:

| Setting                             | Default | Effect                                                                              |
| ----------------------------------- | ------- | ----------------------------------------------------------------------------------- |
| `preprocessor.inferKeysFromLookups` | `true`  | Applies inferred key directives to types returned by lookup fields.                 |
| `preprocessor.inheritInterfaceKeys` | `true`  | Applies key directives to object types from keys defined on implemented interfaces. |

Keep both defaults unless you have a composition-specific reason to change how the source schema is interpreted. Visibility, argument mapping, and field ownership should be expressed with attributes or descriptors in the schema, not by turning lookup inference off.

## Composition-related settings boundary

Composition options belong to the composition command, composition settings, or registry workflow. They are not subgraph server options.

| Setting or option                           | Default | Configure it when                                                                   |
| ------------------------------------------- | ------- | ----------------------------------------------------------------------------------- |
| `--enable-global-object-identification`     | `false` | The composed graph should include Relay-style global object identification fields.  |
| `--include-satisfiability-paths`            | `false` | You want more detailed satisfiability diagnostics.                                  |
| `--exclude-by-tag <tag>`                    | None    | Composition should exclude members with specific tags.                              |
| `merger.addFusionDefinitions`               | `true`  | Advanced composition needs to control Fusion definitions in the merged schema.      |
| `merger.removeUnreferencedDefinitions`      | `true`  | Advanced composition needs to keep otherwise unreferenced definitions.              |
| `satisfiability.ignoredNonAccessibleFields` | Empty   | A known non-accessible field path should be ignored during satisfiability analysis. |

Keep these settings near composition automation unless your tooling requires them in source schema configuration. A subgraph owner usually changes schema metadata and exported settings, then lets the composition pipeline own merge policy.

## Practical recipes

### Minimal Products subgraph

1. Name the schema in `Program.cs`.
2. Add generated type registration.
3. Export the schema.
4. Edit the generated URL.

```csharp
var builder = WebApplication.CreateBuilder(args);

builder
    .AddGraphQL("Products")
    .AddTypes();

var app = builder.Build();

app.MapGraphQL();

return await app.RunWithGraphQLCommandsAsync(args);
```

```bash
dotnet run --project ./Products -- schema export --output ./Products/schema.graphqls
```

Expected files:

```text
Products/schema.graphqls
Products/schema-settings.json
```

### Development and production URLs

Use direct local URLs for local composition and environment interpolation for shared artifacts:

```json
{
  "name": "Products",
  "transports": {
    "http": {
      "url": "{{PRODUCTS_URL}}/graphql"
    }
  },
  "environments": {
    "development": {
      "PRODUCTS_URL": "http://localhost:5001"
    },
    "production": {
      "PRODUCTS_URL": "https://products.internal"
    }
  }
}
```

Then compose for the target environment:

```bash
nitro fusion compose \
  --source-schema-file ./Products/schema.graphqls \
  --environment development
```

### Development exception details

```csharp
builder
    .AddGraphQL("Products")
    .AddTypes()
    .ModifyRequestOptions(o =>
    {
        o.IncludeExceptionDetails = builder.Environment.IsDevelopment();
    });
```

Keep production exception detail output disabled unless your operational policy explicitly enables it.

## Test exported schema and settings

Use the command-line tools as part of local review and CI.

```bash
dotnet restore
dotnet build --no-restore
dotnet run --project ./Products --no-build -- schema export --schema-name Products --output ./Products/schema.graphqls
git diff --exit-code -- ./Products/schema.graphqls ./Products/schema-settings.json
```

Inspect the exported SDL for expected source schema metadata:

```bash
grep -E -n "@lookup|@internal|@key|@shareable" ./Products/schema.graphqls
```

Validate the settings file shape before composition:

```bash
python3 - <<'PY'
import json
from pathlib import Path
settings = json.loads(Path("Products/schema-settings.json").read_text())
assert settings["name"] == "Products"
assert settings["transports"]["http"]["url"].endswith("/graphql")
PY
```

Then run composition with the exported source schema files:

```bash
nitro fusion compose \
  --source-schema-file ./Products/schema.graphqls \
  --archive ./gateway.far \
  --environment development
```

## Use descriptors or attributes instead of settings for schema shape

`schema-settings.json` describes a source schema for composition and transport. It does not rename fields, hide fields, define ownership, or add lookup paths.

Use attributes or descriptors when you need to change schema shape:

| Need                                                             | Use                                                                    |
| ---------------------------------------------------------------- | ---------------------------------------------------------------------- |
| Add a lookup path                                                | `[Lookup]` or the equivalent type descriptor configuration.            |
| Hide a gateway-only lookup                                       | `[Internal]` on the lookup field or containing internal lookup object. |
| Map lookup arguments to fields                                   | `[Is]` on the lookup argument.                                         |
| Declare resolver data requirements                               | `[Require]` on resolver parameters.                                    |
| Hide data from clients while keeping it available to composition | `[Inaccessible]`.                                                      |
| Rename, ignore, bind, or retype fields                           | Object type descriptors or schema descriptors.                         |

For example, use a descriptor when field binding needs to stay centralized:

```csharp
using HotChocolate.Types;

public sealed class InventoryProduct
{
    public int Id { get; init; }

    public string? InternalSku { get; init; }

    public required string Name { get; init; }
}

public sealed class InventoryProductType : ObjectType<InventoryProduct>
{
    protected override void Configure(IObjectTypeDescriptor<InventoryProduct> descriptor)
    {
        descriptor.Field(t => t.InternalSku).Ignore();
        descriptor.Field(t => t.Name).Name("displayName");
    }
}
```

After changing attributes or descriptors, export again and review the SDL before composing.

## Troubleshooting

| Symptom                                                 | Likely cause                                                                                                                | Fix                                                                                                                   |
| ------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------- |
| `schema-settings.json` uses a generic schema name       | The schema was exported without a stable name or the wrong schema was selected.                                             | Use `AddGraphQL("Products")`, rerun export, and pass `--schema-name Products` for multi-schema apps.                  |
| Composition reports duplicate source schema names       | Two settings files use the same `name`.                                                                                     | Give each subgraph a unique, stable name and export again.                                                            |
| Gateway cannot reach a subgraph                         | `transports.http.url` points to `localhost`, the wrong port, or the wrong path for the gateway environment.                 | Use a gateway-reachable URL or environment interpolation.                                                             |
| Composition cannot enter a subgraph for an entity field | The subgraph contributes fields but has no compatible lookup path.                                                          | Add a nullable single-entity `[Lookup]`. Use `[Internal]` for gateway-only lookups.                                   |
| Lookup arguments do not match entity keys               | Argument names or paths differ from the entity key fields.                                                                  | Add `[Is]` to map arguments, or use explicit key metadata when inference is not enough.                               |
| A lookup appears in the public composite schema         | The lookup was not marked internal.                                                                                         | Add `[Internal]` to gateway-only lookup fields.                                                                       |
| Batching fails at runtime                               | Settings advertise batching but the endpoint does not accept it, or the endpoint accepts batching but settings disabled it. | Align `ModifyServerOptions` with `transports.http.capabilities.batching`.                                             |
| Subscriptions fail through the gateway                  | Settings advertise subscriptions for a transport that does not support them.                                                | Set `transports.http.capabilities.subscriptions.supported` to `false` or enable subscription support on the endpoint. |
| Production responses expose exception details           | Development request options were deployed.                                                                                  | Gate `IncludeExceptionDetails` by environment.                                                                        |
| `subgraph-config.json` appears in older samples         | Older configuration shape or workshop material was copied.                                                                  | Use `schema-settings.json`. The Nitro CLI has a `nitro fusion migrate subgraph-config` command for migration.         |

## Next steps

- Build the subgraph shape with [Fusion subgraphs](/docs/hotchocolate/v16/build2/fusion-subgraph).
- Review schema export commands in [Command line](/docs/hotchocolate/v16/build2/server-configuration/command-line).
- Model entity resolution with [Entities and Lookups](/docs/fusion/v16/entities-and-lookups).
- Learn visibility rules in [Schema Exposure and Evolution](/docs/fusion/v16/schema-exposure-and-evolution).
- Compose source schemas with [Fusion CLI](/docs/fusion/v16/cli) and [Composition](/docs/fusion/v16/composition).
- Use [Options Reference](/docs/hotchocolate/v16/api-reference/options) for general server option defaults.
