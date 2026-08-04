---
title: status Command
description: "The `nitro status` command shows the current Nitro CLI session: the logged-in user, the active workspace, and the backend URL when it is not the default."
---

The `nitro status` command displays the current session status, including the logged-in user, the active workspace (if one is selected), and the backend URL when it differs from the default.

The `status` command requires authentication. Run [`nitro login`](./login.md) first or pass `--api-key`.

```shell
nitro status
```
