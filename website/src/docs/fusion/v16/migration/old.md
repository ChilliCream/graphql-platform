---
title: "Migrating from v15 to v16"
---

# Migrating from v15 to v16

<!-- TODO: Preamble about you can't just bump the version because this a redesign of everything, tooling, etc. -->

<!-- TODO: Mention that Nitro needs to be updated to latest version when self-hosting (10.0.18 at time of writing) -->

<!-- TODO: High level overview over the migration -->

## Per repository migration

<!-- TODO: At the start we want to check for and collect satisfiability issues so we can work on them

exmaple ci output:

Validating Fusion configuration of API 'QXBpCmcwMTlkMmIzMGUzNGY3YzQ2OTBjNTgxOTNkYjI1M2EyZg==' against stage 'dev'
├── Downloading existing configuration from 'dev'
│   └── ✓ Downloaded existing legacy v1 configuration from 'dev'.
├── Composing new configuration
│   └── ✕ Failed to compose new configuration.
└── ✕ Failed to validate the Fusion configuration.

## Composition log

❌ [ERR] Unable to access the field 'Review.productVariant'.
     Unable to transition between schemas 'REVIEWS' and 'PRODUCTS' for access to field 'PRODUCTS:Review.productVariant<Product>'.
       No lookups found for type 'Review' in schema 'PRODUCTS'. (UNSATISFIABLE)
Satisfiability validation failed.

 -->

### Migrate subgraph-config.json

For each subgraph in your repository, the existing `subgraph-config.json` file needs to be migrated to the new `schema-settings.json` format.

You can run the following command in the root of your repository and it will find all `subgraph-config.json` files and automatically convert them into `schema-settings.json` files:

```bash
dnx ChilliCream.Nitro.CommandLine fusion migrate subgraph-config
```

> Note: If you can't use .NET 10 / `dnx` you can also install `ChilliCream.Nitro.CommandLine` via `dotnet tool install` and then invoke it via `dotnet nitro ...`.

If you need to do this conversion manually: Create a `schema-settings.json` file next to each `subgraph-config.json` with the following changes:

```diff
 {
-  "subgraph": "products",
-  "http": {
-    "baseAddress": "http://products/graphql"
-  }
+  "version": "1.0.0",
+  "name": "products",
+  "transports": {
+    "http": {
+      "url": "http://products/graphql"
+    }
+  }
 }
```

> Note: By default the Fusion v2 composition assums your subgraph is compatible with the latest features. By adding `"version": "1.0.0"` we tell the composition that this is a legacy (Fusion v1) subgraph, which relaxes certain validations like `@shareable` and re-creates inferences that were present in Fusion v1, like fields ending in `ById` being inferred as `@lookup`.

<!-- TODO: We still need to check the variable batching performance of v15. -->

If your subgraph is using a version older than the latest HotChocolate v15 or your subgraph uses an entirely different technology, you also need to disable variable batching in `schema-settings.json`.

```diff
 {
   "version": "1.0.0",
   "name": "products",
   "transports": {
     "http": {
-      "url": "http://products/graphql"
+      "url": "http://products/graphql",
+      "capabilities": {
+        "batching": {
+          "variableBatching": false
+        }
+      }
     }
   }
 }
```

<!-- TODO: environments section -->

### Update subgraph

