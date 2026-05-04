---
title: environment
---

The `nitro environment` commands manage environments. An environment is a workspace-level grouping that holds named [stages](/docs/nitro/cli/stage) (for example `dev`, `staging`, `production`) and is shared across the APIs in the workspace.

Environments let you align stage names across APIs so promotions, telemetry, and access policies use a consistent vocabulary. Stages themselves are configured per API via `nitro stage edit`.

All `environment` commands require authentication. Run `nitro login` first or pass `--api-key` (see [Global Options](/docs/nitro/cli/global-options)).

# `nitro environment create`

Create a new environment in a workspace.

```shell
nitro environment create --name "<name>"
```

## Options

| Option                          | Env                  | Description                                                                                             |
| ------------------------------- | -------------------- | ------------------------------------------------------------------------------------------------------- |
| `-n, --name <name>`             |                      | Display name of the environment (for example `dev`, `staging`, `production`). Required.                 |
| `--workspace-id <workspace-id>` | `NITRO_WORKSPACE_ID` | ID of the workspace to create the environment in. Falls back to the workspace from the current session. |

When run interactively without `--name`, the CLI prompts for it.

## Examples

Create an environment in the current workspace:

```shell
nitro environment create --name "<name>"
```

Create an environment in a specific workspace:

```shell
nitro environment create \
  --name "<name>" \
  --workspace-id "<workspace-id>"
```

# `nitro environment list`

List all environments in a workspace. Results are paginated, use the returned cursor to fetch the next page.

```shell
nitro environment list
```

## Options

| Option                          | Env                  | Description                                                                |
| ------------------------------- | -------------------- | -------------------------------------------------------------------------- |
| `--workspace-id <workspace-id>` | `NITRO_WORKSPACE_ID` | ID of the workspace. Falls back to the workspace from the current session. |
| `--cursor <cursor>`             | `NITRO_CURSOR`       | Pagination cursor to resume from. Useful for non-interactive paging.       |

## Examples

List environments in the current workspace:

```shell
nitro environment list
```

Page through environments in JSON mode:

```shell
nitro environment list --output json
nitro environment list --output json --cursor "<cursor-from-previous-page>"
```

# `nitro environment show`

Show the details of an environment by its ID.

```shell
nitro environment show "<environment-id>"
```

## Arguments

| Argument | Description                              |
| -------- | ---------------------------------------- |
| `<id>`   | ID of the environment to show. Required. |

## Examples

Show an environment:

```shell
nitro environment show "<environment-id>"
```

Get the workspace name for an environment in a script:

```shell
WORKSPACE=$(nitro environment show "<environment-id>" --output json | jq -r '.workspace.name')
```
