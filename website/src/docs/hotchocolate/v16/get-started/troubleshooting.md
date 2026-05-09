---
title: "Troubleshooting"
description: "Recover from common Hot Chocolate setup, template, startup, browser, Nitro, schema, and package version problems."
---

If you run into issues before you can execute your first query with Hot Chocolate, use this page to diagnose and resolve the most common problems.

Most first-run issues fall into these categories:

- .NET SDK or NuGet environment
- Template installation or project creation
- ASP.NET Core startup and endpoint routing
- Nitro connection state
- Schema construction after code changes

# How to Use This Guide

Start by identifying the symptom you see. Focus only on the section that matches your current problem.

For each issue:

1. Find the matching command, browser message, or first error line.
2. Review the likely cause.
3. Apply the recommended fix.
4. Run the verification step.
5. Return to your original setup or tutorial page.

If you need help later, copy the first relevant error line and the command that produced it. The first error is usually the most helpful for diagnosis.

# Where Is the Problem Happening?

| If you see this | Go to |
| --- | --- |
| `dotnet` is not found, the SDK is missing, or the target framework is not supported. | [The .NET SDK version is wrong or missing](#the-net-sdk-version-is-wrong-or-missing) |
| `dotnet new install HotChocolate.Templates` cannot find or install the package. | [`dotnet new install HotChocolate.Templates` cannot find or install the templates](#dotnet-new-install-hotchocolatetemplates-cannot-find-or-install-the-templates) |
| `dotnet new graphql` is not recognized, creates files in the wrong place, or reports a name conflict. | [`dotnet new graphql` does not create the expected project](#dotnet-new-graphql-does-not-create-the-expected-project) |
| `dotnet restore` or `dotnet build` fails after adding Hot Chocolate packages. | [Restore or build fails because package versions are mixed](#restore-or-build-fails-because-package-versions-are-mixed) |
| `dotnet run` builds but the server exits or cannot bind to a URL. | [The server does not start](#the-server-does-not-start) |
| The browser says it cannot reach `localhost`. | [The browser says localhost refused the connection](#the-browser-says-localhost-refused-the-connection) |
| The app responds, but `/graphql` returns 404. | [`/graphql` returns 404 or the endpoint is missing](#graphql-returns-404-or-the-endpoint-is-missing) |
| Nitro loads, but the schema is unavailable or requests fail. | [Nitro opens but cannot load the schema](#nitro-opens-but-cannot-load-the-schema) |
| The app fails during schema construction after a C# change. | [The schema fails to build](#the-schema-fails-to-build) |

# `dotnet new install HotChocolate.Templates` Cannot Find or Install the Templates

If the install command fails before you can use `dotnet new graphql`, check the following:

| Symptom | Likely cause | Fix |
| --- | --- | --- |
| `dotnet` is not recognized, or `dotnet --info` does not show an SDK. | The .NET SDK is not installed, not on `PATH`, or the terminal was opened before installation completed. | Install a supported [.NET SDK](https://dotnet.microsoft.com/download), reopen the terminal, and run `dotnet --info`. |
| `HotChocolate.Templates` cannot be found. | `nuget.org` is disabled, a private feed does not mirror the package, or network policy blocks NuGet. | Run `dotnet nuget list source` and enable or fix a source that contains the public Hot Chocolate packages. |
| The install command reports feed authentication or certificate errors. | A private feed, proxy, or corporate certificate policy blocks package restore. | Refresh the credentials or proxy configuration required by your organization. |
| The command succeeds, but `graphql` does not appear in the template list. | The template cache is stale, or an older template package is installed. | Uninstall and reinstall the template package. |

First, check your SDK:

```bash
dotnet --info
```

Then check package sources:

```bash
dotnet nuget list source
```

A typical machine has `nuget.org` enabled:

```text
Registered Sources:
  1.  nuget.org [Enabled]
      https://api.nuget.org/v3/index.json
```

If the template package may be stale, reinstall it:

```bash
dotnet new uninstall HotChocolate.Templates
dotnet new install HotChocolate.Templates
```

If your environment pins package versions, use the .NET template version syntax and install the Hot Chocolate template version your project is expected to use:

```bash
dotnet new install HotChocolate.Templates::16.0.0
```

Replace `16.0.0` with the exact v16 package version approved for your environment.

Verification:

```bash
dotnet new list graphql
```

The output should include the Hot Chocolate server template:

```text
Template Name   Short Name  Language
--------------  ----------  --------
GraphQL Server  graphql     C#
```

For .NET CLI template installation details, see the Microsoft [`dotnet new install` documentation](https://learn.microsoft.com/dotnet/core/tools/dotnet-new-install). For package source configuration, see the [`dotnet nuget list source` documentation](https://learn.microsoft.com/dotnet/core/tools/dotnet-nuget-list-source).

# `dotnet new graphql` Does Not Create the Expected Project

If the template is installed but project creation fails or files are not where the tutorial expects, check the following:

| Symptom | Likely cause | Fix |
| --- | --- | --- |
| `No templates or subcommands found matching: 'graphql'.` | The Hot Chocolate templates are not installed for the SDK selected by this terminal. | Run `dotnet new list graphql`. If `GraphQL Server` is missing, reinstall `HotChocolate.Templates`. |
| A different template is selected. | The command uses the wrong short name. | Use the short name `graphql` for the Hot Chocolate server template. |
| The output folder already exists or files would be overwritten. | The project name or output directory conflicts with existing files. | Choose a clean directory or a new project name. |
| The project appears in a parent or sibling directory. | The command ran from a different current directory than expected. | Check the current directory, then create the project with an explicit output path. |
| Visual Studio does not show the template. | The IDE has not refreshed the .NET template cache. | Restart Visual Studio after installing the template package. |

To create the tutorial project, navigate to the folder where you want the `GettingStarted` directory:

```bash
dotnet new graphql --name GettingStarted
```

If you want to remove ambiguity, provide the output directory:

```bash
dotnet new graphql --name GettingStarted --output GettingStarted
```

Then enter the project directory:

```bash
cd GettingStarted
```

Verification:

```bash
dotnet restore
```

The project directory should contain a project file and the starter server files used by the tutorial:

```text
GettingStarted.csproj
Program.cs
Types/
Properties/launchSettings.json
```

Return to [Install and scaffold](/docs/hotchocolate/v16/get-started/install-and-scaffold) when restore succeeds.

# The .NET SDK Version Is Wrong or Missing

If the command line, editor, restore, or build points at an unsupported SDK, review the following:

| Symptom | Likely cause | Fix |
| --- | --- | --- |
| `dotnet` is not recognized or command not found. | The SDK is not installed or not on `PATH`. | Install the [.NET SDK](https://dotnet.microsoft.com/download), then reopen the terminal. |
| `No .NET SDKs were found.` | Only the runtime is installed, or SDK discovery is broken. | Install the SDK, not only the runtime. |
| `NETSDK1045: The current .NET SDK does not support targeting .NET 8.0.` | The selected SDK is older than the target framework. | Install .NET 8 SDK or later for Hot Chocolate v16 getting-started pages. |
| The terminal and editor report different SDKs. | The editor process has an older `PATH` or SDK cache. | Restart the editor and open a new integrated terminal. |
| `dotnet --info` changes when run inside a repository. | A `global.json` file selects a specific SDK. | Run `dotnet --info` from the project directory and inspect `global.json`. |

From your project directory, run:

```bash
dotnet --info
```

For the v16 getting-started pages, the output should include .NET 8 SDK or later:

```text
.NET SDKs installed:
  8.0.404 [/usr/local/share/dotnet/sdk]
```

If an existing repository has `global.json`, confirm it selects an installed SDK. The .NET CLI uses that file when it chooses the SDK for commands in that directory.

Verification:

```bash
dotnet restore
dotnet build
```

Both commands should run without missing SDK or unsupported target framework errors.

For SDK selection details, see Microsoft's [`global.json` overview](https://learn.microsoft.com/dotnet/core/tools/global-json) and [.NET SDK installation documentation](https://learn.microsoft.com/dotnet/core/install/).

# Restore or Build Fails Because Package Versions Are Mixed

All `HotChocolate.*` packages in your app should use the same version family. Mixing versions is a common cause of restore, analyzer, source generation, and runtime errors.

| Symptom | Likely cause | Fix |
| --- | --- | --- |
| `NU1605: Detected package downgrade`. | Two package references require different Hot Chocolate versions. | Align all direct `HotChocolate.*` references to one v16 version. |
| `CS1061` says `AddTypes` cannot be found. | `HotChocolate.Types.Analyzers` is missing, did not restore, or does not match the server package version. | Add or align `HotChocolate.Types.Analyzers`, then rebuild. |
| `MissingMethodException`, missing type errors, or source-generator errors appear after adding a package. | A manually added package uses a different Hot Chocolate version than the template. | Update the manually added package to match the rest of the Hot Chocolate packages. |
| Code copied from another docs version does not compile. | The snippet uses APIs from another Hot Chocolate major version. | Use the v16 page for the feature, or adapt the package versions and APIs together. |

List your package references:

```bash
dotnet list package
```

If you use central package management, also check `Directory.Packages.props`.

For a getting-started v16 project, align direct Hot Chocolate package references such as:

```xml
<PackageReference Include="HotChocolate.AspNetCore" Version="16.x.x" />
<PackageReference Include="HotChocolate.AspNetCore.CommandLine" Version="16.x.x" />
<PackageReference Include="HotChocolate.Types.Analyzers" Version="16.x.x" />
```

Use the same concrete version for every `HotChocolate.*` package in the project.

If you add packages from the command line, specify the version you want:

```bash
dotnet add package HotChocolate.AspNetCore --version 16.x.x
dotnet add package HotChocolate.Types.Analyzers --version 16.x.x
```

Verification:

```bash
dotnet restore
dotnet build
```

The build should complete without downgrade warnings, missing members, or analyzer mismatch errors.

For NuGet restore behavior, see Microsoft docs for [`dotnet restore`](https://learn.microsoft.com/dotnet/core/tools/dotnet-restore) and [`dotnet list package`](https://learn.microsoft.com/dotnet/core/tools/dotnet-list-package).

# The Server Does Not Start

If `dotnet run` builds the project but the host exits before you can open the browser, check the following:

| Symptom | Likely cause | Fix |
| --- | --- | --- |
| `Failed to bind to address ... address already in use.` | Another process already uses the port in `launchSettings.json`. | Stop the other process or change the local URL. |
| `Unable to configure HTTPS endpoint.` | An HTTPS launch profile is selected, but the development certificate is missing or untrusted. | Trust the development certificate or use the HTTP launch profile while learning locally. |
| The app exits immediately after build. | Startup throws an exception before the server starts. | Read the first exception above the shutdown line. Use the schema or package sections if it names Hot Chocolate. |
| The server starts from one project, but not the one you edited. | The command ran from a different directory or startup project. | Run the command from the directory that contains the intended `.csproj`. |

The Hot Chocolate template uses an HTTP launch profile and prints a listening URL like:

```text
Now listening on: http://localhost:5095
Application started. Press Ctrl+C to shut down.
```

If the port is busy, change `Properties/launchSettings.json` in your project:

```json
"applicationUrl": "http://localhost:5096"
```

Then run the server again:

```bash
dotnet run
```

If an existing app uses HTTPS and reports a development certificate problem, follow Microsoft's [ASP.NET Core HTTPS development certificate guidance](https://learn.microsoft.com/aspnet/core/security/enforcing-ssl#trust-the-aspnet-core-https-development-certificate-on-windows-and-macos). The common repair command is:

```bash
dotnet dev-certs https --trust
```

Verification:

- The `dotnet run` terminal stays open.
- The output includes `Now listening on:`.
- You copy the URL from the current run, not from an earlier terminal.

Return to [Install and scaffold](/docs/hotchocolate/v16/get-started/install-and-scaffold) when the server is listening.

# The Browser Says Localhost Refused the Connection

If the server appears to run but the browser shows a network error such as `localhost refused to connect`, `This site can't be reached`, or `ERR_CONNECTION_REFUSED`, check the following:

| Symptom | Likely cause | Fix |
| --- | --- | --- |
| The browser cannot reach `localhost`. | The server is not running anymore. | Check the `dotnet run` terminal. Restart the server if the process stopped. |
| The URL uses the wrong port. | The port changed between runs or differs from the tutorial example. | Copy the active `Now listening on:` URL from the terminal. |
| The URL uses `https` while the server listens on `http`, or the reverse. | The scheme does not match the active endpoint. | Use the exact scheme from the terminal output. |
| The browser opens the app root instead of GraphQL. | The URL is missing `/graphql`. | Add `/graphql` to the listening URL. |
| You are using a container, remote dev environment, or VM. | The server is listening inside a boundary the browser cannot reach. | Use the forwarded port or host URL provided by that environment. |

The terminal output is the source of truth. If you see:

```text
Now listening on: http://localhost:5095
```

Open:

```text
http://localhost:5095/graphql
```

Verification:

- The browser no longer shows a network error page.
- The address bar matches the current listening URL plus `/graphql`.
- Nitro loads, or the server returns a GraphQL endpoint response.

If the browser reaches the app but `/graphql` returns 404, continue to the next section.

# `/graphql` Returns 404 or the Endpoint Is Missing

If the ASP.NET Core app responds but the GraphQL route does not, check the following:

| Symptom | Likely cause | Fix |
| --- | --- | --- |
| `/graphql` returns 404. | `app.MapGraphQL()` is missing or mapped at a different path. | Add or correct the endpoint mapping, then restart the app. |
| Existing REST or Minimal API routes work, but GraphQL does not. | GraphQL services or endpoint mapping were not added to the running app. | Confirm both registration and mapping are in the startup project. |
| The browser opens the wrong project. | Your editor or CLI launched another startup project. | Run from the project that contains the GraphQL setup. |
| A custom endpoint was configured. | GraphQL is mapped somewhere other than `/graphql`. | Open the custom path you passed to `MapGraphQL`. |

For v16 getting-started, `Program.cs` should register GraphQL services:

```csharp
builder.AddGraphQL().AddTypes();
```

And map the endpoint after building the app:

```csharp
app.MapGraphQL();
```

By default, `MapGraphQL()` exposes the endpoint at `/graphql`. If you use a custom path:

```csharp
app.MapGraphQL("/api/graphql");
```

Open that path instead:

```text
http://localhost:5095/api/graphql
```

Verification:

- The mapped path no longer returns 404.
- Opening the mapped path in a browser loads Nitro during local development, unless the tool is disabled.
- Sending a GraphQL request to the mapped path returns a GraphQL response.

For minimal integration, see [Add to an existing app](/docs/hotchocolate/v16/get-started/add-to-existing-app). For endpoint configuration, see [Endpoints](/docs/hotchocolate/v16/server/endpoints).

# Nitro Opens but Cannot Load the Schema

Nitro is a browser-based GraphQL client. It can load even if its document points at the wrong endpoint or if the server failed to build the schema.

| Symptom | Likely cause | Fix |
| --- | --- | --- |
| Nitro opens, but shows the schema as unavailable. | The document endpoint points at a stale URL or the wrong port. | Update the document endpoint to the current `/graphql` URL. |
| Nitro asks you to create a document. | The browser has no saved document for this endpoint. | Create a document and set the HTTP endpoint to the current `/graphql` URL. |
| Requests fail after restarting the server. | The port changed during restart. | Copy the new listening URL and update or reopen the Nitro document. |
| Nitro still shows the old schema after a C# change. | The server was not rebuilt, or Nitro has stale schema state. | Stop the server, rebuild, restart, then refresh the schema or reload the page. |
| Nitro loads, but schema refresh still fails. | The server has a schema build or startup error. | Check the `dotnet run` terminal for the first exception. |

When Nitro asks for an endpoint, use the active URL from the terminal and add `/graphql`:

```text
http://localhost:5095/graphql
```

If the server restarted, do not rely on an older tab. Use the current `Now listening on:` URL.

You can also verify the endpoint outside Nitro by requesting the schema SDL in a browser:

```text
http://localhost:5095/graphql?sdl
```

If schema download is enabled, this returns the schema definition language for the current server.

Verification:

- Nitro points at the same `/graphql` URL as the browser address or configured document endpoint.
- Nitro shows the schema as available.
- The starter query from [Run your first query](/docs/hotchocolate/v16/get-started/run-your-first-query) returns a JSON response with `data`.

# The Schema Fails to Build

If the app fails during startup after you add or change GraphQL types, resolvers, or services, check the following:

| Symptom | Likely cause | Fix |
| --- | --- | --- |
| The exception names schema building or type initialization. | A recent schema change cannot be inferred or registered. | Return to the last passing change, then reapply the change in smaller steps. |
| A resolver parameter cannot be resolved from dependency injection. | The resolver takes a service that was not registered, or Hot Chocolate treats the parameter as a GraphQL argument. | Register the service with ASP.NET Core DI, or change the resolver parameter shape. |
| A field name is duplicated on `Query`. | Two methods map to the same GraphQL field name, such as `GetBook` and `Book`. | Rename one method or configure a different GraphQL name. |
| `AddTypes()` is missing or generated registration is not available. | `HotChocolate.Types.Analyzers` is missing, not restored, or the attributed type is not `partial`. | Add or align the analyzer package, make `[QueryType]` classes `partial`, then rebuild. |
| A type cannot be inferred. | Hot Chocolate cannot infer a GraphQL type from the C# signature. | Use a supported C# shape, make nullability clear, or configure the type explicitly. |

For v16 implementation-first examples:

- Root query contributors use `[QueryType]`
- Classes marked with `[QueryType]` are `partial`
- The project references `HotChocolate.Types.Analyzers`
- `Program.cs` calls `builder.AddGraphQL().AddTypes()`
- Services injected into resolvers are registered with `builder.Services`

Start with your most recent edit. If removing it lets the server start, the environment is likely not the cause.

Build before running:

```bash
dotnet build
```

Then run:

```bash
dotnet run
```

Verification:

- The app starts and prints `Now listening on:`.
- Nitro shows the schema as available.
- The field you expected appears in the schema browser or in the SDL from `/graphql?sdl`.
- A query for that field returns a response with `data`, or a validation error that names only the query you typed.

After you recover, continue with [Queries](/docs/hotchocolate/v16/building-a-schema/queries), [Object Types](/docs/hotchocolate/v16/building-a-schema/object-types), or [Resolvers](/docs/hotchocolate/v16/resolvers-and-data/resolvers) for the deeper model.

# If You Are Still Blocked

Before asking for help, gather a small diagnostic bundle:

- Your operating system
- Whether you scaffolded a new project or added GraphQL to an existing app
- The output of `dotnet --info` from the project directory
- The output of `dotnet list package`
- The exact command you ran
- The first relevant error line
- The `Now listening on:` URL if the server starts
- The browser URL or Nitro endpoint you are using

You can also try a fresh scaffolded project in a clean directory:

```bash
dotnet new graphql --name HcTroubleshootingCheck
cd HcTroubleshootingCheck
dotnet build
dotnet run
```

If the new project works, your machine and package sources are likely healthy, and the issue is in your application. Compare `Program.cs`, package versions, and recent schema changes.

If the new project fails in the same way, focus on SDK selection, NuGet sources, template installation, or local ASP.NET Core startup.

When you need community help, use a minimal reproduction and your diagnostic bundle. Use GitHub issues for reproducible bugs. Use Discussions for setup questions or "what should I check next" questions.

When you are unblocked, choose your next page:

- New scaffolded server: [Install and scaffold](/docs/hotchocolate/v16/get-started/install-and-scaffold)
- Existing ASP.NET Core app: [Add to an existing app](/docs/hotchocolate/v16/get-started/add-to-existing-app)
- Server already running: [Run your first query](/docs/hotchocolate/v16/get-started/run-your-first-query)
- Finished the first query: [Next steps](/docs/hotchocolate/v16/get-started/next-steps)
