---
title: "Set up the project"
description: "Create the Hot Chocolate tutorial project, run the local server, open Nitro, confirm the starter schema, and stop at the first checkpoint."
---

This chapter guides you through creating the project that you will use throughout the tutorial.

By the end of this section, you will have:

- Installed the Hot Chocolate project templates
- Created a project named `LibraryServer`
- Identified the key starter files for upcoming chapters
- Started the server using `dotnet run`
- Opened Nitro at the local `/graphql` endpoint
- Verified that the starter schema is available

All following chapters assume this project structure. If you want to know how to recover the source at any point, see [Source code and checkpoints](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/00-source-code-and-checkpoints/). If you have not prepared your machine, review [Prerequisites](/docs/hotchocolate/v16/get-started/prerequisites/) before proceeding.

# Install the Hot Chocolate templates

Install the template package for your .NET SDK in this terminal session:

```bash
dotnet new install HotChocolate.Templates
```

This command adds the Hot Chocolate templates to `dotnet new`. If the templates are already present, the .NET CLI will indicate that the package is already installed or has been updated. As long as the `graphql` template is available, you can continue.

To confirm the template is installed, list the available GraphQL templates:

```bash
dotnet new list graphql
```

You should see output similar to the following, with a row named `GraphQL Server` and the short name `graphql`:

```text
Template Name   Short Name  Language
--------------  ----------  --------
GraphQL Server  graphql     C#
```

If the install command fails or `graphql` does not appear in the list, refer to [Get started troubleshooting](/docs/hotchocolate/v16/get-started/troubleshooting/) for help with template installation, NuGet access, or SDK selection.

# Create the project

Generate the tutorial project using the `graphql` template:

```bash
dotnet new graphql --name LibraryServer --output LibraryServer
cd LibraryServer
```

Your terminal should now be inside the new project directory.

To verify your current directory on macOS or Linux, run:

```bash
pwd
```

Expected output format:

```text
/your/workspace/LibraryServer
```

On Windows Command Prompt, use `cd` with no arguments:

```bash
cd
```

Expected output format:

```text
C:\your\workspace\LibraryServer
```

Open the project folder in your editor. For Visual Studio Code, you can use:

```bash
code .
```

If `code .` is not recognized, open the `LibraryServer` folder manually in your editor. All remaining commands assume your terminal is in the `LibraryServer` project directory.

# Review the generated files

Before running the server, look at the generated file structure:

```text
LibraryServer/
├── LibraryServer.csproj
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

| File or folder                | Purpose                                                                 |
|-------------------------------|------------------------------------------------------------------------|
| `LibraryServer.csproj`        | Project file that restores Hot Chocolate packages for the server        |
| `Program.cs`                  | ASP.NET Core entry point; registers GraphQL and maps the endpoint       |
| `Types/Query.cs`              | Starter root query type; exposes the template `book` field              |
| `Types/Book.cs`, `Author.cs`  | Starter C# records used by the template schema                         |
| `Properties/launchSettings.json` | Local development launch settings, including localhost URLs           |
| `Properties/ModuleInfo.cs`    | Module metadata for the generated project; no edits needed here now     |

This starter schema provides a baseline. In the next chapter, you will begin adapting the project to the tutorial's library domain.

# Run the server

Start the application:

```bash
dotnet run
```

Leave this terminal running. The server will stop if you close the process or press <kbd>Ctrl</kbd> + <kbd>C</kbd>.

When the app starts, the terminal displays one or more listening URLs. The port number may vary on your machine.

Example output:

```text
Now listening on: http://localhost:5095
Application started. Press Ctrl+C to shut down.
```

Check that:

- You ran the command from the `LibraryServer` project directory
- The terminal shows a `Now listening on:` URL
- The process is still running

If `dotnet run` fails, review the first restore, build, or startup error in the terminal. Make sure you are in the `LibraryServer` directory and that your SDK and package sources match the [Prerequisites](/docs/hotchocolate/v16/get-started/prerequisites/). For setup issues, see [Get started troubleshooting](/docs/hotchocolate/v16/get-started/troubleshooting/).

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

Nitro should load in your browser for the running Hot Chocolate endpoint. Depending on your previous browser state, Nitro may open an editor, a landing screen, or prompt you to create a document.

If Nitro prompts you to create a document:

1. Select **Create Document**
2. Confirm the HTTP endpoint matches the local `/graphql` URL you opened
3. Apply the endpoint setting

Check that:

- The `dotnet run` terminal is still running
- The browser URL ends with `/graphql`
- Nitro is connected to your local endpoint
- Nitro shows that schema information is available or can be loaded

If Nitro does not load, confirm the server is running and the browser URL uses the correct port. If Nitro opens but points to a different endpoint, update the endpoint in Nitro to your local `/graphql` URL.

# Verify the starter schema

Before making any changes, confirm that Hot Chocolate has produced a GraphQL schema.

In Nitro, open the schema view or schema explorer. The UI may differ by Nitro version, but you are looking for schema information for the current endpoint.

Look for these schema elements:

- A root `Query` type
- The starter `book` field on `Query`
- The schema loads without errors

The starter schema should look like this:

```graphql
type Query {
  book: Book!
}

type Book {
  title: String!
  author: Author!
}

type Author {
  name: String!
}
```

You can also run the starter query to verify the setup. Paste this operation into Nitro:

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

Your checkpoint is a response with a top-level `data` property containing a `book` object, or a Nitro schema view showing the `Query.book` field.

This template schema is a starting point. It confirms the project runs, the endpoint is reachable, and Nitro can inspect the server. The next chapter will guide you in modifying this schema for the tutorial project.

# Checkpoint: ready to define your first types

If any checkpoint above did not succeed, resolve the setup issue before moving on.

You are ready for the next chapter when all of these are true:

- Hot Chocolate templates are installed, and `dotnet new list graphql` shows `GraphQL Server` with the short name `graphql`
- The `LibraryServer` project exists
- Your terminal is in the `LibraryServer` project directory
- `dotnet run` starts the server and displays a listening URL
- The local `/graphql` endpoint opens Nitro
- Nitro can load the schema
- The schema contains a root `Query` type with the starter `book` field

If your local state changes later, revisit [Source code and checkpoints](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/00-source-code-and-checkpoints/) to compare or restore the chapter state.

Keep the project folder open for editing, then continue to [Define your first types](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/02-define-your-first-types/).
