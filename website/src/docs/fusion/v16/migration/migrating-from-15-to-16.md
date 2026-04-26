---
title: "Migrating from v15 to v16"
---

# Migrating from v15 to v16

<!-- TODO: Preamble about you can't just bump the version because this a redesign of everything, tooling, etc. -->

<!-- TODO: Mention that Nitro needs to be updated to latest version when self-hosting (10.0.18 at time of writing) -->

<!-- TODO: High level overview over the migration -->

## 1. Prepare subgraph

### Migrate subgraph-config.json

Next we need to migrate the `subgraph-config.json` file to the new `schema-settings.json` file.

You can run the following command in the root of your repository and it will find all `subgraph-config.json` files and automatically convert them into `schema-settings.json` files:

```bash
dnx ChilliCream.Nitro.CommandLine fusion migrate subgraph-config
```

> Note: If you can't use .NET 10 / `dnx` you can also install `ChilliCream.Nitro.CommandLine` via `dotnet tool install` and then invoke it via `dotnet nitro ...`.

If you need to do this conversion manully: Create a `schema-settings.json` file next to each `subgraph-config.json` with the following changes:

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

### Update subgraph

The concept of batch resolvers like `productByIds(ids: [ID!]!)` does no longer exist in Fusion v2. Batching is done on the transport level through [variable and request batching](https://github.com/graphql/graphql-over-http/blob/fb404ac12dde473f3d9f5a1b1026574c7475e1e4/spec/Appendix%20B%20--%20Variable%20Batching.md). This means singular fields like `Query.productById(id: ID!): Product` are invoked with a list of IDs instead of a plural `Query.productsById(ids: [ID!]!): [Product!]` field. Checkout [this GitHub issue](https://github.com/graphql/composite-schemas-spec/issues/25#issue-2173900758) for details on this decision.

<!-- TODO: DataLoader should be a link to documentation -->

Since you don't want multiple invocations of the `Query.productById` field during a single request to hit the database multiple times, you need to ensure your `Query` root fields and `[NodeResolver]` implementations (powering the `Query.node(id: ID!): Node` field) are using `DataLoader`. This is a best practice and ensures the performance of your server does not degrade in comparison to the previous batching fields.

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

If you want to, you can also now [migrate the subgraph to Hot Chocolate v16](#x-update-subgraph-to-v16), but it's not required at this point.

### Update CI / CD

TODO

## Migrate gateway

TODO

## X. Update subgraph to v16

<!-- TODO: Link to Hot Chocolate migration guide and mention that the version: 1.0.0 should be removed. Also `AddSourceSchemaDefaults` (TODO: Check if this modifies the batching already) -->

<!-- TODO: Also make sure to describe how to update `nitro fusion validate` pipeline validation on a per-repo basis -->

## Cleanup

Remove `HotChocolate.Fusion.CommandLine` and `ChilliCream.Nitro.CLI` from pipelines.

<!-- TODO: If people don't use our GitHub actions how can they use `ChilliCream.Nitro.CLI` and `ChilliCream.Nitro.CommandLine` side-by-side as both map to `dotnet nitro`? -->

## Example

You can publish your composed schema using either the GitHub Action or the CLI directly.

<PipelineChoiceTabs>
<PipelineChoiceTabs.GitHubAction>

```yaml
- name: Publish schema
  uses: chillicream/nitro-action@v1
  with:
    command: fusion schema publish
    api-id: ${{ secrets.NITRO_API_ID }}
    api-key: ${{ secrets.NITRO_API_KEY }}
```

</PipelineChoiceTabs.GitHubAction>
<PipelineChoiceTabs.CLI>

```bash
dnx ChilliCream.Nitro.CommandLine fusion schema publish \
  --api-id $NITRO_API_ID \
  --api-key $NITRO_API_KEY
```

</PipelineChoiceTabs.CLI>
</PipelineChoiceTabs>
