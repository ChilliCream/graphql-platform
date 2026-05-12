---
title: "Migrating from 1 to 16"
---

Starting with version 16, the packages align their version numbers with the rest of the platform (HotChocolate, Fusion, Mocha). This means you are migrating from `1.x` to `16.x`.

The packages have been restructured to separate connection settings from feature configuration. The old monolithic `NitroOptions` class has been replaced, feature options are now configured per platform component, and whenever possible, have been consolidated into common packages.

# Update Package References

| Old Package ID                 | New Package ID                    |
| ------------------------------ | --------------------------------- |
| `ChilliCream.Nitro.Core`       | `ChilliCream.Nitro.GraphQL`       |
| `ChilliCream.Nitro`            | `ChilliCream.Nitro.HotChocolate`  |
| `ChilliCream.Nitro.Telemetry`  | `ChilliCream.Nitro.OpenTelemetry` |
| `ChilliCream.Nitro.Azure.Core` | `ChilliCream.Nitro.Azure`         |

`ChilliCream.Nitro.Abstractions` is unchanged.

> **Watch out:** The old `ChilliCream.Nitro` package becomes `ChilliCream.Nitro.HotChocolate`. A new meta-package is published under the old `ChilliCream.Nitro` ID. Don't mix these up when referencing.

### Azure packages consolidated

The old per-component Azure packages (`ChilliCream.Nitro.HotChocolate.Azure`, `ChilliCream.Nitro.Fusion.Azure`) have been replaced by a single `ChilliCream.Nitro.Azure` package. Remove the old ones and add the new one.

# API Changes

## `services.AddNitro()` returns builer

The return type changed from `IServiceCollection` to `INitroBuilder`. This enables fluent chaining for add-on packages like OpenTelemetry and Azure asset caching.

If you were chaining off `IServiceCollection`, capture the builder or access `.Services`:

```csharp
// Before
services
    .AddNitro(options => { options.ApiId = "my-api"; })
    .AddSingleton<MyService>();

// After
services.AddNitro(options => { options.ApiId = "my-api"; });
services.AddSingleton<MyService>();
```

## `AddNitro()` configures connection settings only

The `IServiceCollection.AddNitro()` overload accepts `Action<NitroServiceOptions>`, which contains only connection properties. Feature configuration happens on the GraphQL builder via `ModifyNitroOptions()`.

```csharp
services.AddNitro(options =>
{
    // Connection settings
    options.ApiId = "my-api";
    options.ApiKey = "my-key";
    options.Stage = "production";
    options.ServerUrl = "https://api.chillicream.com";
    options.TelemetryUrl = "https://otel.chillicream.com";

    // Feature options like Metrics and PersistedOperations
    // are no longer available here.
});
```

## HotChocolate: `AddNitro()` is now `ModifyNitroOptions()`

The method on `IRequestExecutorBuilder` has been renamed from `AddNitro()` to `ModifyNitroOptions()`, and the options type changed to `NitroHotChocolateOptions`.

The `ModifyNitroOptions()` method is optional. If you don't need to configure any per-schema options, you can omit it entirely.

**Before**

```csharp
services
    .AddGraphQLServer()
    .AddNitro(options =>
    {
        options.ApiId = "my-api";
        options.EnablePersistedQueries = true;
        options.Metrics.Enabled = true;
    });
```

**After**

```csharp
services.AddNitro(options =>
{
    options.ApiId = "my-api";
});

services
    .AddGraphQLServer()
    .ModifyNitroOptions(options =>
    {
        options.PersistedOperations.Enabled = true;
        options.Metrics.Enabled = true;
    });
```

If you do not need to modify any per-schema options, you can omit the `ModifyNitroOptions()` call entirely.

## Fusion: `ConfigureFromCloud()` is now `AddNitro().AddDefaults()`

**Before**

```csharp
builder.Services
    .AddFusionGatewayServer()
    .ConfigureFromCloud(options =>
    {
        options.ApiId = "my-gateway";
        options.ApiKey = "my-key";
        options.Stage = "production";
    });
```

**After**

```csharp
builder.Services
    .AddNitro(options =>
    {
        options.ApiId = "my-gateway";
        options.ApiKey = "my-key";
        options.Stage = "production";
    })
    .AddDefaults();

builder.Services.AddGraphQLGatewayServer();
```

Per-gateway feature options can be overridden via `ModifyNitroOptions()`:

```csharp
builder.Services
    .AddGraphQLGatewayServer()
    .ModifyNitroOptions(options =>
    {
        options.PersistedOperations.Enabled = true;
        options.Metrics.Enabled = true;
        options.OperationReporting.Enabled = true;
    });
```

If you do not need to modify any per-gateway options, you can omit the `ModifyNitroOptions()` call entirely.

## OTLP exporters: `AddNitroExporter()` replaced by `AddOpenTelemetry()`

`AddOpenTelemetry()` on `INitroBuilder` registers the Nitro OTLP exporters on the meter, tracer, and logger providers. You no longer wire each exporter by hand.

**Before**

```csharp
services.ConfigureOpenTelemetryMeterProvider(x => x.AddNitroExporter());
services.ConfigureOpenTelemetryTracerProvider(x => x.AddNitroExporter());
services.ConfigureOpenTelemetryLoggerProvider(x => x.AddNitroExporter());
```

**After**

