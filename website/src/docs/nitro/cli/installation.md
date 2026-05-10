---
title: Installation
---

<!-- TODO: document the GitHub Actions and Azure Pipelines tasks for installing/running the Nitro CLI on this page. -->

The Nitro CLI ships in several flavors so you can pick whatever fits your environment best.

# .NET tool

If you have the .NET SDK installed, you can install the CLI as a [.NET tool](https://learn.microsoft.com/dotnet/core/tools/global-tools).

Install as a local tool, scoped to a repository via a tool manifest. From the repository root:

```shell
dotnet new tool-manifest
dotnet tool install ChilliCream.Nitro.CommandLine
```

Local tools are restored with `dotnet tool restore` and invoked through `dotnet tool run nitro` (or `dotnet nitro`). Check the manifest (`./.config/dotnet-tools.json`) into source control so every collaborator uses the same version.

Or install globally:

```shell
dotnet tool install -g ChilliCream.Nitro.CommandLine
```

# npm

The CLI is published to npm as [`@chillicream/nitro`](https://www.npmjs.com/package/@chillicream/nitro). For one-off invocations run it with `npx`. The `@latest` tag opts out of npm's local cache so each run pulls the newest release:

```shell
npx @chillicream/nitro@latest --version
```

# Homebrew (macOS and Linux)

The CLI is available through the [`chillicream/tools`](https://github.com/ChilliCream/homebrew-tools) tap:

```shell
brew tap chillicream/tools
brew install nitro-cli
```

To upgrade later:

```shell
brew update
brew upgrade nitro-cli
```

# Pre-built binaries

Pre-built binaries for every supported OS and architecture are attached to each [GitHub release](https://github.com/ChilliCream/graphql-platform/releases).

| Platform                    | Asset                         |
| --------------------------- | ----------------------------- |
| Linux x64                   | `nitro-linux-x64.tar.gz`      |
| Linux x64 (musl, Alpine)    | `nitro-linux-musl-x64.tar.gz` |
| Linux arm64                 | `nitro-linux-arm64.tar.gz`    |
| macOS x64 (Intel)           | `nitro-osx-x64.zip`           |
| macOS arm64 (Apple Silicon) | `nitro-osx-arm64.zip`         |
| Windows x64                 | `nitro-win-x64.zip`           |
| Windows x86                 | `nitro-win-x86.zip`           |

Extract the archive and place the `nitro` binary somewhere on your `PATH`. The binaries are self-contained, no .NET SDK or runtime is required on the target machine.
