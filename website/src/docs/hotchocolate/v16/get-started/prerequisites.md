---
title: "Prerequisites"
description: "Check the tools, network access, browser setup, and GraphQL vocabulary you need before starting Hot Chocolate."
---

Before you begin the Hot Chocolate tutorial, use this page to make sure your environment is ready. By the end, you should know: can this machine run the first Hot Chocolate tutorial?

You will check:

- That a supported .NET SDK is installed and available in your terminal
- Your editor can open and run a .NET project
- NuGet package restore can access the required packages
- Your browser can reach a local GraphQL endpoint and load Nitro
- You are familiar with the GraphQL terms used in the first tutorial

This page does not cover installing templates, creating a server, using Nitro, or deploying to production. Once you have completed these checks, continue with the path that fits your situation.

# Check the Required .NET SDK

Hot Chocolate v16 getting-started examples require the [.NET 8 SDK](https://dotnet.microsoft.com/download) or newer.

You need the **SDK**, not only the runtime. The runtime can run an existing .NET application. The SDK lets you create projects, restore packages, build, run, and test code, and use commands like `dotnet new`, `dotnet build`, and `dotnet run`.

Open the terminal you plan to use and run:

```bash
dotnet --info
```

Look for an **SDKs installed** section with version `8.0.x` or higher:

```text
.NET SDKs installed:
  8.0.404 [/usr/local/share/dotnet/sdk]
```

If you are working in an existing repository, check for a `global.json` file. This file can pin the SDK version for the .NET CLI. If `dotnet --info` shows an older SDK inside the repository than outside, `global.json` is likely the reason.

If you use an editor, also run the command in the editor's terminal. Editors like Visual Studio, VS Code, and Rider may use a different `PATH` until restarted.

# Choose Your Editor Workflow

Hot Chocolate works with any editor that lets you edit C#, run terminal commands, restore NuGet packages, and open a browser. Choose the workflow that fits your preferences:

| Workflow        | Use if you want...                                              | Check before continuing                                      |
|-----------------|----------------------------------------------------------------|--------------------------------------------------------------|
| Visual Studio   | Integrated project creation, restore, run, and debugging       | Visual Studio finds the .NET 8 SDK or later and creates ASP.NET Core projects |
| VS Code         | A lightweight editor with an integrated terminal               | C# extension is installed, and `dotnet --info` works in the VS Code terminal  |
| Rider           | JetBrains .NET tooling and run configurations                  | Rider detects the .NET 8 SDK or later and can restore the project             |
| CLI-first       | Minimal editor assumptions, command-line workflow              | Your shell runs `dotnet --info`, `dotnet new`, `dotnet restore`, and `dotnet run` |

The tutorial works from any of these paths. You will create or open a .NET project, inspect C# files, run the server, and open Nitro in your browser.

# Verify NuGet Access and Package Restore

Hot Chocolate packages and templates are distributed through NuGet. If package restore cannot reach a source with the required packages, setup will fail before you can run any GraphQL code.

List your configured package sources:

```bash
dotnet nuget list source
```

Most development machines use `nuget.org`:

```text
Registered Sources:
  1.  nuget.org [Enabled]
      https://api.nuget.org/v3/index.json
```

In some organizations, you may use an internal mirror or authenticated feed. This is fine as long as it contains the Hot Chocolate packages and templates you need.

Before continuing, confirm at least one of these is true:

- `nuget.org` is enabled and reachable from your network
- Your organization provides an internal source that mirrors the required packages
- You know who manages package sources, proxy settings, and feed credentials for your environment

Offline environments are not the target path for the first tutorial. If restore is blocked by a proxy, certificate policy, private feed authentication, or a disabled package source, resolve that first or use the [getting-started troubleshooting guide](/docs/hotchocolate/v16/get-started/troubleshooting).

For more on NuGet source configuration, see the [.NET CLI NuGet source documentation](https://learn.microsoft.com/dotnet/core/tools/dotnet-nuget-list-source) and the [NuGet package sources documentation](https://learn.microsoft.com/nuget/consume-packages/install-use-packages-visual-studio#package-sources).

# Check Browser Access for Nitro

Nitro is the browser-based GraphQL IDE served by Hot Chocolate during local development. In the first tutorial, you will run a server and open the local GraphQL endpoint (usually `/graphql`) in your browser.

You do not need to open Nitro yet, but you will need:

- A modern browser
- Permission to open `localhost` URLs
- A local environment where firewall, VPN, proxy, or browser security tools do not block the server

After the server starts, the terminal will print a listening URL, such as:

```text
Now listening on: http://localhost:5095
```

You will open the matching GraphQL endpoint:

```text
http://localhost:5095/graphql
```

This page should load Nitro and show the schema is available. If your browser cannot reach the URL, compare the address with the listening URL printed by the server. The port may vary between machines and runs.

For more on endpoint behavior, see [Endpoints](/docs/hotchocolate/v16/server/endpoints). When your server is running, continue to [run your first query](/docs/hotchocolate/v16/get-started/run-your-first-query).

# Learn the GraphQL Terms Used in the Tutorial

You do not need to know the full GraphQL specification before starting. You only need enough vocabulary to follow the tutorial.

Hot Chocolate maps C# types and members to a GraphQL schema. A client sends a query to select fields from that schema. The server runs resolvers and returns data in the shape the client requested.

| Term          | In the first tutorial, this means... |
|---------------|--------------------------------------|
| Schema        | The API contract describing types and fields clients can request |
| Query         | A read operation sent by a client; also the name of the root type for read fields |
| Field         | A selectable value on a GraphQL type, such as `book`, `title`, or `author` |
| Resolver      | The C# method or property that produces a field value |
| Selection set | The nested shape inside `{ ... }` that tells the server which fields to return |
| Response      | The JSON result returned by the server. Successful responses contain `data`; responses can also include `errors` |

Here is a small example that shows the mapping:

```csharp
// Types/Book.cs
public record Book(string Title, Author Author);

// Types/Author.cs
public record Author(string Name);

// Types/Query.cs
[QueryType]
public static partial class Query
{
    public static Book GetBook()
        => new Book("C# in depth.", new Author("Jon Skeet"));
}
```

Hot Chocolate exposes a field on the schema:

```graphql
type Query {
  book: Book!
}
```

A client selects fields:

```graphql
{
  book {
    title
  }
}
```

The response matches the selected shape:

```json
{
  "data": {
    "book": {
      "title": "C# in depth."
    }
  }
}
```

The [GraphQL specification](https://spec.graphql.org/) defines schemas, operations, selection sets, execution, and response shape. The [official GraphQL learning site](https://graphql.org/learn/) is a good next step if you want more background. For Hot Chocolate-specific concepts, start with the [learning path](/docs/hotchocolate/v16/learn).

# Are You Ready to Continue?

Use this checklist to confirm you are ready:

- `dotnet --info` works in your terminal
- The output shows the .NET 8 SDK or later
- Your editor can open and run a .NET project
- NuGet package restore can reach a source with Hot Chocolate packages
- You have a browser that can reach local `localhost` URLs
- You understand the terms schema, query, field, resolver, selection set, and response at a high level

Next, choose your path:

- **You want a new project from the Hot Chocolate template.** Go to [Install and scaffold](/docs/hotchocolate/v16/get-started/install-and-scaffold).
- **You already have an ASP.NET Core app.** Go to [Add Hot Chocolate to an existing app](/docs/hotchocolate/v16/get-started/add-to-existing-app).
- **Your server is already running.** Go to [Run your first query](/docs/hotchocolate/v16/get-started/run-your-first-query).
- **You want an overview before installing.** Read [Hot Chocolate at a glance](/docs/hotchocolate/v16/get-started/at-a-glance).
- **A check failed.** Use [Troubleshooting](/docs/hotchocolate/v16/get-started/troubleshooting) before continuing.

# If a Check Fails

Setup issues are common on new machines or restricted networks. Use the symptom to decide what to do next.

| Symptom | Likely cause | Next action |
|---------|--------------|-------------|
| `dotnet` is not recognized or command not found | The SDK is not installed, not on `PATH`, or the terminal was opened before installation finished | Install the [.NET SDK](https://dotnet.microsoft.com/download), reopen the terminal, and run `dotnet --info` again |
| `dotnet --info` shows an older SDK | The .NET 8 SDK or later is missing, or `global.json` pins an older version | Install a supported SDK and check `global.json` if you are inside an existing repository |
| The command works in a standalone terminal but not in the editor terminal | The editor process has an older environment or SDK detection cache | Restart the editor, open a new integrated terminal, and run `dotnet --info` |
| Template installation or restore cannot find packages | `nuget.org` is disabled, the internal feed lacks packages, or the network blocks access | Run `dotnet nuget list source` and confirm a reachable source contains Hot Chocolate packages |
| Restore reports authentication errors | Private feed credentials are missing or expired | Sign in through your IDE or refresh the feed credentials required by your organization |
| The browser cannot open the local GraphQL endpoint later | The server is not running, the port changed, the URL is wrong, or local traffic is blocked | Use the exact listening URL from the server output and add `/graphql` |
| Nitro loads later but does not show schema availability | The endpoint does not match the running server, startup failed, or the schema failed to build | Check the server terminal output, then use the [troubleshooting guide](/docs/hotchocolate/v16/get-started/troubleshooting) |

Once you resolve the issue, return to the [getting-started gateway](/docs/hotchocolate/v16/get-started) and continue with the path that matches your project.