```csharp
services
    .AddNitro(options =>
    {
        options.ApiId = "my-api";
        options.TelemetryUrl = "https://otel.chillicream.com";
    })
    .AddOpenTelemetry();
```

## `AddNitroTelemetry` replaced

If you were using `AddNitroTelemetry` for non-GraphQL service monitoring, replace it with `AddNitro().AddOpenTelemetry()`:

**Before**

```csharp
services.AddNitroTelemetry(options =>
{
    options.ApiId = apiId;
    options.ApiKey = apiKey;
    options.Stage = stage;
});
```

**After**

```csharp
services
    .AddNitro(options =>
    {
        options.ApiId = apiId;
        options.ApiKey = apiKey;
        options.Stage = stage;
    })
    .AddOpenTelemetry();
```

## Asset cache is now global on `INitroBuilder`

The asset cache is no longer configured per-schema on the request executor builder. Instead, it is a global singleton configured on `INitroBuilder`. A default file system cache is registered automatically.

**Before**

```csharp
services
    .AddGraphQLServer()
    .AddNitro()
    .AddBlobStorageAssetCache(o =>
    {
        o.Client = blobServiceClient;
        o.ContainerName = "my-container";
    });
```

**After**

```csharp
services
    .AddNitro(options =>
    {
        options.ApiId = "my-api";
        options.ApiKey = "my-key";
    })
    .AddBlobStorageAssetCache(o =>
    {
        o.Client = blobServiceClient;
        o.ContainerName = "my-container";
    });
```

# Source Generator: `AddDefaults()`

The `ChilliCream.Nitro` meta-package ships with a Roslyn source generator.

## Compile-time warnings for missing integration packages

If your project references HotChocolate or Fusion but is missing the corresponding Nitro integration package, you get a compile-time warning:

| Warning    | Trigger                                                           | Fix                       |
| ---------- | ----------------------------------------------------------------- | ------------------------- |
| **NS0001** | HotChocolate referenced without `ChilliCream.Nitro.HotChocolate`  | Add the package reference |
| **NS0002** | HotChocolate.Fusion referenced without `ChilliCream.Nitro.Fusion` | Add the package reference |

## Generated `AddDefaults()` extension method

When the correct integration package is referenced, the generator emits an `AddDefaults()` extension method on `INitroBuilder`. This method wires up the default Nitro integration with the GraphQL pipeline in a single call.

For HotChocolate projects, `AddDefaults()` is equivalent to calling `services.AddGraphQLServer().ModifyNitroOptions()`. For Fusion projects, it is equivalent to calling `services.AddFusionGatewayServer().ModifyNitroOptions()`.

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

# Complete Before/After Examples

## HotChocolate Standalone

**Before**

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddNitro(options =>
    {
        options.ApiId = "my-api";
        options.ApiKey = "my-key";
        options.Stage = "production";
        options.EnablePersistedQueries = true;
        options.Metrics.Enabled = true;
    });

builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddNitro();

var app = builder.Build();
app.MapGraphQL();
app.Run();
```

**After**

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddNitro(options =>
    {
        options.ApiId = "my-api";
        options.ApiKey = "my-key";
        options.Stage = "production";
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

var app = builder.Build();
app.MapGraphQL();
app.Run();
```

## Fusion Gateway

**Before**

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddFusionGatewayServer()
    .ConfigureFromCloud(options =>
    {
        options.ApiId = "my-gateway";
        options.ApiKey = "my-key";
        options.Stage = "production";
    });

var app = builder.Build();
app.MapGraphQL();
app.Run();
```

**After**

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddNitro(options =>
    {
        options.ApiId = "my-gateway";
        options.ApiKey = "my-key";
        options.Stage = "production";
    })
    .AddDefaults();

builder.Services.AddGraphQLGatewayServer();

var app = builder.Build();
app.MapGraphQL();
app.Run();
```

## With OpenTelemetry

**Before**

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddNitro(options =>
    {
        options.ApiId = "my-api";
        options.Metrics.Enabled = true;
    });

services.ConfigureOpenTelemetryMeterProvider(x => x.AddNitroExporter());
services.ConfigureOpenTelemetryTracerProvider(x => x.AddNitroExporter());
services.ConfigureOpenTelemetryLoggerProvider(x => x.AddNitroExporter());

var app = builder.Build();
app.MapGraphQL();
app.Run();
```

**After**

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddNitro(options =>
    {
        options.ApiId = "my-api";
        options.ApiKey = "my-key";
        options.Stage = "production";
        options.TelemetryUrl = "https://otel.chillicream.com";
    })
    .AddDefaults()
    .AddOpenTelemetry();

builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .ModifyNitroOptions(options =>
    {
        options.Metrics.Enabled = true;
    });

var app = builder.Build();
app.MapGraphQL();
app.Run();
```

## With Azure Blob Storage Asset Cache

**Before**

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddNitro()
    .AddBlobStorageAssetCache(o =>
    {
        o.Client = blobServiceClient;
        o.ContainerName = "assets";
    });

var app = builder.Build();
app.MapGraphQL();
app.Run();
```

**After**

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddNitro(options =>
    {
        options.ApiId = "my-api";
        options.ApiKey = "my-key";
    })
    .AddDefaults()
    .AddBlobStorageAssetCache(o =>
    {
        o.Client = blobServiceClient;
        o.ContainerName = "assets";
    });

builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>();

var app = builder.Build();
app.MapGraphQL();
app.Run();
```
