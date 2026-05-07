---
title: stage
---

The `nitro stage` commands manage the stages of an API. Stages represent deployment targets (for example dev, staging, production) that artifacts like schemas, clients, and fusion configurations are published to.

Stages are not created with a dedicated `create` command. Instead, the full set of stages for an API is declared together with `nitro stage edit`, either interactively or by passing a JSON `--configuration`. Conditions on a stage (such as `afterStage`) do not have any effect besides how the UI for the stages is being rendered.

All `stage` commands require authentication. Run `nitro login` first or pass `--api-key` (see [Global Options](/docs/nitro/cli/global-options)).

# `nitro stage edit`

Edit the stages of an API. When `--configuration` is omitted, an interactive editor lets you add, rename, reorder, and delete stages. In non-interactive mode the configuration must be supplied as JSON.

```shell
nitro stage edit \
  --configuration "[{\"name\":\"dev\",\"displayName\":\"Dev\",\"conditions\":[]}]" \
  --api-id "<api-id>"
```

## Options

| Option                            | Env            | Description                                                                                                                                                                                  |
| --------------------------------- | -------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `--api-id <api-id>`               | `NITRO_API_ID` | ID of the API whose stages you are editing. Required.                                                                                                                                        |
| `--configuration <configuration>` |                | Stage configuration as a JSON array. Each entry has `name`, `displayName`, and a `conditions` array (for example `[{"afterStage":"dev"}]`). If omitted, the CLI opens an interactive editor. |

## Examples

Open the interactive editor for an API:

```shell
nitro stage edit --api-id "<api-id>"
```

Replace the stages of an API with a single `dev` stage:

```shell
nitro stage edit \
  --api-id "<api-id>" \
  --configuration "[{\"name\":\"dev\",\"displayName\":\"Dev\",\"conditions\":[]}]"
```

Define a `dev` to `prod` promotion chain:

```shell
nitro stage edit \
  --api-id "<api-id>" \
  --configuration "[{\"name\":\"dev\",\"displayName\":\"Dev\",\"conditions\":[]},{\"name\":\"prod\",\"displayName\":\"Production\",\"conditions\":[{\"afterStage\":\"dev\"}]}]"
```

# `nitro stage list`

List all stages of an API, including their conditions.

```shell
nitro stage list --api-id "<api-id>"
```

## Options

| Option              | Env            | Description              |
| ------------------- | -------------- | ------------------------ |
| `--api-id <api-id>` | `NITRO_API_ID` | ID of the API. Required. |

# `nitro stage delete`

Delete a single stage by name. Removing a stage that other parts of your workflow depend on may fail, resolve those dependencies first.

```shell
nitro stage delete \
  --stage "<stage-name>" \
  --api-id "<api-id>"
```

## Options

| Option              | Env            | Description                                                                                                                 |
| ------------------- | -------------- | --------------------------------------------------------------------------------------------------------------------------- |
| `--api-id <api-id>` | `NITRO_API_ID` | ID of the API the stage belongs to. Required.                                                                               |
| `--stage <stage>`   | `NITRO_STAGE`  | Name of the stage to delete. Required.                                                                                      |
| `--force`           |                | Skip the confirmation prompt. Required when running non-interactively (for example in CI) or together with `--output json`. |
