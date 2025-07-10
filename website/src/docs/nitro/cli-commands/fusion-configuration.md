---
title: Fusion Configuration Management
---

# Fusion Configuration Publish

The `nitro fusion-configuration publish` command provides a set of subcommands that allow you to begin, start, validate, cancel, and commit a fusion configuration publish, facilitating the management and deployment of fusion configurations.

## Begin a Configuration Publish

The `nitro fusion-configuration publish begin` command requests a deployment slot to begin the publish process of a fusion configuration.

```shell
nitro fusion-configuration publish begin \
  --tag <tag> \
  --stage <stage> \
  --api-id <api-id> \
  [--subgraph-id <subgraph-id>] \
  [--subgraph-name <subgraph-name>] \
  [--wait-for-approval] \
  [--api-key <api-key>]
```

**Options:**

- `--tag <tag>` **(required)**: Specifies the tag of the schema version to deploy. You can set it from the environment variable `NITRO_TAG`.

- `--stage <stage>` **(required)**: Specifies the name of the stage. You can set it from the environment variable `NITRO_STAGE`.

- `--api-id <api-id>` **(required)**: Specifies the ID of the API. You can set it from the environment variable `NITRO_API_ID`.

- `--subgraph-id <subgraph-id>`: Specifies the ID of the subgraph. You can set it from the environment variable `NITRO_SUBGRAPH_ID`.

- `--subgraph-name <subgraph-name>`: Specifies the name of the subgraph. You can set it from the environment variable `NITRO_SUBGRAPH_NAME`.

- `--wait-for-approval`: Waits for a user to approve the schema change in the app in case of a breaking change.

- `--api-key <api-key>`: Specifies the API key used for authentication. You can set it from the environment variable `NITRO_API_KEY`.

## Start a Fusion Configuration Publish

The `nitro fusion-configuration publish start` command initiates the publish process of a fusion configuration.
This command has to be executed just after the completion of the `begin` command to confirm the deployment slot.

```shell
nitro fusion-configuration publish start \
  --request-id <request-id> \
  [--api-key <api-key>]
```

**Options:**

- `--request-id <request-id>`: Specifies the ID of the request. You do not have to provide this when you executed the `begin` command in the same session.

- `--api-key <api-key>`: Specifies the API key used for authentication. You can set it from the environment variable `NITRO_API_KEY`.

## Validate a Fusion Configuration

The `nitro fusion-configuration publish validate` command validates a fusion configuration against the schema and clients.
This step is optional and can be used to ensure that the configuration is correct before committing the publish.

```shell
nitro fusion-configuration publish validate \
  --request-id <request-id> \
  --configuration <configuration> \
  [--api-key <api-key>]
```

**Options:**

- `--request-id <request-id>`: Specifies the ID of the request. You do not have to provide this when you executed the `begin` command in the same session.

- `--configuration <configuration>` **(required)**: Specifies the path to the fusion configuration file. You can set it from the environment variable `NITRO_FUSION_CONFIG_FILE`.

- `--api-key <api-key>`: Specifies the API key used for authentication. You can set it from the environment variable `NITRO_API_KEY`.

## Commit a Fusion Configuration Publish

The `nitro fusion-configuration publish commit` command commits the publish process of a fusion configuration.

```shell
nitro fusion-configuration publish commit \
  --request-id <request-id> \
  --configuration <configuration> \
  [--api-key <api-key>]
```

**Options:**

- `--request-id <request-id>`: Specifies the ID of the request. You do not have to provide this when you executed the `begin` command in the same session.

- `--configuration <configuration>` **(required)**: Specifies the path to the fusion configuration file. You can set it from the environment variable `NITRO_FUSION_CONFIG_FILE`.

- `--api-key <api-key>`: Specifies the API key used for authentication. You can set it from the environment variable `NITRO_API_KEY`.

## Cancel a Fusion Configuration Publish

The `nitro fusion-configuration publish cancel` command cancels the publish process of a fusion configuration.

```shell
nitro fusion-configuration publish cancel \
  --request-id <request-id> \
  [--api-key <api-key>]
```

**Options:**

- `--request-id <request-id>`: Specifies the ID of the request. You do not have to provide this when you executed the `begin` command in the same session.

- `--api-key <api-key>`: Specifies the API key used for authentication. You can set it from the environment variable `NITRO_API_KEY`.

# Download the Most Recent Gateway Configuration

The `nitro fusion-configuration download` command is used to download the most recent gateway configuration.

```shell
nitro fusion-configuration download \
  --stage <stage> \
  --api-id <api-id> \
  [--output-file <output-file>] \
  [--api-key <api-key>]
```

**Options:**

- `--stage <stage>` **(required)**: Specifies the name of the stage. You can set it from the environment variable `NITRO_STAGE`.

- `--api-id <api-id>` **(required)**: Specifies the ID of the API. You can set it from the environment variable `NITRO_API_ID`.

- `--output-file <output-file>`: Specifies the path and name of the output file where the downloaded configuration will be saved. You can set it from the environment variable `NITRO_OUTPUT_FILE`.

- `--api-key <api-key>`: Specifies the API key used for authentication. It's required if you are not logged in. You can set it from the environment variable `NITRO_API_KEY`.
