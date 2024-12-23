---
title: Client Management
---

The `nitro client` command provides a set of subcommands that allow you to create, delete, upload, validate, publish, unpublish client versions, as well as list all clients of an API, show details of a specific client, and download queries from a stage.

# Create a Client

The `nitro client create` command is used to create a new client.

```shell
nitro client create --api-id abc123 --name "My Client"
```

**Options**

- `--api-id <api-id>`: Specifies the ID of the API for which you want to create a client. This ID can be retrieved with the `nitro api list` command. You can set it from the environment variable `NITRO_API_ID`.
- `--name <name>`: Specifies a name for the client for future reference. You can set it from the environment variable `NITRO_API_KEY_NAME`.

**Global Options**

- `--cloud-url <cloud-url>`
- `--api-key <api-key>`
- `--output <json>`

# Delete a Client

The `nitro client delete` command is used to delete a client by its ID.

```shell
nitro client delete client123
```

**Arguments**

- `<id>`: Specifies the ID of the client you want to delete.

**Options**

- `--force`: If provided, the command will not ask for confirmation before deleting.

**Global Options**

- `--cloud-url <cloud-url>`
- `--api-key <api-key>`
- `--output <json>`

# Upload a Client Version

The `nitro client upload` command is used to upload a new client version.

```shell
nitro client upload --tag v1.0.1 --operations-file ./operations.json --client-id client123
```

**Options**

- `--tag <tag>` **(required)**: Specifies the tag of the client version to upload. It creates a new version of the client with the specified tag. The tag can be any string, but it's recommended to use a version number (e.g., v1.0.1) or a commit hash. You can set it from the environment variable `NITRO_TAG`.
- `--operations-file <operations-file>` **(required)**: Specifies the path to the JSON file with the operations. This is a file containing persisted queries in Relay style. You can set it from the environment variable `NITRO_OPERATIONS_FILE`.
- `--client-id <client-id>` **(required)**: Specifies the ID of the client. This ID can be retrieved with the `nitro client list` command. You can set it from the environment variable `NITRO_CLIENT_ID`.

**Global Options**

- `--cloud-url <cloud-url>`
- `--api-key <api-key>`
- `--output <json>`

# Validate a Client Version

The `nitro client validate` command is used to validate a client version.

```shell
nitro client validate --stage production --client-id client123 --operations-file ./operations.json
```

**Options**

- `--stage <stage>` **(required)**: Specifies the name of the stage. This is the name of the environment where the client will be validated. You can set it from the environment variable `NITRO_STAGE`.
- `--client-id <client-id>` **(required)**: Specifies the ID of the client. You can set it from the environment variable `NITRO_CLIENT_ID`.
- `--operations-file <operations-file>` **(required)**: Specifies the path to the JSON file with the operations. You can set it from the environment variable `NITRO_OPERATIONS_FILE`.

**Global Options**

- `--cloud-url <cloud-url>`
- `--api-key <api-key>`
- `--output <json>`

# Publish a Client Version

The `nitro client publish` command is used to publish a client version to a stage.

```shell
nitro client publish --tag v1.0.0 --stage production --client-id client123
```

**Options**

- `--tag <tag>` **(required)**: Specifies the tag of the client version to publish. You can set it from the environment variable `NITRO_TAG`.
- `--stage <stage>` **(required)**: Specifies the name of the stage. You can set it from the environment variable `NITRO_STAGE`.
- `--client-id <client-id>` **(required)**: Specifies the ID of the client. You can set it from the environment variable `NITRO_CLIENT_ID`.
- `--force`: If provided, the command will not ask for confirmation before publishing.
- `--wait-for-approval`: If provided, waits for a user to approve the schema change in case of a breaking change.

**Global Options**

- `--cloud-url <cloud-url>`
- `--api-key <api-key>`
- `--output <json>`

# Unpublish a Client Version

The `nitro client unpublish` command is used to unpublish a client version from a stage.

```shell
nitro client unpublish --tag v1.0.0 --stage production --client-id client123
```

**Options**

- `--tag <tag>` **(required)**: Specifies the tag of the client version to unpublish. You can set it from the environment variable `NITRO_TAG`.
- `--stage <stage>` **(required)**: Specifies the name of the stage. You can set it from the environment variable `NITRO_STAGE`.
- `--client-id <client-id>` **(required)**: Specifies the ID of the client. You can set it from the environment variable `NITRO_CLIENT_ID`.

**Global Options**

- `--cloud-url <cloud-url>`
- `--api-key <api-key>`
- `--output <json>`

# Download Queries from a Stage

The `nitro client download` command is used to download the queries from a stage.

```shell
nitro client download --api-id abc123 --stage production --output ./queries --format relay
```

**Options**

- `--api-id <api-id>` **(required)**: Specifies the ID of the API from which to download the queries. You can set it from the environment variable `NITRO_API_ID`.
- `--stage <stage>` **(required)**: Specifies the name of the stage. You can set it from the environment variable `NITRO_STAGE`.
- `--output <output>` **(required)**: Specifies the path where the queries will be stored.
- `--format <folder|relay>`: Specifies the format in which the queries will be stored. Options are `folder` or `relay`.

**Global Options**

- `--cloud-url <cloud-url>`
- `--api-key <api-key>`
- `--output <json>`

# List All Clients

The `nitro client list` command is used to list all clients of an API.

```shell
nitro client list --api-id abc123
```

**Options**

- `--api-id <api-id>`: Specifies the ID of the API for which you want to list the clients. You can set it from the environment variable `NITRO_API_ID`.
- `--cursor <cursor>`: Specifies the cursor to start the query (for pagination). You can set it from the environment variable `NITRO_CURSOR`.

**Global Options**

- `--cloud-url <cloud-url>`
- `--api-key <api-key>`
- `--output <json>`

# Show Client Details

The `nitro client show` command is used to show details of a specific client.

```shell
nitro client show client123 --fields publishedVersions,versions
```

**Arguments**

- `<id>`: The ID of the client whose details you want to show.

**Options**

- `--fields <publishedVersions|versions>`: Specifies additional fields to display in the client detail prompt. You can specify multiple fields separated by commas.

**Global Options**

- `--cloud-url <cloud-url>`
- `--api-key <api-key>`
- `--output <json>`
