---
title: fusion
---

The `nitro fusion` commands manage [Fusion](/docs/fusion) configurations. A Fusion configuration is the composed gateway artifact built from one or more source schemas. Once published to a stage, the gateway loads it and starts serving the federated graph.

> Note: For using these commands in CI/CD pipelines (uploading source schemas, publishing configurations, validating pull requests), see [Deployment and CI/CD](/docs/fusion/v16/deployment-and-ci-cd).

# `nitro fusion upload`

Upload a source schema for a later composition. The schema is stored on the Nitro backend under the given API and tag and can be referenced by name from a subsequent `nitro fusion publish` call (via `--source-schema`).

```shell
nitro fusion upload \
  --api-id "<api-id>" \
  --tag "<tag>" \
  --source-schema-file "<source-schema-file>"
```

## Options

| Option                                          | Env            | Description                                                                                       |
| ----------------------------------------------- | -------------- | ------------------------------------------------------------------------------------------------- |
| `--api-id <api-id>`                             | `NITRO_API_ID` | ID of the API. Required.                                                                          |
| `--tag <tag>`                                   | `NITRO_TAG`    | Tag of the schema version being uploaded (for example a Git commit SHA or release tag). Required. |
| `-f, --source-schema-file <source-schema-file>` |                | Path to a source schema file (`.graphqls`) or to a directory that contains one. Required.         |
| `-w, --working-directory <working-directory>`   |                | Working directory for the command. Used for relative paths.                                       |

> `--source-schema-file` accepts either a schema file or a directory. In both cases, a `schema-settings.json` file is expected to sit next to the schema file (when a directory is given, both files must be inside that directory).

## Examples

Upload a single source schema:

```shell
nitro fusion upload \
  --api-id "<api-id>" \
  --tag "v1" \
  --source-schema-file ./products/schema.graphqls
```

# `nitro fusion publish`

Publish a Fusion configuration to a stage.

```shell
nitro fusion publish \
  --api-id "<api-id>" \
  --stage "<stage>" \
  --tag "<tag>" \
  --source-schema products
```

## Options

| Option                                          | Env                        | Description                                                                                                                                                                                            |
| ----------------------------------------------- | -------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `--api-id <api-id>`                             | `NITRO_API_ID`             | ID of the API. Required.                                                                                                                                                                               |
| `--tag <tag>`                                   | `NITRO_TAG`                | Tag of the schema version to deploy (for example a Git commit SHA or release tag). Required.                                                                                                           |
| `--stage <stage>`                               | `NITRO_STAGE`              | Name of the stage to publish to. Required.                                                                                                                                                             |
| `-s, --source-schema <source-schema>`           |                            | One or more source schemas to include in the composition. Each value is either a name (`example`) or a name plus version (`example@1.0.0`). When the version is omitted, the value of `--tag` is used. |
| `-f, --source-schema-file <source-schema-file>` |                            | One or more paths to a source schema file (`.graphqls`) or to a directory that contains one.                                                                                                           |
| `-a, --archive <archive>`                       | `NITRO_FUSION_CONFIG_FILE` | Path to a Fusion archive file. The `--configuration` alias is deprecated.                                                                                                                              |
| `--legacy-v1-archive <legacy-v1-archive>`       |                            | Path to a Fusion v1 archive file. Only intended for use during the migration from Fusion v1 to Fusion v2+.                                                                                             |
| `--force`                                       |                            | Skip confirmation prompts for deletes and overwrites.                                                                                                                                                  |
| `--wait-for-approval`                           | `NITRO_WAIT_FOR_APPROVAL`  | Block the command until a reviewer approves the deployment. Required when the stage gates deployments.                                                                                                 |
| `-w, --working-directory <working-directory>`   |                            | Working directory for the command. Used for resolving relative paths and auto-discovering source schema files.                                                                                         |

