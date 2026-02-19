---
title: "Nitro CLI Reference"
---

Complete reference for all `nitro fusion` commands, including options, examples, and exit codes.

# Installation

Install the Nitro CLI as a .NET tool:

```bash
dotnet tool install --global ChilliCream.Nitro.CommandLine
```

Or add it to your project's tool manifest:

```bash
dotnet new tool-manifest
dotnet tool install ChilliCream.Nitro.CommandLine
```

Update to the latest version:

```bash
dotnet tool update --global ChilliCream.Nitro.CommandLine
```

# Commands

## `nitro fusion compose`

Composes source schemas into a Fusion archive (`.far` file).

### Syntax

```bash
nitro fusion compose [options]
```

### Options

| Option                                  | Description                                                      | Default                                                          |
| --------------------------------------- | ---------------------------------------------------------------- | ---------------------------------------------------------------- |
| `--source-schema-file <path>`           | Path to a source schema `.graphqls` file (can be repeated)       | Scans working directory for all `.graphql` and `.graphqls` files |
| `--archive <path>`                      | Output path for the Fusion archive                               | `./gateway.far`                                                  |
| `--environment <name>`                  | Environment name for variable substitution                       | `ASPNETCORE_ENVIRONMENT` or `Development`                        |
| `--enable-global-object-identification` | Enable Relay-style node queries                                  | `false`                                                          |
| `--include-satisfiability-paths`        | Include satisfiability diagnostic paths                          | `false`                                                          |
| `--watch`                               | Watch mode: recomposes on file changes                           | `false`                                                          |
| `--working-directory <path>`            | Working directory for resolving relative paths                   | Current directory                                                |
| `--exclude-tag <tag>`                   | Exclude fields/types by tag during composition (can be repeated) | None                                                             |

### Schema File Resolution

- Each `.graphqls` file must have a companion `-settings.json` file (e.g., `schema.graphqls` + `schema-settings.json`)
- If no `--source-schema-file` is specified, the CLI scans the working directory for all `.graphql` and `.graphqls` files

### Examples

**Compose from specific files:**

```bash
nitro fusion compose \
  --source-schema-file ./Products/schema.graphqls \
  --source-schema-file ./Reviews/schema.graphqls \
  --archive gateway.far \
  --environment Development \
  --enable-global-object-identification
```

**Auto-discover and compose all schemas in current directory:**

```bash
nitro fusion compose --archive gateway.far
```

**Watch mode for local development:**

```bash
nitro fusion compose --watch
```

**Exclude experimental features:**

```bash
nitro fusion compose \
  --exclude-tag experimental \
  --exclude-tag internal-only \
  --archive gateway.far
```

### Exit Codes

- `0`: Composition succeeded
- Non-zero: Composition failed (see error output for details)

---

## `nitro fusion upload`

Uploads a source schema to Nitro cloud for later composition.

### Syntax

```bash
nitro fusion upload [options]
```

### Options

| Option                        | Description                      | Required                           |
| ----------------------------- | -------------------------------- | ---------------------------------- |
| `--source-schema-file <path>` | Path to the source schema file   | Yes                                |
| `--tag <version>`             | Version tag for this upload      | Yes                                |
| `--api-id <id>`               | Nitro API identifier             | Yes                                |
| `--api-key <key>`             | Nitro API key for authentication | Yes                                |
| `--working-directory <path>`  | Working directory                | No (defaults to current directory) |

### Examples

**Upload a source schema:**

```bash
nitro fusion upload \
  --source-schema-file ./src/Products/schema.graphqls \
  --tag v1.2.3 \
  --api-id QXBpCmcwMTk5MGUzNDVlMWU3MjMyYjc2MjYxYzFiNjRkMGQzYg== \
  --api-key $NITRO_API_KEY
```

**Upload with git commit SHA as tag:**

```bash
nitro fusion upload \
  --source-schema-file ./schema.graphqls \
  --tag $(git rev-parse --short HEAD) \
  --api-id QXBpCmcwMTk5MGUzNDVlMWU3MjMyYjc2MjYxYzFiNjRkMGQzYg== \
  --api-key $NITRO_API_KEY
```

### Exit Codes

- `0`: Upload succeeded
- Non-zero: Upload failed (authentication failure, network error, etc.)

---

## `nitro fusion publish`

Publishes a composed Fusion configuration to a stage on Nitro cloud. Supports three modes.

### Mode 1: From Source Schema Files (Performs Composition Internally)

Compose and publish in one step.

#### Syntax

```bash
nitro fusion publish [options]
```

#### Options

