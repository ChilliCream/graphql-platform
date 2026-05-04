---
title: pat
---

The `nitro pat` commands manage Personal Access Tokens (PATs). A PAT is bound to your user account and acts on your behalf, so it inherits your access across every workspace and API you are a member of. PATs are intended for personal automation, scripts on your machine, and bootstrapping operations that need broader permissions than an [API key](/docs/nitro/cli/api-key) provides (for example creating workspaces or managing members).

To use a PAT for non-interactive CLI calls, pass it via `--api-key` (or `NITRO_API_KEY`). The Nitro server accepts both PATs and API keys through the same option.

> Treat a PAT like a password. It can do anything you can do, store the secret in a secret manager and revoke it as soon as you no longer need it.

All `pat` commands require authentication. Run `nitro login` first or pass `--api-key` (see [Global Options](/docs/nitro/cli/global-options)).

# `nitro pat create`

Create a new personal access token. The secret is returned only once at creation time, store it in a secure location (for example a secret manager) before closing the terminal.

```shell
nitro pat create \
  --description "<description>" \
  --expires "<days>"
```

## Options

| Option                        | Env                 | Description                                                            |
| ----------------------------- | ------------------- | ---------------------------------------------------------------------- |
| `--description <description>` | `NITRO_DESCRIPTION` | Human-readable description used to identify the token later. Required. |
| `--expires <expires>`         | `NITRO_EXPIRES`     | Expiration time of the token in days. Default: `180`.                  |

## Examples

Create a token with the default 180-day expiration:

```shell
nitro pat create --description "<description>"
```

Create a short-lived token:

```shell
nitro pat create --description "<description>" --expires "30"
```

Capture the secret in a script:

```shell
SECRET=$(nitro pat create \
  --description "<description>" \
  --output json | jq -r '.secret')
```

Use the captured secret to authenticate subsequent CLI calls:

```shell
nitro workspace list --api-key "$SECRET"
```

# `nitro pat list`

List the personal access tokens on your user account. Results are paginated, use the returned cursor to fetch the next page. Secrets are not returned, only metadata.

```shell
nitro pat list
```

## Options

| Option              | Env            | Description                                                          |
| ------------------- | -------------- | -------------------------------------------------------------------- |
| `--cursor <cursor>` | `NITRO_CURSOR` | Pagination cursor to resume from. Useful for non-interactive paging. |

## Examples

List your tokens:

```shell
nitro pat list
```

Page through tokens in JSON mode:

```shell
nitro pat list --output json
nitro pat list --output json --cursor "<cursor-from-previous-page>"
```

# `nitro pat revoke`

Revoke a personal access token by its ID. Once revoked, any client using the token loses access immediately.

```shell
nitro pat revoke "<pat-id>"
```

## Arguments

| Argument | Description                                          |
| -------- | ---------------------------------------------------- |
| `<id>`   | ID of the personal access token to revoke. Required. |

## Options

| Option    | Description                                                                                                                 |
| --------- | --------------------------------------------------------------------------------------------------------------------------- |
| `--force` | Skip the confirmation prompt. Required when running non-interactively (for example in CI) or together with `--output json`. |

## Examples

Revoke with confirmation:

```shell
nitro pat revoke "<pat-id>"
```

Revoke in a script (no prompt):

```shell
nitro pat revoke "<pat-id>" --force
```

For narrower, non-user-bound automation (CI/CD, deploy pipelines, telemetry from your GraphQL server), prefer an [API key](/docs/nitro/cli/api-key) scoped to a single API or workspace.
