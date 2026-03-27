TODO: Preamble about you can't just bump the version because this a redesign of everything, tooling, etc.

TODO: Mention that Nitro needs to be updated to latest version when self-hosting

## 1. Prepare subgraph

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

> Note: By default the Fusion composition assums your subgraph is compatible with the latest features. By adding `"version": "1.0.0"` we tell the composition that this is a legacy (Fusion v1) subgraph, which relaxes certain validations like `@shareable` and re-creates inferences that were present in Fusion v1, like fields ending in `ById` being inferred as `@lookup`.

The concept of batch resolvers like `productByIds(ids: [ID!]!)` does not exist in Fusion v2. There batching is done on the transport level and fields like `productById(id: ID!)` are invoked multiple times during a single request to resolve a list of products. For this to be efficient your lookups like `productById` need to use DataLoader to batch requests.

You also need to enable this transport batching: `.WithOptions(new GraphQLServerOptions { EnableBatching = true })`.

TODO: For < 15 you also need to set the batchingMode to ApolloRequestBatching in the schema-settings.json.

If you want to, you can also now [migrate the subgraph to Hot Chocolate v16](#x-update-subgraph-to-v16), but it's not required at this point.

## X. Update subgraph to v16

TODO: Link to Hot Chocolate migration guide and mention that the version: 1.0.0 should be removed. Also `AddSourceSchemaDefaults` (TODO: Check if this modifies the batching already)