| Option                                  | Description                                   | Required            |
| --------------------------------------- | --------------------------------------------- | ------------------- |
| `--source-schema-file <path>`           | Path to source schema files (can be repeated) | Yes (for this mode) |
| `--tag <version>`                       | Version tag                                   | Yes                 |
| `--stage <name>`                        | Target deployment stage                       | Yes                 |
| `--api-id <id>`                         | Nitro API identifier                          | Yes                 |
| `--api-key <key>`                       | Nitro API key                                 | Yes                 |
| `--environment <name>`                  | Environment name for variable substitution    | No                  |
| `--enable-global-object-identification` | Enable Relay-style node queries               | No                  |

#### Example

```bash
nitro fusion publish \
  --source-schema-file ./Products/schema.graphqls \
  --source-schema-file ./Reviews/schema.graphqls \
  --tag v1.0.0 \
  --stage production \
  --api-id QXBpCmcwMTk5MGUzNDVlMWU3MjMyYjc2MjYxYzFiNjRkMGQzYg== \
  --api-key $NITRO_API_KEY
```

---

### Mode 2: From Source Schema Identifiers (Downloads from Nitro, Composes, Publishes)

Reference previously uploaded schemas by name and version.

#### Syntax

```bash
nitro fusion publish [options]
```

#### Options

| Option                           | Description                                 | Required            |
| -------------------------------- | ------------------------------------------- | ------------------- |
| `--source-schema <name@version>` | Source schema identifiers (can be repeated) | Yes (for this mode) |
| `--tag <version>`                | Version tag                                 | Yes                 |
| `--stage <name>`                 | Target deployment stage                     | Yes                 |
| `--api-id <id>`                  | Nitro API identifier                        | Yes                 |
| `--api-key <key>`                | Nitro API key                               | Yes                 |

#### Example

```bash
nitro fusion publish \
  --source-schema products-api@v1.0.0 \
  --source-schema reviews-api@v1.0.0 \
  --tag v1.0.0 \
  --stage production \
  --api-id QXBpCmcwMTk5MGUzNDVlMWU3MjMyYjc2MjYxYzFiNjRkMGQzYg== \
  --api-key $NITRO_API_KEY
```

**Omit version to use latest:**

```bash
nitro fusion publish \
  --source-schema products-api \
  --source-schema reviews-api \
  --tag v1.0.0 \
  --stage production \
  --api-id QXBpCmcwMTk5MGUzNDVlMWU3MjMyYjc2MjYxYzFiNjRkMGQzYg== \
  --api-key $NITRO_API_KEY
```

---

### Mode 3: From a Pre-Composed Archive

Publish an existing `.far` file.

#### Syntax

```bash
nitro fusion publish [options]
```

#### Options

| Option             | Description                           | Required            |
| ------------------ | ------------------------------------- | ------------------- |
| `--archive <path>` | Path to a pre-composed Fusion archive | Yes (for this mode) |
| `--tag <version>`  | Version tag                           | Yes                 |
| `--stage <name>`   | Target deployment stage               | Yes                 |
| `--api-id <id>`    | Nitro API identifier                  | Yes                 |
| `--api-key <key>`  | Nitro API key                         | Yes                 |

#### Example

```bash
nitro fusion publish \
  --archive gateway.far \
  --tag v1.0.0 \
  --stage production \
  --api-id QXBpCmcwMTk5MGUzNDVlMWU3MjMyYjc2MjYxYzFiNjRkMGQzYg== \
  --api-key $NITRO_API_KEY
```

---

### Sub-Commands for Advanced Orchestration

For complex deployment scenarios (e.g., blue-green deployments, rollback coordination), use the publish sub-commands:

#### `nitro fusion publish begin`

Request a deployment slot.

```bash
nitro fusion publish begin \
  --stage production \
  --tag v1.0.0 \
  --api-id QXBpCmcwMTk5MGUzNDVlMWU3MjMyYjc2MjYxYzFiNjRkMGQzYg== \
  --api-key $NITRO_API_KEY
```

#### `nitro fusion publish start`

Claim the deployment slot.

```bash
nitro fusion publish start \
  --api-key $NITRO_API_KEY
```

#### `nitro fusion publish validate`

Validate before committing.

```bash
nitro fusion publish validate \
  --configuration gateway.fgp \
  --api-key $NITRO_API_KEY
```

#### `nitro fusion publish cancel`

Cancel the deployment.

```bash
nitro fusion publish cancel \
  --api-key $NITRO_API_KEY
```

#### `nitro fusion publish commit`

Commit the deployment.

```bash
nitro fusion publish commit \
  --configuration gateway.fgp \
  --api-key $NITRO_API_KEY
```

