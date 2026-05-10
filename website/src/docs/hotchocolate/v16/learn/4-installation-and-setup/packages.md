---
title: "Packages"
description: "Choose the Hot Chocolate v16 packages for an ASP.NET Core server, align versions, add feature packages, and verify restore and build."
---

When you search NuGet, you'll find many `HotChocolate.*` packages. To get started, pick the package that matches the server you want to build. Add feature packages only when your schema or hosting scenario requires them.

By the end of this page, you'll know:

- which package to use for a standard ASP.NET Core GraphQL endpoint
- when to add the analyzer package for v16 implementation-first examples
- which feature packages cover authorization, data, and subscriptions
- how to keep all Hot Chocolate package versions in sync
- when to use the project template instead of installing packages directly

# Choose packages based on your goal

Begin by installing the minimum server package. Only add feature packages when your code or setup needs them.

| If you want to | Start with | Why |
| --- | --- | --- |
| Create a new starter server | `HotChocolate.Templates` | Installs a .NET CLI template. This is not a runtime dependency. |
| Add GraphQL to an ASP.NET Core app | `HotChocolate.AspNetCore` | The main ASP.NET Core server integration. Provides GraphQL service registration, endpoint mapping, HTTP transport, and Nitro. |
| Use v16 implementation-first examples | `HotChocolate.Types.Analyzers` | Enables source-generated type registration for `[QueryType]`, `[MutationType]`, `[SubscriptionType]`, and `AddTypes()`. |
| Protect GraphQL fields with roles or policies | `HotChocolate.AspNetCore.Authorization` | Connects Hot Chocolate authorization with ASP.NET Core authorization. |
| Add filtering, sorting, projections, or data middleware | `HotChocolate.Data` | Supplies common data features for resolvers and data sources. |
| Use an Entity Framework Core `DbContext` factory | `HotChocolate.Data.EntityFramework` | Adds helpers for Entity Framework-aware registration. |
| Use additional scalars or specialty types | `HotChocolate.Types.Scalars`, `HotChocolate.Types.NodaTime`, `HotChocolate.Types.Scalars.Upload`, or `HotChocolate.Types.Spatial` | Pick the package for your scalar or type family: validation scalars, NodaTime, file upload, or spatial GeoJSON types. |
| Add local or single-instance subscriptions | `HotChocolate.AspNetCore`; add `HotChocolate.Subscriptions.InMemory` if not already included | ASP.NET Core servers usually include the in-memory provider. Other hosts or explicit references may need the provider package. |
| Add subscriptions across multiple server instances | A shared provider package, such as `HotChocolate.Subscriptions.Redis`, `HotChocolate.Subscriptions.Nats`, `HotChocolate.Subscriptions.Postgres`, or `HotChocolate.Subscriptions.RabbitMQ` | Multi-instance deployments require a shared pub/sub system outside the server process. |

Before installing a package, make sure you know which feature or setup path requires it.

# Keep all Hot Chocolate packages on the same version

Always align the versions of your Hot Chocolate packages. This is the most important rule for working with Hot Chocolate v16.

Every direct `HotChocolate.*` package reference in your server project must use the same v16 version. All Hot Chocolate packages are released together as a set. If you mix versions, you may run into restore errors, build failures, schema construction issues, or runtime problems.

To check your current package versions, run this command from the directory containing your `.csproj` file:

```bash
dotnet list package
```

If your solution uses central package management, also review `Directory.Packages.props`.

For a v16 project, make sure entries like these all use the same version:

```xml
<PackageReference Include="HotChocolate.AspNetCore" Version="16.x.x" />
<PackageReference Include="HotChocolate.Types.Analyzers" Version="16.x.x" />
<PackageReference Include="HotChocolate.Data" Version="16.x.x" />
```

Do not mix v15, v16, and v17 package references in a single Hot Chocolate server project. Use the same concrete v16 version for every `HotChocolate.*` package.

External dependencies, such as database drivers, broker clients, or Microsoft ASP.NET Core packages, have their own compatibility requirements.

