---
title: schema
---

The `nitro schema` commands manage the GraphQL schema (SDL) of an API. A schema version is uploaded with a `--tag` (for example `v1` or a Git commit SHA) and can then be validated against, or published to, a stage (for example `dev`, `staging`, `production`).

The typical flow is: `upload` a new version, `validate` it against the target stage to detect breaking changes, then `publish` it once the changes are safe. `download` retrieves the schema currently published to a stage, which is useful for code generation and local tooling.

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

Upload a schema version tagged with a Git commit:

```shell
nitro schema upload \
  --api-id "<api-id>" \
  --tag "$(git rev-parse HEAD)" \
  --schema-file ./schema.graphqls
```

Upload using environment variables (useful in CI):

```shell
export NITRO_API_ID="<api-id>"
export NITRO_TAG="v1"
export NITRO_SCHEMA_FILE="./schema.graphqls"
nitro schema upload
```

# `nitro schema publish`

Publish a previously uploaded schema version to a stage. By default the publish fails if the version introduces breaking changes. Use `--force` to override, or `--wait-for-approval` to pause until a reviewer approves the change in the Nitro UI.

```shell
nitro schema publish \
  --api-id "<api-id>" \
  --tag "<tag>" \
  --stage "<stage>"
```

## Options

| Option                | Env                       | Description                                                                            |
| --------------------- | ------------------------- | -------------------------------------------------------------------------------------- |
| `--api-id <api-id>`   | `NITRO_API_ID`            | ID of the API. Required.                                                               |
| `--tag <tag>`         | `NITRO_TAG`               | Tag of the schema version to deploy. Required.                                         |
| `--stage <stage>`     | `NITRO_STAGE`             | Name of the stage to publish to. Required.                                             |
| `--force`             |                           | Skip confirmation prompts and publish even when the version contains breaking changes. |
| `--wait-for-approval` | `NITRO_WAIT_FOR_APPROVAL` | Wait for a reviewer to approve the deployment in Nitro before completing.              |

`--force` and `--wait-for-approval` are mutually exclusive.

## Examples

Publish a previously uploaded version to `dev`:

```shell
nitro schema publish \
  --api-id "<api-id>" \
  --tag "<tag>" \
  --stage "dev"
```

Publish to `production` and wait for manual approval:

```shell
nitro schema publish \
  --api-id "<api-id>" \
  --tag "<tag>" \
  --stage "production" \
  --wait-for-approval
```

Force-publish a version with known breaking changes:

```shell
nitro schema publish \
  --api-id "<api-id>" \
  --tag "<tag>" \
  --stage "dev" \
  --force
```

# `nitro schema validate`

Validate a schema file against a stage without publishing it. The server returns the list of breaking, dangerous, and safe changes, plus any operations from registered clients that would break.

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

Validate against a `dev` stage in a pull request check:

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
