# CLAUDE.md - Claude Code Configuration

This file provides guidance to Claude Code when working with this repository.

## Project Overview

This is the **ChilliCream GraphQL Platform** - an open-source GraphQL ecosystem for .NET containing:

- **Hot Chocolate**: GraphQL server framework for .NET
- **Strawberry Shake**: Type-safe GraphQL client for .NET
- **Green Donut**: Data fetching primitives including DataLoader for batching/caching, pagination, and other data fetching tools
- **Nitro**: GraphQL IDE and API cockpit

## Quick Start

### Using Devcontainer (Recommended for Agents)

This repository includes a devcontainer with all dependencies pre-installed:

- .NET SDKs 6, 7, 8, 9, 10
- Node.js LTS + Yarn
- GitHub CLI + Azure CLI
- Docker-in-docker support

The devcontainer automatically runs `init.sh` on creation, so you're ready to go immediately.

### Manual Setup

If not using devcontainer, initialize the repository:

```bash
./init.sh
```

This script:

1. Installs website dependencies (`cd website && yarn`)
2. Restores .NET packages (`./build.sh restore`)

### Build

```bash
./build.sh compile
```

### Run Tests

```bash
./build.sh test
```

## Directory Structure

```text
src/
├── All.slnx                   # Main solution (244 projects)
├── HotChocolate/              # Core GraphQL server
│   ├── Core/                  # Execution engine, types, validation
│   │   ├── Abstractions/      # Core interfaces and contracts
│   │   ├── Execution/         # Query execution pipeline
│   │   ├── Types/             # GraphQL type system
│   │   ├── Validation/        # Query validation
│   │   └── Subscriptions/     # Real-time subscriptions
│   ├── AspNetCore/            # ASP.NET Core integration
│   ├── Language/              # GraphQL parsing & syntax
│   ├── Fusion-vnext/          # Distributed GraphQL (next gen)
│   ├── Data/                  # EF Core, MongoDB, etc.
│   └── Utilities/             # Shared utilities
├── GreenDonut/                # Data fetching primitives (DataLoader, pagination, etc.)
├── CookieCrumble/             # Snapshot testing framework
└── StrawberryShake/           # GraphQL client

website/                       # Gatsby documentation site
templates/                     # Project templates
.build/                        # Nuke build automation
```

## Technology Stack

- **.NET**: SDK 10.0.100 (supports .NET 8, 9, 10)
- **Build System**: Nuke.Build
- **Testing**: xUnit with CookieCrumble for snapshots
- **Documentation**: Gatsby + React + TypeScript

## Code Conventions

### C# Style

- File-scoped namespaces (`namespace Foo;`)
- 4-space indentation
- Expression-bodied members where appropriate
- Records for immutable data types

### Project Naming

- `HotChocolate.{Feature}` - Server features
- `HotChocolate.{Feature}.Tests` - Test projects
- `GreenDonut.{Feature}` - Data fetching features

### Testing

- Tests live in parallel `*.Tests` projects
- Use `CookieCrumble` for snapshot testing
- Snapshot files go in `__snapshots__/` directories

## Common Tasks

### Adding a New Feature

1. Create project in appropriate `src/HotChocolate/{Area}/` directory
2. Add corresponding test project with `.Tests` suffix
3. Add projects to `src/All.slnx`

### Running Specific Tests

```bash
dotnet test src/HotChocolate/Core/test/Types.Tests/HotChocolate.Types.Tests.csproj
```

### Building Documentation

```bash
cd website
yarn start    # Development server
yarn build    # Production build
```

## Key Patterns

### Type System

GraphQL types are defined in `src/HotChocolate/Core/src/Types/`. The type system uses:

- `ObjectType<T>` for object types
- `InputObjectType<T>` for input types
- `InterfaceType<T>` for interfaces
- Descriptors pattern for configuration

### Execution Pipeline

The execution pipeline in `src/HotChocolate/Core/src/Execution/` uses:

- Middleware pattern for request processing
- `IQueryExecutor` as the main entry point
- `IResolverContext` for field resolution

### DataLoader Pattern

Green Donut implements data fetching primitives:

- `DataLoaderBase<TKey, TValue>` for custom loaders
- Automatic batching and caching
- Pagination support
- Integration with Hot Chocolate via `[DataLoader]` attribute

## Important Files

- `global.json` - .NET SDK version
- `.editorconfig` - Code style rules
- `nuget.config` - Package sources
- `src/All.slnx` - Main solution file
- `.build/Build.csproj` - Build automation

## Debugging Tips

- Solution file: `src/All.slnx`
- Use `dotnet build` for quick compilation checks
- Snapshot mismatches show diffs in test output
- Check `__snapshots__/` for expected test output
