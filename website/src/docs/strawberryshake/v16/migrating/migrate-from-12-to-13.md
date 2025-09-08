---
title: Migrate Strawberry Shake from 12 to 13
---

This guide will walk you through the manual migration steps to update your Strawberry Shake GraphQL client to version 13.

# How to update

In this release we tried to simplify the setup of Strawberry Shake by introducing meta packages for specific use cases. For this reason upgrading has become a little different than just bumping all of the version numbers.

## 1. Update tools

Update `StrawberryShake.Tools` in the `dotnet-tools.json` file to the latest `13.x.x`:

```json
{
  "version": 1,
  "isRoot": true,
  "tools": {
    "strawberryshake.tools": {
      "version": "13.0.2",
      "commands": ["dotnet-graphql"]
    }
  }
}
```

## 2. Cleanup packages

Remove the following packages from your project:

- StrawberryShake.CodeGeneration.CSharp.Analyzers
- StrawberryShake.Transport.Http
- StrawberryShake.Transport.WebSockets

## 3. Update packages

Update **all** of the remaining `StrawberryShake.*` packages in your project to the latest `13.x.x`.

## 4. Clean project

Run `dotnet clean` and remove the `Generated` directory or the directory you designated as `outputDirectoryName` in the `.graphqlrc.json` file.

## 5. Install meta package

Select one of the new meta packages we created for StrawberryShake, depending on your use case:

| Package                | Description                                                                                                                                                                           |
| ---------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| StrawberryShake.Blazor | For Blazor projects, use this package in your project, and we pre-configured it to generate Razor components automatically and use a client-side store for reactive web applications. |
| StrawberryShake.Maui   | For Maui projects, use this package in your project, and we pre-configured it to generate and use a client-side store for reactive mobile applications.                               |
| StrawberryShake.Server | For consoles or backend-to-backend communication, we have the server profile, which does not have a client store but gives you a strongly typed client.                               |

Install the latest `13.x.x` of that meta package into your project.

## 6. Generate C# types

Run `dotnet build` to generate the C# types from your `.graphql` documents.

# Breaking changes

Things that have been removed or had a change in behavior that may cause your code not to compile or lead to unexpected behavior at runtime if not addressed.

## No more Source Generators

Strawberry Shake does now **no longer** use Source Generators to create C# types from your `.graphql` documents. Mainly because Source Generators can not (yet) be chained, so this led to lots of problems with Source Generators interfering with each other.

Consequently there isn't a v13 of the `StrawberryShake.CodeGeneration.CSharp.Analyzers` NuGet package.

Instead of Source Generators we are now using MSBuild. This means that C# types from your `.graphql` documents are only generated once you build your project.
