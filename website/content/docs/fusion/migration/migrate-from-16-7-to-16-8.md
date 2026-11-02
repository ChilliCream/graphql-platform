---
title: Migrate Hot Chocolate Fusion from 16.7 to 16.8
description: "Migration guide for Hot Chocolate Fusion v16.7 to v16.8: configure cost settings in the Fusion archive instead of with ModifyCostOptions."
---

Update every Hot Chocolate Fusion package in the application to version 16.8 before applying these changes.

# Deprecations

## ModifyCostOptions is obsolete

Configure Fusion cost settings in the archive instead of with `ModifyCostOptions`. In v16, values configured with `ModifyCostOptions` override the composition settings. The callback will be removed in v17.

Before:

```csharp
builder.Services
    .AddGraphQLGatewayServer()
    .ModifyCostOptions(options => options.MaxFieldCost = 2_000);
```

After:

```shell
nitro fusion settings set max-field-cost 2000 --archive ./graph.far
```
