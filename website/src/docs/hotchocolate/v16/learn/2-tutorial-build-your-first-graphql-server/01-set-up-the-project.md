---
title: "Set up the project"
description: "Install the Hot Chocolate templates, create CatalogServer, run the starter server, open Nitro, and verify the first query."
---

In this chapter, you will create the project that you will use for the product catalog tutorial.

By the end of this chapter, you will have:

- Installed the Hot Chocolate project templates
- Created a project named `CatalogServer`
- Built and started the server
- Opened Nitro at the local `/graphql` endpoint
- Run the generated starter query
- Identified the files you will edit in the next chapter

If you have not prepared your machine yet, review [Prerequisites](/docs/hotchocolate/v16/get-started/prerequisites/) before continuing.

# Install the Hot Chocolate templates

Install the template package for your .NET SDK:

```bash
dotnet new install HotChocolate.Templates
```

This command adds the Hot Chocolate templates to `dotnet new`. If the templates are already installed, the .NET CLI may report that the package is already present or has been updated.

Confirm that the GraphQL server template is available:

```bash
dotnet new list graphql
```

You should see output that includes a `GraphQL Server` template with the short name `graphql`:

```text
Template Name   Short Name  Language
--------------  ----------  --------
GraphQL Server  graphql     C#
```

If the template does not appear, see [Get started troubleshooting](/docs/hotchocolate/v16/get-started/troubleshooting/) for help with template installation, NuGet access, or SDK selection.

# Create CatalogServer

Create the tutorial project:

```bash
dotnet new graphql --name CatalogServer --output CatalogServer
cd CatalogServer
```

All remaining commands in this tutorial assume your terminal is inside the `CatalogServer` directory.

To check your current directory on macOS or Linux, run:

```bash
pwd
```

Expected output format:

```text
/your/workspace/CatalogServer
```

On Windows Command Prompt, use `cd` with no arguments:

```bash
cd
```

Expected output format:

```text
C:\your\workspace\CatalogServer
```

Open the project folder in your editor. For Visual Studio Code, you can run:

```bash
code .
```

If `code .` is not available, open the `CatalogServer` folder manually in your editor.

# Review the generated files

The generated project should look similar to this:

```text
CatalogServer/
├── CatalogServer.csproj
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

Focus on these files for now:

| File or folder | Purpose |
| --- | --- |
| `CatalogServer.csproj` | Project file that restores the Hot Chocolate packages |
| `Program.cs` | ASP.NET Core entry point that registers GraphQL and maps the endpoint |
| `Types/Query.cs` | Starter root query type with the generated `book` field |
| `Types/Book.cs`, `Types/Author.cs` | Starter C# records used by the generated schema |
| `Properties/launchSettings.json` | Local development URLs used by `dotnet run` |
| `Properties/ModuleInfo.cs` | Module metadata for the generated project |

The template starts with a small book example. You will replace it with the product catalog domain in the next chapter.

# Build the project

Build the project before starting the server:

```bash
dotnet build
```

You should see:

```text
Build succeeded.
```

If the build fails, read the first error in the terminal. Confirm that you are in the `CatalogServer` directory and that your SDK and NuGet sources match the [Prerequisites](/docs/hotchocolate/v16/get-started/prerequisites/).

# Run the server

Start the application:

```bash
dotnet run
```

Leave this terminal running. The server stops when you close the process or press <kbd>Ctrl</kbd> + <kbd>C</kbd>.

When the app starts, the terminal displays one or more listening URLs. The port number can vary on your machine.

Example output:

```text
Now listening on: http://localhost:5095
Application started. Press Ctrl+C to shut down.
```

Check that:

- You ran the command from the `CatalogServer` project directory
- The terminal shows a `Now listening on:` URL
- The process is still running

# Open Nitro

Copy the listening URL from your terminal and append `/graphql`.

For example, if your terminal shows:

```text
http://localhost:5095
```

Open this address in your browser:

```text
http://localhost:5095/graphql
```

Use the port from your terminal, not the example. If you see an HTTPS URL and your local certificate is trusted, you can use the HTTPS address with `/graphql` as well.

Nitro should load for the running Hot Chocolate endpoint. Depending on your previous browser state, Nitro may open an editor, a landing screen, or ask you to create a document.

If Nitro asks you to create a document:

1. Select **Create Document**
2. Confirm the HTTP endpoint matches the local `/graphql` URL you opened
3. Apply the endpoint setting

If Nitro does not load, confirm that the `dotnet run` terminal is still running and that the browser URL uses the same port shown in the terminal.

# Run the starter query

Before changing any code, confirm that the generated schema works.

Paste this operation into Nitro:

```graphql
{
  book {
    title
    author {
      name
    }
  }
}
```

Run the query.

Expected response:

```json
{
  "data": {
    "book": {
      "title": "C# in depth.",
      "author": {
        "name": "Jon Skeet"
      }
    }
  }
}
```

This verifies that:

- The server is running
- `/graphql` is mapped
- Nitro can connect to the endpoint
- Hot Chocolate generated a schema
- The starter resolver returns data

# Checkpoint: ready to define catalog types

Continue when all of these are true:

- `dotnet new list graphql` shows the `GraphQL Server` template
- The `CatalogServer` project exists
- Your terminal is in the `CatalogServer` directory
- `dotnet build` succeeds
- `dotnet run` starts the server and displays a listening URL
- Nitro opens at the local `/graphql` endpoint
- The starter query returns a top-level `data` property with a `book` object

Keep the project folder open. In the next chapter, you will replace the generated book example with the first product catalog types: [Define your first types](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/02-define-your-first-types/).
