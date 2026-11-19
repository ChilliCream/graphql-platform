---
title: Migrate Hot Chocolate Fusion from 16.7 to 16.8
description: "Migration guide for Hot Chocolate Fusion v16.7 to v16.8: review code that assumes non-empty selection sets, now that the gateway accepts empty selection sets by default."
---

Update every Hot Chocolate Fusion package in the application to version 16.8 before applying these changes.

# Deprecations

## EnableEmptySelectionSets deprecated

`FusionOptions.EnableEmptySelectionSets` and `ValidationOptions.EnableEmptySelectionSets` are obsolete and will be removed in version 17. Remove settings that set them to `true`, which is now the default, see [Empty selection sets are valid by default](#empty-selection-sets-are-valid-by-default).

# Behavioral breaking changes

## Empty selection sets are valid by default

The gateway accepts empty selection sets (`{ }`) on composite fields, on fragments, and on query and mutation operations, as the [September 2026 edition](https://spec.graphql.org/September2026/) of the GraphQL specification allows. In 16.7, `FusionOptions.EnableEmptySelectionSets` defaulted to `false`, and such an operation failed validation. A field with an empty selection set resolves to an empty object, such as `{ "me": {} }`, and an empty query or mutation returns `{ "data": {} }`. An empty subscription operation is still invalid.

The gateway selects `__typename` on the source schema in place of an empty selection set, so subgraphs need no update.

To keep rejecting empty selection sets, for example while other tools that process the same documents reject them, set the obsolete option to `false`:

```csharp
builder
    .AddGraphQLGateway()
    .ModifyOptions(o => o.EnableEmptySelectionSets = false);
```