### Exit Codes

- `0`: Publish succeeded
- Non-zero: Publish failed (composition error, validation failure, etc.)

---

## `nitro fusion download`

Downloads the latest gateway configuration from Nitro.

### Syntax

```bash
nitro fusion download [options]
```

### Options

| Option            | Description            | Required                       |
| ----------------- | ---------------------- | ------------------------------ |
| `--stage <name>`  | Stage to download from | Yes                            |
| `--api-id <id>`   | Nitro API identifier   | Yes                            |
| `--api-key <key>` | Nitro API key          | Yes                            |
| `--output <path>` | Output file path       | No (defaults to `gateway.fgp`) |

### Examples

```bash
nitro fusion download \
  --stage production \
  --api-id QXBpCmcwMTk5MGUzNDVlMWU3MjMyYjc2MjYxYzFiNjRkMGQzYg== \
  --api-key $NITRO_API_KEY \
  --output gateway.fgp
```

### Exit Codes

- `0`: Download succeeded
- Non-zero: Download failed (authentication failure, stage not found, etc.)

---

## `nitro fusion validate`

Validates a composed schema against a stage.

### Syntax

```bash
nitro fusion validate [options]
```

### Options

| Option                        | Description                                   | Required               |
| ----------------------------- | --------------------------------------------- | ---------------------- |
| `--source-schema-file <path>` | Path to source schema files (can be repeated) | Yes (for this mode)    |
| `--archive <path>`            | Path to a pre-composed archive                | Yes (alternative mode) |
| `--stage <name>`              | Stage to validate against                     | Yes                    |
| `--api-id <id>`               | Nitro API identifier                          | Yes                    |
| `--api-key <key>`             | Nitro API key                                 | Yes                    |

### Examples

**Validate from source schema files:**

```bash
nitro fusion validate \
  --source-schema-file ./Products/schema.graphqls \
  --stage production \
  --api-id QXBpCmcwMTk5MGUzNDVlMWU3MjMyYjc2MjYxYzFiNjRkMGQzYg== \
  --api-key $NITRO_API_KEY
```

**Validate from an archive:**

```bash
nitro fusion validate \
  --archive gateway.far \
  --stage production \
  --api-id QXBpCmcwMTk5MGUzNDVlMWU3MjMyYjc2MjYxYzFiNjRkMGQzYg== \
  --api-key $NITRO_API_KEY
```

### Exit Codes

- `0`: Validation passed (no breaking changes)
- Non-zero: Validation failed (breaking changes detected or composition error)

### Use in CI

```yaml
- name: Validate Schema
  run: |
    nitro fusion validate \
      --source-schema-file ./schema.graphqls \
      --stage production \
      --api-id ${{ secrets.NITRO_API_ID }} \
      --api-key ${{ secrets.NITRO_API_KEY }}
```

---

## `nitro fusion run`

Starts a local Fusion gateway from an archive file.

### Syntax

```bash
nitro fusion run <archive> [options]
```

### Options

| Option            | Description                | Default |
| ----------------- | -------------------------- | ------- |
| `--port <number>` | Port to run the gateway on | `5000`  |

### Examples

```bash
nitro fusion run gateway.far --port 5000
```

This command:

1. Starts a local Fusion gateway
2. Loads the configuration from `gateway.far`
3. Opens a browser with the Nitro IDE (Banana Cake Pop) at `http://localhost:5000/graphql`

Use this for local testing of composed configurations without deploying to cloud infrastructure.

### Exit Codes

- `0`: Gateway exited cleanly
- Non-zero: Gateway startup failed (invalid archive, port in use, etc.)

---

## `nitro fusion settings set`

Modifies composition settings in a Fusion archive.

### Syntax

```bash
nitro fusion settings set <SETTING_NAME> <SETTING_VALUE> [options]
```

### Options

| Option             | Description                          | Default         |
| ------------------ | ------------------------------------ | --------------- |
| `--archive <path>` | Path to the Fusion archive to modify | `./gateway.far` |

### Available Settings

#### `global-object-identification`

Enable or disable Relay-style node queries.

```bash
nitro fusion settings set global-object-identification true --archive gateway.far
```

Values: `true` or `false`

#### `cache-control-merge-behavior`

How to merge cache control directives from multiple subgraphs.

```bash
nitro fusion settings set cache-control-merge-behavior include --archive gateway.far
```

Values:

- `ignore`: Ignore all cache control directives
- `include`: Include cache control directives
- `include-private`: Include cache control directives including private caches

#### `tag-merge-behavior`

