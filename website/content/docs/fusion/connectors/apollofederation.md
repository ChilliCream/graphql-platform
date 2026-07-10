---
title: "Apollo Federation Connector"
description: "Put a Fusion gateway in front of your existing Apollo Federation subgraphs. Composition auto-detects Apollo Federation SDL and translates @key, @requires, and _entities into the GraphQL Federation model, with no changes to your subgraphs."
---

Fusion supports two subgraph protocols: GraphQL Federation and Apollo Federation. GraphQL Federation is the open specification (formerly the GraphQL Composite Schemas specification) that a Fusion gateway speaks directly. Apollo Federation is Apollo's model for distributed GraphQL. This page explains the Apollo Federation connector, which lets a Fusion gateway run Apollo Federation subgraphs.

The connector lets you put a Fusion gateway in front of existing Apollo Federation subgraphs without changing them. During composition, Fusion reads each subgraph's Apollo Federation SDL, detects Apollo Federation schemas, and translates them into the GraphQL Federation model. At runtime, the gateway speaks Apollo's wire protocol (the `_entities` field with typed representations) to Apollo Federation subgraphs. It speaks the GraphQL Federation protocol through lookup fields to GraphQL Federation subgraphs in the same graph. Your subgraphs keep their existing SDL, `__resolveReference` and reference resolvers, and deployment model.

There is no separate mode to enable. Detection happens per source schema, so one graph can mix Apollo Federation subgraphs (in any language: Apollo Server, HotChocolate.ApolloFederation, graphql-java, and others) with GraphQL Federation subgraphs and compose them into the composed schema.

Use this page when you want to run Apollo Federation subgraphs as they are. To move a subgraph to the GraphQL Federation protocol instead, see [Coming from Apollo Federation](../migration/coming-from-apollo-federation.md). The two protocols interoperate, so you can move one subgraph at a time.

# How the Connector Works

The connector does two things: it translates Apollo Federation schemas at composition time, and it uses Apollo's entity protocol at runtime.

```mermaid
flowchart LR
    Client --> Gateway["Fusion Gateway"]
    Gateway -->|"_entities protocol"| Apollo["Apollo Federation subgraph"]
    Gateway -->|"lookup fields"| GraphQLFed["GraphQL Federation subgraph"]
```

**At composition time**, Fusion inspects each source schema. When a schema carries the Apollo Federation `@link` to `https://specs.apollo.dev/federation`, the composer recognizes it as an Apollo Federation subgraph and runs a translation pass over it.

That pass maps Apollo Federation directives onto the GraphQL Federation model: `@key` becomes lookup fields, `@requires` becomes `@require`, `@external` fields are resolved into the model, and the Relay `node` field becomes a lookup. Fusion removes the Apollo Federation infrastructure types (`_service`, `_entities`, `_Entity`, `_Any`). The result is the composed schema. Clients do not see Apollo Federation directives or infrastructure types.

**At runtime**, when the gateway needs to resolve entity fields from an Apollo Federation subgraph, it calls that subgraph as an Apollo router would. It sends the `_entities(representations: [...])` query with typed representations (`{ __typename, <key fields> }`) and reads the entities back. The gateway enters GraphQL Federation subgraphs through their lookup fields instead. Both paths run inside the same query plan.

Because detection happens per source schema, mixed graphs need no configuration. An Apollo Federation subgraph and a GraphQL Federation subgraph that both contribute fields to `Product` merge into one `Product` type in the composed schema.

# Getting the Subgraph Schema

Composition consumes the subgraph's Apollo Federation SDL in the exact form that the subgraph exposes through Apollo's `_service` field:

```graphql
query {
  _service {
    sdl
  }
}
```

Pass this SDL to the composer unchanged. It is the `@link`-carrying document that still contains the Apollo Federation directives (`@key`, `@requires`, `@external`, and so on).

Translation from Apollo Federation directives to the GraphQL Federation model happens inside composition, so the SDL must reach the composer in that form. A schema that has already been stripped of its Apollo Federation directives cannot be detected as an Apollo Federation schema and cannot be translated.

Save each subgraph's `_service { sdl }` output to a `.graphqls` file, one file per subgraph. Give each file a companion `<name>-settings.json` that tells the gateway how to reach the subgraph at runtime. See the [Schema Settings File Reference](../cli.md#schema-settings-file-reference) for the settings file format.

