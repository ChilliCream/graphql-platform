---
title: "Reference"
---

This page is the complete, precise reference for the skillz command surface, JSON output, exit codes, environment variables, file locations, and supported agents. It is meant for lookup, not for learning. To learn by doing, start at [Installing Skills](/docs/skillz/installing-skills) and [Authoring Skills](/docs/skillz/authoring-skills).

Every command prints the same per-command help at the terminal. Run `dnx skillz <command> --help` to see the synopsis, arguments, and options for that command.

```bash
dnx skillz add --help
```

```text
Description:
  Add a skill from a source

Usage:
  skillz add [<source>] [options]

Arguments:
  <source>  Source to fetch skills from (e.g., owner/repo, URL, local path)

Options:
  -g, --global    Install globally
  -a, --agent     Target agent(s)
  -s, --skill     Skill name filter(s)
  -y, --yes       Skip prompts (non-interactive)
  --all           Install all skills to all agents
  --copy          Copy instead of symlinking
  --full-depth    Full-depth clone
  -l, --list      List available skills without installing
  -?, -h, --help  Show help and usage information
```

> The binary on your PATH is `skillz`. Install it with `dotnet tool install -g skillz`, or run it without installing via `dnx skillz <command>` (requires the .NET 10 SDK or later). Both forms accept the same commands, arguments, and options documented below.

# Commands

skillz has five commands: `add`, `remove`, `list`, `update`, and `init`. Each command's options are independent. There are no shared global options beyond `--help` and `--version` (see [Global flags](#global-flags)).

## skillz add &lt;source&gt;

Add skills from a source. Skills are discovered at the source, then materialized into a canonical store and linked into each target agent's skills directory.

```text
dnx skillz add <source> [options]
```

### Arguments

| Argument | Arity | Required | Description                                                                                                                                                        |
| -------- | ----- | -------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `source` | one   | yes      | Source to fetch skills from (for example `owner/repo`, a full git URL, or a local path). If omitted, the command exits 1 with `Missing required argument: source`. |

### Options

