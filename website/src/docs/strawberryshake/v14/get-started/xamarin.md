---
title: "Get started with Strawberry Shake and Xamarin"
---

In this tutorial we will walk you through the basics of adding a Strawberry Shake GraphQL client to a .NET project. For this example we will create a Blazor for WebAssembly application and fetch some simple data from our demo backend.

Strawberry Shake is not limited to Blazor and can be used with any .NET standard compliant library.

In this tutorial, we will teach you:

- How to add the Strawberry Shake CLI tools.
- How to generate source code from .graphql files, that contain operations.
- How to use the generated client in a classical or reactive way.

## Step 1: Add the Strawberry Shake CLI tools

The Strawberry Shake tool will help you to set up your project to create a GraphQL client.

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

Strawberry Shake has meta packages, that will help pulling in all necessary dependencies in your project. Choose between either of these:

a. For Blazor add the `StrawberryShake.Blazor` package to your project.

```bash
dotnet add Demo package StrawberryShake.Blazor
```

b. For MAUI add the `StrawberryShake.Maui` package to your project.

```bash
dotnet add Demo package StrawberryShake.Maui
```

c. For Console apps add the `StrawberryShake.Server` package to your project.

```bash
dotnet add Demo package StrawberryShake.Server
```

## Step 4: Add a GraphQL client to your project using the CLI tools

To add a client to your project, you need to run `dotnet graphql init {{ServerUrl}} -n {{ClientName}}`.

In this tutorial we will use our GraphQL workshop to create a list of sessions that we will add to our Blazor application.

> If you want to have a look at our GraphQL workshop head over [here](https://github.com/ChilliCream/graphql-workshop).

1. Add the conference client to your Blazor application.

```bash
dotnet graphql init https://workshop.chillicream.com/graphql/ -n ConferenceClient -p ./Demo
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
      "url": "https://workshop.chillicream.com/graphql/",
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

4. Create new query document `GetSessions.graphql` with the following content:

```graphql
query GetSessions {
  sessions(order: { title: ASC }) {
    nodes {
      title
    }
  }
}
```

5. Compile your project.

```bash
dotnet build
```

With the project compiled, you should now see in the directory `./obj/<configuration>/<target-framework>/berry` the generated code that your applications can leverage. For example, if you've run a Debug build for .NET 8, the path would be `./obj/Debug/net8.0/berry`.

![Visual Studio code showing the generated directory.](../shared/berry_generated.png)

6. Head over to the `Program.cs` and add the new `ConferenceClient` to the dependency injection.

> In some IDEs it is still necessary to reload the project after the code was generated to update the IntelliSense. So, if you have any issues in the next step with IntelliSense just reload the project and everything should be fine.

```csharp
public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.RootComponents.Add<App>("#app");

        builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

        builder.Services
            .AddConferenceClient()
            .ConfigureHttpClient(client => client.BaseAddress = new Uri("https://workshop.chillicream.com/graphql"));

        await builder.Build().RunAsync();
    }
}
```

7. Go to `_Imports.razor` and add `Demo.GraphQL` to the common imports

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
@using Demo.Shared // (from .NET 8, `Demo.Layout`)
@using Demo.GraphQL
```

## Step 5: Use the ConferenceClient to perform a simple fetch

In this section we will perform a simple fetch with our `ConferenceClient`. We will not yet look at state or other things that come with our client but just perform a simple fetch.

1. Head over to `Pages/Index.razor` (from .NET 8, `Home.razor`).

2. Add inject the `ConferenceClient` beneath the `@pages` directive.

```html
@page "/" @inject ConferenceClient ConferenceClient;
```

3. Introduce a code directive at the bottom of the file.

```html
@page "/" @inject ConferenceClient ConferenceClient;

<h1>Hello, world!</h1>

Welcome to your new app.

<SurveyPrompt Title="How is Blazor working for you?" />

@code { }
```

4. Now let's fetch the titles with our client.

```html
@page "/" @inject ConferenceClient ConferenceClient;

<h1>Hello, world!</h1>

Welcome to your new app.

<SurveyPrompt Title="How is Blazor working for you?" />

@code { private string[] titles = Array.Empty<string
  >(); protected override async Task OnInitializedAsync() { // Execute our
  GetSessions query var result = await
  ConferenceClient.GetSessions.ExecuteAsync(); // aggregate the titles from the
  result titles = result.Data.Sessions.Nodes.Select(t => t.Title).ToArray(); //
  signal the components that the state has changed. StateHasChanged(); }
  }</string
>
```

5. Last, let's render the titles on our page as a list.

