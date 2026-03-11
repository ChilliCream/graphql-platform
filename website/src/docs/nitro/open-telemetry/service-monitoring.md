---
title: "Service Monitoring"
---

Nitro’s OpenTelemetry support extends beyond GraphQL, allowing you to gather and analyze telemetry data from any .NET application. Whether you have REST APIs, background workers, or other services, you can seamlessly centralize logging and tracing in Nitro for a unified observability experience.

> **Note**: If you are looking specifically for GraphQL telemetry, please refer to the [Operation Monitoring](https://chillicream.com/docs/nitro/open-telemetry/operation-monitoring/#connect-your-service-to-the-telemetry-system) section.

## Prerequisites

1. **.NET Application**: You’ll need a .NET project (e.g., ASP.NET Core, worker service, etc.) where you want to enable telemetry.
2. **ChilliCream.Nitro.Telemetry**: Make sure to reference version **15.0.0** or **14.1.0** of the `ChilliCream.Nitro.Telemetry` package.
3. **OpenTelemetry**: Have the OpenTelemetry packages or extensions configured in your project.

## Quick Start

### 1. Install Required Packages

In your .NET project, install the following NuGet packages if they are not already present:

```shell
dotnet add package ChilliCream.Nitro.Telemetry --version 15.0.0
dotnet add package OpenTelemetry --version <appropriate version>
dotnet add package OpenTelemetry.Extensions.Hosting --version <appropriate version>
```

### 2. Configure OpenTelemetry Exporters

Configure your OpenTelemetry **tracer** and **logger** providers to export data to Nitro. For example, in your `Program.cs` or `Startup.cs`:

```csharp
services.ConfigureOpenTelemetryTracerProvider(x => x.AddNitroExporter());
services.ConfigureOpenTelemetryLoggerProvider(x => x.AddNitroExporter());
```

If you’re using ASP.NET Core, you might do this in the `ConfigureServices` method.

You can also add additional instrumentation as needed, such as for HTTP requests or background jobs.

```shell
dotnet add package OpenTelemetry.Extensions.Hosting
dotnet add package OpenTelemetry.Instrumentation.AspNetCore
```

```csharp
services.ConfigureOpenTelemetryTracerProvider(x =>
{
    x.AddAspNetCoreInstrumentation();
    x.AddNitroExporter();
});
```

### 3. Register Nitro Telemetry

Next, register Nitro telemetry with the appropriate **API credentials** in the same method:

```csharp
services.AddNitroTelemetry(options =>
{
    options.ApiId = apiId;     // Replace with your Nitro API ID
    options.ApiKey = apiKey;   // Replace with your Nitro API Key
    options.Stage = stage;     // Replace with your environment or stage name
});
```

These options tell Nitro where to send the collected telemetry.

### 4. View Your Traces

Once your service is running, head over to the **Nitro dashboard**.

- Select your API.
- Choose **OpenTelemetry** from the trace overview dropdown.

You’ll see a unified view of all the HTTP requests, background worker jobs, or other .NET processes you’re tracking through OpenTelemetry.