| Option         | Alias  | Type                | Default         | Description                                                                                                                                                                                         |
| -------------- | ------ | ------------------- | --------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `--global`     | `-g`   | bool                | `false`         | Install to the global scope (your home directory) instead of the current project.                                                                                                                   |
| `--agent`      | `-a`   | string (repeatable) | (auto-detected) | Target agent(s). Repeatable and space-separated (`--agent claude-code cursor` or `--agent claude-code --agent cursor`). `--agent *` targets every agent. See [Supported agents](#supported-agents). |
| `--skill`      | `-s`   | string (repeatable) | (all)           | Install only skills whose name matches the given filter(s). Repeatable.                                                                                                                             |
| `--yes`        | `-y`   | bool                | `false`         | Skip all prompts and run non-interactively.                                                                                                                                                         |
| `--all`        | (none) | bool                | `false`         | Install all skills to all agents. Implies `--skill *`, `--agent *`, and `--yes`.                                                                                                                    |
| `--copy`       | (none) | bool                | `false`         | Copy skill files into each agent directory instead of symlinking. Use for agents that do not follow symlinks.                                                                                       |
| `--full-depth` | (none) | bool                | `false`         | Widen discovery to scan nested directories in the source, not only the source root. This does not change the git clone (clones are always shallow).                                                 |
| `--list`       | `-l`   | bool                | `false`         | List the available skills at the source without installing anything, then exit.                                                                                                                     |

The default install mode is symlink: skillz materializes each skill once into a canonical store and creates a relative symlink from each agent directory back to it. Pass `--copy` to copy instead. When every selected agent shares one skills directory, copy is used automatically.

For sources, scopes, and install mechanics, see [Installing Skills](/docs/skillz/installing-skills). For agent targeting, see [Supported agents](#supported-agents).

## skillz remove [skills...]

Remove installed skills. With no skill names and an interactive terminal, skillz prompts you to select which skills to remove.

```text
dnx skillz remove [skills...] [options]
```

### Arguments

| Argument | Arity        | Required | Description                                                                                          |
| -------- | ------------ | -------- | ---------------------------------------------------------------------------------------------------- |
| `skills` | zero or more | no       | Skill names to remove. Matched case-insensitively. With no names and no `--all`, runs interactively. |

### Options

| Option     | Alias  | Type                | Default | Description                                                  |
| ---------- | ------ | ------------------- | ------- | ------------------------------------------------------------ |
| `--global` | `-g`   | bool                | `false` | Remove from the global scope instead of the current project. |
| `--agent`  | `-a`   | string (repeatable) | (all)   | Limit removal to specific agent(s). Repeatable.              |
| `--yes`    | `-y`   | bool                | `false` | Skip prompts and run non-interactively.                      |
| `--all`    | (none) | bool                | `false` | Remove every installed skill.                                |

In interactive mode, skillz shows a multiselect prompt followed by a confirmation prompt. The confirmation defaults to no. Declining the confirmation prints `Removal cancelled` and exits with code 130 (see [Exit codes](#exit-codes)).

```text
$ dnx skillz remove alpha
# exit 130

Removal cancelled
```

## skillz list

List installed skills. With no options, lists the current project's skills.

```text
dnx skillz list [options]
```

This command takes no positional arguments.

### Options

| Option     | Alias  | Type                | Default | Description                                                                           |
| ---------- | ------ | ------------------- | ------- | ------------------------------------------------------------------------------------- |
| `--global` | `-g`   | bool                | `false` | List skills in the global scope instead of the current project.                       |
| `--agent`  | `-a`   | string (repeatable) | (none)  | Filter the listing to specific agent(s). Repeatable.                                  |
| `--format` | (none) | `text` \| `json`    | `text`  | Output format. `json` emits a JSON array to stdout (see [JSON output](#json-output)). |
| `--json`   | (none) | bool                | `false` | Shorthand for `--format json`.                                                        |

JSON output is enabled when either `--json` is present or `--format json` is set (case-insensitive). Enabling JSON suppresses the banner and writes the array to stdout.

## skillz update [skills...]

Check for available updates and print the exact command to apply each one.

```text
dnx skillz update [skills...] [options]
```

> Aliases: `upgrade` and `check`. `dnx skillz upgrade` and `dnx skillz check` are identical to `dnx skillz update`.

<Warning>

**`update` only reports.** It never modifies files or lock files. For each skill that has an update, it prints a `skillz add ...` command you can copy and run to apply it (run it as `dnx skillz add ...` if you use `dnx`). The command always exits 0, even when updates are available. Its output ends with `no updates were applied.`

</Warning>

### Arguments

| Argument | Arity        | Required | Description                                                                           |
| -------- | ------------ | -------- | ------------------------------------------------------------------------------------- |
| `skills` | zero or more | no       | Optional skill names to check. With names and no scope flag, both scopes are checked. |

### Options

| Option      | Alias | Type | Default | Description                                                                |
| ----------- | ----- | ---- | ------- | -------------------------------------------------------------------------- |
| `--global`  | `-g`  | bool | `false` | Check global skills only.                                                  |
| `--project` | `-p`  | bool | `false` | Check project skills only.                                                 |
| `--yes`     | `-y`  | bool | `false` | Skip the interactive scope prompt. Non-interactive runs check both scopes. |

With neither `-g` nor `-p`, an interactive terminal prompts you to choose Project, Global, or Both. Passing both flags, or running non-interactively, checks both scopes.

```text
$ dnx skillz update -g

Checking for skill updates...

Checking global skill 1/1: my-skill
Found 1 global update(s)

Update available: my-skill
  Run: skillz add owner/repo/skills/my-skill -g -y

Updates available for 1 skill(s); no updates were applied.
```

## skillz init [name]

Scaffold a new skill directory containing a `SKILL.md` template. This command has no options.

```text
dnx skillz init [name]
```

### Arguments

| Argument | Arity       | Required | Description                                                                                                                                                       |
| -------- | ----------- | -------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `name`   | zero or one | no       | Skill name. Creates `<name>/SKILL.md`. The name is sanitized to a slug. With no name, derives the slug from the current directory and writes `SKILL.md` in place. |

If the target `SKILL.md` already exists, skillz does not overwrite it. It prints `Skill already exists at <path>` and exits 0.

```text
$ dnx skillz init my-skill

Initialized skill: my-skill

Created:
  my-skill/SKILL.md

Next steps:
  1. Edit my-skill/SKILL.md to define your skill instructions
  2. Update the name and description in the frontmatter

Publishing:
  GitHub: Push to a repo, then skillz add <owner>/<repo>
  URL:    Host the file, then skillz add https://example.com/my-skill/SKILL.md
```

See [Authoring Skills](/docs/skillz/authoring-skills) for the `SKILL.md` format and publishing workflow.

## Global flags

These are provided on the root command and on every subcommand.

| Option      | Aliases    | Type | Description                                            |
| ----------- | ---------- | ---- | ------------------------------------------------------ |
| `--help`    | `-h`, `-?` | bool | Show help. On a subcommand, shows that command's help. |
| `--version` | (none)     | bool | Print the installed skillz version and exit 0.         |

Running `skillz` with no arguments shows the banner and exits 0. Running `dnx skillz --help` shows curated top-level help. A bare `--` token is removed before parsing, so `dnx skillz add --agent codex -- owner/repo` parses the same as without it.

# JSON output

`dnx skillz list --json` (equivalently `dnx skillz list --format json`) prints a JSON array to stdout. Each element describes one installed skill.

```bash
dnx skillz list --json
```

```json
[
  {
    "name": "alpha",
    "path": "/home/you/project/.agents/skills/alpha",
    "scope": "project",
    "agents": ["Claude Code", "Windsurf"]
  }
]
```

| Field    | Type     | Description                                                                                                                |
| -------- | -------- | -------------------------------------------------------------------------------------------------------------------------- |
| `name`   | string   | The skill's sanitized name (its on-disk directory name).                                                                   |
| `path`   | string   | Absolute path to the skill in the canonical store.                                                                         |
| `scope`  | string   | `"project"` or `"global"`. `"global"` when listed with `-g`, otherwise `"project"`.                                        |
| `agents` | string[] | Display names of agents linked to this skill (for example `Claude Code`). Empty when the skill is not linked to any agent. |

The array is written to stdout, so you can pipe it to a tool such as `jq`. Banner and progress output are suppressed in JSON mode.

# Exit codes

skillz uses exactly three exit codes.

| Code  | Name      | Meaning                                                                                                                                        |
| ----- | --------- | ---------------------------------------------------------------------------------------------------------------------------------------------- |
| `0`   | Success   | The command completed. Note that `update` exits 0 even when updates are available, and `remove` exits 0 when there is nothing to remove.       |
| `1`   | Failure   | The command failed (for example an invalid agent name, no valid skills found, or a file system error). The error message is written to stderr. |
| `130` | Cancelled | The operation was cancelled, either by declining an interactive confirmation or by pressing Ctrl+C.                                            |

# Environment variables

| Variable                  | Default          | Effect                                                                                                                                                            |
| ------------------------- | ---------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `SKILLZ_CLONE_TIMEOUT_MS` | `300000`         | Git clone timeout in milliseconds (5 minutes). Must parse to a positive integer, otherwise the default is used. Raise it for large repositories or slow networks. |
| `GITHUB_TOKEN`            | (unset)          | A GitHub API token. Used only by `update` to raise GitHub API rate limits when checking for updates. Not used for cloning.                                        |
| `GH_TOKEN`                | (unset)          | Fallback GitHub API token. Consulted by `update` after `GITHUB_TOKEN`. Same effect.                                                                               |
| `INSTALL_INTERNAL_SKILLS` | (unset)          | Set to `1` or `true` to include skills their authors marked as internal in discovery. By default such skills are hidden.                                          |
| `CLAUDE_CONFIG_DIR`       | `~/.claude`      | Overrides the Claude Code configuration directory used for detection and global installs.                                                                         |
| `CODEX_HOME`              | `~/.codex`       | Overrides the Codex configuration directory used for detection and global installs.                                                                               |
| `VIBE_HOME`               | `~/.vibe`        | Overrides the Mistral Vibe configuration directory used for detection and global installs.                                                                        |
| `XDG_DATA_HOME`           | `~/.local/share` | Root for the global lock location. The global lock lives at `$XDG_DATA_HOME/skillz/.skill-lock.json`.                                                             |

`XDG_CONFIG_HOME` (default `~/.config`) and `XDG_STATE_HOME` (default `~/.local/state`) affect where skillz keeps its own configuration and logs, and where several agents resolve their global directories.

For private repositories, skillz shells out to `git` and uses your existing git credentials (SSH agent, credential helper, `gh` auth). See [Troubleshooting](/docs/skillz/troubleshooting) for authentication and rate-limit guidance.

# File locations

skillz writes a lock file per scope and materializes skills into a canonical store. Project scope is the current working directory; global scope is your home directory.

| Scope   | Lock file                                                                              | Canonical skills store |
| ------- | -------------------------------------------------------------------------------------- | ---------------------- |
| Project | `skills-lock.json` (in the current working directory)                                  | `./.agents/skills`     |
| Global  | `~/.local/share/skillz/.skill-lock.json` (or `$XDG_DATA_HOME/skillz/.skill-lock.json`) | `~/.agents/skills`     |

Non-universal agents receive a symlink from their own directory (for example `.claude/skills/<name>`) back into the canonical store. Universal agents use the canonical store directly. See [Supported agents](#supported-agents) for per-agent directories.

# Supported agents

skillz supports more than 50 agents (55 in total). It detects which agents are installed on your machine by probing each agent's configuration directory, and it detects which agent it is running inside by reading environment variables that agent hosts set. When run inside a detected agent, `add` runs non-interactively and targets that agent plus the universal agents.

Agent identifiers are case-sensitive (the identifier is `github-copilot`, not `copilot`). The `--agent` option is repeatable and accepts multiple values per token. `--agent *` targets all agents. "Universal" agents share the `.agents/skills` store, so installing a skill for one universal agent makes it visible to all of them.

In the table below, project directories are relative to the working directory and global directories are absolute (`~` is your home directory, `<config>` is `$XDG_CONFIG_HOME` or `~/.config`). Agents marked "universal" use the shared `.agents/skills` store at project scope.

| Identifier       | Display name       | Project directory            | Global directory                             |
| ---------------- | ------------------ | ---------------------------- | -------------------------------------------- |
| `adal`           | AdaL               | `.adal/skills`               | `~/.adal/skills`                             |
| `aider-desk`     | AiderDesk          | `.aider-desk/skills`         | `~/.aider-desk/skills`                       |
| `amp`            | Amp                | `.agents/skills` (universal) | `<config>/agents/skills`                     |
| `antigravity`    | Antigravity        | `.agents/skills` (universal) | `~/.gemini/antigravity/skills`               |
| `augment`        | Augment            | `.augment/skills`            | `~/.augment/skills`                          |
| `bob`            | IBM Bob            | `.bob/skills`                | `~/.bob/skills`                              |
| `claude-code`    | Claude Code        | `.claude/skills`             | `~/.claude/skills` (via `CLAUDE_CONFIG_DIR`) |
| `cline`          | Cline              | `.agents/skills` (universal) | `~/.agents/skills`                           |
| `codearts-agent` | CodeArts Agent     | `.codeartsdoer/skills`       | `~/.codeartsdoer/skills`                     |
| `codebuddy`      | CodeBuddy          | `.codebuddy/skills`          | `~/.codebuddy/skills`                        |
| `codemaker`      | Codemaker          | `.codemaker/skills`          | `~/.codemaker/skills`                        |
| `codestudio`     | Code Studio        | `.codestudio/skills`         | `~/.codestudio/skills`                       |
| `codex`          | Codex              | `.agents/skills` (universal) | `~/.codex/skills` (via `CODEX_HOME`)         |
| `command-code`   | Command Code       | `.commandcode/skills`        | `~/.commandcode/skills`                      |
| `continue`       | Continue           | `.continue/skills`           | `~/.continue/skills`                         |
| `cortex`         | Cortex Code        | `.cortex/skills`             | `~/.snowflake/cortex/skills`                 |
| `crush`          | Crush              | `.crush/skills`              | `~/.config/crush/skills`                     |
| `cursor`         | Cursor             | `.agents/skills` (universal) | `~/.cursor/skills`                           |
| `deepagents`     | Deep Agents        | `.agents/skills` (universal) | `~/.deepagents/agent/skills`                 |
| `devin`          | Devin for Terminal | `.devin/skills`              | `<config>/devin/skills`                      |
| `dexto`          | Dexto              | `.agents/skills` (universal) | `~/.agents/skills`                           |
| `droid`          | Droid              | `.factory/skills`            | `~/.factory/skills`                          |
| `firebender`     | Firebender         | `.agents/skills` (universal) | `~/.firebender/skills`                       |
| `forgecode`      | ForgeCode          | `.forge/skills`              | `~/.forge/skills`                            |
| `gemini-cli`     | Gemini CLI         | `.agents/skills` (universal) | `~/.gemini/skills`                           |
| `github-copilot` | GitHub Copilot     | `.agents/skills` (universal) | `~/.copilot/skills`                          |
| `goose`          | Goose              | `.goose/skills`              | `<config>/goose/skills`                      |
| `hermes-agent`   | Hermes Agent       | `.hermes/skills`             | `~/.hermes/skills`                           |
| `iflow-cli`      | iFlow CLI          | `.iflow/skills`              | `~/.iflow/skills`                            |
| `junie`          | Junie              | `.junie/skills`              | `~/.junie/skills`                            |
| `kilo`           | Kilo Code          | `.kilocode/skills`           | `~/.kilocode/skills`                         |
| `kimi-cli`       | Kimi Code CLI      | `.agents/skills` (universal) | `~/.config/agents/skills`                    |
| `kiro-cli`       | Kiro CLI           | `.kiro/skills`               | `~/.kiro/skills`                             |
| `kode`           | Kode               | `.kode/skills`               | `~/.kode/skills`                             |
| `mcpjam`         | MCPJam             | `.mcpjam/skills`             | `~/.mcpjam/skills`                           |
| `mistral-vibe`   | Mistral Vibe       | `.vibe/skills`               | `~/.vibe/skills` (via `VIBE_HOME`)           |
| `mux`            | Mux                | `.mux/skills`                | `~/.mux/skills`                              |
| `neovate`        | Neovate            | `.neovate/skills`            | `~/.neovate/skills`                          |
| `openclaw`       | OpenClaw           | `skills`                     | `~/.openclaw/skills`                         |
| `opencode`       | OpenCode           | `.agents/skills` (universal) | `<config>/opencode/skills`                   |
| `openhands`      | OpenHands          | `.openhands/skills`          | `~/.openhands/skills`                        |
| `pi`             | Pi                 | `.pi/skills`                 | `~/.pi/agent/skills`                         |
| `pochi`          | Pochi              | `.pochi/skills`              | `~/.pochi/skills`                            |
| `qoder`          | Qoder              | `.qoder/skills`              | `~/.qoder/skills`                            |
| `qwen-code`      | Qwen Code          | `.qwen/skills`               | `~/.qwen/skills`                             |
| `replit`         | Replit             | `.agents/skills` (universal) | `<config>/agents/skills`                     |
| `roo`            | Roo Code           | `.roo/skills`                | `~/.roo/skills`                              |
| `rovodev`        | Rovo Dev           | `.rovodev/skills`            | `~/.rovodev/skills`                          |
| `tabnine-cli`    | Tabnine CLI        | `.tabnine/agent/skills`      | `~/.tabnine/agent/skills`                    |
| `trae`           | Trae               | `.trae/skills`               | `~/.trae/skills`                             |
| `trae-cn`        | Trae CN            | `.trae/skills`               | `~/.trae-cn/skills`                          |
| `universal`      | Universal          | `.agents/skills` (universal) | `<config>/agents/skills`                     |
| `warp`           | Warp               | `.agents/skills` (universal) | `~/.agents/skills`                           |
| `windsurf`       | Windsurf           | `.windsurf/skills`           | `~/.codeium/windsurf/skills`                 |
| `zencoder`       | Zencoder           | `.zencoder/skills`           | `~/.zencoder/skills`                         |

Passing an unknown identifier exits 1 and prints the full list of valid identifiers:

```text
$ dnx skillz add ./local-path --yes --agent bogus
# exit 1

Invalid agents: bogus
```

# See also

- [Installing Skills](/docs/skillz/installing-skills): sources, scopes, and the install workflow in depth.
- [Authoring Skills](/docs/skillz/authoring-skills): the `SKILL.md` format, `dnx skillz init`, and publishing.
- [Troubleshooting](/docs/skillz/troubleshooting): authentication, rate limits, timeouts, and common errors.