```html
@page "/" @inject ConferenceClient ConferenceClient;

<h1>Hello, world!</h1>

Welcome to your new app.

<SurveyPrompt Title="How is Blazor working for you?" />

<ul>
  @foreach (string title in titles) {
  <li>@title</li>
  }
</ul>

@code { private string[] titles = Array.Empty<string
  >(); protected override async Task OnInitializedAsync() { // Execute our
  GetSessions query var result = await
  ConferenceClient.GetSessions.ExecuteAsync(); // aggregate the titles from the
  result titles = result.Data.Sessions.Nodes.Select(t => t.Title).ToArray(); //
  signal the components that the state has changed. StateHasChanged(); }
  }</string
>
```

5. Start the Blazor application with `dotnet run --project ./Demo` and see if your code works.

![Started Blazor application in Microsoft Edge](../shared/berry_session_list.png)

## Step 6: Using the built-in store with reactive APIs

The simple fetch of our data works. But every time we visit the index page it will fetch the data again although the data does not change often. Strawberry Shake also comes with state management where you can control the entity store and update it when you need to. In order to best interact with the store we will use `System.Reactive` from Microsoft. Let's get started :)

1. Install the package `System.Reactive`.

```bash
dotnet add Demo package System.Reactive
```

2. Next, let us update the `_Imports.razor` with some more imports, namely `System`, `System.Reactive.Linq`, `System.Linq` and `StrawberryShake`.

```csharp
@using System
@using System.Reactive.Linq
@using System.Linq
@using System.Net.Http
@using System.Net.Http.Json
@using Microsoft.AspNetCore.Components.Forms
@using Microsoft.AspNetCore.Components.Routing
@using Microsoft.AspNetCore.Components.Web
@using Microsoft.AspNetCore.Components.Web.Virtualization
@using Microsoft.AspNetCore.Components.WebAssembly.Http
@using Microsoft.JSInterop
@using Demo
@using Demo.Shared // (from .NET 8, `Demo.Layout`)
@using Demo.GraphQL
@using StrawberryShake
```

3. Head back to `Pages/Index.razor` (from .NET 8, `Home.razor`) and replace the code section with the following code:

```csharp
private string[] titles = Array.Empty<string>();
private IDisposable storeSession;

protected override void OnInitialized()
{
    storeSession =
        ConferenceClient
            .GetSessions
            .Watch(StrawberryShake.ExecutionStrategy.CacheFirst)
            .Where(t => !t.Errors.Any())
            .Select(t => t.Data.Sessions.Nodes.Select(t => t.Title).ToArray())
            .Subscribe(result =>
            {
                titles = result;
                StateHasChanged();
            });
}
```

Instead of fetching the data we watch the data for our request. Every time entities of our results are updated in the entity store our subscribe method will be triggered.

Also we specified on our watch method that we want to first look at the store and only of there is nothing in the store we want to fetch the data from the network.

Last, note that we are storing a disposable on our component state called `storeSession`. This represents our session with the store. We need to dispose the session when we no longer display our component.

4. Implement `IDisposable` and handle the `storeSession` dispose.

```csharp
@page "/"
@inject ConferenceClient ConferenceClient;
@implements IDisposable

<h1>Hello, world!</h1>

Welcome to your new app.

<SurveyPrompt Title="How is Blazor working for you?" />

<ul>
@foreach (var title in titles)
{
    <li>@title</li>
}
</ul>

@code {
    private string[] titles = Array.Empty<string>();
    private IDisposable storeSession;

    protected override void OnInitialized()
    {
        storeSession =
            ConferenceClient
                .GetSessions
                .Watch(StrawberryShake.ExecutionStrategy.CacheFirst)
                .Where(t => !t.Errors.Any())
                .Select(t => t.Data.Sessions.Nodes.Select(t => t.Title).ToArray())
                .Subscribe(result =>
                {
                    titles = result;
                    StateHasChanged();
                });
    }

    public void Dispose()
    {
        storeSession?.Dispose();
    }
}
```

Every time we move away from our index page Blazor will dispose our page which consequently will dispose our store session.

5. Start the Blazor application with `dotnet run --project ./Demo` and see if your code works.

![Started Blazor application in Microsoft Edge](../shared/berry_session_list.png)

The page will look unchanged.

6. Next, open the developer tools of your browser and switch to the developer tools console. Refresh the site so that we get a fresh output.

![Microsoft Edge developer tools show just one network interaction.](../shared/berry_session_list_network.png)

7. Switch between the `Index` and the `Counter` page (back and forth) and watch the console output.

The Blazor application just fetched a single time from the network and now only gets the data from the store.

## Step 7: Using GraphQL mutations

In this step we will introduce a mutation that will allow us to rename a session. For this we need to change our Blazor page a bit.

1. We need to get the session id for our session so that we can call the `renameSession` mutation. For this we will rewrite our `GetSessions` operation.

```graphql
query GetSessions {
  sessions(order: { title: ASC }) {
    nodes {
      ...SessionInfo
    }
  }
}

fragment SessionInfo on Session {
  id
  title
}
```

2. Next we need to restructure the `Index.razor` (from .NET 8, `Home.razor`) page.
