---
title: Schema Management
---

The `nitro schema` command provides a set of subcommands that allow you to upload, validate, and publish schemas.

# Publish a Schema

The `nitro schema publish` command is used to publish a schema version to a stage.

```shell
nitro schema publish \
  --tag v1.0.0 \
  --stage production \
  --api-id QXBpCmdiOGRiYzk5NmRiNTI0OWRlYWIyM2ExNGRiYjdhMTIzNA==
```

**Options**

- `--tag <tag>` **(required)**: Specifies the tag of the schema version to deploy. It creates a new version of the schema with the specified tag. The tag can be any string, but it's recommended to use a version number (e.g., v1, v2) or a commit hash. You can set it from the environment variable `NITRO_TAG`.
- `--stage <stage>` **(required)**: Specifies the name of the stage. This is the name of the environment where the schema will be published. You can set it from the environment variable `NITRO_STAGE`.
- `--api-id <api-id>` **(required)**: Specifies the ID of the API to which you are uploading the schema. This ID can be retrieved with the `nitro api list` command. You can set it from the environment variable `NITRO_API_ID`.
- `--force`: Forces the operation to succeed even if there are errors.
- `--api-key <api-key>`: Specifies the API key used for authentication. It doesn't have to be provided when you are logged in. Otherwise, it's the secret that `nitro api-key create` returns. You can set it from the environment variable `NITRO_API_KEY`.
- `--wait-for-approval`: Waits for a user to approve the schema change in the app in case of a breaking change.

# Validate a Schema

The `nitro schema validate` command is used to validate a new client version.

```shell
nitro schema validate \
  --stage development \
  --api-id QXBpCmdiOGRiYzk5NmRiNTI0OWRlYWIyM2ExNGRiYjdhMTIzNA== \
  --schema-file /path/to/your/schema.graphql
```

**Options**

- `--stage <stage>` **(required)**: Specifies the name of the stage. This is the name of the environment where the schema will be validated. You can set it from the environment variable `NITRO_STAGE`.
- `--api-id <api-id>` **(required)**: Specifies the ID of the API against which the schema will be validated. This ID can be retrieved with the `nitro api list` command. You can set it from the environment variable `NITRO_API_ID`.
- `--schema-file <schema-file>` **(required)**: Specifies the path to the GraphQL SDL schema file to be validated. You can set it from the environment variable `NITRO_SCHEMA_FILE`.
- `--api-key <api-key>`: Specifies the API key used for authentication. It doesn't have to be provided when you are logged in. Otherwise, it's the secret that `nitro api-key create` returns. You can set it from the environment variable `NITRO_API_KEY`.

# Upload a Schema

The `nitro schema upload` command is used to upload a new schema version.

```shell
nitro schema upload \
  --tag v1.0.0 \
  --schema-file /path/to/your/schema.graphql \
  --api-id QXBpCmdiOGRiYzk5NmRiNTI0OWRlYWIyM2ExNGRiYjdhMTIzNA==
```

**Options**

- `--tag <tag>` **(required)**: Specifies the tag of the schema version to deploy. It creates a new version of the schema with the specified tag. The tag can be any string, but it's recommended to use a version number (e.g., v1, v2) or a commit hash. You can set it from the environment variable `NITRO_TAG`.
- `--schema-file <schema-file>` **(required)**: Specifies the path to the GraphQL SDL schema file to be uploaded. This should be a .graphql file containing the schema definition. You can set it from the environment variable `NITRO_SCHEMA_FILE`.

- `--api-id <api-id>` **(required)**: Specifies the ID of the API to which you are uploading the schema. This ID can be retrieved with the `nitro api list` command. You can set it from the environment variable `NITRO_API_ID`.

- `--api-key <api-key>`: Specifies the API key used for authentication. It doesn't have to be provided when you are logged in. Otherwise, it's the secret that `nitro api-key create` returns. You can set it from the environment variable `NITRO_API_KEY`.

# Download a Schema

The `nitro schema download` command is used to download a schema from a stage.

```shell
nitro schema download --api-id abc123 --stage production --file ./schema.graphql
```

**Options**

- `--api-id <api-id>` **(required)**: Specifies the ID of the API from which to download the schema. You can set it from the environment variable `NITRO_API_ID`.
- `--stage <stage>` **(required)**: Specifies the name of the stage. You can set it from the environment variable `NITRO_STAGE`.
- `--file <file>` **(required)**: Specifies the file path where the schema will be stored.

**Global Options**

- `--cloud-url <cloud-url>`
- `--api-key <api-key>`
- `--output <json>`

<!-- spell-checker:ignore Cmdi, Yjdh -->
