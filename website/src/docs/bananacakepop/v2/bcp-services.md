--- 
title: Connect you API
---

BananaCake Pop can be smoothly integrated into your HotChocolate server, enabling utilization of the Persisted Query Storage found within the client registry, to report operations and collect open telemetry. Your server will establish a connection with BananaCake Pop, retrieving persisted queries based on their unique hashes. Additional information on the client registry can be found [here](/docs/bananacakepop/v2/apis/client-registry).

## Getting Started
To get started, follow these steps:

1. Set up a client registry as instructed [here](/docs/bananacakepop/v2/apis/client-registry).

2. Install the BananaCakePop package from NuGet using the following command:
```bash
dotnet add package BananaCakePop.Services
```

3. Configure your services as shown in the following code snippet:
```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddBananaCakePopServices(x =>
    {
        x.ApiId = "VGhpcyBpcyBub3QgYSByZWFsIGFwaSBpZA==";
        x.ApiKey = "Tm9wZSwgdGhpcyBpcyBhbHNvIG5vIHJlYWwga2V5IDspIA==";
        x.Stage = "dev";
    })
    .UseOnlyPersistedQueriesAllowed() // optional
    .UsePersistedQueryPipeline();

var app = builder.Build();

app.MapGraphQL();

app.Run();
```

4. Retrieve the API id and API key from Barista using the `barista api list` and `barista api-key create` commands respectively. Instructions for these commands can be found [here](/docs/barista/v1).

Congratulations! You have successfully integrated BananaCake Pop into your HotChocolate server. You can now publish new versions of your clients and your server will automatically retrieve the latest persisted queries.
