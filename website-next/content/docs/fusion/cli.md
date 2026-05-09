---
title: "Nitro CLI Reference"
---

The Nitro CLI provides the `nitro fusion` command group for composing, validating, publishing, and running Fusion gateway configurations. Use these commands for local development workflows and for publishing configurations to Nitro Cloud.

# Installation

## npm (recommended)

```shell
npm install -g @chillicream/nitro
```

The npm package bundles platform-specific native binaries. Supported platforms: linux-x64, linux-musl-x64, linux-arm64, osx-x64, osx-arm64, win-x64, win-x86, win-arm64.

## Homebrew (macOS)

```shell
brew install ChilliCream/tools/nitro-cli
```

## .NET tool

Install globally:

```shell
dotnet tool install --global ChilliCream.Nitro.CommandLine
```

Or install per-project:

```shell
dotnet new tool-manifest
dotnet tool install ChilliCream.Nitro.CommandLine
```

## Verify installation

```shell
nitro --version
```

# Commands Overview

| Command        | Purpose                                         | Cloud |
| -------------- | ----------------------------------------------- | ----- |
| `compose`      | Compose source schemas into a Fusion archive    | No    |
| `download`     | Download gateway configuration from Nitro Cloud | Yes   |
| `migrate`      | Migrate v1 to v2 configuration files            | No    |
| `publish`      | Publish a Fusion configuration to a stage       | Yes   |
| `run`          | Start a local Fusion gateway                    | No    |
| `settings set` | Configure composition settings in an archive    | No    |
| `upload`       | Upload a source schema for later composition    | Yes   |
| `validate`     | Validate a schema against a stage               | Yes   |

# Cloud Authentication Options

Commands that interact with Nitro Cloud (`download`, `publish`, `upload`, `validate`) accept these global options:

| Option            | Description                | Default                 |
| ----------------- | -------------------------- | ----------------------- |
| `--api-key <key>` | API key for authentication | `NITRO_API_KEY` env var |

If `--api-key` is not provided and no `NITRO_API_KEY` environment variable is set, the CLI uses the session from `nitro login`.

# nitro fusion compose

Composes source schemas into a Fusion archive (`.far` file).

## Syntax

```shell
nitro fusion compose [options]
```

## Options

| Option                                        | Description                                     | Default                                                      |
| --------------------------------------------- | ----------------------------------------------- | ------------------------------------------------------------ |
| `--source-schema-file <path>` (alias: `-f`)   | Path to a source schema file. Can be repeated.  | Auto-discovers `*.graphql`/`*.graphqls` in working directory |
| `--archive <path>` (alias: `-a`)              | Output path for the Fusion archive              | `./gateway.far`                                              |
| `--environment <name>` (alias: `--env`, `-e`) | Environment name for variable substitution      | `ASPNETCORE_ENVIRONMENT` or `Development`                    |
| `--enable-global-object-identification`       | Enable Relay-style global object identification | `false`                                                      |
| `--include-satisfiability-paths`              | Include satisfiability diagnostic paths         | `false`                                                      |
| `--watch`                                     | Recompose on file changes                       | `false`                                                      |
| `--exclude-by-tag <tag>`                      | Exclude fields/types by tag. Can be repeated.   | --                                                           |
| `--working-directory <path>` (alias: `-w`)    | Working directory for resolving paths           | Current directory                                            |

Each `.graphqls` file must have a companion `-settings.json` file (for example, `schema.graphqls` requires `schema-settings.json`). If no `--source-schema-file` is specified, the CLI scans the working directory for all `.graphql` and `.graphqls` files.

## Examples

Compose from specific files:

```shell
nitro fusion compose \
  --source-schema-file ./Products/schema.graphqls \
  --source-schema-file ./Reviews/schema.graphqls \
  --archive gateway.far \
  --environment Development \
  --enable-global-object-identification
```

Auto-discover and compose all schemas in the current directory:

```shell
nitro fusion compose --archive gateway.far
```

Watch mode for local development:

```shell
nitro fusion compose --watch
```

Exclude fields tagged as experimental or internal:

```shell
nitro fusion compose \
  --exclude-by-tag experimental \
  --exclude-by-tag internal-only \
  --archive gateway.far
```

# nitro fusion download

Downloads gateway configuration from Nitro Cloud.

## Syntax

```shell
nitro fusion download [options]
```

## Options

| Option                 | Description        | Default                |
| ---------------------- | ------------------ | ---------------------- |
| `--api-id <id>`        | The API identifier | `NITRO_API_ID` env var |
| `--stage <name>`       | The stage name     | `NITRO_STAGE` env var  |
| `--output-file <path>` | Output file path   | `./gateway.far`        |

## Examples

