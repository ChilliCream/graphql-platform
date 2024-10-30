---
title: Client Management
---

The `nitro client` command provides a set of subcommands that allow you to upload, validate, publish, and unpublish client versions, as well as create a new client, list all clients of an API, and show details of a specific client.

# Publish a Client

The `nitro client publish` command is used to publish a client version to a stage.

```shell
nitro client publish  \
  --tag v1.0.0  \
  --stage production  \
  --client-id Q2xpZW50CmdiOGRiYzk5NmRiNTI0OWRlYWIyM2ExNGRiYjdhMTIzNA==
```

**Options:**

- `--tag <tag>` **(required)**: Specifies the tag of the client version to deploy. It creates a new version of the client with the specified tag. The tag can be any string, but it's recommended to use a version number (e.g., v1, v2) or a commit hash. You can set it from the environment variable `NITRO_TAG`.

- `--stage <stage>` **(required)**: Specifies the name of the stage. This is the name of the environment where the client will be published. You can set it from the environment variable `NITRO_STAGE`.

- `--client-id <client-id>` **(required)**: Specifies the ID of the client. This ID can be retrieved with the `nitro client list` command. You can set it from the environment variable `NITRO_CLIENT_ID`.

- `--api-key <api-key>`: Specifies the API key used for authentication. It doesn't have to be provided when you are logged in. Otherwise, it's the secret that `nitro api-key create` returns. You can set it from the environment variable `NITRO_API_KEY`.

- `--wait-for-approval`: Waits for a user to approve the schema change in the app in case of a breaking change.

# Unpublish a Client

The `nitro client unpublish` command is used to unpublish a client version from a stage.

```shell
nitro client unpublish \
  --tag v1.0.0 \
  --stage production \
  --client-id Q2xpZW50CmdiOGRiYzk5NmRiNTI0OWRlYWIyM2ExNGRiYjdhMTIzNA==
```

**Options:**

The options for the `unpublish` command are the same as for the `publish` command.

# Validate a Client

The `nitro client validate` command is used to validate a client version.

```shell
nitro client validate \
  --stage production \
  --client-id Q2xpZW50CmdiOGRiYzk5NmRiNTI0OWRlYWIyM2ExNGRiYjdhMTIzNA== \
  --operations-file ./operations.json
```

**Options:**

- `--stage <stage>` **(required)**: Specifies the name of the stage. This is the name of the environment where the client will be validated. You can set it from the environment variable `NITRO_STAGE`.

- `--client-id <client-id>` **(required)**: Specifies the ID of the client. This ID can be retrieved with the `nitro client list` command. You can set it from the environment variable `NITRO_CLIENT_ID`.

- `--operations-file <operations-file>` **(required)**: Specifies the path to the JSON file with the operations. This is a file containing persisted queries in relay style. You can set it from the environment variable `NITRO_OPERATIONS_FILE`.

- `--api-key <api-key>`: Specifies the API key used for authentication. It doesn't have to be provided when you are logged in. Otherwise, it's the secret that `nitro api-key create` returns. You can set it from the environment variable `NITRO_API_KEY`.

# Upload a Client

The `nitro client upload` command is used to upload a new client version.

```shell
nitro client upload --tag v1.0.1 --operations-file ./operations.json --client-id Q2xpZW50CmdiMDk4MDEyODM0MTI0MDIxNDgxMjQzMTI0MTI=
```

**Options:**

- `--tag <tag>` **(required)**: Specifies the tag of the client version to deploy. It creates a new version of the client with the specified tag. The tag can be any string, but it's recommended to use a version number (e.g., v1, v2) or a commit hash. You can set it from the environment variable `NITRO_TAG`.

- `--operations-file <operations-file>` **(required)**: Specifies the path to the JSON file with the operations. This is a file containing persisted queries in relay style. You can set it from the environment variable `NITRO_OPERATIONS_FILE`.

- `--client-id <client-id>` **(required)**: Specifies the ID of the client. This ID can be retrieved with the `nitro client list` command. You can set it from the environment variable `NITRO_CLIENT_ID`.

- `--api-key <api-key>`: Specifies the API key used for authentication. It doesn't have to be provided when you are logged in. Otherwise, it's the secret that `nitro api-key create` returns. You can set it from the environment variable `NITRO_API_KEY`.

# Create Client

The `nitro client create` command is used to create a new client.

```shell
nitro client create --api-id QXBpCmdiOGRiYzk5NmRiNTI0OWRlYWIyM2ExNGRiYjdhMTIzNA==
```

**Options:**

- `--api-id <api-id>`: Specifies the ID of the API for which you want to create a client. This ID can be retrieved with the `nitro api list` command. You can set it from the environment variable `NITRO_API_ID`.

# List all Clients

The `nitro client list` command is used to list all clients of an API.

```shell
nitro client list --api-id QXBpCmdiOGRiYzk5NmRiNTI0OWRlYWIyM2ExNGRiYjdhMTIzNA==
```

**Options:**

- `--api-id <api-id>`: Specifies the ID of the API for which you want to list the clients. This ID can be retrieved with the `nitro api list` command. You can set it from the environment variable `NITRO_API_ID`.

# Show Client Details

The `nitro client show` command is used to show details of a specific client.

```shell
nitro client show Q2xpZW50CmdiMDk4MDEyODM0MTI0MDIxNDgxMjQzMTI0MTI=
```

**Arguments:**

- `<id>`: The ID of the client whose details you want to show. This ID can be retrieved with the `nitro client list` command.

<!-- spell-checker:ignore Cmdi, Yjdh -->
