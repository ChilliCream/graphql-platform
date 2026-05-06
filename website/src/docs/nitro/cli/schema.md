---
title: schema
---

The `nitro schema` commands manage the GraphQL schema (SDL) of an API.

The typical flow is: `upload` a new version, `validate` it against the target stage to detect breaking changes, then `publish` it once the changes are safe. `download` retrieves the schema currently published to a stage, which is useful for code generation and local tooling.

> For HotChocolate Fusion gateways, use the [`nitro fusion`](/docs/nitro/cli/fusion) commands instead.

All `schema` commands require authentication. Run `nitro login` first or pass `--api-key` (see [Global Options](/docs/nitro/cli/global-options)).

# `nitro schema upload`

Upload a new schema version to an API. The version is identified by a tag and is not yet published to any stage.

```shell
nitro schema upload \
  --api-id "<api-id>" \
  --tag "<tag>" \
  --schema-file <file-path>
```

## Options

| Option                        | Env                 | Description                                                                |
| ----------------------------- | ------------------- | -------------------------------------------------------------------------- |
| `--api-id <api-id>`           | `NITRO_API_ID`      | ID of the API to upload to. Required.                                      |
| `--tag <tag>`                 | `NITRO_TAG`         | Tag of the new schema version, for example `v1` or a Git commit. Required. |
| `--schema-file <schema-file>` | `NITRO_SCHEMA_FILE` | Path to the GraphQL file with the schema definition. Required.             |

## Examples

Upload a schema version:

```shell
nitro schema upload \
  --api-id "<api-id>" \
  --tag "v1" \
  --schema-file ./schema.graphqls
```

# `nitro schema publish`

Publish a previously uploaded schema version to a stage. The version is identified by its tag.

```shell
nitro schema publish \
  --api-id "<api-id>" \
  --tag "<tag>" \
  --stage "<stage>"
```

## Options

| Option                | Env                       | Description                                                                                                                               |
| --------------------- | ------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------- |
| `--api-id <api-id>`   | `NITRO_API_ID`            | ID of the API. Required.                                                                                                                  |
| `--tag <tag>`         | `NITRO_TAG`               | Tag of the schema version to publish. Required.                                                                                           |
| `--stage <stage>`     | `NITRO_STAGE`             | Name of the stage to publish to. Required.                                                                                                |
| `--force`             |                           | Skip confirmation prompts and publish even when the version contains breaking changes. Mutually exclusive with `--wait-for-approval`.     |
| `--wait-for-approval` | `NITRO_WAIT_FOR_APPROVAL` | Block the command until a reviewer approves the deployment. Mutually exclusive with `--force`. Required when the stage gates deployments. |

## Examples

Publish to `dev`:

```shell
nitro schema publish \
  --api-id "<api-id>" \
  --tag "v1" \
  --stage "dev"
```

Publish to a gated stage and wait for approval:

```shell
nitro schema publish \
  --api-id "<api-id>" \
  --tag "v1" \
  --stage "production" \
  --wait-for-approval
```

# `nitro schema validate`

Validate a new schema version against a stage without publishing it. Run this in your pull request validation workflow to catch breaking changes before they are merged.

```shell
nitro schema validate \
  --api-id "<api-id>" \
  --stage "<stage>" \
  --schema-file <file-path>
```

## Options

| Option                        | Env                 | Description                                                    |
| ----------------------------- | ------------------- | -------------------------------------------------------------- |
| `--api-id <api-id>`           | `NITRO_API_ID`      | ID of the API. Required.                                       |
| `--stage <stage>`             | `NITRO_STAGE`       | Name of the stage to validate against. Required.               |
| `--schema-file <schema-file>` | `NITRO_SCHEMA_FILE` | Path to the GraphQL file with the schema definition. Required. |

## Examples

Validate against the `dev` stage:

```shell
nitro schema validate \
  --api-id "<api-id>" \
  --stage "dev" \
  --schema-file ./schema.graphqls
```

# `nitro schema download`

Download the schema currently published to a stage and write it to a file.

```shell
nitro schema download \
  --api-id "<api-id>" \
  --stage "<stage>" \
  --output-file <file-path>
```

## Options

| Option                        | Env                 | Description                                                         |
| ----------------------------- | ------------------- | ------------------------------------------------------------------- |
| `--api-id <api-id>`           | `NITRO_API_ID`      | ID of the API. Required.                                            |
| `--stage <stage>`             | `NITRO_STAGE`       | Name of the stage to download from. Required.                       |
| `--output-file <output-file>` | `NITRO_OUTPUT_FILE` | Path to write the schema to. If the file exists, it is overwritten. |

## Examples

Download the `dev` schema to a local file:

```shell
nitro schema download \
  --api-id "<api-id>" \
  --stage "dev" \
  --output-file ./schema.graphqls
```