```shell
nitro fusion download \
  --api-id QXBpCmcwMTk5MGUzNDVlMWU3MjMyYjc2MjYxYzFiNjRkMGQzYg== \
  --stage production \
  --output-file gateway.far
```

# nitro fusion migrate

Migrates configuration files from v1 to v2 format.

## Syntax

```shell
nitro fusion migrate <TARGET> [options]
```

## Arguments

| Argument | Values            | Description      |
| -------- | ----------------- | ---------------- |
| `TARGET` | `subgraph-config` | Migration target |

## Options

| Option                                     | Description                               | Default           |
| ------------------------------------------ | ----------------------------------------- | ----------------- |
| `--working-directory <path>` (alias: `-w`) | Directory to scan for configuration files | Current directory |

## Behavior

The `subgraph-config` target converts `subgraph-config.json` files to the `schema-settings.json` format. The migration maps `subgraph` to `name` and `http.baseAddress` to `transports.http.url`. If a `schema-settings.json` file already exists in the target directory, the migration skips that directory.

## Examples

Migrate all subgraph configs in the current directory:

```shell
nitro fusion migrate subgraph-config
```

Migrate configs in a specific directory:

```shell
nitro fusion migrate subgraph-config --working-directory ./subgraphs
```

# nitro fusion publish

Publishes a Fusion configuration to a stage on Nitro Cloud. Three input modes are available (mutually exclusive).

## Syntax

```shell
nitro fusion publish [options]
```

## Options

| Option                                         | Description                                | Default                |
| ---------------------------------------------- | ------------------------------------------ | ---------------------- |
| `--archive <path>` (alias: `-a`)               | Pre-composed Fusion archive                | --                     |
| `--source-schema <name@version>` (alias: `-s`) | Source schema identifier. Can be repeated. | --                     |
| `--source-schema-file <path>` (alias: `-f`)    | Source schema file. Can be repeated.       | --                     |
| `--api-id <id>`                                | The API identifier                         | `NITRO_API_ID` env var |
| `--stage <name>`                               | Target stage                               | `NITRO_STAGE` env var  |
| `--tag <tag>`                                  | Version tag                                | `NITRO_TAG` env var    |
| `--working-directory <path>` (alias: `-w`)     | Working directory                          | Current directory      |
| `--source-metadata <json>`                     | JSON metadata about the source             | --                     |

## Input Modes

**Mode 1: Pre-composed archive.** Publish an existing `.far` file with `--archive`.

```shell
nitro fusion publish \
  --archive gateway.far \
  --tag v1.0.0 \
  --stage production \
  --api-id QXBpCmcwMTk5MGUzNDVlMWU3MjMyYjc2MjYxYzFiNjRkMGQzYg==
```

**Mode 2: Source schema files.** Compose and publish from local files with `--source-schema-file`.

```shell
nitro fusion publish \
  --source-schema-file ./Products/schema.graphqls \
  --source-schema-file ./Reviews/schema.graphqls \
  --tag v1.0.0 \
  --stage production \
  --api-id QXBpCmcwMTk5MGUzNDVlMWU3MjMyYjc2MjYxYzFiNjRkMGQzYg==
```

**Mode 3: Uploaded schema references.** Reference previously uploaded schemas by name and version with `--source-schema`.

```shell
nitro fusion publish \
  --source-schema products-api@v1.0.0 \
  --source-schema reviews-api@v1.0.0 \
  --tag v1.0.0 \
  --stage production \
  --api-id QXBpCmcwMTk5MGUzNDVlMWU3MjMyYjc2MjYxYzFiNjRkMGQzYg==
```

## Sub-Commands for Advanced Orchestration

For complex deployment scenarios such as blue-green deployments or manual validation gates, use the publish sub-commands.

### nitro fusion publish begin

Request a deployment slot.

| Option                     | Description                                | Default                |
| -------------------------- | ------------------------------------------ | ---------------------- |
| `--api-id <id>`            | The API identifier                         | `NITRO_API_ID` env var |
| `--stage <name>`           | Target stage                               | `NITRO_STAGE` env var  |
| `--tag <tag>`              | Version tag                                | `NITRO_TAG` env var    |
| `--wait-for-approval`      | Wait for manual approval before proceeding | `false`                |
| `--source-metadata <json>` | JSON metadata about the source             | --                     |

```shell
nitro fusion publish begin \
  --stage production \
  --tag v1.0.0 \
  --api-id QXBpCmcwMTk5MGUzNDVlMWU3MjMyYjc2MjYxYzFiNjRkMGQzYg==
```

### nitro fusion publish start

Start composition for the deployment.

| Option              | Description           | Default                        |
| ------------------- | --------------------- | ------------------------------ |
| `--request-id <id>` | Deployment request ID | Uses cached value from `begin` |

