---
title: Connect your API
---

Nitro can be smoothly integrated into your HotChocolate server, enabling utilization of the Persisted Operation Storage found within the client registry, to report operations and collect open telemetry. Your server will establish a connection with Nitro, retrieving persisted operations based on their unique hashes. Additional information on the client registry can be found [here](/docs/nitro/apis/client-registry).

## Getting Started

To get started, follow these steps:

1. Set up a client registry as instructed [here](/docs/nitro/apis/client-registry).

2. Install the Nitro package from NuGet using the following command:

```bash
dotnet add package ChilliCream.Nitro
```

3. Configure your services as shown in the following code snippet:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddInstrumentation() // if you want to use telemetry
    .AddNitro(x =>
    {
        x.ApiId = "VGhpcyBpcyBub3QgYSByZWFsIGFwaSBpZA==";
        x.ApiKey = "Tm9wZSwgdGhpcyBpcyBhbHNvIG5vIHJlYWwga2V5IDspIA==";
        x.Stage = "dev";
    })
    .UseOnlyPersistedOperationAllowed() // optional
    .UsePersistedOperationPipeline(); // if you want to use persisted operations

var app = builder.Build();

app.MapGraphQL();

app.Run();
```

4. Retrieve the API id and API key from Nitro using the `nitro api list` and `nitro api-key create` commands respectively. Instructions for these commands can be found [here](/docs/nitro/cli).

Congratulations! You have successfully integrated Nitro into your HotChocolate server. You can now publish new versions of your clients and your server will automatically retrieve the latest persisted operations.

<!-- spell-checker:ignore Ghpcy, Bpcy, ZWFs -->