> `--source-schema-file` accepts either a schema file or a directory. In both cases, a `schema-settings.json` file is expected to sit next to the schema file (when a directory is given, both files must be inside that directory).

## Examples

Compose and publish from previously uploaded source schemas:

```shell
nitro fusion publish \
  --api-id "<api-id>" \
  --stage "dev" \
  --tag "v1" \
  --source-schema products \
  --source-schema reviews
```

Compose and publish from local source schema files in one step:

```shell
nitro fusion publish \
  --api-id "<api-id>" \
  --stage "dev" \
  --tag "v1" \
  --source-schema-file ./products/schema.graphqls \
  --source-schema-file ./reviews/schema.graphqls
```

Publish a pre-composed archive:

```shell
nitro fusion publish \
  --api-id "<api-id>" \
  --stage "dev" \
  --tag "v1" \
  --archive ./gateway.far
```

# Advanced: multi-step publish

> Reach for these commands only when `nitro fusion publish` cannot model your pipeline, for example when validation must run in one CI job and the deploy must run in a separate, manually approved job. For everything else, prefer `nitro fusion publish`. The subcommands below split the same flow into individual steps, which is more error-prone and harder to monitor.

A multi-step publish is driven by a single request ID. `begin` allocates a deployment slot and prints a request ID, every following step references that ID (either explicitly via `--request-id` or implicitly via local state that the CLI caches between commands in the same job). The standard order is `begin` → `start` → `validate` → `commit`. `cancel` releases the slot at any time before `commit`.

## `nitro fusion publish begin`

Begin a Fusion configuration publish by requesting a deployment slot for a stage. The returned request ID identifies the publish for every subsequent step.

```shell
nitro fusion publish begin \
  --api-id "<api-id>" \
  --tag "<tag>" \
  --stage "<stage>"
```

| Option                | Env                       | Description                                                                                            |
| --------------------- | ------------------------- | ------------------------------------------------------------------------------------------------------ |
| `--api-id <api-id>`   | `NITRO_API_ID`            | ID of the API. Required.                                                                               |
| `--tag <tag>`         | `NITRO_TAG`               | Tag of the schema version to deploy. Required.                                                         |
| `--stage <stage>`     | `NITRO_STAGE`             | Name of the stage to publish to. Required.                                                             |
| `--wait-for-approval` | `NITRO_WAIT_FOR_APPROVAL` | Block the command until a reviewer approves the deployment. Required when the stage gates deployments. |

## `nitro fusion publish start`

Mark the publish as started. After this step the deployment is in flight and the configuration is being applied to the gateway.

```shell
nitro fusion publish start --request-id "<request-id>"
```

| Option                      | Env                | Description                                                                                           |
| --------------------------- | ------------------ | ----------------------------------------------------------------------------------------------------- |
| `--request-id <request-id>` | `NITRO_REQUEST_ID` | Request ID returned by `begin`. Falls back to the cached ID from the previous step in the same shell. |

## `nitro fusion publish validate`

Validate a composed Fusion archive against everything currently published to the stage targeted by the request.

```shell
nitro fusion publish validate \
  --request-id "<request-id>" \
  --archive "<archive-file>"
```

| Option                      | Env                        | Description                                                                                           |
| --------------------------- | -------------------------- | ----------------------------------------------------------------------------------------------------- |
| `--request-id <request-id>` | `NITRO_REQUEST_ID`         | Request ID returned by `begin`. Falls back to the cached ID from the previous step in the same shell. |
| `-a, --archive <archive>`   | `NITRO_FUSION_CONFIG_FILE` | Path to the Fusion archive to validate. Required. The `--configuration` alias is deprecated.          |

## `nitro fusion publish commit`

Commit the Fusion configuration, finalizing the publish. After this step the new configuration is live on the stage.

```shell
nitro fusion publish commit \
  --request-id "<request-id>" \
  --archive "<archive-file>"
```

