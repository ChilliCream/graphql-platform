---
title: Environment
---

The `nitro environment` command provides a set of subcommands that allow you to manage environments.

# Create an Environment

The `nitro environment create` command is used to create a new environment.

```shell
nitro environment create --name "Development Environment"
```

**Options**

- `--name <name>` or `-n <name>`: Specifies the name of the environment.
- `--workspace-id <workspace-id>`: Specifies the ID of the workspace. You can set it from the environment variable `NITRO_WORKSPACE_ID`.

**Global Options**

- `--cloud-url <cloud-url>`
- `--api-key <api-key>`
- `--output <json>`

# Show Environment Details

The `nitro environment show` command is used to show the details of an environment.

```shell
nitro environment show env123
```

**Arguments**

- `<id>`: Specifies the ID of the environment whose details you want to see.

**Global Options**

- `--cloud-url <cloud-url>`
- `--api-key <api-key>`
- `--output <json>`
