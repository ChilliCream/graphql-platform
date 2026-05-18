---
title: api-key Command
---

The `nitro api-key` commands manage API keys. API keys authenticate non-interactive use of the CLI and the Nitro server, and are intended for CI/CD pipelines, deployments, and telemetry reporting from your GraphQL server.

A key is scoped either to a single API (via `--api-id`) or to an entire workspace (via `--workspace-id`). API-scoped keys can only operate on the API they were created for, workspace-scoped keys can operate on every API in the workspace.

Optionally, an API key can additionally be restricted to a single stage with the `--stage-condition` option. This lets you issue, for example, a `dev`-only key that cannot publish to `prod`.

> If you need broader, user-level access (for example to automate workspace administration), use a [Personal Access Token](./pat.md) instead.

All `api-key` commands require authentication. Run `nitro login` first or pass `--api-key` (see [Global Options](./global-options.md)).

# `nitro api-key create`

Create a new API key. The secret is returned only once at creation time, store it in a secure location (for example a secret manager or a CI secret) before closing the terminal.

```shell
nitro api-key create \
  --name "<name>" \
  --api-id "<api-id>"
```

## Options

| Option                                | Env                  | Description                                                                                                                                            |
| ------------------------------------- | -------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `--name <name>`                       | `NITRO_API_KEY_NAME` | Display name for the API key, used for later reference. Required.                                                                                      |
| `--api-id <api-id>`                   | `NITRO_API_ID`       | ID of the API to scope the key to. Required unless `--workspace-id` is set. Get the ID from `nitro api list` or the API overview page in the Nitro UI. |
| `--workspace-id <workspace-id>`       | `NITRO_WORKSPACE_ID` | ID of the workspace to scope the key to. Falls back to the workspace from the current session. Required unless `--api-id` is set.                      |
| `--stage-condition <stage-condition>` |                      | _(Preview)_ Restrict the key to a single stage by name. If omitted, the key is valid for all stages.                                                   |

When run interactively without `--api-id` or `--workspace-id`, the CLI prompts you to pick between an API-scoped or workspace-scoped key.

## Examples

Create an API-scoped key:

```shell
nitro api-key create --name "<name>" --api-id "<api-id>"
```

Create a workspace-scoped key with an explicit workspace:

```shell
nitro api-key create --name "<name>" --workspace-id "<workspace-id>"
```

Restrict a key to a single stage:

```shell
nitro api-key create \
  --name "<name>" \
  --api-id "<api-id>" \
  --stage-condition "<stage-name>"
```

Capture the secret in a script:

```shell
SECRET=$(nitro api-key create \
  --name "<name>" \
  --api-id "<api-id>" \
  --output json | jq -r '.secret')
```

# `nitro api-key list`

List the API keys in a workspace. Results are paginated, use the returned cursor to fetch the next page.

```shell
nitro api-key list
```

## Options

| Option                          | Env                  | Description                                                                |
| ------------------------------- | -------------------- | -------------------------------------------------------------------------- |
| `--workspace-id <workspace-id>` | `NITRO_WORKSPACE_ID` | ID of the workspace. Falls back to the workspace from the current session. |
| `--cursor <cursor>`             | `NITRO_CURSOR`       | Pagination cursor to resume from. Useful for non-interactive paging.       |

# `nitro api-key delete`

Delete an API key by its ID. Once deleted, any client using the key loses access immediately.

```shell
nitro api-key delete "<api-key-id>"
```

## Arguments

| Argument | Description                            |
| -------- | -------------------------------------- |
| `<id>`   | ID of the API key to delete. Required. |

## Options

| Option    | Description                                                                                                                 |
| --------- | --------------------------------------------------------------------------------------------------------------------------- |
| `--force` | Skip the confirmation prompt. Required when running non-interactively (for example in CI) or together with `--output json`. |
