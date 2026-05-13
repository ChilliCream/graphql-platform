---
title: api Command
---

The `nitro api` commands manage APIs in a workspace.

Each API has a kind that determines how it behaves: `service` for a single GraphQL service, `gateway` for a federated gateway, or `collection` for grouping related APIs together.

All `api` commands require authentication. Run `nitro login` first or pass `--api-key` (see [Global Options](./global-options.md)).

# `nitro api create`

Create a new API in a workspace. The path must start with `/` and uniquely identifies the API within its workspace.

```shell
nitro api create \
  --name "<name>" \
  --path "<path>"
```

## Options

| Option                          | Env                  | Description                                                                                     |
| ------------------------------- | -------------------- | ----------------------------------------------------------------------------------------------- |
| `--name <name>`                 | `NITRO_API_NAME`     | The name of the API. Required.                                                                  |
| `--path <path>`                 | `NITRO_API_PATH`     | The path to the API. Must start with `/`. Required.                                             |
| `--workspace-id <workspace-id>` | `NITRO_WORKSPACE_ID` | ID of the workspace to create the API in. Falls back to the workspace from the current session. |
| `--kind <kind>`                 | `NITRO_API_KIND`     | The kind of the API. One of `collection`, `gateway`, `service`.                                 |

## Examples

Create an API in the workspace from the current session:

```shell
nitro api create --name "<name>" --path "/products"
```

Create an API in an explicit workspace:

```shell
nitro api create \
  --name "<name>" \
  --path "/products" \
  --workspace-id "<workspace-id>"
```

Create a gateway API:

```shell
nitro api create \
  --name "<name>" \
  --path "/products/catalog" \
  --kind gateway
```

# `nitro api list`

List all APIs in a workspace. Results are paginated, use the returned cursor to fetch the next page.

```shell
nitro api list
```

## Options

| Option                          | Env                  | Description                                                                |
| ------------------------------- | -------------------- | -------------------------------------------------------------------------- |
| `--workspace-id <workspace-id>` | `NITRO_WORKSPACE_ID` | ID of the workspace. Falls back to the workspace from the current session. |
| `--cursor <cursor>`             | `NITRO_CURSOR`       | Pagination cursor to resume from. Useful for non-interactive paging.       |

# `nitro api show`

Show the details of a single API by its ID.

```shell
nitro api show "<api-id>"
```

## Arguments

| Argument | Description                      |
| -------- | -------------------------------- |
| `<id>`   | ID of the API to show. Required. |

# `nitro api set-settings`

Update the schema registry settings of an API. These settings control how breaking and dangerous schema changes are evaluated when publishing new schema versions.

```shell
nitro api set-settings "<api-id>" \
  --treat-dangerous-as-breaking \
  --allow-breaking-schema-changes
```

## Arguments

| Argument | Description                        |
| -------- | ---------------------------------- |
| `<id>`   | ID of the API to update. Required. |

## Options

| Option                            | Env                                   | Description                                                                                             |
| --------------------------------- | ------------------------------------- | ------------------------------------------------------------------------------------------------------- |
| `--treat-dangerous-as-breaking`   | `NITRO_TREAT_DANGEROUS_AS_BREAKING`   | Treat dangerous schema changes as breaking. Required when running non-interactively.                    |
| `--allow-breaking-schema-changes` | `NITRO_ALLOW_BREAKING_SCHEMA_CHANGES` | Allow breaking schema changes when no published client breaks. Required when running non-interactively. |

When run interactively, the CLI prompts for any setting you omit.

## Examples

Treat dangerous changes as breaking and reject any breaking change:

```shell
nitro api set-settings "<api-id>" \
  --treat-dangerous-as-breaking true \
  --allow-breaking-schema-changes false
```

Allow breaking changes when no client is affected:

```shell
nitro api set-settings "<api-id>" \
  --treat-dangerous-as-breaking true \
  --allow-breaking-schema-changes true
```

# `nitro api delete`

Delete an API by its ID. This removes the API and all of its schema versions, clients, and stages.

```shell
nitro api delete "<api-id>"
```

## Arguments

| Argument | Description                        |
| -------- | ---------------------------------- |
| `<id>`   | ID of the API to delete. Required. |

## Options

| Option    | Description                                                                                                                 |
| --------- | --------------------------------------------------------------------------------------------------------------------------- |
| `--force` | Skip the confirmation prompt. Required when running non-interactively (for example in CI) or together with `--output json`. |
