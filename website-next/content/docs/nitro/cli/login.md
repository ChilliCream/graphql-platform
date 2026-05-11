---
title: login Command
---

The `nitro login` command logs you in interactively through your default browser. After authenticating, the CLI prompts you to select a default workspace (skipped when you only have one) and persists the session locally so subsequent commands don't need `--api-key`.

For non-interactive environments such as CI/CD, skip `nitro login` entirely and authenticate per-invocation with `--api-key` instead (see [Global Options](./global-options.md)).

```shell
nitro login
```

# Arguments

| Argument | Description                                                                     |
| -------- | ------------------------------------------------------------------------------- |
| `<url>`  | URL of the Nitro backend. Only needed for self-hosted or dedicated deployments. |

# Examples

Log in against the default Nitro Cloud:

```shell
nitro login
```

Log in against a self-hosted or dedicated deployment using the positional argument:

```shell
nitro login "<nitro-backend-url>"
```