```shell
nitro fusion publish start
```

### nitro fusion publish validate

Validate the configuration before committing.

| Option                           | Description                | Default                        |
| -------------------------------- | -------------------------- | ------------------------------ |
| `--request-id <id>`              | Deployment request ID      | Uses cached value from `begin` |
| `--archive <path>` (alias: `-a`) | Fusion archive to validate | --                             |

```shell
nitro fusion publish validate --archive gateway.far
```

### nitro fusion publish commit

Finalize the deployment.

| Option                           | Description              | Default                        |
| -------------------------------- | ------------------------ | ------------------------------ |
| `--request-id <id>`              | Deployment request ID    | Uses cached value from `begin` |
| `--archive <path>` (alias: `-a`) | Fusion archive to deploy | --                             |

```shell
nitro fusion publish commit --archive gateway.far
```

### nitro fusion publish cancel

Cancel the deployment.

| Option              | Description           | Default                        |
| ------------------- | --------------------- | ------------------------------ |
| `--request-id <id>` | Deployment request ID | Uses cached value from `begin` |

```shell
nitro fusion publish cancel
```

### Orchestrated Workflow

A typical advanced deployment follows this sequence:

1. `nitro fusion publish begin` -- reserve a deployment slot
2. `nitro fusion publish start` -- start composition
3. `nitro fusion publish validate` -- validate the configuration
4. `nitro fusion publish commit` -- finalize the deployment

To abort at any point, run `nitro fusion publish cancel`.

# nitro fusion run

Starts a local Fusion gateway from an archive file.

## Syntax

```shell
nitro fusion run <ARCHIVE_FILE> [options]
```

## Options

| Argument/Option                 | Description                | Default            |
| ------------------------------- | -------------------------- | ------------------ |
| `<ARCHIVE_FILE>` (required)     | Path to the Fusion archive | --                 |
| `--port <number>` (alias: `-p`) | Port to bind               | Random unused port |

The gateway auto-opens a browser with the Nitro IDE. CORS is enabled, and `GraphQL-Preflight` and `Authorization` headers are propagated.

## Examples

Start a gateway on a specific port:

```shell
nitro fusion run gateway.far --port 5000
```

Start a gateway on a random port:

```shell
nitro fusion run gateway.far
```

# nitro fusion settings set

Modifies composition settings in a Fusion archive.

## Syntax

```shell
nitro fusion settings set <SETTING_NAME> <SETTING_VALUE> [options]
```

## Options

| Option                                        | Description                                | Default                                   |
| --------------------------------------------- | ------------------------------------------ | ----------------------------------------- |
| `--archive <path>` (alias: `-a`)              | Fusion archive to modify                   | --                                        |
| `--environment <name>` (alias: `--env`, `-e`) | Environment name for variable substitution | `ASPNETCORE_ENVIRONMENT` or `Development` |

## Available Settings

| Setting                        | Values                                 | Description                             |
| ------------------------------ | -------------------------------------- | --------------------------------------- |
| `global-object-identification` | `true`, `false`                        | Enable Relay-style node queries         |
| `cache-control-merge-behavior` | `ignore`, `include`, `include-private` | How to merge `@cacheControl` directives |
| `tag-merge-behavior`           | `ignore`, `include`, `include-private` | How to merge `@tag` directives          |
| `exclude-by-tag`               | Comma-separated tags                   | Exclude fields/types by tag             |

## Examples

Enable global object identification:

```shell
nitro fusion settings set global-object-identification true --archive gateway.far
```

Configure cache control merging:

```shell
nitro fusion settings set cache-control-merge-behavior include --archive gateway.far
```

Exclude tagged fields:

```shell
nitro fusion settings set exclude-by-tag experimental,internal-only --archive gateway.far
```

# nitro fusion upload

Uploads a source schema to Nitro Cloud for later composition.

## Syntax

```shell
nitro fusion upload [options]
```

## Options

| Option                                      | Description                    | Default                |
| ------------------------------------------- | ------------------------------ | ---------------------- |
| `--api-id <id>`                             | The API identifier             | `NITRO_API_ID` env var |
| `--tag <tag>`                               | Version tag                    | `NITRO_TAG` env var    |
| `--source-schema-file <path>` (alias: `-f`) | Source schema file (required)  | --                     |
| `--working-directory <path>` (alias: `-w`)  | Working directory              | Current directory      |
| `--source-metadata <json>`                  | JSON metadata about the source | --                     |

## Examples

Upload a source schema:

```shell
nitro fusion upload \
  --source-schema-file ./src/Products/schema.graphqls \
  --tag v1.2.3 \
  --api-id QXBpCmcwMTk5MGUzNDVlMWU3MjMyYjc2MjYxYzFiNjRkMGQzYg==
```

