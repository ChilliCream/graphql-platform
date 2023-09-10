---
title: API Management
---

The `barista api` command provides a set of subcommands that allow you to manage APIs.

# Create an API 

The `barista api create` command is used to create a new API.

```shell
barista api create
```


# Delete an API 

The `barista api delete` command is used to delete an API by its ID.

```shell
barista api delete abc123
```

**Arguments:**

- `<id>`: Specifies the ID of the API you want to delete.

# List all Apis

The `barista api list` command is used to list all APIs of a workspace.

```shell
barista api list
```


# Show API Details

The `barista api show` command is used to show the details of an API.

```shell
barista api show abc123
```

**Arguments:**

- `<id>`: Specifies the ID of the API whose details you want to see.

# Set API Settings 

The `barista api set-settings` command is used to set the settings of an API.

```shell
barista api set-settings abc123
```

**Arguments:**

- `<id>`: Specifies the ID of the API whose settings you want to set.