The concept of batch resolvers like `productByIds(ids: [ID!]!)` does no longer exist in Fusion v2. Batching is done on the transport level through [variable and request batching](https://github.com/graphql/graphql-over-http/blob/fb404ac12dde473f3d9f5a1b1026574c7475e1e4/spec/Appendix%20B%20--%20Variable%20Batching.md). This means singular fields like `Query.productById(id: ID!): Product` are invoked with a list of IDs instead of a plural `Query.productsById(ids: [ID!]!): [Product!]` field. Checkout [this GitHub issue](https://github.com/graphql/composite-schemas-spec/issues/25#issue-2173900758) for details on this decision.

Since you don't want multiple invocations of the `Query.productById` field during a single request to hit the database multiple times, you need to ensure your `Query` root fields and `[NodeResolver]` implementations (powering the `Query.node(id: ID!): Node` field) are using [`DataLoader`](/docs/hotchocolate/v16/resolvers-and-data/dataloader). This is a best practice and ensures the performance of your server does not degrade in comparison to the previous batching fields.

If an entity currently only has batch `Query` root fields in your subgraph, you'll also have to add a singular field:

```diff
 type Query {
   productsById(ids: [ID!]!): [Product!] @lookup @internal
+  productByid(id: ID!): Product @lookup @internal
 }
```

Variable and request batching aren't enabled by default, so you also need to update your `Program.cs` to enable it:

```diff
- app.MapGraphQL();
+ app.MapGraphQL().WithOptions(new GraphQLServerOptions { EnableBatching = true })`.
```

If you want to, you can also now [migrate the subgraph to Hot Chocolate v16](#migrate-subgraph-to-v16), but it's not required at this point.

### Migrate workflows

The migration to Fusion v2 is designed to happen one subgraph repository at a time. While some of your subgraphs are still on v15 and others are already on v16, the gateway needs to keep working for both. The workflow changes in this section ensure that both archive formats stay available side-by-side until every subgraph has been migrated and the gateway itself is cut over.

In Fusion v15 each subgraph pipeline produces a Fusion gateway package (`.fgp`) and publishes it back to Nitro as the latest archive. In Fusion v16 the equivalent artifact is the Fusion archive (`.far`). To bridge the two formats during the transition, the v15 compose step is kept in place and the freshly composed `.fgp` is embedded into the published `.far` through the `--legacy-v1-archive` option. v15 gateways continue to download the embedded `.fgp`, v16 gateways download the `.far` directly.

A typical subgraph repository has two workflows that need updating: the **deployment workflow** that publishes the subgraph's archive to Nitro and the **PR validation workflow** that ensures the composed schema introduces no breaking changes. The same transition strategy applies to both, only the final Nitro command changes while the existing v15 download and compose steps stay in place.

#### Deployment workflow

In practice this means three changes to your existing deployment pipeline:

1. **Add** a step in the build job that uploads the source schema to Nitro, so the v16 publish can reference it.
2. **Keep** the v15 compose step in the deploy job. It is still responsible for producing an up-to-date `.fgp`.
3. **Replace** the final `dotnet nitro fusion-configuration publish commit` with `dotnet nitro fusion publish` and pass the freshly composed `.fgp` via `--legacy-v1-archive`.

Below is the existing v15 pipeline for reference:

```bash
# BUILD JOB
dotnet run --project ./src/SubgraphA -- schema export --output schema.graphql
dotnet fusion subgraph pack -w ./src/SubgraphA

# DEPLOY JOB
dotnet fusion subgraph config set http \
  --url <subgraph-url> \
  -c ${{ github.workspace }}/subgraph/subgraph-a.fsp
dotnet nitro fusion-configuration publish begin \
  --tag <tag> \
  --api-id <api-id> \
  --subgraph-name subgraph-a \
  --stage <stage> \
  --api-key <api-key>
dotnet nitro fusion-configuration publish start \
  --api-key <api-key>
dotnet nitro fusion-configuration download \
  --api-id <api-id> \
  --stage <stage> \
  --output-file ./gateway.fgp \
  --api-key <api-key>
dotnet fusion compose \
  -p ./gateway.fgp \
  --enable-nodes \
  -s ${{ github.workspace }}/subgraph
dotnet nitro fusion-configuration publish commit \
  --configuration ./gateway.fgp \
  --api-key <api-key>
```

##### Upload the source schema in the build job

Add a step to the build job that uploads the exported source schema to Nitro. The `tag` is later used by the publish step to find the matching upload.

<PipelineChoiceTabs>
<PipelineChoiceTabs.GitHubAction>

```yaml
- uses: ChilliCream/nitro-fusion-upload@v16
  with:
    tag: <tag>
    api-id: <api-id>
    api-key: <api-key>
    source-schema-files: |
      ./src/SubgraphA/schema.graphql
```

</PipelineChoiceTabs.GitHubAction>
<PipelineChoiceTabs.CLI>

```bash
dotnet nitro fusion upload \
  --tag "<tag>" \
  --api-id "<api-id>" \
  --api-key "<api-key>" \
  --source-schema-file "./src/SubgraphA/schema.graphql"
```

</PipelineChoiceTabs.CLI>
</PipelineChoiceTabs>

> Note: The `dotnet fusion subgraph pack` step is still required while the v15 compose step runs in the deploy job, since v15 composition consumes the `.fsp` archive. It can be removed once the subgraph is migrated to v16 and the v15 compose step is dropped (see [Cleanup](#cleanup)).

##### Replace `publish commit` with `nitro fusion publish` in the deploy job

In the deploy job, leave the existing v15 commands that download the latest `.fgp` and run v15 composition untouched. Only the trailing `dotnet nitro fusion-configuration publish commit` is removed:

<!--
TODO: This should remove all of the steps that deal with the registry. just download and compose
      Discuss this with Pascal if we should just cancel instead to keep the idempotency
 -->

```diff
- dotnet nitro fusion-configuration publish commit \
-   --configuration ./gateway.fgp \
-   --api-key <api-key>
```

Replace it with `dotnet nitro fusion publish`, passing the freshly composed `gateway.fgp` via `--legacy-v1-archive`. This composes a new `.far`, embeds the `.fgp` inside it, and uploads the result as the latest archive.

<PipelineChoiceTabs>
<PipelineChoiceTabs.GitHubAction>

```yaml
- uses: ChilliCream/nitro-fusion-publish@v16
  with:
    tag: <tag>
    stage: <stage>
    api-id: <api-id>
    api-key: <api-key>
    legacy-v1-archive: ./gateway.fgp
    source-schemas: |
      subgraph-a
```

</PipelineChoiceTabs.GitHubAction>
<PipelineChoiceTabs.CLI>

```bash
dotnet nitro fusion publish \
  --tag "<tag>" \
  --stage "<stage>" \
  --api-id "<api-id>" \
  --api-key "<api-key>" \
  --source-schema "subgraph-a" \
  --legacy-v1-archive "./gateway.fgp"
```

</PipelineChoiceTabs.CLI>
</PipelineChoiceTabs>

> Note: `dotnet nitro fusion publish` should run **after** the subgraph application has been deployed. Once it succeeds, the new archive becomes the latest in Nitro and the gateway will start routing traffic against the new schema, so the subgraph must already be reachable at that URL.

> Note: `--legacy-v1-archive` is only required during the transition. Once every subgraph has been migrated to v16 and the gateway has been cut over to consume `.far` directly, the v15 compose step and the `--legacy-v1-archive` option can be removed (see [Cleanup](#cleanup)).

#### PR validation workflow

In addition to the deployment workflow, most subgraph repositories have a PR validation workflow that downloads the latest archive, runs composition with the proposed change, and verifies that the composed schema introduces no breaking changes. Below are the relevant v15 steps for reference:

```bash
dotnet run --project ./src/SubgraphA -- schema export --output schema.graphql
dotnet fusion subgraph pack -w ./src/SubgraphA
dotnet nitro fusion-configuration download \
  --api-id <api-id> \
  --stage <stage> \
  --output-file ./gateway.fgp \
  --api-key <api-key>
dotnet fusion compose \
  --package-file ./gateway.fgp \
  --enable-nodes \
  --subgraph-package-file ./src/SubgraphA/subgraph-a.fsp
dotnet nitro fusion-configuration validate \
  --stage <stage> \
  --api-id <api-id> \
  --configuration ./gateway.fgp \
  --api-key <api-key>
```

As with the deployment workflow, the v15 download and compose steps stay in place during the transition so the v15 composition path keeps being validated. Only the final `dotnet nitro fusion-configuration validate` is replaced by `dotnet nitro fusion validate`. Pass the freshly composed `gateway.fgp` via `--legacy-v1-archive` so the validation also covers the embedded v15 archive:

```diff
- dotnet nitro fusion-configuration validate \
-   --stage <stage> \
-   --api-id <api-id> \
-   --configuration ./gateway.fgp \
-   --api-key <api-key>
```

<PipelineChoiceTabs>
<PipelineChoiceTabs.GitHubAction>

```yaml
- uses: ChilliCream/nitro-fusion-validate@v16
  with:
    stage: <stage>
    api-id: <api-id>
    api-key: <api-key>
    legacy-v1-archive: ./gateway.fgp
    source-schema-files: |
      ./src/SubgraphA/schema.graphql
```

</PipelineChoiceTabs.GitHubAction>
<PipelineChoiceTabs.CLI>

```bash
dotnet nitro fusion validate \
  --stage "<stage>" \
  --api-id "<api-id>" \
  --api-key "<api-key>" \
  --legacy-v1-archive "./gateway.fgp" \
  --source-schema-file "./src/SubgraphA/schema.graphql"
```

</PipelineChoiceTabs.CLI>
</PipelineChoiceTabs>

### Migrate subgraph to v16

<!-- TODO: Link to Hot Chocolate migration guide and mention that the version: 1.0.0 should be removed. Also `AddSourceSchemaDefaults` (TODO: Check if this modifies the batching already) -->

## Migrate gateway

TODO

## Cleanup

Remove `HotChocolate.Fusion.CommandLine` and `ChilliCream.Nitro.CLI` from pipelines.

<!-- TODO: If people don't use our GitHub actions how can they use `ChilliCream.Nitro.CLI` and `ChilliCream.Nitro.CommandLine` side-by-side as both map to `dotnet nitro`? -->