| Option                      | Env                        | Description                                                                                           |
| --------------------------- | -------------------------- | ----------------------------------------------------------------------------------------------------- |
| `--request-id <request-id>` | `NITRO_REQUEST_ID`         | Request ID returned by `begin`. Falls back to the cached ID from the previous step in the same shell. |
| `-a, --archive <archive>`   | `NITRO_FUSION_CONFIG_FILE` | Path to the Fusion archive to commit. Required. The `--configuration` alias is deprecated.            |

## `nitro fusion publish cancel`

Cancel an in-progress publish and release the deployment slot. Run this from the failure branch of any job between `begin` and `commit`.

```shell
nitro fusion publish cancel --request-id "<request-id>"
```

| Option                      | Env                | Description                                                                                           |
| --------------------------- | ------------------ | ----------------------------------------------------------------------------------------------------- |
| `--request-id <request-id>` | `NITRO_REQUEST_ID` | Request ID returned by `begin`. Falls back to the cached ID from the previous step in the same shell. |

# `nitro fusion validate`

Validate a Fusion configuration against a stage. Composes the supplied source schemas (or uses a pre-composed archive) and runs the same checks as `publish` without requesting a deployment slot.

```shell
nitro fusion validate \
  --api-id "<api-id>" \
  --stage "<stage>" \
  --archive "<archive-file>"
```

## Options

| Option                                          | Env                        | Description                                                                                                |
| ----------------------------------------------- | -------------------------- | ---------------------------------------------------------------------------------------------------------- |
| `--api-id <api-id>`                             | `NITRO_API_ID`             | ID of the API. Required.                                                                                   |
| `--stage <stage>`                               | `NITRO_STAGE`              | Name of the stage to validate against. Required.                                                           |
| `-a, --archive <archive>`                       | `NITRO_FUSION_CONFIG_FILE` | Path to a pre-composed Fusion archive file. The `--configuration` alias is deprecated.                     |
| `--legacy-v1-archive <legacy-v1-archive>`       |                            | Path to a Fusion v1 archive file. Only intended for use during the migration from Fusion v1 to Fusion v2+. |
| `-f, --source-schema-file <source-schema-file>` |                            | One or more paths to a source schema file (`.graphqls`) or to a directory that contains one.               |

> `--source-schema-file` accepts either a schema file or a directory. In both cases, a `schema-settings.json` file is expected to sit next to the schema file (when a directory is given, both files must be inside that directory).

## Examples

Validate by composing source schemas on the fly:

```shell
nitro fusion validate \
  --api-id "<api-id>" \
  --stage "dev" \
  --source-schema-file ./products/schema.graphqls \
  --source-schema-file ./reviews/schema.graphqls
```

Validate a pre-composed archive:

```shell
nitro fusion validate \
  --api-id "<api-id>" \
  --stage "dev" \
  --archive ./gateway.far
```

# `nitro fusion download`

Download the most recent gateway configuration of a stage to a local archive file.

```shell
nitro fusion download \
  --api-id "<api-id>" \
  --stage "<stage>" \
  --output-file "<output-file>"
```

## Options

| Option                        | Env                 | Description                                                                         |
| ----------------------------- | ------------------- | ----------------------------------------------------------------------------------- |
| `--api-id <api-id>`           | `NITRO_API_ID`      | ID of the API. Required.                                                            |
| `--stage <stage>`             | `NITRO_STAGE`       | Name of the stage to download from. Required.                                       |
| `--version <version>`         |                     | Version of the archive format to request. Defaults to the latest archive version.   |
| `--output-file <output-file>` | `NITRO_OUTPUT_FILE` | File path to write the archive to. When omitted, the archive is streamed to stdout. |

## Examples

Download the live `dev` configuration:

```shell
nitro fusion download \
  --api-id "<api-id>" \
  --stage "dev" \
  --output-file ./gateway.far
```

# `nitro fusion compose`

Compose multiple source schemas into a single composite schema and write the result to a Fusion archive.

