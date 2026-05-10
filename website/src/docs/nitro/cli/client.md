---
title: client
---

The `nitro client` commands manage clients of an API. A client is a registered consumer of a GraphQL API (for example a web app, a mobile app, or another service) along with the set of operations it sends.

A client owns a sequence of versions, each identified by a tag and containing a set of persisted operations. Versions are published to a stage to mark them as live.

All `client` commands require authentication. Run `nitro login` first or pass `--api-key` (see [Global Options](/docs/nitro/cli/global-options)).

# `nitro client create`

Create a new client under an API.

```shell
nitro client create \
  --name "<name>" \
  --api-id "<api-id>"
```

## Options

| Option              | Env                 | Description                                                                                                                              |
| ------------------- | ------------------- | ---------------------------------------------------------------------------------------------------------------------------------------- |
| `--name <name>`     | `NITRO_CLIENT_NAME` | Display name of the client. Required.                                                                                                    |
| `--api-id <api-id>` | `NITRO_API_ID`      | ID of the API the client belongs to. Required when no workspace is set in the session. Get the ID from `nitro api list` or the Nitro UI. |

## Examples

Create a client interactively (prompts for missing values):

```shell
nitro client create
```

Create a client non-interactively:

```shell
nitro client create --name "<name>" --api-id "<api-id>"
```

# `nitro client upload`

Upload a new client version with the operations the client sends. The version is identified by a tag and is not yet published to any stage.

```shell
nitro client upload \
  --client-id "<client-id>" \
  --tag "<tag>" \
  --operations-file <file-path>
```

## Options

| Option                                | Env                     | Description                                                                |
| ------------------------------------- | ----------------------- | -------------------------------------------------------------------------- |
| `--client-id <client-id>`             | `NITRO_CLIENT_ID`       | ID of the client. Required.                                                |
| `--tag <tag>`                         | `NITRO_TAG`             | Tag of the new client version, for example `v1` or a Git commit. Required. |
| `--operations-file <operations-file>` | `NITRO_OPERATIONS_FILE` | Path to the JSON file with the persisted operations. Required.             |

## Examples

Upload a client version:

```shell
nitro client upload \
  --client-id "<client-id>" \
  --tag "v1" \
  --operations-file ./operations.json
```

# `nitro client publish`

Publish a previously uploaded client version to a stage. The version is identified by its tag.

```shell
nitro client publish \
  --client-id "<client-id>" \
  --tag "<tag>" \
  --stage "<stage>"
```

## Options

| Option                    | Env                       | Description                                                                                                                               |
| ------------------------- | ------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------- |
| `--client-id <client-id>` | `NITRO_CLIENT_ID`         | ID of the client. Required.                                                                                                               |
| `--tag <tag>`             | `NITRO_TAG`               | Tag of the client version to publish. Required.                                                                                           |
| `--stage <stage>`         | `NITRO_STAGE`             | Name of the stage to publish to. Required.                                                                                                |
| `--force`                 |                           | Skip confirmation prompts and publish even when the version contains breaking operations. Mutually exclusive with `--wait-for-approval`.  |
| `--wait-for-approval`     | `NITRO_WAIT_FOR_APPROVAL` | Block the command until a reviewer approves the deployment. Mutually exclusive with `--force`. Required when the stage gates deployments. |

## Examples

Publish to `dev`:

```shell
nitro client publish \
  --client-id "<client-id>" \
  --tag "v1" \
  --stage "dev"
```

Publish to a gated stage and wait for approval:

```shell
nitro client publish \
  --client-id "<client-id>" \
  --tag "v1" \
  --stage "production" \
  --wait-for-approval
```

# `nitro client validate`

Validate a new client version against a stage without publishing it. Run this in your pull request validation workflow to catch breaking operations before they are merged.

```shell
nitro client validate \
  --client-id "<client-id>" \
  --stage "<stage>" \
  --operations-file <file-path>
```

## Options

| Option                                | Env                     | Description                                                    |
| ------------------------------------- | ----------------------- | -------------------------------------------------------------- |
| `--client-id <client-id>`             | `NITRO_CLIENT_ID`       | ID of the client. Required.                                    |
| `--stage <stage>`                     | `NITRO_STAGE`           | Name of the stage to validate against. Required.               |
| `--operations-file <operations-file>` | `NITRO_OPERATIONS_FILE` | Path to the JSON file with the persisted operations. Required. |

## Examples

Validate against the `dev` stage:

```shell
nitro client validate \
  --client-id "<client-id>" \
  --stage "dev" \
  --operations-file ./operations.json
```

# `nitro client unpublish`

Unpublish one or more client version tags from a stage. The version is not deleted, only removed from the stage.

