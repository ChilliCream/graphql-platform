---
title: API Key Management
---

The `nitro api-key` command provides a set of subcommands that allow you to manage API keys.

# Create a Key

The `nitro api-key create` command is used to create a new API key.

> **Important:** Use the value prefixed with `Secret:` as the api key value you pass to `ChilliCream.Nitro`

```shell
nitro api-key create --api-id abc123
```

**Options**

- `--api-id <api-id>`: Specifies the ID of the API for which you want to create a new API key. This ID can be retrieved with the `nitro api list` command. You can set it from the environment variable `NITRO_API_ID`.

# Delete a Key

The `nitro api-key delete` command is used to delete an API key by its ID.

```shell
nitro api-key delete api-key123
```

**Arguments**

- `<id>`: Specifies the ID of the API key you want to delete.

# List all Keys

The `nitro api-key list` command is used to list all API keys of a workspace.

```shell
nitro api-key list
```
