This is your guide to connect your Fusion gateway to the cloud for updates on gateway configuration. 

**What's covered in this guide?**

- Tools installation for operating the schema registry and managing the fusion lifecycle.
- Preparation of the cloud environment, including the creation of an API and its API key, and setting up a stage for deployment.
  
For a visual guide to get your Fusion graph up and running, you can check out this video tutorial. 

<Video videoId="peMdejyrKD4" />

The example used in this guid is availabe [on GitHub](https://github.com/ChilliCream/hotchocolate-examples/tree/master/misc/Fusion1)

## Step 1: Getting the Tools

First, you'll need two tools: [Barista](/docs/barista/v1) and Fusion. These tools will help you to operate the schema registry and manage the Fusion lifecycle.

**Install Barista**

Use the following command to download and install the dotnet tool Barista:

```bash
dotnet tool install --global Barista --version 1.0.0
```

**Install Fusion Dotnet Tools**

Next, download and install the Fusion dotnet tools with the following command:

```bash
dotnet tool install --global HotChocolate.Fusion.CommandLine --version 13.3.0
```

## Step 2: Prepare the Cloud

### 2.1: Creating an API

Create an API in BananaCakePop to push your configuration to. You can do this in the BananaCakePop app or just run the following command:

```bash
barista api create
```

You will need the ID of the API later. If you already have an API (or created it over the BananaCakePop UI), you can list the APIs with:

```bash
barista api list
```

### 2.2: Create an API key

Your gateway needs an API key to access the configuration. Generate an API key with the following command:

```bash
barista api-key create
```

You will then be prompted to select your API, after which the key will be generated.

### 2.3: Setting up a Stage

A stage is a representation of your API in a specific environment. You'll need a stage to deploy your API to. You can manage the stages of an API either in BananaCakePop or by running:

```bash
barista stage edit
```

## Step 3: Prepare your Graph

### 3.1: Pack the Subgraph

First, navigate to the folder of your subgraph and run the following command to pack it:

```bash
fusion subgraph pack
```

### 3.2: Compose the Gateway

Next, navigate to the folder of your gateway and run the following command to compose the subgraph and gateway into a new fusion configuration:

```bash
fusion compose -p gateway.fgp -s ../Subgraphs/Reviews/Review.fsp
```

### 3.3: Push your Initial Configuration to the Cloud

Before starting your service, the API already needs a configuration. Otherwise, the startup will fail. This is an intentional feature - we don't want you to lose time with a wrongly configured gateway.

You can push your configuration by running the following commands:

```bash
barista fusion-configuration publish begin --stage your-stage-name --api-id "QXBpCnR...oZUFwaUlk==" --subgraph-name "the-name-of-the-subgraph" --tag 1.0.0
barista fusion-configuration publish start
barista fusion-configuration publish commit --configuration /path/to your/gateway.fgp
```

**We'll delve deeper into each of these commands later.**

### 3.4: Connecting your Gateway to the Cloud

To connect your gateway to the cloud, you need to add the `BananaCakePop.Services` to your Gateway. Run the following command to add the package...

```bash
dotnet add package BananaCakePop.Services  
```

... and then, call `ConfigureFromCloud` on the gateway builder in your code:

```csharp
builder.Services
    .AddFusionGatewayServer()
    .ConfigureFromCloud(
        (options) =>
        {
            options.Stage = "your-stage-name";
            options.ApiId = ""QXBiCnR...oZUFwaUlk==";
            options.ApiKey = "Ore923hf*************************";
        })
```

Congratulations! Your gateway is now connected to the cloud! This allows the configuration of your gateway to be live updated without needing redeployment on every subgraph change. 

## Step 4: Understanding the Fusion Lifecycle

The lifecycle of Fusion is mainly driven by the lifecycle of your subgraphs. Your gateway does not need to be redeployed. When you have changes on your subgraph and you want to push a new configuration, your CI Pipeline should follow these steps:

### 4.1: Requesting a Deployment Slot

The nature of Fusion means that each deployment of a subgraph composes a new configuration. This means you cannot deploy two subgraphs concurrently; they must be deployed in order. 

BananaCakePop coordinates deployments so that there is always one active deployment.

When you run the following command, you enqueue a deployment. The command will complete when your slot is ready for deployment:

```bash
barista fusion-configuration publish begin --stage the-name-of-thestage --api-id "QXBpCmdhZDY...zMzI1ZQ==" --subgraph-name "the-name-of-the-subgraph" --tag 1.1.0 
```

> **Note:** 
> - **--tag:** This does not have to be a version string; it can be any string. So, commit IDs, timestamps, or a combination of tag, hash, and subgraph name are possible.
> - **--subgraph-name:** If your subgraph is also registered in BananaCakePop, you can pass the `--subgraph-id` instead of the name.

### 4.2: Starting the Composition

Once your deployment slot is ready, you must tell BananaCakePop that you're still interested in the deployment:

```bash
barista fusion-configuration publish start
```

> **Note:** 
> - **--request-id:** This is an optional property. The `publish begin` command returns a request ID and also stores this ID in a file in `/tmp`. If you run the `publish begin` and `publish start` commands on different systems, you'll need to pass along the request ID.

### 4.3: Download the Latest Fusion Configuration

To compose the gateway, you need to get the latest fusion configuration:

```bash
barista fusion-configuration download --stage the-name-of-thestage --api-id "QXBpCmdhZDY...zMzI1ZQ=="  
```

This command will store the Gateway configuration in the executing directory as `gateway.fgp`. You can change this by passing along the `--output-file` parameter.

### 4.4: Pack the Subgraph

Prepare your subgraph for composition by running:

```bash
fusion subgraph pack
```

You can also extract the `schema.graphql` by running your app with `RunWithGraphQLCommands`, which enables you to get the latest schema in your build pipeline easily. Just run:

```bash
dotnet run -- schema export --output schema.graphql
```

In your code:

```csharp
var builder = WebApplication.CreateBuilder(args);

// ...

app.RunWithGraphQLCommands(args);
```

### 4.5: Compose the Gateway

With your subgraph ready and the latest fusion configuration downloaded, you can now compose the gateway:

```bash
fusion compose -p gateway.fgp -s ../Subgraphs/Reviews/Review.fsp
```

You now have a new fusion configuration!

### 4.6: Deploy your Subgraph

Now that everything is ready, it's time to deploy your subgraph.

### 4.7: Commit your Changes

After deploying your subgraph, upload the new configuration to BananaCakePop and commit the changes:

```bash
barista fusion-configuration publish commit --configuration gateway.fgp
```

At this point, several things happen:

1. Your gateway gets notified about the new configuration and updates itself.
2. If you have other releases queued, these will be unblocked and become ready.

Following this guide, you'll be able to efficiently manage the lifecycle of your subgraphs and their corresponding Fusion configurations, enabling seamless and uninterrupted deployments of your API updates.