```shell
nitro client unpublish \
  --client-id "<client-id>" \
  --stage "<stage>" \
  --tag "<tag>"
```

## Options

| Option                    | Env               | Description                                                                                      |
| ------------------------- | ----------------- | ------------------------------------------------------------------------------------------------ |
| `--client-id <client-id>` | `NITRO_CLIENT_ID` | ID of the client. Required.                                                                      |
| `--stage <stage>`         | `NITRO_STAGE`     | Name of the stage to unpublish from. Required.                                                   |
| `--tag <tag>`             | `NITRO_TAG`       | Tag of the client version to unpublish. Pass multiple times to unpublish several tags. Required. |

## Examples

Unpublish a single tag:

```shell
nitro client unpublish \
  --client-id "<client-id>" \
  --stage "dev" \
  --tag "<tag>"
```

Unpublish multiple tags in one call:

```shell
nitro client unpublish \
  --client-id "<client-id>" \
  --stage "dev" \
  --tag "v1" \
  --tag "v2"
```

# `nitro client download`

Download all persisted operations of the client currently published to a stage. Writes either a single JSON file (Relay-style) or a directory with one `.graphql` file per operation.

```shell
nitro client download \
  --api-id "<api-id>" \
  --stage "<stage>" \
  --path <file-path>
```

## Options

| Option                       | Env            | Description                                                                                                                        |
| ---------------------------- | -------------- | ---------------------------------------------------------------------------------------------------------------------------------- |
| `--api-id <api-id>`          | `NITRO_API_ID` | ID of the API. Required.                                                                                                           |
| `--stage <stage>`            | `NITRO_STAGE`  | Name of the stage to download from. Required.                                                                                      |
| `--path <path>`              |                | Path to write the operations to. A file path for `relay`, a directory for `folder`. Required.                                      |
| `--format <folder \| relay>` |                | Output format. `relay` writes a single JSON map of `id -> operation`, `folder` writes one file per operation. Defaults to `relay`. |

## Examples

Download Relay-style persisted operations:

```shell
nitro client download \
  --api-id "<api-id>" \
  --stage "dev" \
  --path ./operations.json
```

Download as a folder of `.graphql` files:

```shell
nitro client download \
  --api-id "<api-id>" \
  --stage "dev" \
  --path ./operations \
  --format folder
```

# `nitro client list`

List all clients of an API. Results are paginated, use the returned cursor to fetch the next page.

```shell
nitro client list --api-id "<api-id>"
```

## Options

| Option              | Env            | Description                                                          |
| ------------------- | -------------- | -------------------------------------------------------------------- |
| `--api-id <api-id>` | `NITRO_API_ID` | ID of the API. Required when running non-interactively.              |
| `--cursor <cursor>` | `NITRO_CURSOR` | Pagination cursor to resume from. Useful for non-interactive paging. |

# `nitro client list versions`

List all versions of a client, including ones that have never been published to a stage.

```shell
nitro client list versions --client-id "<client-id>"
```

## Options

| Option                    | Env               | Description                                                          |
| ------------------------- | ----------------- | -------------------------------------------------------------------- |
| `--client-id <client-id>` | `NITRO_CLIENT_ID` | ID of the client. Required when running non-interactively.           |
| `--cursor <cursor>`       | `NITRO_CURSOR`    | Pagination cursor to resume from. Useful for non-interactive paging. |

# `nitro client list published-versions`

List only the versions of a client that are currently published to at least one stage.

```shell
nitro client list published-versions --client-id "<client-id>"
```

## Options

| Option                    | Env               | Description                                                          |
| ------------------------- | ----------------- | -------------------------------------------------------------------- |
| `--client-id <client-id>` | `NITRO_CLIENT_ID` | ID of the client. Required when running non-interactively.           |
| `--cursor <cursor>`       | `NITRO_CURSOR`    | Pagination cursor to resume from. Useful for non-interactive paging. |

# `nitro client show`

Show the details of a single client by its ID.

```shell
nitro client show "<client-id>"
```

## Arguments

| Argument | Description                         |
| -------- | ----------------------------------- |
| `<id>`   | ID of the client to show. Required. |

# `nitro client delete`

Delete a client by its ID. This removes the client and all of its versions.

```shell
nitro client delete "<client-id>"
```

## Arguments

| Argument | Description                                                                                                      |
| -------- | ---------------------------------------------------------------------------------------------------------------- |
| `<id>`   | ID of the client to delete. Required when running non-interactively. Interactive runs prompt to select a client. |

## Options

| Option    | Description                                                                                                                 |
| --------- | --------------------------------------------------------------------------------------------------------------------------- |
| `--force` | Skip the confirmation prompt. Required when running non-interactively (for example in CI) or together with `--output json`. |
