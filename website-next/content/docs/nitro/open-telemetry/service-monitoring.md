---
title: "Service Monitoring"
---

Nitro’s OpenTelemetry support extends beyond GraphQL, allowing you to gather and analyze telemetry data from any .NET application. Whether you have REST APIs, background workers, or other services, you can seamlessly centralize logging and tracing in Nitro for a unified observability experience.

> **Note**: If you are looking specifically for GraphQL telemetry, please refer to the [Operation Monitoring](https://chillicream.com/docs/nitro/open-telemetry/operation-monitoring/#connect-your-service-to-the-telemetry-system) section.

## Prerequisites

1. **.NET Application**: You’ll need a .NET project (e.g., ASP.NET Core, worker service, etc.) where you want to enable telemetry.
2. **ChilliCream.Nitro.OpenTelemetry**: Make sure to reference the `ChilliCream.Nitro` meta-package and the `ChilliCream.Nitro.OpenTelemetry` package.
3. **OpenTelemetry**: Have the OpenTelemetry packages or extensions configured in your project.

## Quick Start

### 1. Install Required Packages

In your .NET project, install the following NuGet packages if they are not already present:

```shell
dotnet add package ChilliCream.Nitro
dotnet add package ChilliCream.Nitro.OpenTelemetry
dotnet add package OpenTelemetry --version <appropriate version>
dotnet add package OpenTelemetry.Extensions.Hosting --version <appropriate version>
```

### 2. Register Nitro and OpenTelemetry Exporters

Register the Nitro connection and OpenTelemetry exporters. Call `AddNitro` with your API credentials, then chain `AddOpenTelemetry()` to register the OTLP exporters for tracing, metrics, and logging. For example, in your `Program.cs` or `Startup.cs`:

```csharp
services
    .AddNitro(options =>
    {
        options.ApiId = apiId;     // Replace with your Nitro API ID
        options.ApiKey = apiKey;   // Replace with your Nitro API Key
        options.Stage = stage;     // Replace with your environment or stage name
    })
    .AddOpenTelemetry();
```

### 3. Add Additional Instrumentation

You can continue configuring OpenTelemetry providers for non-Nitro instrumentation as needed, such as for ASP.NET Core or HTTP requests.

```shell
dotnet add package OpenTelemetry.Extensions.Hosting
dotnet add package OpenTelemetry.Instrumentation.AspNetCore
```

```csharp
services.ConfigureOpenTelemetryTracerProvider(x =>
{
    x.AddAspNetCoreInstrumentation();
});
```

### 4. View Your Traces

Once your service is running, head over to the **Nitro dashboard**.

- Select your API.
- Choose **OpenTelemetry** from the trace overview dropdown.

You’ll see a unified view of all the HTTP requests, background worker jobs, or other .NET processes you’re tracking through OpenTelemetry.
