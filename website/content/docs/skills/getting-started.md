---
title: "Getting Started"
description: "Run the `skills` CLI with `dnx` and add your first Agent Skill to Claude Code or another AI coding agent in five minutes using `dnx skills add`."
---

By the end of this guide you will have run `skills` and added your first skill to your agent.

`skills` is a .NET CLI that installs, updates, and authors [Agent Skills](https://agentskills.io/home), the portable `SKILL.md` files that extend AI coding agents like Claude Code, Cursor, and GitHub Copilot. You point it at a source (a GitHub repo, a git URL, or a local folder), and it places the skill where your agent looks for it. Your agent then loads the skill on demand when a task matches it.

This page takes you from zero to one working skill. It takes about five minutes.

# Prerequisites

Before you start, make sure you have the following:

| Requirement                                                   | Why you need it                                                                                                                                                             |
| ------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| [.NET 10 SDK or newer](https://dotnet.microsoft.com/download) | To run `skills` with `dnx`, the way these docs show it. The `dnx` command ships with the .NET 10 SDK.                                                                       |
| An AI coding agent                                            | The target for your skill. This guide uses [Claude Code](https://platform.claude.com/docs/en/agents-and-tools/agent-skills/overview). The `skills` CLI supports 55+ agents. |

These docs run `skills` with `dnx`, so there is nothing to install first. If you would rather have `skills` on your `PATH`, install the global tool instead (it needs only the .NET SDK 8.0 or newer). See [Install as a global tool](#install-as-a-global-tool).

# Run `skills`

These docs run `skills` with `dnx`, which needs no install. You can also install it as a global tool if you prefer `skills` on your `PATH`.

## Run with dnx

`dnx` ships with the .NET 10 SDK and runs a tool straight from NuGet, the way `npx` runs a package from npm. Nothing is installed up front.

```bash
dnx skills add anthropics/skills --agent claude-code
```

On the first run, `dnx` prompts you to confirm the package download before it proceeds:

```text
Tool package skills@<version> will be downloaded from source https://api.nuget.org/v3/index.json.
Proceed? [y/n] (y):
```

After you confirm, the install proceeds and prints the summary shown in [Add your first skill](#add-your-first-skill). The first run downloads the package into your NuGet cache; later runs reuse it and start immediately. `dnx skills` is shorthand for `dotnet tool exec skills`.

To skip the download prompt (for example in CI), pass `--yes`:

```bash
dnx --yes skills add anthropics/skills --agent claude-code
```

You can control the `skills` version that `dnx` runs and where it fetches it from:

| Command                              | What it does                        |
| ------------------------------------ | ----------------------------------- |
| `dnx skills@0.3.0 add ...`           | Runs an exact version.              |
| `dnx skills@0.3.* add ...`           | Runs the latest 0.3.x version.      |
| `dnx --prerelease skills add ...`    | Allows prerelease versions.         |
| `dnx --source <feed> skills add ...` | Fetches from a specific NuGet feed. |

If your repository has a `.config/dotnet-tools.json` manifest that lists `skills`, `dnx` honors the pinned version from the manifest, which keeps one-shot runs consistent with the version your team committed.

## Install as a global tool

If you would rather have `skills` on your `PATH`, install it as a global [.NET tool](https://learn.microsoft.com/dotnet/core/tools/global-tools). This needs the .NET SDK 8.0 or newer.

```bash
dotnet tool install -g skills
```

```text
You can invoke the tool using the following command: skills
Tool 'skills' (version '0.3.0') was successfully installed.
```

With the global tool you run `skills` directly: drop the `dnx` prefix from every example in these docs (`dnx skills add ...` becomes `skills add ...`). Confirm the install with `skills --version`, which prints a value like `0.3.0+<git-sha>` (the trailing suffix records the exact commit the build came from).

To pin `skills` to a version your whole team shares, install it as a local tool through a manifest instead. From the repository root:

```bash
dotnet new tool-manifest
dotnet tool install skills
```

Local tools are restored with `dotnet tool restore` and run through `dotnet skills`. Check the manifest (`./.config/dotnet-tools.json`) into source control so every collaborator uses the same version.

# Add your first skill

You are ready to install a skill. The example below installs skills from [anthropics/skills](https://github.com/anthropics/skills), Anthropic's reference repository, targeting Claude Code.

```bash
dnx skills add anthropics/skills --agent claude-code
```

Agent names are case-sensitive. Use `claude-code`, not `claude` or `Claude`. For GitHub Copilot the name is `github-copilot`.

The `skills` CLI fetches the source, discovers the skills, installs them, and prints a summary:

```text
Source: https://github.com/anthropics/skills.git
Found 2 skill(s)

┌─Installation Summary─────────────────────────────────────────────────────────┐
│ Canonical: /your-project/.agents/skills                                      │
│ Symlinked:  Claude Code                                                      │
└──────────────────────────────────────────────────────────────────────────────┘
┌─Installed 2 skill(s)─────────────────────────────────────────────────────────┐
│ ✓ pdf                                                                        │
│   → /your-project/.claude/skills/pdf                                         │
│ ✓ docx                                                                       │
│   → /your-project/.claude/skills/docx                                        │
└──────────────────────────────────────────────────────────────────────────────┘

Done!  Review skills before use; they run with full agent permissions.
```

> The trailing line is a deliberate safety nudge. A skill can include scripts that the agent runs with your agent's full permissions. Install skills only from sources you trust, and read a new skill before you use it.

If you omit `--agent`, the CLI runs interactively and lets you pick which agents to target from the ones it detects on your machine. To install only specific skills from a multi-skill source, add `--skill <name>` (repeatable). For the full set of source forms, scopes, and flags, see [Installing Skills](./installing-skills.md).

**Checkpoint.** If everything worked, you should see the `Installed 2 skill(s)` panel and the `Done!` line, with no error. If you saw an `Invalid agents` panel, you misspelled the agent name (it is case-sensitive). If you saw `No valid skills found`, the source had no `SKILL.md` with both a `name` and a `description`. See [Troubleshooting](./troubleshooting.md) for more.

# See what is installed

To confirm what landed in your project, list the installed skills:

```bash
dnx skills list
```

```text
Project Skills

Skill  Path                   Agents
pdf    ./.claude/skills/pdf   Claude Code
docx   ./.claude/skills/docx  Claude Code
```

The table shows each skill's name, where it lives on disk, and the agents linked to it (shown by their display name, `Claude Code`). To list global skills instead, add `-g`. For machine-readable output, add `--json`.

# Use the skill

The skill now lives where your agent looks for it. You do not need to do anything else.

Your agent loads each installed skill's `name` and `description` at startup, then reads the full skill only when a task matches the description. This is [progressive disclosure](https://platform.claude.com/docs/en/agents-and-tools/agent-skills/overview): the description is the trigger, and the body loads on demand. Start a task that matches the skill, and your agent picks it up automatically.

# Update or remove the global tool

If you run `skills` with `dnx`, you never update it yourself: each run fetches the version you ask for. The commands below apply when you installed the global tool.

To update the global `skills` tool to the latest version:

```bash
dotnet tool update -g skills
```

```text
Tool 'skills' was successfully updated from version '0.3.0' to version '<new>'.
```

To uninstall it:

```bash
dotnet tool uninstall -g skills
```

```text
Tool 'skills' (version '0.3.0') was successfully uninstalled.
```

Note that `dotnet tool update` updates the `skills` CLI, not your installed skills. To check your skills for updates, use the `dnx skills update` command, covered in [Installing Skills](./installing-skills.md).

# Next steps

- [Installing Skills](./installing-skills.md): every source form, project versus global scope, picking agents and skills, symlink versus copy, and checking for skill updates.
- [Authoring Skills](./authoring-skills.md): scaffold a `SKILL.md` with `dnx skills init`, write a good description, and publish your skill.
- [Reference](./reference.md): the complete command and flag surface.
