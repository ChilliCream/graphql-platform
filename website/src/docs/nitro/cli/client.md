---
title: client
---

The `nitro client` commands manage clients of an API. A client is a registered consumer of a GraphQL API (for example a web app, a mobile app, or another service) along with the set of operations it sends. Registering a client lets Nitro detect when a schema change would break an operation that real consumers depend on.

A client owns a sequence of versions, each identified by a tag and containing a set of persisted operations. Versions are published to a stage to mark them as live, and `validate` runs the same breaking-change detection that `publish` does without committing the result.

All `client` commands require authentication. Run `nitro login` first or pass `--api-key` (see [Global Options](/docs/nitro/cli/global-options)).

# `nitro client create`

Create a new client under an API.

```shell
nitro client create \
  --api-id "<api-id>" \
  --name "<name>"
```

## Options

| Option              | Env                 | Description                            |
| ------------------- | ------------------- | -------------------------------------- |
| `--api-id <api-id>` | `NITRO_API_ID`      | ID of the API to attach the client to. |
| `--name <name>`     | `NITRO_CLIENT_NAME` | Display name of the client.            |

When run interactively, the CLI prompts for any option you omit. Both options are required when running non-interactively.

## Examples

Create a client non-interactively:

```shell
nitro client create \
  --api-id "<api-id>" \
  --name "<name>"
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

Upload a client version tagged with a Git commit:

```shell
nitro client upload \
  --client-id "<client-id>" \
  --tag "$(git rev-parse HEAD)" \
  --operations-file ./operations.json
```

# `nitro client publish`

Publish a previously uploaded client version to a stage. By default the publish fails if the version contains operations that break against the stage's schema. Use `--force` to override, or `--wait-for-approval` to pause until a reviewer approves the change in the Nitro UI.

```shell
nitro client publish \
  --client-id "<client-id>" \
  --tag "<tag>" \
  --stage "<stage>"
```

## Options

| Option                    | Env                       | Description                                                                               |
| ------------------------- | ------------------------- | ----------------------------------------------------------------------------------------- |
| `--client-id <client-id>` | `NITRO_CLIENT_ID`         | ID of the client. Required.                                                               |
| `--tag <tag>`             | `NITRO_TAG`               | Tag of the client version to deploy. Required.                                            |
| `--stage <stage>`         | `NITRO_STAGE`             | Name of the stage to publish to. Required.                                                |
| `--force`                 |                           | Skip confirmation prompts and publish even when the version contains breaking operations. |
| `--wait-for-approval`     | `NITRO_WAIT_FOR_APPROVAL` | Wait for a reviewer to approve the deployment in Nitro before completing.                 |

`--force` and `--wait-for-approval` are mutually exclusive.

## Examples

Publish a client version to `dev`:

```shell
nitro client publish \
  --client-id "<client-id>" \
  --tag "<tag>" \
  --stage "dev"
```

Publish to `production` and wait for manual approval:

```shell
nitro client publish \
  --client-id "<client-id>" \
  --tag "<tag>" \
  --stage "production" \
  --wait-for-approval
```

# `nitro client validate`

Validate a client's operations against a stage without publishing. Returns the operations that would break against the schema currently published to the stage.

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

Validate a client against the `dev` stage in a pull request check:

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

Download the persisted operations of the client version currently published to a stage. Writes either a single JSON file (Relay-style) or a directory with one `.graphql` file per operation.

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

## Examples

List clients for an API:

```shell
nitro client list --api-id "<api-id>"
```

Page through all clients in JSON mode:

```shell
nitro client list --api-id "<api-id>" --output json
nitro client list --api-id "<api-id>" --output json --cursor "<cursor-from-previous-page>"
```

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

## Examples

List all versions of a client:

```shell
nitro client list versions --client-id "<client-id>"
```

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

## Examples

List published versions of a client:

```shell
nitro client list published-versions --client-id "<client-id>"
```

# `nitro client show`

Show the details of a single client by its ID.

```shell
nitro client show "<client-id>"
```

## Arguments

| Argument | Description                         |
| -------- | ----------------------------------- |
| `<id>`   | ID of the client to show. Required. |

## Examples

Show a client:

```shell
nitro client show "<client-id>"
```

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

## Examples

Delete with confirmation:

```shell
nitro client delete "<client-id>"
```

Delete in a script (no prompt):

```shell
nitro client delete "<client-id>" --force
```
