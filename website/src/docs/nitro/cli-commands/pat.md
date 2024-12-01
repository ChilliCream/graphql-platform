---
title: Personal Access Token Management
---

The `nitro pat` command provides a set of subcommands that allow you to manage Personal Access Tokens (PATs). PATs are tokens associated with your user account that can be used for authenticating with the Nitro platform in scripts, automation tools, and CI/CD pipelines.

# Create a Personal Access Token

> **Important:** Use the value prefixed with `Secret:` as the API key value you pass to `nitro`.

The `nitro pat create` command is used to create a new personal access token.

```shell
nitro pat create --description "Automation Token" --expires 180
```

**Options**

- `--description <description>`: Provides a description for the PAT to help you identify it later. You can set it from the environment variable `NITRO_DESCRIPTION`.
- `--expires <expires>`: Specifies the expiration time of the PAT in days. The default is 180 days. You can set it from the environment variable `NITRO_EXPIRES`.

**Global Options**

- `--cloud-url <cloud-url>`
- `--api-key <api-key>`
- `--output <json>`

## List All Personal Access Tokens

The `nitro pat list` command is used to list all personal access tokens associated with your account.

```shell
nitro pat list
```

**Options**

- `--cursor <cursor>`: Specifies the cursor to start the query (for pagination). Useful in non-interactive mode. You can set it from the environment variable `NITRO_CURSOR`.

**Global Options**

- `--cloud-url <cloud-url>`
- `--api-key <api-key>`
- `--output <json>`

### Revoke a Personal Access Token

The `nitro pat revoke` command is used to revoke a personal access token by its ID.

```shell
nitro pat revoke UGVyc29uYWxBY2Nlc3NUb2tlbjpUaGlzIElzIE5vdCBBIFRva2VuIDspIA==
```

**Arguments**

- `<id>`: Specifies the ID of the personal access token you want to revoke.

**Options**

- `--force`: If provided, the command will not ask for confirmation before revoking.

**Global Options**

- `--cloud-url <cloud-url>`
- `--api-key <api-key>`
- `--output <json>`
