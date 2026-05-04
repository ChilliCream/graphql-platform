---
title: status
---

The `nitro status` command displays the current session status, including the logged-in user, the active workspace (if one is selected), and the backend URL when it differs from the default.

If no session exists, the command exits with an error and instructs you to run `nitro login`.

All `status` commands require authentication. Run `nitro login` first or pass `--api-key` (see [Global Options](/docs/nitro/cli/global-options)).

```shell
nitro status
```

## Examples

Show the current session:

```shell
nitro status
```
