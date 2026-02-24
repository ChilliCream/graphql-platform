---
title: "Attribute and Directive Reference"
---

Quick reference for all Fusion-related attributes and their GraphQL directive equivalents. For detailed usage and examples, follow the links to the relevant guide pages.

# Attribute and Directive Reference Table

| Attribute                   | Directive       | Description                                          | Guide Page                                                                                                         |
| --------------------------- | --------------- | ---------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------ |
| `[ObjectType<T>]`           | —               | Maps static class as extension to entity type T      | [Entities and Lookups](/docs/fusion/v16/entities-and-lookups)                                                      |
| `[QueryType]`               | —               | Marks class as contributing Query root fields        | [Getting Started](/docs/fusion/v16/getting-started)                                                                |
| `[MutationType]`            | —               | Marks class as contributing Mutation root fields     | [Getting Started](/docs/fusion/v16/getting-started)                                                                |
| `[SubscriptionType]`        | —               | Marks class as contributing Subscription root fields | [Getting Started](/docs/fusion/v16/getting-started)                                                                |
| `[Lookup]`                  | `@lookup`       | Declares field as entity lookup resolver             | [Entities and Lookups](/docs/fusion/v16/entities-and-lookups)                                                      |
| `[NodeResolver]`            | —               | Marks as Relay node resolver                         | [Entities and Lookups](/docs/fusion/v16/entities-and-lookups)                                                      |
| `[Internal]`                | `@internal`     | Hides lookup from composed schema                    | [Entities and Lookups](/docs/fusion/v16/entities-and-lookups)                                                      |
| `[Shareable]`               | `@shareable`    | Allows multiple subgraphs to resolve field           | [Entities and Lookups](/docs/fusion/v16/entities-and-lookups)                                                      |
| `[Parent(requires: "...")]` | —               | Declares field requirements from parent              | [Entities and Lookups](/docs/fusion/v16/entities-and-lookups)                                                      |
| `[Require("...")]`          | `@require`      | Declares complex field requirements                  | [Getting Started](/docs/fusion/v16/getting-started), [Adding a Subgraph](/docs/fusion/v16/adding-a-subgraph)       |
| `[EntityKey("...")]`        | `@key`          | Declares entity key for resolution                   | [Entities and Lookups](/docs/fusion/v16/entities-and-lookups)                                                      |
| `[BindMember(nameof(...))]` | —               | Replaces raw FK with resolved entity                 | [Adding a Subgraph](/docs/fusion/v16/adding-a-subgraph)                                                            |
| `[Tag("...")]`              | `@tag`          | Applies tag for composition filtering                | [Composition](/docs/fusion/v16/composition)                                                                        |
| `[DataLoader]`              | —               | Source-generates DataLoader interface                | [Getting Started](/docs/fusion/v16/getting-started), [Entities and Lookups](/docs/fusion/v16/entities-and-lookups) |
| `[UsePaging]`               | —               | Enables cursor-based pagination                      | [Getting Started](/docs/fusion/v16/getting-started)                                                                |
| `[ID<T>]`                   | —               | Declares field as Relay-style ID                     | [Entities and Lookups](/docs/fusion/v16/entities-and-lookups)                                                      |
| `[Inaccessible]`            | `@inaccessible` | Hides from composite schema                          | [Composition](/docs/fusion/v16/composition)                                                                        |
| `[Override(from: "...")]`   | `@override`     | Migrates field ownership                             | [Deployment and CI/CD](/docs/fusion/v16/deployment-and-ci-cd)                                                      |
| `[Provides("...")]`         | `@provides`     | Declares locally-resolvable subfields                | [Entities and Lookups](/docs/fusion/v16/entities-and-lookups)                                                      |
| `[External]`                | `@external`     | Field defined by another subgraph                    | [Entities and Lookups](/docs/fusion/v16/entities-and-lookups)                                                      |

# See Also

- **[Getting Started](/docs/fusion/v16/getting-started)** — Introduction to Fusion attributes in practice
- **[Entities and Lookups](/docs/fusion/v16/entities-and-lookups)** — Deep dive into entity resolution
- **[Composition](/docs/fusion/v16/composition)** — How attributes affect schema merging
- **[Nitro CLI Reference](/docs/fusion/v16/nitro-cli-reference)** — Command-line tools for composition
