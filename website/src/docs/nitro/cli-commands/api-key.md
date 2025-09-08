---
title: API Key Management
---

The `nitro api-key` command provides a set of subcommands that allow you to manage API keys.

# Create an API Key

The `nitro api-key create` command is used to create a new API key.

> **Important:** Use the value prefixed with `Secret:` as the API key value you pass to `nitro`.

```shell
nitro api-key create --name "My API Key" --api-id abc123
```

**Options**

- `--name <name>`: Specifies a name for the API key for future reference. You can set it from the environment variable `NITRO_API_KEY_NAME`.
- `--api-id <api-id>`: Specifies the ID of the API for which you want to create the API key. This ID can be retrieved with the `nitro api list` command. You can set it from the environment variable `NITRO_API_ID`.
- `--workspace-id <workspace-id>`: Specifies the ID of the workspace. If not provided, the default workspace is used. You can set it from the environment variable `NITRO_WORKSPACE_ID`.

**Global Options**

- `--cloud-url <cloud-url>`
- `--api-key <api-key>`
- `--output <json>`

# Delete an API Key

The `nitro api-key delete` command is used to delete an API key by its ID.

```shell
nitro api-key delete api-key123
```

**Arguments**

- `<id>`: Specifies the ID of the API key you want to delete.

**Options**

- `--force`: If provided, the command will not ask for confirmation before deleting.

**Global Options**

- `--cloud-url <cloud-url>`
- `--api-key <api-key>`
- `--output <json>`

# List All API Keys

The `nitro api-key list` command is used to list all API keys of a workspace.

```shell
nitro api-key list
```

**Options**

- `--cursor <cursor>`: Specifies the cursor to start the query (for pagination). Useful in non-interactive mode. You can set it from the environment variable `NITRO_CURSOR`.
- `--workspace-id <workspace-id>`: Specifies the ID of the workspace. If not provided, the default workspace is used. You can set it from the environment variable `NITRO_WORKSPACE_ID`.

**Global Options**

- `--cloud-url <cloud-url>`
- `--api-key <api-key>`
- `--output <json>`