```shell
nitro fusion compose \
  --source-schema-file "<source-schema-file>" \
  --archive "<archive-file>"
```

## Options

| Option                                          | Env                        | Description                                                                                                                                                               |
| ----------------------------------------------- | -------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `-f, --source-schema-file <source-schema-file>` |                            | One or more paths to a source schema file (`.graphqls`) or to a directory that contains one. When omitted, source schemas are auto-discovered from the working directory. |
| `-a, --archive <archive>`                       | `NITRO_FUSION_CONFIG_FILE` | Path to the output Fusion archive file. The `--configuration` alias is deprecated.                                                                                        |
| `-e, --env, --environment <environment>`        |                            | Name of the environment used for value substitution in `schema-settings.json` files.                                                                                      |
| `--enable-global-object-identification`         |                            | Add the `Query.node` field for global object identification.                                                                                                              |
| `--include-satisfiability-paths`                |                            | Include paths in satisfiability error messages to make composition errors easier to diagnose.                                                                             |
| `--watch`                                       |                            | Watch source files for changes and recompose automatically.                                                                                                               |
| `-w, --working-directory <working-directory>`   |                            | Working directory for the command. Used for relative paths and source schema auto-discovery.                                                                              |
| `--exclude-by-tag <exclude-by-tag>`             |                            | One or more tags to exclude from the composition.                                                                                                                         |

> `--source-schema-file` accepts either a schema file or a directory. In both cases, a `schema-settings.json` file is expected to sit next to the schema file (when a directory is given, both files must be inside that directory).

## Examples

Compose a gateway from two source schemas:

```shell
nitro fusion compose \
  --source-schema-file ./products/schema.graphqls \
  --source-schema-file ./reviews/schema.graphqls \
  --archive ./gateway.far \
  --env "dev"
```

Auto-discover source schemas from a working directory:

```shell
nitro fusion compose \
  --working-directory ./subgraphs \
  --archive ./gateway.far
```

# `nitro fusion settings set`

Set a Fusion composition setting on a Fusion archive. Use this to flip composition-level toggles after a composition has been produced, without recomposing from sources.

```shell
nitro fusion settings set <SETTING_NAME> <SETTING_VALUE> \
  --archive "<archive-file>"
```

## Arguments

| Argument          | Description                                                                                                                                   |
| ----------------- | --------------------------------------------------------------------------------------------------------------------------------------------- |
| `<SETTING_NAME>`  | Name of the setting to change. One of `cache-control-merge-behavior`, `exclude-by-tag`, `global-object-identification`, `tag-merge-behavior`. |
| `<SETTING_VALUE>` | New value for the setting. Required.                                                                                                          |

## Options

| Option                                   | Env                        | Description                                                                                     |
| ---------------------------------------- | -------------------------- | ----------------------------------------------------------------------------------------------- |
| `-a, --archive <archive>`                | `NITRO_FUSION_CONFIG_FILE` | Path to the Fusion archive file to update. Required. The `--configuration` alias is deprecated. |
| `-e, --env, --environment <environment>` |                            | Name of the environment used for value substitution in `schema-settings.json` files.            |

## Examples

Enable global object identification on an archive:

```shell
nitro fusion settings set global-object-identification "true" \
  --archive ./gateway.far \
  --env "dev"
```

# `nitro fusion run`

Start a Fusion gateway locally with the specified archive. Useful for smoke-testing a composed archive before publishing. Only supports Fusion v2.

```shell
nitro fusion run "<archive-file>"
```

## Arguments

| Argument         | Description                                       |
| ---------------- | ------------------------------------------------- |
| `<ARCHIVE_FILE>` | Path to the Fusion archive file to run. Required. |

## Options

| Option              | Description                          |
| ------------------- | ------------------------------------ |
| `-p, --port <port>` | The port the gateway will listen on. |

## Examples

Run a gateway on port 5000:

```shell
nitro fusion run ./gateway.far --port 5000
```
