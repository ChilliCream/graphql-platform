---
title: "Fusion"
---

![Image](images/fusion-0.webp)

Nitro can be used as your orchestrator for your Fusion gateway. It deeply integrates with your development workflow and allows you to publish, validate, consume and monitor your Fusion gateway.

On the Fusion dashboard you can now see the tracing information of your gateway and your subgraphs.

# Dashboard

The Fusion dashboard gives you a quick overview of your gateway and subgraphs. It shows you the status of your gateway and the status of your subgraphs. You can also see the latest telemetry data insights of your gateway and subgraphs.

## Topology

![Image](images/fusion-1.webp)

The topology view shows you the connections between your gateway and your subgraphs. You can also see which clients are connected to your gateway and how many operations they are executing.

## Status

![Image](images/fusion-2.webp)
The status view shows you a quick overview of the status of your gateway. With the indicators for latency, throughput and errors you see how your gateway statistics developed between the previous and the current time range.

You also see the essential information about your gateway, such as the version, the stage, how many subgraphs are connected and how many clients are connected.

## Subgraphs

![Image](images/fusion-3.webp)
The subgraphs view shows you a quick overview over your connected subgraphs. You can see the latency, throughput and error rate of each subgraph.

# Gateway Management

With Fusion you compose your gateway configuration locally when you deploy a subgraph. This means that you somehow need to inform your gateway that there is a new configuration available.

With Nitro you can automate this process. You can configure your gateway to automatically pull the latest configuration from Nitro. This way you can be sure that your gateway always has the latest configuration. You can also validate your configuration against the schema and client registry to make sure that your change does not break any clients.

## Configure your gateway

To configure your Fusion gateway to pull the configuration from Nitro, you need to install the `ChilliCream.Nitro` package. You can do this by running the following command in your project's root directory:

```bash
dotnet add package ChilliCream.Nitro
```

After installing the package, you need to configure the services in your startup class. Below is a sample implementation in C#:

```csharp
builder.Services
    .AddFusionGatewayServer()
    .ConfigureFromCloud(x =>
    {
        x.ApiKey = "<<your-fusion-api-key>>";
        x.ApiId = "QXBpCmc5NGYwZTIzNDZhZjQ0NjBmYTljNDNhZDA2ZmRkZDA2Ng==";
        x.Stage = "dev";
    })
```

> **Tip: Using Environment Variables**
>
> Alternatively, you can set the required values using environment variables. This method allows you to call `ConfigureFromCloud` without explicitly passing parameters.
>
> - `NITRO_API_KEY` maps to `ApiKey`
> - `NITRO_API_ID` maps to `ApiId`
> - `NITRO_STAGE` maps to `Stage`
>
> ```csharp
> builder.Services
>     .AddFusionGatewayServer()
>     .ConfigureFromCloud();
> ```
>
> In this setup, the API key, ID, and stage are set through environment variables.

Now your gateway will be notified whenever there is a new configuration available and will automatically pull it.

## Configure Your Subgraphs

To set up your subgraphs to be linked with your gateway, you need to follow these steps:

### Step 1: Install ChilliCream.Nitro Package

First, ensure that the `ChilliCream.Nitro` package is installed in your subgraph projects. If not, you can install it by running the following command in the root directory of each subgraph project:

```bash
dotnet add package ChilliCream.Nitro
```

### Step 2: Configure Services in Startup

After installing the package, configure the Nitro Services on your schema. Here is an example of how you can do this:

```csharp
services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddNitro(x =>
    {
        x.ApiKey = "<<your-subgraph-api-key>>";
        x.ApiId = "<<your-subgraph-api-id>>";
        x.Stage = "dev";
    })
    .AddInstrumentation(); // Enable GraphQL telemetry

services
    .AddOpenTelemetry()
    .WithTracing(x =>
    {
        x.AddHttpClientInstrumentation();
        x.AddAspNetCoreInstrumentation();
        x.AddNitroExporter();
        // Register more instrumentation providers such as Entity Framework Core, HttpClient, etc.
    });
```

> **Tip: Using Environment Variables**
>
> Alternatively, you can also set the required values using environment variables.

