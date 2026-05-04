---
title: session
---

The session commands manage your CLI authentication and provide quick access to the Nitro web UI. Use `nitro login` to authenticate interactively, `nitro logout` to clear the local session, `nitro status` to inspect who you're logged in as, and `nitro launch` to open Nitro in your default browser.

For non-interactive environments such as CI/CD, skip `nitro login` entirely and authenticate per-invocation with `--api-key` instead (see [Global Options](/docs/nitro/cli/global-options)).

# `nitro login`

The `nitro login` command logs you in interactively through your default browser. After authenticating, the CLI prompts you to select a default workspace (skipped when you only have one) and persists the session locally so subsequent commands don't need `--api-key`.

```shell
nitro login
```

## Arguments

| Argument | Description                                                                     |
| -------- | ------------------------------------------------------------------------------- |
| `<url>`  | URL of the Nitro backend. Only needed for self-hosted or dedicated deployments. |

## Options

| Option                    | Env               | Description                                                                                                             |
| ------------------------- | ----------------- | ----------------------------------------------------------------------------------------------------------------------- |
| `--cloud-url <cloud-url>` | `NITRO_CLOUD_URL` | URL of the Nitro backend. Only needed for self-hosted or dedicated deployments. Defaults to `identity.chillicream.com`. |

## Examples

Log in against the default Nitro Cloud:

```shell
nitro login
```

Log in against a self-hosted or dedicated deployment using the positional argument:

```shell
nitro login "<nitro-backend-url>"
```

Log in against a self-hosted or dedicated deployment using the option:

```shell
nitro login --cloud-url "<nitro-backend-url>"
```

# `nitro logout`

The `nitro logout` command logs you out and removes the locally stored session information. After logout, subsequent commands either need a fresh `nitro login` or an explicit `--api-key`.

```shell
nitro logout
```

## Examples

Log out of the current session:

```shell
nitro logout
```

# `nitro status`

The `nitro status` command displays the current session status, including the logged-in user, the active workspace (if one is selected), and the backend URL when it differs from the default.

If no session exists, the command exits with an error and instructs you to run `nitro login`.

The `status` command requires authentication. Run `nitro login` first or pass `--api-key`.

```shell
nitro status
```

## Examples

Show the current session:

```shell
nitro status
```

# `nitro launch`

The `nitro launch` command opens the Nitro web UI in your default browser. It's a convenience shortcut, useful for jumping from the terminal into the UI to create a workspace, browse APIs, or inspect telemetry.

```shell
nitro launch
```

## Examples

Open the Nitro web UI:

```shell
nitro launch
```
