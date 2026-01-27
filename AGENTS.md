# AGENTS.md - OpenAI Codex Configuration

This file provides guidance to OpenAI Codex and other AI coding agents when working with this repository.

## Project Summary

**ChilliCream GraphQL Platform** - A comprehensive GraphQL ecosystem for .NET

| Product         | Description                                            |
| --------------- | ------------------------------------------------------ |
| Hot Chocolate   | GraphQL server framework                               |
| Strawberry Shake| Type-safe GraphQL client                               |
| Green Donut     | Data fetching primitives (DataLoader, pagination, etc.)|
| Nitro           | GraphQL IDE                                            |

## Setup Instructions

### Using Devcontainer (Recommended)

This repository includes a devcontainer (`.devcontainer/`) with everything pre-installed:

- .NET SDKs 6, 7, 8, 9, 10
- Node.js LTS + Yarn
- GitHub CLI + Azure CLI
- Docker-in-docker support

The devcontainer automatically runs `init.sh` on creation - no manual setup needed.

### Manual Setup

If not using devcontainer, initialize first:

```bash
./init.sh
```

This restores website packages (yarn) and .NET packages.

### Build

```bash
./build.sh compile
```

### Test

```bash
./build.sh test
```

### Single Project Build

```bash
dotnet build src/HotChocolate/Core/src/Types/HotChocolate.Types.csproj
```

## Repository Layout

```text
src/All.slnx                   # Master solution (244 projects)

src/HotChocolate/              # GraphQL Server
  Core/
    Abstractions/              # Interfaces, contracts
    Execution/                 # Query execution
    Types/                     # Type system
    Validation/                # Query validation
    Subscriptions/             # Real-time updates
  AspNetCore/                  # HTTP integration
  Language/                    # Parser, syntax tree
  Fusion-vnext/                # Distributed GraphQL
  Data/                        # Database integrations

src/GreenDonut/                # Data fetching primitives
src/StrawberryShake/           # GraphQL client
src/CookieCrumble/             # Snapshot testing

website/                       # Documentation (Gatsby)
templates/                     # Project templates
.build/                        # Build scripts (Nuke)
```

## Tech Stack

| Component | Technology     |
| --------- | -------------- |
| Runtime   | .NET 8/9/10    |
| SDK       | 10.0.100       |
| Build     | Nuke.Build     |
| Tests     | xUnit          |
| Snapshots | CookieCrumble  |
| Docs      | Gatsby + React |

## Coding Standards

### C# Conventions

- File-scoped namespaces: `namespace HotChocolate.Types;`
- 4-space indentation
- Use records for immutable types
- Expression-bodied members preferred
- No trailing whitespace

### Naming Conventions

- Projects: `HotChocolate.{Feature}`
- Tests: `HotChocolate.{Feature}.Tests`
- Interfaces: `I{Name}` prefix
- Async methods: `{Name}Async` suffix

### File Organization

- One type per file (generally)
- Test files mirror source structure
- Snapshots in `__snapshots__/` folders

## Testing Guidelines

### Running Tests

```bash
# All tests
./build.sh test

# Specific project
dotnet test src/HotChocolate/Core/test/Types.Tests/

# Specific test
dotnet test --filter "FullyQualifiedName~MyTestName"
```

### Snapshot Testing

- Uses CookieCrumble library
- Snapshots stored in `__snapshots__/`
- Update snapshots by deleting old file and re-running test

### Test Structure

```csharp
[Fact]
public async Task MyFeature_Should_WorkCorrectly()
{
    // Arrange
    var schema = await new ServiceCollection()
        .AddGraphQL()
        .AddQueryType<Query>()
        .BuildSchemaAsync();

    // Act
    var result = await schema.ExecuteAsync("{ field }");

    // Assert
    result.MatchSnapshot();
}
```

## Key Abstractions

### GraphQL Types

```csharp
public class BookType : ObjectType<Book>
{
    protected override void Configure(IObjectTypeDescriptor<Book> descriptor)
    {
        descriptor.Field(b => b.Title).Type<NonNullType<StringType>>();
    }
}
```

### DataLoader

```csharp
public class BookByIdDataLoader : BatchDataLoader<int, Book>
{
    protected override async Task<IReadOnlyDictionary<int, Book>> LoadBatchAsync(
        IReadOnlyList<int> keys, CancellationToken ct)
    {
        // Batch load implementation
    }
}
```

### Resolvers

```csharp
public class Query
{
    public Book GetBook([Service] IBookRepository repo, int id)
        => repo.GetById(id);
}
```

## Important Paths

| Path                 | Purpose              |
| -------------------- | -------------------- |
| `src/All.slnx`       | Open this in IDE     |
| `global.json`        | .NET SDK version     |
| `.editorconfig`      | Code style rules     |
| `.build/Build.csproj`| Build configuration  |
| `website/`           | Documentation source |

## Common Modifications

### Add New Type Extension

1. Create in `src/HotChocolate/Core/src/Types/Types/`
2. Add tests in `src/HotChocolate/Core/test/Types.Tests/`
3. Export from appropriate namespace

### Add Database Integration

1. Create project in `src/HotChocolate/Data/`
2. Reference `HotChocolate.Execution`
3. Implement `IQueryableExecutable` or similar

### Modify Execution Pipeline

1. Changes go in `src/HotChocolate/Core/src/Execution/`
2. Middleware in `Processing/` directory
3. Add tests covering the pipeline stage

## Troubleshooting

| Issue             | Solution                                   |
| ----------------- | ------------------------------------------ |
| Build fails       | Run `./init.sh` first                      |
| Missing packages  | Run `./build.sh restore`                   |
| Snapshot mismatch | Check `__snapshots__/` for expected output |
| SDK not found     | Install .NET SDK 10.0.100                  |

## Links

- Documentation: `website/src/docs/`
- Examples: `templates/`
- Build scripts: `.build/`
