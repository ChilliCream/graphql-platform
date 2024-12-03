---
title: Workspace Management
---

The `nitro workspace` command provides a set of subcommands that allow you to manage workspaces.

# Create a Workspace

The `nitro workspace create` command is used to create a new workspace.

```shell
nitro workspace create --name "My Workspace" --default
```

**Options**

- `--name <name>`: Specifies the name of the workspace.
- `--default`: If provided, sets the created workspace as the default workspace.

**Global Options**

- `--cloud-url <cloud-url>`
- `--api-key <api-key>`
- `--output <json>`

# Set Default Workspace

The `nitro workspace set-default` command is used to select a workspace and set it as your default workspace.

```shell
nitro workspace set-default
```

**Global Options**

- `--cloud-url <cloud-url>`
- `--api-key <api-key>`
- `--output <json>`

# List all Workspaces

The `nitro workspace list` command is used to list all workspaces.

```shell
nitro workspace list
```

**Global Options**

- `--cloud-url <cloud-url>`
- `--api-key <api-key>`
- `--output <json>`

# Show Workspace Details

The `nitro workspace show` command is used to show the details of a workspace.

```shell
nitro workspace show abc123
```

**Arguments:**

- `<id>`: Specifies the ID of the workspace whose details you want to see.

**Global Options**

- `--cloud-url <cloud-url>`
- `--api-key <api-key>`
- `--output <json>`

# Show Current Workspace

The `nitro workspace current` command is used to show the name of the currently selected workspace.

```shell
nitro workspace current
```

**Global Options**

- `--cloud-url <cloud-url>`
- `--api-key <api-key>`
- `--output <json>`
