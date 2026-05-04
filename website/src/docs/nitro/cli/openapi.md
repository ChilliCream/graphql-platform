---
title: openapi
---

The `nitro openapi` commands manage OpenAPI collections. An OpenAPI collection registers one or more OpenAPI documents with Nitro and tracks their versions across stages, so consumers always pick up the spec that matches the stage they target.

A typical workflow is: `create` a collection on an API, `upload` a new version of its OpenAPI documents, optionally `validate` that version against a stage, then `publish` it.

> Validation runs automatically as part of `nitro openapi publish`. Use `nitro openapi validate` only when you need to gate a deploy step in a separate pipeline job.

All `openapi` commands require authentication. Run `nitro login` first or pass `--api-key` (see [Global Options](/docs/nitro/cli/global-options)).

# `nitro openapi create`

Create a new OpenAPI collection on an API.

```shell
nitro openapi create \
  --name "<name>" \
  --api-id "<api-id>"
```

## Options

| Option              | Env                             | Description                                                                                                                                  |
| ------------------- | ------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------- |
| `--name <name>`     | `NITRO_OPENAPI_COLLECTION_NAME` | Display name of the OpenAPI collection. Required.                                                                                            |
| `--api-id <api-id>` | `NITRO_API_ID`                  | ID of the API the collection belongs to. Required when no workspace is set in the session. Get the ID from `nitro api list` or the Nitro UI. |

## Examples

Create a collection interactively (prompts for missing values):

```shell
nitro openapi create
```

Create a collection non-interactively:

```shell
nitro openapi create --name "<name>" --api-id "<api-id>"
```

# `nitro openapi upload`

Upload a new version of an OpenAPI collection. OpenAPI documents are picked up via glob patterns.

```shell
nitro openapi upload \
  --openapi-collection-id "<collection-id>" \
  --tag "<tag>" \
  --pattern "<pattern>"
```

## Options

| Option                                            | Env                           | Description                                                                                |
| ------------------------------------------------- | ----------------------------- | ------------------------------------------------------------------------------------------ |
| `--openapi-collection-id <openapi-collection-id>` | `NITRO_OPENAPI_COLLECTION_ID` | ID of the OpenAPI collection. Required.                                                    |
| `--tag <tag>`                                     | `NITRO_TAG`                   | Tag of the version being uploaded (for example a Git commit SHA or release tag). Required. |
| `-p, --pattern <pattern>`                         |                               | One or more glob patterns selecting the OpenAPI document files. Required.                  |

## Examples

Upload all OpenAPI documents matching a pattern:

```shell
nitro openapi upload \
  --openapi-collection-id "<collection-id>" \
  --tag "v1" \
  --pattern "./**/*.graphql"
```

# `nitro openapi publish`

Publish a previously uploaded OpenAPI collection version to a stage. The version is identified by its tag.

```shell
nitro openapi publish \
  --openapi-collection-id "<collection-id>" \
  --tag "<tag>" \
  --stage "<stage>"
```

## Options

| Option                                            | Env                           | Description                                                                                                                               |
| ------------------------------------------------- | ----------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------- |
| `--openapi-collection-id <openapi-collection-id>` | `NITRO_OPENAPI_COLLECTION_ID` | ID of the OpenAPI collection. Required.                                                                                                   |
| `--tag <tag>`                                     | `NITRO_TAG`                   | Tag of the version to publish. Required.                                                                                                  |
| `--stage <stage>`                                 | `NITRO_STAGE`                 | Name of the stage to publish to. Required.                                                                                                |
| `--force`                                         |                               | Skip confirmation prompts for deletes and overwrites. Mutually exclusive with `--wait-for-approval`.                                      |
| `--wait-for-approval`                             | `NITRO_WAIT_FOR_APPROVAL`     | Block the command until a reviewer approves the deployment. Mutually exclusive with `--force`. Required when the stage gates deployments. |

## Examples

Publish to `dev`:

```shell
nitro openapi publish \
  --openapi-collection-id "<collection-id>" \
  --stage "dev" \
  --tag "v1"
```

Publish to a gated stage and wait for approval:

```shell
nitro openapi publish \
  --openapi-collection-id "<collection-id>" \
  --stage "production" \
  --tag "v1" \
  --wait-for-approval
```

# `nitro openapi validate`

Validate a new OpenAPI collection version against a stage without publishing it. Use this in CI to catch breaking changes before a deploy job runs.

```shell
nitro openapi validate \
  --openapi-collection-id "<collection-id>" \
  --stage "<stage>" \
  --pattern "<pattern>"
```

## Options

| Option                                            | Env                           | Description                                                               |
| ------------------------------------------------- | ----------------------------- | ------------------------------------------------------------------------- |
| `--openapi-collection-id <openapi-collection-id>` | `NITRO_OPENAPI_COLLECTION_ID` | ID of the OpenAPI collection. Required.                                   |
| `--stage <stage>`                                 | `NITRO_STAGE`                 | Name of the stage to validate against. Required.                          |
| `-p, --pattern <pattern>`                         |                               | One or more glob patterns selecting the OpenAPI document files. Required. |

## Examples

Validate against the `dev` stage:

```shell
nitro openapi validate \
  --openapi-collection-id "<collection-id>" \
  --stage "dev" \
  --pattern "./**/*.graphql"
```

# `nitro openapi list`

List all OpenAPI collections of an API. Results are paginated, use the returned cursor to fetch the next page.

```shell
nitro openapi list --api-id "<api-id>"
```

## Options

| Option              | Env            | Description                                                          |
| ------------------- | -------------- | -------------------------------------------------------------------- |
| `--api-id <api-id>` | `NITRO_API_ID` | ID of the API. Falls back to interactive selection when omitted.     |
| `--cursor <cursor>` | `NITRO_CURSOR` | Pagination cursor to resume from. Useful for non-interactive paging. |

## Examples

List collections for an API:

```shell
nitro openapi list --api-id "<api-id>"
```

Page through collections in JSON mode:

```shell
nitro openapi list --api-id "<api-id>" --output json
nitro openapi list --api-id "<api-id>" --output json --cursor "<cursor-from-previous-page>"
```

# `nitro openapi delete`

Delete an OpenAPI collection by its ID. Once deleted, the collection and all its versions are no longer accessible.

```shell
nitro openapi delete "<openapi-collection-id>"
```

## Arguments

| Argument | Description                                       |
| -------- | ------------------------------------------------- |
| `<id>`   | ID of the OpenAPI collection to delete. Required. |

## Options

| Option    | Description                                                                                                                 |
| --------- | --------------------------------------------------------------------------------------------------------------------------- |
| `--force` | Skip the confirmation prompt. Required when running non-interactively (for example in CI) or together with `--output json`. |

## Examples

Delete with confirmation:

```shell
nitro openapi delete "<openapi-collection-id>"
```

Delete in a script (no prompt):

```shell
nitro openapi delete "<openapi-collection-id>" --force
```
