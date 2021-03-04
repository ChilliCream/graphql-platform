# Get started with Strawberry Shake

In this tutorial we will walk you through the basics of adding a Strawberry Shake GraphQL client to a .NET project. For this example we will create a Blazor for WebAssembly application and fetch some simple data from our demo backend.

Strawberry Shake is not limited to Blazor and can be used with any .NET standard compliant library.

In this tutorial, we will teach you:

- How to add the Strawberry Shake CLI tools.
- How to generate source code from .graphql files, that contain operations.
- How to use the generated client in a classical or reactive way.

## Step 1: Add the Strawberry Shake CLI tools

The Strawberry Shake tool will help you to setup your project to create a GraphQL client.

Open your preferred terminal and select a directory where you want to add the code of this tutorial.

1. Create a dotnet tool-manifest.

```bash
dotnet new tool-manifest
```

2. Install the Strawberry Shake tools.

```bash
dotnet tool install StrawberryShake.Tools --local
```

## Step 2: Create a Blazor WebAssembly project

Next, we will create our Blazor project so that we have a little playground.

1 Create a new Blazor for WebAssembly application.

```bash
dotnet new wasm -n Demo
```

## Step 2: Install the required packages

Strawberry Shake supports multiple GraphQL transport protocols. In this example we will use the standard GraphQL over HTTP protocol to interact with our GraphQL server.

1. Add the `StrawberryShake.Transport.Http` package to your project.

```bash
dotnet add Demo package StrawberryShake.Transport.Http
```

2. Add the `StrawberryShake.CodeGeneration.CSharp.Analyzers` package to your project in order to add our code generation.

```bash
dotnet add Demo package StrawberryShake.CodeGeneration.CSharp.Analyzers
```

When using the HTTP protocol we also need the HttpClientFactory and the Microsoft dependency injection.

## Step 3: Add a client to your project using the CLI tools

To add a client to your project, you need to run the `dotnet graphql init {{ServerUrl}} -n {{ClientName}}`. If you wanted to create a client for our [Workshop example](https://github.com/ChilliCream/graphql-workshop), whose api live api you can find here [https://hc-conference-app.azurewebsites.net/graphql/](https://hc-conference-app.azurewebsites.net/graphql/), the command would look like this:

```bash
dotnet graphql init https://hc-conference-app.azurewebsites.net/graphql/ -n ConferenceClient
```

This will create three files in the root of your solutions:

1. `.graphqlrc.json` This file will provide information where your schema and the `.graphql` files, that contain your operations, are. This will enable other tools to work with Strawberry Shake. In addition to that, the file contains the Strawberry Shake configuration. For more information about this, check the Strawberry Shake [configuration documentation](./configuration.md).
2. `schema.graphql` This file contains the schema of the API you want to consume, which was downloaded by the CLI tool and saved to that file.
3. `schema.extensions.grpahql` This file contains local extensions of your remote schema. Currently, this is only used to define what types of you schema are entities and what are just simple data.

Now you can define your operations in `.graphql` files. Using the default settings, Strawberry Shake will scan the whole solution for any `.graphql` files.
In our case, we could simply create a file `getSessions.graphql` at root level and add the following query to it, to query all existing sessions:

```graphql
query {
  sessions {
    nodes {
      id
      startTime
      title
      trackId
      duration
      endTime
      abstract
    }
  }
}
```

After this, simply build your project (If you are using Rider, it might be required to do `Invalidate Caches / Restart` to get the source generators working properly). The client will be regenerated on every build of your project.
It will be regenerated even if you have compile errors in your main project, because the generator runs independently of your project.

## Step 3: Set up your generated client and use it

After you generated your client, it has to added to your DI container. When using HTTP as transport layer, a HttpClient has to be added:

```csharp
  services.AddHttpClient(
    "ConferenceClient",
    c =>
    {
        c.BaseAddress = new Uri("https://hc-conference-app.azurewebsites.net/graphql/");
    });
```

If you are using subscriptions, you need to add the websocket client as well:

```csharp
  services.AddWebSocketClient(
    "ConferenceClient",
    c =>
    {
        c.BaseAddress = new Uri("wss://hc-conference-app.azurewebsites.net/graphql/");
    });
```

At last add the generated chat client to the DI:

```csharp
services.AddConferenceClient();
```
