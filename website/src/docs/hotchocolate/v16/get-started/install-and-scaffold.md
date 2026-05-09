---
title: "Install and scaffold"
description: "Install the Hot Chocolate project templates, create a GraphQL server, build it, run it locally, and open Nitro."
---

This guide walks you through setting up a Hot Chocolate GraphQL server from a prepared .NET environment. By the end, you will have:

- Installed the Hot Chocolate templates for the .NET CLI
- Created a project named `GettingStarted`
- Built the project
- Started the local server
- Opened Nitro at the GraphQL endpoint

You will run your first GraphQL query on the next page. Here, the focus is on getting the server running and confirming that the endpoint is accessible.

Before starting, make sure you have completed the [Prerequisites](/docs/hotchocolate/v16/get-started/prerequisites) or have a supported .NET SDK, NuGet access, and a browser ready.

# Create your first Hot Chocolate server project

Hot Chocolate provides a project template for the .NET CLI. This template generates a small ASP.NET Core application that includes:

- A local GraphQL endpoint at `/graphql`
- Starter GraphQL types
- The required package references for the server
- Nitro available at the endpoint during local development

For this tutorial, use `GettingStarted` as your project name. Using the same name helps keep your files and terminal output consistent with the examples in later pages.

# Install the Hot Chocolate templates

Install the template package once for your current .NET SDK installation:

```bash
dotnet new install HotChocolate.Templates
```

This command adds the Hot Chocolate templates to `dotnet new`. The output may vary depending on your SDK version, but you should see a template named `GraphQL Server` with the short name `graphql`.

If the templates are already installed, the .NET CLI may indicate that the package is already available or has been updated. As long as `dotnet new graphql` is recognized, you are ready to proceed.

To verify the templates are installed, run:

```bash
dotnet new list graphql
```

The output may include additional columns, depending on your .NET SDK version. A valid result includes `GraphQL Server` with the short name `graphql`.

```text
Template Name   Short Name  Language
--------------  ----------  --------
GraphQL Server  graphql     C#
```

If the install command fails or `graphql` does not appear in the list, see [Troubleshooting](/docs/hotchocolate/v16/get-started/troubleshooting) for help with template installation, NuGet access, or SDK selection.

# Scaffold the server project

Create a new project using the `graphql` template:

```bash
dotnet new graphql --name GettingStarted
```

This command creates a `GettingStarted` directory. Change into this directory before building or running the app:

```bash
cd GettingStarted
```

After changing directories, your terminal should be inside the generated project. You can check this with `pwd` on macOS or Linux, or `cd` on Windows.

If you want to explore the generated files, open the folder in your editor:

```bash
code .
```

The `code .` command works if the VS Code CLI is installed. If it does not work, open the folder from your editor manually.

Opening the editor is optional. The following CLI commands work from any terminal as long as you are in the `GettingStarted` project directory.

# Check what was created

Before running the server, review the generated project structure:

```text
GettingStarted/
├── GettingStarted.csproj
├── Program.cs
├── Properties/
│   ├── launchSettings.json
│   └── ModuleInfo.cs
├── Types/
│   ├── Author.cs
│   ├── Book.cs
│   └── Query.cs
├── appsettings.Development.json
└── appsettings.json
```

The exact package versions and target framework depend on the template version you installed.

For now, focus on these main components:

| File or folder | Purpose |
|---|---|
| `GettingStarted.csproj` | Project file with Hot Chocolate package references |
| `Program.cs` | ASP.NET Core entry point that registers GraphQL and maps the endpoint |
| `Types/` | Starter C# types used to build the initial schema |
| `Properties/launchSettings.json` | Local development launch settings, including the localhost URL |
| `Properties/ModuleInfo.cs` | Module metadata for the generated server |

You will learn more about how these files work together in the generated project explainer. For now, continue with the setup steps.

# Restore and build the project

Build the project with:

```bash
dotnet build
```

This command restores NuGet packages if needed and compiles the project.

You should see output like:

```text
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

If restore or build fails, check that:

- You are inside the `GettingStarted` directory
- `dotnet --version` reports a supported SDK
- NuGet can reach the configured package sources
- The template and Hot Chocolate packages are compatible versions

For troubleshooting steps, see [Troubleshooting](/docs/hotchocolate/v16/get-started/troubleshooting).

# Run the server

Start the application with:

```bash
dotnet run
```

Leave this terminal open. The server will stop if you close the process or press <kbd>Ctrl</kbd> + <kbd>C</kbd>.

When the app starts, the terminal displays the URL it is listening on. The port may vary between machines and projects.

Look for output like:

```text
Now listening on: http://localhost:5095
Application started. Press Ctrl+C to shut down.
```

Copy the URL from your terminal and add `/graphql` to it. For example:

```text
http://localhost:5095/graphql
```

If your terminal shows a different port, use that port in the URL.

If the server does not start or the port is already in use, refer to [Troubleshooting](/docs/hotchocolate/v16/get-started/troubleshooting).

# Open Nitro

Open your browser and navigate to the local `/graphql` endpoint. For example:

```text
http://localhost:5095/graphql
```

Nitro should load and connect to your local endpoint. Depending on your browser and Nitro's state, you might see a landing screen, an editor, or a prompt to create a document for the endpoint.

Check that:

- The `dotnet run` terminal is still running
- The browser loads Nitro
- Nitro points to your local `/graphql` endpoint

You do not need to write a query yet. The next page will guide you through running the starter query and viewing the JSON response.

If the browser cannot connect, review these common issues:

| Symptom | What to check |
|---|---|
| The browser says the site cannot be reached | Is `dotnet run` still running? Did you copy the correct port? |
| You see an ASP.NET Core 404 page | Does the URL end with `/graphql`? |
| Nitro does not load | Refresh the page, verify the server is running, or check Nitro troubleshooting |

For additional help, see [Troubleshooting](/docs/hotchocolate/v16/get-started/troubleshooting).

# Continue to the first query

You now have a local Hot Chocolate server running and Nitro open at the GraphQL endpoint.

Keep the terminal running and continue to [Run your first query](/docs/hotchocolate/v16/get-started/run-your-first-query).
