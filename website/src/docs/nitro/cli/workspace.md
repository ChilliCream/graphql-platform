---
title: workspace
---

The `nitro workspace` commands manage workspaces. A workspace is the top-level container in Nitro, every API, environment, and API key belongs to exactly one workspace.

The CLI tracks a default workspace per session so most other commands can omit `--workspace-id`. Use `nitro workspace set-default` to change it and `nitro workspace current` to see what is selected.

All `workspace` commands require authentication. Run `nitro login` first or pass `--api-key` (see [Global Options](/docs/nitro/cli/global-options)).

# `nitro workspace create`

Create a new workspace.

```shell
nitro workspace create --name "<name>"
```

## Options

| Option          | Description                                                                                                     |
| --------------- | --------------------------------------------------------------------------------------------------------------- |
| `--name <name>` | Display name of the workspace. Required.                                                                        |
| `--default`     | Set the created workspace as the default for the current session. Pass `--default false` to opt out explicitly. |

When run interactively without `--name`, the CLI prompts for it.

# `nitro workspace list`

List the workspaces you have access to. Results are paginated, use the returned cursor to fetch the next page.

```shell
nitro workspace list
```

## Options

| Option              | Env            | Description                                                          |
| ------------------- | -------------- | -------------------------------------------------------------------- |
| `--cursor <cursor>` | `NITRO_CURSOR` | Pagination cursor to resume from. Useful for non-interactive paging. |

# `nitro workspace show`

Show the details of a workspace by its ID.

```shell
nitro workspace show "<workspace-id>"
```

## Arguments

| Argument | Description                            |
| -------- | -------------------------------------- |
| `<id>`   | ID of the workspace to show. Required. |

# `nitro workspace current`

Show the name of the currently selected workspace.

```shell
nitro workspace current
```

# `nitro workspace set-default`

Set the default workspace for the current session. In interactive mode the CLI shows a picker, in non-interactive mode pass `--workspace-id`.

```shell
nitro workspace set-default
```

## Options

| Option                          | Env                  | Description                                                                  |
| ------------------------------- | -------------------- | ---------------------------------------------------------------------------- |
| `--workspace-id <workspace-id>` | `NITRO_WORKSPACE_ID` | ID of the workspace to set as the default. Required in non-interactive mode. |
