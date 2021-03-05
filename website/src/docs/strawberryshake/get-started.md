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

1. First, a new solution called `Demo.sln`.

```bash
dotnet new sln -n Demo
```

2. Create a new Blazor for WebAssembly application.

```bash
dotnet new wasm -n Demo
```

3. Add the project to the solution `Demo.sln`.

```bash
dotnet sln add ./Demo
```

## Step 3: Install the required packages

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

3. Add the `Microsoft.Extensions.DependencyInjection` package to your project in order to add our code generation.

```bash
dotnet add Demo package Microsoft.Extensions.DependencyInjection
```

3. Add the `Microsoft.Extensions.Http` package to your project in order to add our code generation.

```bash
dotnet add Demo package Microsoft.Extensions.Http
```

## Step 4: Add a GraphQL client to your project using the CLI tools

To add a client to your project, you need to run the `dotnet graphql init {{ServerUrl}} -n {{ClientName}}`.

In this tutorial we will use our GraphQL workshop to create a list of sessions that we will add to our Blazor application.

> If you want to have a look at our GraphQL workshop head over [here](https://github.com/ChilliCream/graphql-workshop).

1. Add the conference client to your Blazor application.

```bash
dotnet graphql init https://hc-conference-app.azurewebsites.net/graphql/ -n ConferenceClient -p ./Demo
```

2. Customize the namespace of the generated client to be `Demo.GraphQL`. For this head over to the `.graphqlrc.json` and insert a namespace property to the `StrawberryShake` section.

```json
{
  "schema": "schema.graphql",
  "documents": "**/*.graphql",
  "extensions": {
    "strawberryShake": {
      "name": "ConferenceClient",
      "namespace": "Demo.GraphQL",
      "url": "https://hc-conference-app.azurewebsites.net/graphql/",
      "dependencyInjection": true
    }
  }
}
```

Now that everything is in place let us write our first query to ask for a list of session titles of the conference API.

3. Choose your favorite IDE and the solution. If your are using VSCode do the following:

```bash
code ./Demo
```

3. Create new query document `GetSessions.graphql` with the following content:

```graphql
query GetSessions {
  sessions(order: { title: ASC }) {
    nodes {
      title
    }
  }
}
```

4. Compile your project.

```bash
dotnet build
```

With the project compiled you now should see a directory `Generated`. The generated code is just there for the IDE, the actual code was injected directly into roslyn through source generators.

IMAGE 1

5. Head over to the `Program.cs` and add the new `ConferenceClient` to the dependency injection.

> In some IDEs it is still necessary to reload the project after the code was generated to update the IntelliSense. So, if you have any issues in the next step with IntelliSense just reload the project and everything should be fine.

```csharp
public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.RootComponents.Add<App>("#app");

        builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

        // adds the conference client to the dependency injection.
        builder.Services.AddConferenceClient();

        // configures the transport for the conference client.
        builder.Services.AddHttpClient(
            ConferenceClient.ClientName,
            client => client.BaseAddress = new Uri("https://hc-conference-app.azurewebsites.net/graphql"));

        await builder.Build().RunAsync();
    }
}
```

6. Go to `_Imports.razor` and add `Demo.GraphQL` to the common imports

```razor
@using System.Net.Http
@using System.Net.Http.Json
@using Microsoft.AspNetCore.Components.Forms
@using Microsoft.AspNetCore.Components.Routing
@using Microsoft.AspNetCore.Components.Web
@using Microsoft.AspNetCore.Components.Web.Virtualization
@using Microsoft.AspNetCore.Components.WebAssembly.Http
@using Microsoft.JSInterop
@using Demo
@using Demo.Shared
@using Demo.GraphQL
```

## Step 5: Use the ConferenceClient to perform a simple fetch

In this section we will perform a simple fetch with our `ConferenceClient`. We will not yet look at state or other things that come with our client but just perform a simple fetch.

1. Head over to `Pages/Index.razor`.

2. Add inject the `ConferenceClient` beneath the `@pages` directive.

```razor
@page "/"
@inject ConferenceClient ConferenceClient;

<h1>Hello, world!</h1>

Welcome to your new app.

<SurveyPrompt Title="How is Blazor working for you?" />
```

3. Introduce a code section at the bottom of the file.

```razor
@page "/"
@inject ConferenceClient ConferenceClient;

<h1>Hello, world!</h1>

Welcome to your new app.

<SurveyPrompt Title="How is Blazor working for you?" />

@code {

}
```

4. Now lets fetch the titles with our client.

```razor
@page "/"
@inject ConferenceClient ConferenceClient;

<h1>Hello, world!</h1>

Welcome to your new app.

<SurveyPrompt Title="How is Blazor working for you?" />

@code {
    private string[] titles = Array.Empty<string>();

    protected override async Task OnInitializedAsync()
    {
        // Execute our GetSessions query
        var result = await ConferenceClient.GetSessions.ExecuteAsync();

        // aggregate the titles from the result
        titles = result.Data.Sessions.Nodes.Select(t => t.Title).ToArray();

        // signal the components that the state has changed.
        StateHasChanged();
    }
}
```

5. Start the Blazor application with `dotnet run ./Demo` and see if your code works.

IMAGE BROWSER

## Step 6: Using the built-in store with reactive APIs.

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
