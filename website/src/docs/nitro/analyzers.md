---
title: "Analyzers"
---

The `ChilliCream.Nitro` meta-package ships with a Roslyn source generator that simplifies Nitro integration setup. It provides compile-time warnings when required packages are missing and generates an `AddDefaults()` extension method that wires everything up in a single call.

# Compile-time warnings

If your project references HotChocolate or Fusion but is missing the corresponding Nitro integration package, you get a compile-time warning:

| Warning    | Trigger                                                           |
| ---------- | ----------------------------------------------------------------- |
| **NS0001** | HotChocolate referenced without `ChilliCream.Nitro.HotChocolate`  |
| **NS0002** | HotChocolate.Fusion referenced without `ChilliCream.Nitro.Fusion` |

# Generated `AddDefaults()` extension method

When the correct integration package is referenced, the generator emits an `AddDefaults()` extension method on `INitroBuilder`. This method wires up the default Nitro integration with the GraphQL pipeline in a single call.

```csharp
builder.Services
    .AddNitro(options =>
    {
        options.ApiId = "my-api";
        options.ApiKey = "my-key";
        options.Stage = "production";
    })
    .AddDefaults();
```

You can still customize per-schema options by calling `ModifyNitroOptions()` on the builder afterwards:

```csharp
builder.Services
    .AddNitro(options =>
    {
        options.ApiId = "my-api";
        options.ApiKey = "my-key";
    })
    .AddDefaults();

builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .ModifyNitroOptions(options =>
    {
        options.PersistedOperations.Enabled = true;
        options.Metrics.Enabled = true;
    });
```

# Troubleshooting

If `AddDefaults()` does not appear in IntelliSense:

1. Verify that `ChilliCream.Nitro` is referenced (this is the meta-package containing the source generator).
2. Verify that the matching integration package is referenced (`ChilliCream.Nitro.HotChocolate` for HotChocolate projects, `ChilliCream.Nitro.Fusion` for Fusion projects).
3. Rebuild the project so the source generator can run.

If you cannot use the source generator, call the explicit method instead: `.AddHotChocolate()` or `.AddFusion()` on the `INitroBuilder`.