This configuration enables your subgraph to interact with the Nitro services, including telemetry and instrumentation.

### Step 3: Create a Subgraph Configuration File

Each subgraph requires a specific configuration file named `subgraph-config.json`. This file should be placed in the root directory of the subgraph project, next to the `.csproj` file.

Here’s an example of what the `subgraph-config.json` file should look like:

```json
{
  "subgraph": "Order", // Name of the subgraph
  "http": { "baseAddress": "http://localhost:59093/graphql" }, // Default HTTP settings
  "extensions": {
    "nitro": {
      "apiId": "<<your-subgraph-api-id>>"
    }
  }
}
```

This file is required for the topology to recognize and display your subgraph correctly.

### Step 4: Pack your subgraph and compose your Gateway

After configuring your subgraph you have to `pack` your subgraph and `compose` your gateway.
This process links your subgraph with the gateway, ensuring a cohesive GraphQL architecture.

## Integration into your CI/CD pipeline

The deployment of a subgraph is a multi step process. To integrate Nitro into this process you need to install the Nitro CLI. You can find more information about Nitro CLI in the [Nitro CLI Documentation](/docs/nitro/cli).

```bash
dotnet new tool-manifest
dotnet tool install ChilliCream.Nitro.CLI
```

You will also need the [Command Line Tools](https://www.nuget.org/packages/HotChocolate.Fusion.CommandLine) for packing and composing your subgraph.

```bash
dotnet tool install HotChocolate.Fusion.CommandLine
```

### 1. Pack the subgraph

All changes to the gateway originate from a subgraph. Once the subgraph is ready to be deployed, you need to pack it. Packing a subgraph will create a subgraph package file that contains the schema, the extensions and the configuration of the subgraph.

To easily access the newest schema and extensions, you can use the `schema export` command from the [Command Line Extension](/docs/hotchocolate/v13/server/command-line). This command exports your current schema into a specified output file.

```bash
dotnet run -- schema export --output schema.graphql
dotnet fusion subgraph pack
```

This step is usually done in a separate build step in your CI/CD pipeline where you build and test your project before you go into the deployment phase.

### 2. Wait for a deployment slot

Once your changes are ready to be deployed, you need to wait for a deployment slot. There can only ever be one deployment at the time. If there is already a deployment in progress, you need to wait until it is finished.

Nitro helps you coordinate your subgraph deployments. You register for a deployment by calling:

```bash
dotnet nitro fusion-configuration publish begin \
  --stage <<stage-id>> \
  --tag <<tag>> \
  --api-id <<your-fusion-api-id>> \
  --subgraph-name <<subgraph-name>> \
  --api-key <<your-fusion-api-key>>
```

This command will complete once your turn has come and you can start deploying your subgraph.

### 3. Start the deployment

Once you have a deployment slot, you need to notify Nitro that you are still interested in the slot. You do this by calling:

```bash
dotnet nitro fusion-configuration publish start --api-key <<your-fusion-api-key>>
```

### 4. Configure the subgraph

As most likely, your connection information is different from environment to environment, you need to configure the url of your subgraph. You can do this by calling:

```bash
dotnet fusion subgraph config set http \
  --url <<url>>
  -c path/to/your/subgraph/config.fsp
```

### 5. Compose the subgraph

To compose the subgraph, you first need to fetch the latest configuration from Nitro. You can do this by calling:

```bash
dotnet nitro fusion-configuration download \
  --api-id <<your-fusion-api-id>> \
  --stage <<name-of-the-stage>> \
  --output-file ./gateway.fgp \
  --api-key <<your-fusion-api-key>>
```

This will download the latest configuration from Nitro and save it to the specified file (`gateway.fgp`).

Now you can compose the subgraph by calling:

```bash
dotnet fusion compose -p ./gateway.fgp -s path/to/your/subgraph/config.fsp
```

### 6. Validate the subgraph (optional)

If you want to make sure that your subgraph is compatible with the schema and client registry, you can validate it by calling:

```bash
dotnet nitro fusion-configuration publish validate --configuration ./gateway.fgp --api-key <<your-fusion-api-key>>
```

In case the validation fails, you will get an error message. You have to cancel the deployment manually though. You can add deployment step to your CI/CD pipeline which will cancel the deployment if the validation fails by calling:

```bash
dotnet nitro fusion-configuration publish cancel --api-key <<your-fusion-api-key>>
```

### 7. Deploy the subgraph

Now it's time to deploy your subgraph to your infrastructure

### 8. Commit the deployment

To complete the deployment, you need to commit the deployment. This will notify Nitro that you are done with the deployment and that the next deployment can start. Nitro will also notify your gateway that the deployment is finished and that it can pull the latest configuration.

You can commit the deployment by calling:

```bash
dotnet nitro fusion-configuration publish commit --configuration ./gateway.fgp --api-key <<your-fusion-api-key>>
```

# Distributed Telemetry

![Image](images/fusion-4.webp)
Nitro provides a distributed telemetry solution for your Fusion Gateway. It allows you to monitor your gateway and all your subgraphs in one place. You can inspect the traces of your operations on the gateway and see how they are executed on the subgraphs.

To enable telemetry for your gateway and subgraphs, all of them need to be configured to send telemetry data to Nitro. Your subgraphs can be configured to send telemetry data by using the [ChilliCream.Nitro](https://www.nuget.org/packages/ChilliCream.Nitro/) package. You can find more information about how to configure your subgraphs in the [Open Telemetry](/docs/nitro/open-telemetry/operation-monitoring) guide.

To send telemetry data from the gateway you need to add the instrumentation and the exporter to your gateway.

```csharp
builder.Services
    .AddFusionGatewayServer()
    .ConfigureFromCloud()
    .CoreBuilder
    .AddInstrumentation();

builder.Services
    .AddOpenTelemetry()
    .WithTracing(b => b
        .AddHttpClientInstrumentation()
        .AddAspNetCoreInstrumentation()
        .AddNitroExporter());
```

Now your gateway will send the telemetry data to Nitro. To connect your subgraphs to the gateway, you need to add an extension to your `subgraph-config.json`. You need to specify the `apiId` of the subgraph

```json
{
  "subgraph": "Order",
  "http": { "baseAddress": "http://localhost:59093/graphql" },
  "extensions": {
    "nitro": {
      "apiId": "QXBpCmc4ZjdhZTUxYjE5YTY0ZjFiYjcwNTc3NjJkMDkzOTg2Nw=="
    }
  }
}
```

# Cache

The `ChilliCream.Nitro` package provides caching for persisted operations and fusion
configurations, improving your system's resilience and performance. By first accessing a local cache
for configurations before querying the server, your infrastructure becomes more robust, minimizing
dependency on real-time server communications. This approach not only speeds up access to necessary
configurations but also ensures your system remains stable and responsive, even during network
fluctuations.

We offer two types of caches: `FileSystemCache` for storing data on your local file system, and
`BlobStorageCache` for storing data in Azure Blob Storage.

Here’s how you add caching to your service:

For GraphQL services:

```csharp
services
    .AddGraphQLServer()
    .AddAssetCache<TCache>()
```

For fusion services:

```csharp
services
    .AddFusionGatewayServer()
    .ConfigureFromCloud()
    .AddAssetCache<TCache>()
```

## `FileSystemCache`

This default cache stores data in the `assets` folder of your project. You can change the folder like this:

```csharp
services
    .AddGraphQLServer()
    .AddFileSystemAssetCache(x =>
    {
        x.CacheDirectory = "cache"; // Your cache folder
    })
```

## `BlobStorageCache`

This cache stores your data in Azure Blob Storage.

You need to install the `ChilliCream.Nitro.Azure` package:

```bash
dotnet add package ChilliCream.Nitro.Azure
```

Set it up with:

```csharp
services
    .AddGraphQLServer()
    .AddBlobStorageAssetCache(x =>
    {
        x.ContainerName = "your-container-name";
        x.Client = new BlobServiceClient(
            new Uri("https://yourblobstorage.blob.core.windows.net/"),
            new DefaultAzureCredential());
    })
```

## Custom `IAssetCache`

If you need a specific cache setup, you can make your own by implementing the `IAssetCache`
interface. This lets you decide how queries and configurations are cached according to your needs.