How to merge tags from multiple subgraphs.

```bash
nitro fusion settings set tag-merge-behavior include --archive gateway.far
```

Values:

- `ignore`: Ignore all tags
- `include`: Include tags
- `include-private`: Include tags including private tags

#### `exclude-by-tag`

Comma-separated list of tags to exclude during composition.

```bash
nitro fusion settings set exclude-by-tag experimental,internal-only --archive gateway.far
```

### Exit Codes

- `0`: Setting updated successfully
- Non-zero: Update failed (invalid archive, unknown setting, invalid value, etc.)

---

## Schema Export via Subgraph CLI

Each subgraph can export its schema using the HotChocolate CLI:

### Syntax

```bash
dotnet run -- schema export [options]
```

### Options

| Option            | Description      | Default                           |
| ----------------- | ---------------- | --------------------------------- |
| `--output <path>` | Output file path | `schema.graphqls` in project root |

### Examples

**Export to default location:**

```bash
dotnet run -- schema export
```

**Export to specific file:**

```bash
dotnet run -- schema export --output ./schemas/products.graphqls
```

This command:

1. Starts the subgraph application
2. Extracts the GraphQL schema
3. Writes it to the specified file
4. Exits

Enable automatic schema export on startup (non-production only):

```csharp
builder
    .AddGraphQL("products-api")
    .AddDefaultSettings()
    .AddProductTypes()
    .ExportSchemaOnStartup(); // Exports to schema.graphqls on every run
```

---

# schema-settings.json Format Reference

Each subgraph requires a `schema-settings.json` file for composition. This file lives alongside the `.graphqls` schema file.

## Complete Format

```json
{
  "name": "products-api",
  "transports": {
    "http": {
      "clientName": "fusion",
      "url": "{{API_URL}}"
    },
    "subscriptions": {
      "transport": "sse"
    }
  },
  "extensions": {
    "nitro": {
      "apiId": "QXBpCmcwMTk5MGUzNDVlMWU3MjMyYjc2MjYxYzFiNjRkMGQzYg=="
    }
  },
  "environments": {
    "aspire": {
      "API_URL": "http://localhost:5110/graphql"
    },
    "dev": {
      "API_URL": "https://dev.example.com/graphql"
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

## Field Descriptions

### `name`

**Type:** `string` (required)

The unique source schema name used in composition. Must match the name used in `builder.AddGraphQL("products-api")`.

### `transports.http.clientName`

**Type:** `string` (optional, defaults to `"fusion"`)

The named HTTP client the gateway uses to communicate with this subgraph. Must match what the gateway configures via `builder.Services.AddHttpClient("fusion")`. If omitted, defaults to `"fusion"`.

### `transports.http.url`

**Type:** `string` (required)

The URL template for the subgraph's GraphQL endpoint. Use `{{VARIABLE_NAME}}` for environment-specific substitution.

### `transports.subscriptions.transport`

**Type:** `"sse"` | `"ws"` (optional)

The transport protocol for real-time subscriptions. Defaults to `"sse"` (Server-Sent Events).

### `extensions.nitro.apiId`

**Type:** `string` (optional)

The Nitro API identifier for this subgraph. Required if using Nitro cloud features.

### `environments`

**Type:** `object` (optional)

Per-environment variable substitutions. The active environment is selected via the `--environment` flag during composition or via `ASPNETCORE_ENVIRONMENT` when using Aspire.

## Example: Multiple Environments

```json
{
  "name": "reviews-api",
  "transports": {
    "http": {
      "clientName": "fusion",
      "url": "{{API_URL}}"
    }
  },
  "environments": {
    "aspire": {
      "API_URL": "http://localhost:5120/graphql"
    },
    "dev": {
      "API_URL": "https://ccc-eu1-demo-ca-reviews.westeurope.azurecontainerapps.io/graphql"
    },
    "production": {
      "API_URL": "https://reviews-api.prod.example.com/graphql"
    }
  }
}
```

When composing with `--environment dev`, `{{API_URL}}` resolves to the dev URL. When deploying to production, use `--environment production`.

---

# Environment Variables

The Nitro CLI respects these environment variables:

| Variable                 | Purpose                                 |
| ------------------------ | --------------------------------------- |
| `NITRO_API_KEY`          | Default API key (overrides `--api-key`) |
| `NITRO_API_ID`           | Default API ID (overrides `--api-id`)   |
| `NITRO_STAGE`            | Default stage (overrides `--stage`)     |
| `ASPNETCORE_ENVIRONMENT` | Default environment for `--environment` |

Set these in CI/CD to avoid passing credentials as command-line arguments.
