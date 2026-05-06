---
title: environment
---

<!-- TODO: Rewrite this -->

The `nitro environment` commands manage environments. An environment is a workspace-level grouping that holds named [stages](/docs/nitro/cli/stage) (for example `dev`, `staging`, `production`) and is shared across the APIs in the workspace.

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

# `nitro environment show`

Show the details of an environment by its ID.

```shell
nitro environment show "<environment-id>"
```

## Arguments

| Argument | Description                              |
| -------- | ---------------------------------------- |
| `<id>`   | ID of the environment to show. Required. |