Upload with a git commit SHA as the tag:

```shell
nitro fusion upload \
  --source-schema-file ./schema.graphqls \
  --tag $(git rev-parse --short HEAD) \
  --api-id QXBpCmcwMTk5MGUzNDVlMWU3MjMyYjc2MjYxYzFiNjRkMGQzYg==
```

# nitro fusion validate

Validates a schema against a stage on Nitro Cloud. Two input modes are available (mutually exclusive).

## Syntax

```shell
nitro fusion validate [options]
```

## Options

| Option                                      | Description                           | Default                |
| ------------------------------------------- | ------------------------------------- | ---------------------- |
| `--archive <path>` (alias: `-a`)            | Fusion archive to validate            | --                     |
| `--source-schema-file <path>` (alias: `-f`) | Source schema files. Can be repeated. | --                     |
| `--api-id <id>`                             | The API identifier                    | `NITRO_API_ID` env var |
| `--stage <name>`                            | Stage to validate against             | `NITRO_STAGE` env var  |

## Examples

Validate from source schema files:

```shell
nitro fusion validate \
  --source-schema-file ./Products/schema.graphqls \
  --stage production \
  --api-id QXBpCmcwMTk5MGUzNDVlMWU3MjMyYjc2MjYxYzFiNjRkMGQzYg==
```

Validate from an archive:

```shell
nitro fusion validate \
  --archive gateway.far \
  --stage production \
  --api-id QXBpCmcwMTk5MGUzNDVlMWU3MjMyYjc2MjYxYzFiNjRkMGQzYg==
```

# Schema Settings File Reference

Each source schema requires a `schema-settings.json` file alongside its `.graphqls` schema file. This file configures the source schema name, transport settings, and per-environment variable substitutions.

```json
{
  "name": "products-api",
  "transports": {
    "http": {
      "url": "{{API_URL}}"
    }
  },
  "environments": {
    "development": {
      "API_URL": "http://localhost:5110/graphql"
    },
    "production": {
      "API_URL": "https://api.example.com/graphql"
    }
  }
}
```

## Fields

### `name`

**Type:** `string` (required)

The unique source schema name used in composition. Must match the name used in `builder.AddGraphQL("products-api")`.

### `transports.http.url`

**Type:** `string` (required)

The URL template for the subgraph's GraphQL endpoint. Use `{{VARIABLE_NAME}}` for environment-specific substitution.

### `transports.http.clientName`

**Type:** `string` (optional, defaults to `"fusion"`)

The named HTTP client the gateway uses to communicate with this subgraph. Must match what the gateway configures via `builder.Services.AddHttpClient("fusion")`.

### `transports.subscriptions.transport`

**Type:** `"sse"` | `"ws"` (optional)

The transport protocol for real-time subscriptions. Defaults to `"sse"` (Server-Sent Events).

### `extensions.nitro.apiId`

**Type:** `string` (optional)

The Nitro Cloud API identifier for this subgraph. Required when using Nitro Cloud features like `upload` or `publish`.

### `environments`

**Type:** `object` (optional)

Per-environment variable substitutions. The active environment is selected via the `--environment` flag during composition or via the `ASPNETCORE_ENVIRONMENT` environment variable.

```json
{
  "environments": {
    "development": {
      "API_URL": "http://localhost:5110/graphql"
    },
    "staging": {
      "API_URL": "https://staging.example.com/graphql"
    },
    "production": {
      "API_URL": "https://api.example.com/graphql"
    }
  }
}
```

When composing with `--environment staging`, all `{{API_URL}}` placeholders resolve to the staging URL.

# Environment Variables

The Nitro CLI reads these environment variables as defaults for their corresponding options.

| Variable                   | Corresponding Option | Used By                                    |
| -------------------------- | -------------------- | ------------------------------------------ |
| `NITRO_API_ID`             | `--api-id`           | download, publish, upload, validate        |
| `NITRO_API_KEY`            | `--api-key`          | download, publish, upload, validate        |
| `NITRO_STAGE`              | `--stage`            | download, publish, validate                |
| `NITRO_TAG`                | `--tag`              | publish, upload                            |
| `NITRO_FUSION_CONFIG_FILE` | `--archive`          | publish                                    |
| `NITRO_OUTPUT_FILE`        | `--output-file`      | download                                   |
| `NITRO_REQUEST_ID`         | `--request-id`       | publish begin/start/validate/commit/cancel |
| `ASPNETCORE_ENVIRONMENT`   | `--environment`      | compose, settings set                      |

Set these in CI/CD pipelines to avoid passing values as command-line arguments.

# Exit Codes

- `0` -- success
- Non-zero -- failure (error details printed to stderr)
