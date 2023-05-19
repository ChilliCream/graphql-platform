---
title:  Schema Management 
---

The `barista schema` command provides a set of subcommands that allow you to upload, validate, and publish schemas.

# Publish a Schema 

The `barista schema publish` command is used to publish a schema version to a stage.

```shell
barista schema publish \
  --tag v1.0.0 \
  --stage production \
  --api-id QXBpCmdiOGRiYzk5NmRiNTI0OWRlYWIyM2ExNGRiYjdhMTIzNA==
```

**Options**

- `--tag <tag>` **(required)**: Specifies the tag of the schema version to deploy. It creates a new version of the schema with the specified tag. The tag can be any string, but it's recommended to use a version number (e.g., v1, v2) or a commit hash. You can set it from the environment variable `BARISTA_TAG`.
  
- `--stage <stage>` **(required)**: Specifies the name of the stage. This is the name of the environment where the schema will be published. You can set it from the environment variable `BARISTA_STAGE`.
  
- `--api-id <api-id>` **(required)**: Specifies the ID of the API to which you are uploading the schema. This ID can be retrieved with the `barista api list` command. You can set it from the environment variable `BARISTA_API_ID`.
  
- `--force`: Forces the operation to succeed even if there are errors.
  
- `--api-key <api-key>`: Specifies the API key used for authentication. It doesn't have to be provided when you are logged in. Otherwise, it's the secret that `barista api-key create` returns. You can set it from the environment variable `BARISTA_API_KEY`.

# Validate a Schema 

The `barista schema validate` command is used to validate a new client version.

```shell
barista schema validate \
  --stage development \
  --api-id QXBpCmdiOGRiYzk5NmRiNTI0OWRlYWIyM2ExNGRiYjdhMTIzNA== \
  --schema-file /path/to/your/schema.graphql
```

**Options**

- `--stage <stage>` **(required)**: Specifies the name of the stage. This is the name of the environment where the schema will be validated. You can set it from the environment variable `BARISTA_STAGE`.
  
- `--api-id <api-id>` **(required)**: Specifies the ID of the API against which the schema will be validated. This ID can be retrieved with the `barista api list` command. You can set it from the environment variable `BARISTA_API_ID`.
  
- `--schema-file <schema-file>` **(required)**: Specifies the path to the GraphQL SDL schema file to be validated. You can set it from the environment variable `BARISTA_SCHEMA_FILE`.
  
- `--api-key <api-key>`: Specifies the API key used for authentication. It doesn't have to be provided when you are logged in. Otherwise, it's the secret that `barista api-key create` returns. You can set it from the environment variable `BARISTA_API_KEY`.

# Upload a Schema 

The `barista schema upload` command is used to upload a new schema version.

```shell
barista schema upload \
  --tag v1.0.0 \
  --schema-file /path/to/your/schema.graphql \
  --api-id QXBpCmdiOGRiYzk5NmRiNTI0OWRlYWIyM2ExNGRiYjdhMTIzNA==
```

**Options**

- `--tag <tag>` **(required)**: Specifies the tag of the schema version to deploy. It creates a new version of the schema with the specified tag. The tag can be any string, but it's recommended to use a version number (e.g., v1, v2) or a commit hash. You can set it from the environment variable `BARISTA_TAG`.
  
- `--schema-file <schema-file>` **(required)**: Specifies the path to the GraphQL SDL schema file to be uploaded. This should be a .graphql file containing the schema definition. You can set it from the environment variable `BARISTA_SCHEMA_FILE`.

- `--api-id <api-id>` **(required)**: Specifies the ID of the API to which you are uploading the schema. This ID can be retrieved with the `barista api list` command. You can set it from the environment variable `BARISTA_API_ID`.

- `--api-key <api-key>`: Specifies the API key used for authentication. It doesn't have to be provided when you are logged in. Otherwise, it's the secret that `barista api-key create` returns. You can set it from the environment variable `BARISTA_API_KEY`.
