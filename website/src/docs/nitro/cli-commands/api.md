---
title: API Management
---

The `nitro api` command provides a set of subcommands that allow you to manage APIs.

# Create an API

The `nitro api create` command is used to create a new API.

```shell
nitro api create --name "My API" --path "/my-api"
```

**Options**

- `--name <name>`: Specifies the name of the API. You can set it from the environment variable `NITRO_API_NAME`.
- `--path <path>`: Specifies the path to the API. You can set it from the environment variable `NITRO_API_PATH`.
- `--workspace-id <workspace-id>`: Specifies the ID of the workspace. If not provided, the default workspace is used. You can set it from the environment variable `NITRO_WORKSPACE_ID`.

**Global Options**

- `--cloud-url <cloud-url>`
- `--api-key <api-key>`
- `--output <json>`

# Delete an API

The `nitro api delete` command is used to delete an API by its ID.

```shell
nitro api delete abc123
```

**Arguments**

- `<id>`: Specifies the ID of the API you want to delete.

**Options**

- `--force`: If provided, the command will not ask for confirmation before deleting.

**Global Options**

- `--cloud-url <cloud-url>`
- `--api-key <api-key>`
- `--output <json>`

# Set API Settings

The `nitro api set-settings` command is used to set the settings of an API.

```shell
nitro api set-settings abc123 --treat-dangerous-as-breaking --allow-breaking-schema-changes
```

**Arguments**

- `<id>`: Specifies the ID of the API whose settings you want to set.

**Options**

- `--treat-dangerous-as-breaking`: If provided, dangerous changes will be treated as breaking changes. You can set it from the environment variable `NITRO_TREAT_DANGEROUS_AS_BREAKING`.
- `--allow-breaking-schema-changes`: If provided, allows breaking schema changes when no client breaks. You can set it from the environment variable `NITRO_ALLOW_BREAKING_SCHEMA_CHANGES`.

**Global Options**

- `--cloud-url <cloud-url>`
- `--api-key <api-key>`
- `--output <json>`

# Show API Details

The `nitro api show` command is used to show the details of an API.

```shell
nitro api show abc123
```

**Arguments**

- `<id>`: Specifies the ID of the API whose details you want to see.

**Global Options**

- `--cloud-url <cloud-url>`
- `--api-key <api-key>`
- `--output <json>`