For more on .NET package tools, see the Microsoft docs for [`dotnet list package`](https://learn.microsoft.com/dotnet/core/tools/dotnet-list-package), [Central Package Management](https://learn.microsoft.com/nuget/consume-packages/central-package-management), and [NuGet package versioning](https://learn.microsoft.com/nuget/concepts/package-versioning).

# Install the minimum server package

If you already have an ASP.NET Core app or an empty ASP.NET Core project, follow this path to add Hot Chocolate.

From the directory containing your `.csproj` file, run:

```bash
dotnet add package HotChocolate.AspNetCore
dotnet add package HotChocolate.Types.Analyzers
```

`HotChocolate.AspNetCore` provides the server integration for `AddGraphQL()` and `MapGraphQL()`. `HotChocolate.Types.Analyzers` enables the source-generated registration used in the v16 Learn and Get Started examples.

If your project already uses a specific Hot Chocolate v16 version, make sure to add any new Hot Chocolate packages with that same version.

After installing the packages, restore and build your project:

```bash
dotnet restore
dotnet build
```

You should see:

```text
Build succeeded.
```

This confirms your package graph and compiler are in sync. The hosting setup pages will help you register GraphQL services and map the `/graphql` endpoint.

For the next step, continue with [ASP.NET Core setup](/docs/hotchocolate/v16/learn/4-installation-and-setup/aspnet-core/) or [Existing ASP.NET Core app](/docs/hotchocolate/v16/learn/4-installation-and-setup/existing-aspnet-core-app/).

# Add feature packages when your server needs them

Installing a feature package is only the first step. Most features also require you to register services, configure the schema, set up endpoints, or add infrastructure.

| If you want to | Install | Then configure |
| --- | --- | --- |
| Protect fields, object types, or operations with roles and policies | `HotChocolate.AspNetCore.Authorization` | Register ASP.NET Core authorization, call `.AddAuthorization()`, add auth middleware, and use Hot Chocolate authorization attributes or directives. See [Authorization](/docs/hotchocolate/v16/securing-your-api/authorization/). |
| Expose `where` filter arguments | `HotChocolate.Data` | Call `.AddFiltering()` and apply filtering to fields. See [Filtering](/docs/hotchocolate/v16/resolvers-and-data/filtering/). |
| Expose `order` sort arguments | `HotChocolate.Data` | Call `.AddSorting()` and apply sorting to fields. See [Sorting](/docs/hotchocolate/v16/resolvers-and-data/sorting/). |
| Project selected fields into an `IQueryable` data source | `HotChocolate.Data` | Call `.AddProjections()` and apply projections before filtering and sorting. See [Projections](/docs/hotchocolate/v16/resolvers-and-data/projections/). |
| Use an Entity Framework Core `DbContext` factory in resolvers | `HotChocolate.Data.EntityFramework` | Register the EF Core factory with ASP.NET Core and call `RegisterDbContextFactory<T>()`. See [Entity Framework Core](/docs/hotchocolate/v16/integrations/entity-framework/). |
| Expose additional scalar or specialty runtime types | `HotChocolate.Types.Scalars`, `HotChocolate.Types.NodaTime`, `HotChocolate.Types.Scalars.Upload`, or `HotChocolate.Types.Spatial` | Register the matching types, such as `.AddNodaTime()` or `.AddSpatialTypes()`, or apply explicit scalar types. See [Scalars](/docs/hotchocolate/v16/building-a-schema/scalars/), [Uploading files](/docs/hotchocolate/v16/server/files/), and [Spatial Data](/docs/hotchocolate/v16/integrations/spatial-data/). |
| Add subscriptions during local development | `HotChocolate.AspNetCore`; add `HotChocolate.Subscriptions.InMemory` if needed | For ASP.NET Core servers, the in-memory provider is usually included. Call `.AddInMemorySubscriptions()`, enable WebSockets, and add subscription fields. See [Subscriptions](/docs/hotchocolate/v16/building-a-schema/subscriptions/). |
| Add subscriptions across multiple app instances | `HotChocolate.Subscriptions.Redis`, `HotChocolate.Subscriptions.Nats`, `HotChocolate.Subscriptions.Postgres`, or `HotChocolate.Subscriptions.RabbitMQ` | Configure the matching broker and verify events cross server instances. Start from [Subscriptions](/docs/hotchocolate/v16/building-a-schema/subscriptions/), then apply your deployment requirements. |
| Add persisted operation storage | `HotChocolate.PersistedOperations.FileSystem`, `HotChocolate.PersistedOperations.Redis`, or another storage package | Configure the store and request pipeline. See [Persisted operations](/docs/hotchocolate/v16/performance/trusted-documents/). |
| Add instrumentation | `HotChocolate.Diagnostics` | Configure diagnostics and your observability backend. See [Instrumentation](/docs/hotchocolate/v16/server/instrumentation/). |

Keep this table up to date. If a feature page recommends a more specific provider package, follow that page after confirming your base package set.

# Use templates for new projects

Let Hot Chocolate create a starter project for you by using the project templates.

```bash
dotnet new install HotChocolate.Templates
dotnet new graphql --name LibraryServer --output LibraryServer
```

The template's short name is `graphql`. The generated project includes all required runtime package references, starter GraphQL types, an ASP.NET Core entry point, and a `/graphql` endpoint.

Use direct package installation if you already have an ASP.NET Core app and want to add GraphQL alongside your existing routes.

| Choose | When |
| --- | --- |
| Template path | You want a new project with the recommended starter structure. |
| Direct package path | You already have a project file, middleware, routes, authentication, health checks, or deployment settings that you want to keep. |

To confirm the template is installed, list available templates:

```bash
dotnet new list graphql
```

You should see a template named `GraphQL Server` with the short name `graphql`.

For a full walkthrough, see [Install and scaffold](/docs/hotchocolate/v16/get-started/install-and-scaffold/). For .NET template commands, refer to the Microsoft docs for [`dotnet new install`](https://learn.microsoft.com/dotnet/core/tools/dotnet-new-install) and [`dotnet new`](https://learn.microsoft.com/dotnet/core/tools/dotnet-new).

# Check packages after every change

After you add, remove, or update any package, always verify your setup:

```bash
dotnet restore
dotnet build
```

Check that:

- every direct `HotChocolate.*` reference in your server project uses the same v16 version
- central package management does not override a project with a different Hot Chocolate version
- each feature package you installed has a matching configuration step in `Program.cs` or in your type configuration
- the app builds before you start debugging GraphQL endpoint behavior

If `dotnet restore` reports a package downgrade or dependency conflict, align all Hot Chocolate versions first. If the build fails with missing methods like `AddTypes()`, `AddFiltering()`, or `AddInMemorySubscriptions()`, make sure the required package is installed and restored.

# Upgrade package references together

When you upgrade your Hot Chocolate app, always update all Hot Chocolate packages as a group.

Before you change any application code, follow this checklist:

1. List all direct `HotChocolate.*` references in each server project.
2. Update every reference to the same target v16 version.
3. Remove old or unused references only after the migration guide or feature page confirms they're no longer needed.
4. Run `dotnet restore`.
5. Run `dotnet build`.
6. Review feature-specific migration notes for authorization, data, subscriptions, scalars, adapters, and hosting.

If you're moving from v15 to v16, read [Migrate from 15 to 16](/docs/hotchocolate/v16/migrating/migrate-from-15-to-16/) before relying on package changes alone.

# Troubleshoot package setup

| Symptom | Likely cause | Fix | How to verify |
| --- | --- | --- | --- |
| Restore reports a package downgrade or dependency conflict. | Hot Chocolate package versions differ, or central package management overrides a project. | Align every direct `HotChocolate.*` reference in the server project to the same v16 version. | `dotnet restore` succeeds without downgrade warnings. |
| Build succeeds, but startup fails with a missing method or type-load error. | The app restored mixed Hot Chocolate package versions. | Check direct package references, align versions, restore, and rebuild. | The server starts far enough to build the schema or reaches the next setup error. |
| `dotnet new graphql` is not found. | The template package is not installed for the current SDK, or NuGet could not restore it. | Install `HotChocolate.Templates`, confirm NuGet access, then run `dotnet new list graphql`. | The `GraphQL Server` template appears with the short name `graphql`. |
| `[Authorize]` compiles but has no GraphQL effect. | The authorization package or Hot Chocolate authorization registration is missing, or the Microsoft attribute was used instead of the Hot Chocolate attribute. | Install `HotChocolate.AspNetCore.Authorization`, register authorization, and follow the [Authorization](/docs/hotchocolate/v16/securing-your-api/authorization/) page. | Unauthorized requests produce the expected GraphQL authorization error. |
| Filtering, sorting, or projections do not appear in the schema. | `HotChocolate.Data` is missing, or the feature is not registered and applied. | Install `HotChocolate.Data`, register the data feature, apply the field middleware, and rebuild. | The schema contains the expected arguments. |
| Subscriptions work locally but not across server instances. | The app uses the in-memory provider in a deployment that needs shared pub/sub. | Choose and configure a shared provider package such as Redis, NATS, Postgres, or RabbitMQ. | An event published on one instance reaches a subscriber connected to another instance. |

For first-server recovery, see [Get Started troubleshooting](/docs/hotchocolate/v16/get-started/troubleshooting/). For NuGet restore behavior, refer to the Microsoft docs for [`dotnet restore`](https://learn.microsoft.com/dotnet/core/tools/dotnet-restore), [`dotnet add package`](https://learn.microsoft.com/dotnet/core/tools/dotnet-add-package), and [NuGet package restore](https://learn.microsoft.com/nuget/consume-packages/package-restore).

# Next steps

You are ready to move on when restore and build both succeed, and every installed package has a matching setup or configuration step.

- Go to [Install and scaffold](/docs/hotchocolate/v16/get-started/install-and-scaffold/) if you want to generate a new project.
- Use [ASP.NET Core setup](/docs/hotchocolate/v16/learn/4-installation-and-setup/aspnet-core/) for a clean, hosted server.
- See [Existing ASP.NET Core app](/docs/hotchocolate/v16/learn/4-installation-and-setup/existing-aspnet-core-app/) if you need to add GraphQL to an existing app.
- Visit the feature pages for [Authorization](/docs/hotchocolate/v16/securing-your-api/authorization/), [Filtering](/docs/hotchocolate/v16/resolvers-and-data/filtering/), [Sorting](/docs/hotchocolate/v16/resolvers-and-data/sorting/), [Projections](/docs/hotchocolate/v16/resolvers-and-data/projections/), and [Subscriptions](/docs/hotchocolate/v16/building-a-schema/subscriptions/).
