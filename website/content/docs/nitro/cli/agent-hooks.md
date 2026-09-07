---
title: agent hooks Command
description: "Wire a coding harness (Claude Code, Codex CLI, or Opencode) into Nitro's turn-boundary hooks with the `nitro agent hooks` commands: install, status, and uninstall, per harness."
---

The `nitro agent hooks` commands install, inspect, and remove the turn-boundary hook entries that connect a coding harness to Nitro. Once installed, three things happen automatically inside a coding session:

- **Actor identity**: the first prompt of a session is prefixed with the session's actor name and role, so the transcript records who was acting.
- **Unread-mail digests**: a prompt sent while `nitro agent mail` has unread messages for the session's actor is prefixed with a nudge naming how many are unread.
- **Idle wake and delivery**: a session that goes idle with mail still unread is pushed a delivery so the actor does not have to be re-prompted by hand.

Each harness wires these in differently. `nitro agent hooks <harness>` is a sibling command group per harness; `nitro agent hooks` itself only groups them.

```shell
nitro agent hooks claude install
nitro agent hooks codex install
nitro agent hooks opencode install
```

# Claude Code

Claude Code hook entries live in a `settings.json` file and cover four events: `SessionStart`, `UserPromptSubmit`, `Stop`, and `SessionEnd`. Each entry runs `nitro agent hook claude <event>`. The `Stop` entry is a hard gate: it can block the turn from ending, up to three times per turn, until unread mail is read, resetting on the next `UserPromptSubmit`.

`install` and `uninstall` only ever touch the entries Nitro itself owns; hook entries another tool added for the same event are left in place.

## `nitro agent hooks claude install`

Add or update this CLI's Claude Code turn-boundary hook entries.

```shell
nitro agent hooks claude install
```

### Options

| Option                    | Description                                                                                                                            |
| ------------------------- | -------------------------------------------------------------------------------------------------------------------------------------- |
| `--scope <project\|user>` | Where the settings file lives: `user` (`~/.claude/settings.json`) or `project` (`<workspace>/.claude/settings.json`). Default: `user`. |
| `--output <json>`         | The output format. Setting `json` also enables non-interactive mode (see [Global Options](./global-options.md)).                       |

### Examples

```shell
nitro agent hooks claude install
nitro agent hooks claude install --scope project
```

## `nitro agent hooks claude status`

Show whether this CLI's Claude Code hook entries are missing, current, or outdated.

```shell
nitro agent hooks claude status
```

### Options

Same `--scope` and `--output` options as `install`.

### Examples

```shell
nitro agent hooks claude status
nitro agent hooks claude status --scope project
```

## `nitro agent hooks claude uninstall`

Remove this CLI's Claude Code turn-boundary hook entries.

```shell
nitro agent hooks claude uninstall
```

### Options

Same `--scope` and `--output` options as `install`.

### Examples

```shell
nitro agent hooks claude uninstall
nitro agent hooks claude uninstall --scope project
```

# Codex CLI

Codex CLI hook entries are user-scoped only, under `$CODEX_HOME` (or `~/.codex` when `CODEX_HOME` is unset): `hooks.json` carries `SessionStart`, `UserPromptSubmit`, and `SessionEnd` entries, each running `nitro agent hook codex <event>`. Codex has no `Stop`-equivalent hook, so the turn-idle gate instead wraps the `notify` program in `config.toml`: when mail is unread at notify time, Nitro queues a follow-up turn via Codex's own `codex queue` mechanism. If `notify` already pointed at another program, that program is preserved and still runs.

## `nitro agent hooks codex install`

Add or update this CLI's Codex CLI turn-boundary hook and notify entries.

```shell
nitro agent hooks codex install
```

### Options

| Option            | Description                                                                                                      |
| ----------------- | ---------------------------------------------------------------------------------------------------------------- |
| `--output <json>` | The output format. Setting `json` also enables non-interactive mode (see [Global Options](./global-options.md)). |

### Examples

```shell
nitro agent hooks codex install
```

## `nitro agent hooks codex status`

Show whether this CLI's Codex CLI hook and notify entries are missing, current, or outdated.

```shell
nitro agent hooks codex status
```

Same `--output` option as `install`.

## `nitro agent hooks codex uninstall`

Remove this CLI's Codex CLI turn-boundary hook entries and restore any wrapped foreign notify program.

```shell
nitro agent hooks codex uninstall
```

Same `--output` option as `install`.

# Opencode

The Opencode integration is a single generated JavaScript plugin, `nitro-hooks.js`, that Opencode auto-loads from its plugin folder. The shim spawns `nitro agent hook opencode <event>` (`session-created`, `session-deleted`, `session-idle`, `chat-message`) as Opencode raises the matching `session.created`, `session.deleted`, `session.idle`, and `chat.message` events, and applies the parts the command returns to the current chat output.

## `nitro agent hooks opencode install`

Add or update Nitro's fail-open Opencode teammate-context plugin.

```shell
nitro agent hooks opencode install
```

### Options

| Option                    | Description                                                                                                                                                                                                                     |
| ------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `--scope <project\|user>` | Where the plugin lives: `user` (`$XDG_CONFIG_HOME/opencode/plugins/nitro-hooks.js`, falling back to `~/.config` when `XDG_CONFIG_HOME` is unset) or `project` (`<workspace>/.opencode/plugin/nitro-hooks.js`). Default: `user`. |
| `--output <json>`         | The output format. Setting `json` also enables non-interactive mode (see [Global Options](./global-options.md)).                                                                                                                |

### Examples

```shell
nitro agent hooks opencode install
nitro agent hooks opencode install --scope project
```

## `nitro agent hooks opencode status`

Show whether Nitro's Opencode plugin is missing, current, or outdated.

```shell
nitro agent hooks opencode status
```

Same `--scope` and `--output` options as `install`.

## `nitro agent hooks opencode uninstall`

Remove Nitro's Opencode teammate-context plugin.

```shell
nitro agent hooks opencode uninstall
```

Same `--scope` and `--output` options as `install`.

# Caveats

- **No hard turn gate.** Unlike Claude Code's `Stop`-hook block and Codex's notify-driven queue, Opencode has no mechanism to hold a turn open. Instead, a session that goes idle with unread mail gets a soft push: Nitro delivers a follow-up prompt to the Opencode server directly, prefixed so the shim can strip it and mark the turn as Nitro's own. Mail that arrives while that pushed turn is still in flight is delivered on the next human prompt, not the next idle transition, because a Nitro-pushed turn does not rearm the idle-push gate.
- **Fail-open shim.** If `nitro` is missing, not on `PATH`, or times out, the generated plugin swallows the failure and returns nothing to Opencode. A teammate without Nitro installed sees no error and no behavior change.
- **Gitignore project-scope installs that are local only.** A project-scope plugin at `.opencode/plugin/nitro-hooks.js` is normally committed so the whole team gets it. Add it to `.gitignore` when the install is local-only, not a team convention.
- **Opencode 1.18 is the recommended minimum.** `install` and `status` warn when Opencode reports (or fails to report) an older version, but never block the command.
- **`OPENCODE_SERVER_PASSWORD` is captured at registration.** When Opencode sets this environment variable, the generated shim forwards it on `session.created`, and Nitro stores it in the local workspace database alongside the session's other endpoint details.