# Composing an Apollo Federation Subgraph

<!-- PENDING: assumes the federation-gated key-inference suppression fix lands before the preview build; if it does not, replace with the schema-settings InferKeysFromLookups:false workaround -->

Compose Apollo Federation subgraphs the same way you compose GraphQL Federation subgraphs. There is no flag to set. Point `nitro fusion compose` at the source schema files, and the composer detects and translates Apollo Federation subgraphs automatically:

```bash
nitro fusion compose \
  --source-schema-file ./accounts/schema.graphqls \
  --source-schema-file ./reviews/schema.graphqls \
  --archive gateway.far
```

You can list Apollo Federation and GraphQL Federation source schema files in the same command. Composition produces a single `.far` archive that contains the composed schema. If a subgraph uses an Apollo Federation feature that Fusion does not yet support, composition fails with a specific error code (see [Current Limitations](#current-limitations)).

For the full command reference, see [nitro fusion compose](../cli.md#nitro-fusion-compose). For the composition rules and the complete log-code list, see [Composition](../composition.md).

# What Translates to What

Composition resolves Apollo Federation constructs before clients see the schema. The composed schema follows the GraphQL Federation model, so clients and downstream tools do not see Apollo Federation directives.

| Apollo Federation                          | Handled during composition as                                                                     |
| ------------------------------------------ | ------------------------------------------------------------------------------------------------- |
| `@key(fields: "...")`                      | Generated lookup fields, one per key. Single, composite, and nested keys are supported.           |
| `_entities` / `__resolveReference`         | Kept as the runtime contract. The gateway calls them over the `_entities` protocol like a router. |
| `@requires(fields: "...")`                 | Translated to `@require`, including nested object requirements and requirement chains.            |
| `@provides(fields: "...")`                 | Honored. The `@external` fields it references are kept resolvable from the providing subgraph.    |
| `@external`                                | External fields are resolved into the model.                                                      |
| `@shareable`                               | Preserved. Key fields are marked shareable automatically.                                         |
| `@override(from: "...")`                   | Preserved as override semantics.                                                                  |
| `@inaccessible`                            | Preserved.                                                                                        |
| `@tag`                                     | Preserved.                                                                                        |
| Union members contributed across subgraphs | Merged into a single union in the composed schema.                                                |

# Global Object Identification

If your Apollo Federation subgraphs implement the Relay `node` field (a `Query.node(id: ID!): Node` field over a `Node` interface), the connector turns it into a lookup during composition. Keep the Apollo Federation SDL as exported. You do not need to add Fusion's `@lookup` directive to the field.

## Configure node resolution

Choose how the gateway resolves `node(id:)` when you compose the gateway archive. Fusion records the mode in the execution schema, so every compatible gateway that loads the archive uses the same behavior.

| CLI value       | Execution-schema value | Behavior                                                                                                                                                                                                                                                                      |
| --------------- | ---------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `gateway`       | `GATEWAY`              | The gateway decodes the ID, determines the object type, and routes the lookup to the source schema that owns that type. This is the default.                                                                                                                                  |
| `source-schema` | `SOURCE_SCHEMA`        | The gateway forwards the opaque ID to a source schema with a public root `Query.node(id: ID!): Node` lookup. That source schema determines the object type and can resolve the concrete `Node` implementations that it declares. Use this mode when the gateway cannot decode your identifiers. |

To use source-schema resolution, enable Global Object Identification and select the mode in the compose command:

```bash
nitro fusion compose \
  --source-schema-file ./accounts/schema.graphqls \
  --source-schema-file ./reviews/schema.graphqls \
  --archive gateway.far \
  --enable-global-object-identification \
  --node-resolution source-schema
```

To update an existing archive, enable Global Object Identification before changing the node-resolution setting:

```bash
nitro fusion settings set global-object-identification true \
  --archive gateway.far

nitro fusion settings set node-resolution source-schema \
  --archive gateway.far
```

For the settings command reference, see [nitro fusion settings set](../cli.md#nitro-fusion-settings-set). If you compose through Aspire, set `EnableGlobalObjectIdentification` to `true` and `NodeResolution` to `NodeResolution.SourceSchema` in `GraphQLCompositionSettings`. See [Composition settings](../aspire-integration.md#composition-settings).

## Verify node resolution

After a successful composition, Nitro prints the archive path:

```text
✅ Composite schema written to '/absolute/path/to/gateway.far'.
```

The generated execution schema should also contain:

```graphql
schema @fusion__execution(nodeResolution: SOURCE_SCHEMA) {
  query: Query
}
```

Deploy a `SOURCE_SCHEMA` archive only to compatible gateways that support the `@fusion__execution` metadata. An older gateway can fall back to gateway-side ID decoding.

## Troubleshoot source-schema resolution

If composition reports `Source-schema node resolution requires global object identification to be enabled.`, you selected `source-schema` without enabling Global Object Identification. Add both `--enable-global-object-identification` and `--node-resolution source-schema` to the compose command.

Source-schema resolution covers only concrete `Node` implementations declared by a source schema with a public root `Query.node(id: ID!): Node` lookup. If a valid dispatcher covers some implementations but another composite `Node` type is uncovered, composition emits an `UNSATISFIABLE_QUERY_PATH` warning. A request for an uncovered type can return `node: null`, with an error if the source resolver reports one. Add the type as a `Node` implementation in a source schema with a public root node lookup. If no public root dispatcher covers any `Node` implementation, composition fails.

See [GraphQL Global Object Identification](../entities-and-lookups.md#graphql-global-object-identification) for the required schema shape and the [composition log-code reference](../composition.md#log-codes-reference) for diagnostic details.

# Runtime Behavior

The gateway uses Apollo's entity protocol when it routes to Apollo Federation subgraphs.

**Entity batching.** When the gateway needs several entities of the same type from one subgraph, it sends one `_entities` call with all representations in the `representations` array. Identical representations are de-duplicated.

When a query plan needs several different lookups from the same subgraph, the gateway dispatches them together as one batched request.

**Requirement threading.** When a field on one subgraph depends on data owned by another (the `@requires` case), the gateway resolves the required fields first. It then threads them into the representation it sends to the subgraph that needs them.

**Error propagation.** Errors returned by a subgraph and transport failures that prevent the gateway from reaching it are attached to the affected result paths and surfaced in the gateway response.

# Current Limitations

The connector is under active development and ships as a preview. Composition rejects unsupported Apollo Federation features with a specific error code, so the gateway does not produce a schema that would misbehave at runtime.

- **Apollo Federation v1 is not supported.** A subgraph must be an Apollo Federation v2 schema, which means it carries a `@link` to `https://specs.apollo.dev/federation`. A v1 subgraph (which has no such `@link`) is rejected with `FEDERATION_V1_NOT_SUPPORTED`. Upgrade the subgraph to Apollo Federation v2.
- **Several Apollo Federation v2 directives are not supported.** Composition rejects `@interfaceObject`, `@composeDirective`, `@authenticated`, `@requiresScopes`, and `@policy` with `FEDERATION_DIRECTIVE_NOT_SUPPORTED`. Remove the directive, or express the equivalent with a GraphQL Federation construct.

Both error codes are listed in the [Composition log-code reference](../composition.md#log-codes-reference).

Feature support tracks the [GraphQL Hive federation-gateway-audit](https://github.com/graphql-hive/federation-gateway-audit) compliance suite, and the set of supported features grows as the connector passes more of that suite.

# Relationship to Migrating

The connector runs your Apollo Federation subgraphs as they are. It does not rewrite them. When you want to move a subgraph to the GraphQL Federation protocol (`[Lookup]` in place of `@key`, `[Require]` in place of `@requires`, and so on), see [Coming from Apollo Federation](../migration/coming-from-apollo-federation.md).

You do not have to choose one protocol for the whole graph. Apollo Federation and GraphQL Federation subgraphs compose together, so you can put the gateway in front of your existing Apollo Federation fleet first. Then you can move subgraphs to the GraphQL Federation protocol one at a time while the rest keep running unchanged.

# Next Steps

- Compose and run a gateway: [nitro fusion compose](../cli.md#nitro-fusion-compose) and [Getting Started](../getting-started.md).
- Understand entity resolution in the GraphQL Federation model: [Entities and Lookups](../entities-and-lookups.md).
- Move subgraphs to the GraphQL Federation protocol: [Coming from Apollo Federation](../migration/coming-from-apollo-federation.md).
