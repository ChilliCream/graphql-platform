---
title: Skillz
metaTitle: "Skillz: Agent Skills CLI for .NET"
description: "skillz is a .NET CLI that installs, updates, and authors Agent Skills: portable SKILL.md files you can share across Claude Code, Cursor, and 50+ agents."
---

skillz is the .NET CLI that installs, updates, and authors [Agent Skills](https://www.anthropic.com/engineering/equipping-agents-for-the-real-world-with-agent-skills): portable `SKILL.md` files that teach an AI coding agent how you work. Stop re-explaining your conventions every session. Package the knowledge once, then hand the same skill to Claude Code, Cursor, GitHub Copilot, and 50+ other agents with a single command.

Run it with `dnx`, which ships with the .NET 10 SDK, so there is nothing to install first:

```bash
dnx skillz add anthropics/skills --agent claude-code
```

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

skillz detects the agents you have installed, then symlinks the skill into each one from a single canonical store. The agent loads the skill on its own when a task matches.

These docs show every command as `dnx skillz`. Prefer skillz on your `PATH`? Install the global tool with `dotnet tool install -g skillz`, then drop the `dnx` prefix and run `skillz` directly.

Need the prerequisites or a step-by-step walkthrough? See [Get started](./getting-started.md). The source lives at [github.com/ChilliCream/skillz](https://github.com/ChilliCream/skillz), and the package is on [nuget.org](https://www.nuget.org/packages/skillz).

# What are Agent Skills

A skill is a folder with a single `SKILL.md` file. That file has YAML frontmatter (a `name` and a `description`) followed by a Markdown body of instructions. Optional subfolders carry supporting material the agent reads only when it needs it.

Think of a skill as an onboarding guide for a new team member. You write down a procedure once (how to run your test suite, how your commit messages are formatted, which API your team prefers), and the agent references the relevant section when a task calls for it. A general-purpose agent becomes a specialist in your codebase without bespoke prompt engineering on every request.

Skills differ from prompts. A prompt is conversation-level guidance you paste in for one task. A skill loads on demand across every conversation, so you never repaste the same instructions again.

The `SKILL.md` format is an open standard created by Anthropic and adopted across the ecosystem. The same file works in Claude Code, Cursor, GitHub Copilot, and more than 50 other agents. To go deeper, read Anthropic's [Agent Skills overview](https://platform.claude.com/docs/en/agents-and-tools/agent-skills/overview) and the [open specification](https://agentskills.io/specification) at agentskills.io.

# How skills load: progressive disclosure

Skills stay out of the agent's context window until they are needed. The format loads in three levels, each at a different time. This is the single most important idea to understand, because it is why you can install many skills without paying a context penalty.

| Level                 | What loads                                            | When                                    | Token cost            |
| --------------------- | ----------------------------------------------------- | --------------------------------------- | --------------------- |
| Level 1: Metadata     | The `name` and `description` from the frontmatter     | Always, at startup                      | ~100 tokens per skill |
| Level 2: Instructions | The `SKILL.md` Markdown body                          | When the skill is triggered             | Keep under ~5k tokens |
| Level 3: Resources    | Bundled files in `references/`, `scripts/`, `assets/` | On demand, when the body points to them | Effectively unlimited |

At startup the agent loads only each skill's `name` and `description`. The `description` is what the agent matches against your request to decide whether to activate the skill, so write it to describe both what the skill does and when to use it. When a request matches, the agent reads the full body. Bundled resources never enter context until the body references them, which is why supporting material can be effectively unbounded.

The open standard frames the same flow as Discovery, Activation, and Execution. For the full model, see Anthropic's [overview](https://platform.claude.com/docs/en/agents-and-tools/agent-skills/overview) and the [agentskills.io home page](https://agentskills.io/home).

# What a skill looks like

A skill is, at minimum, a folder containing one `SKILL.md` file. Everything else is optional and loaded only when referenced:

```text
my-skill/
├── SKILL.md       # Required: frontmatter (name + description) + Markdown instructions
├── references/    # Optional: extra docs the agent reads on demand
├── scripts/       # Optional: code the agent runs via the shell
└── assets/        # Optional: templates, schemas, images, data files
```

The folder name should match the `name` in the frontmatter. Here is a minimal, valid `SKILL.md` with the two required fields:

```markdown
---
name: roll-dice
description: Rolls one or more dice and returns the results. Use when the user asks to roll dice, flip for a decision, or pick a random number in a range.
---

# Roll dice

When the user asks to roll dice, parse the count and number of sides (default to one six-sided die),
then return each roll and the total.
```

The `dnx skillz init` command scaffolds this layout for you; [Authoring Skills](./authoring-skills.md) walks through it with the generated `SKILL.md` and folder tree. The full field reference, including optional frontmatter, lives in the [open specification](https://agentskills.io/specification) and in [Authoring Skills](./authoring-skills.md).

# Why skillz

skillz brings the install-once, run-anywhere skill workflow to the .NET SDK. Three things make it worth adding to your toolbox.

Project-scoped installs are recorded in a `skills-lock.json` file in your working directory. Commit it, and your whole team shares one reproducible skill set, the same way a lock file pins your package dependencies.

One install reaches every detected agent. skillz materializes each skill once in a canonical store, then links it into the directory each installed agent expects. You do not manage per-agent copies by hand.

Personal skills install globally with `--global`. Those live outside any single project and follow you across every repository you work in.

# The ecosystem

The Agent Skills world has three distinct layers. Knowing which is which helps you find what you need.

| Layer         | What it is                                | Where                                                                                                             |
| ------------- | ----------------------------------------- | ----------------------------------------------------------------------------------------------------------------- |
| Format / spec | The open `SKILL.md` standard              | [agentskills.io](https://agentskills.io/specification), [anthropics/skills](https://github.com/anthropics/skills) |
| Discovery     | A directory of published skills to browse | [skills.sh](https://www.skills.sh/)                                                                               |
| Installers    | CLIs that fetch and install skills        | skillz (.NET), [npx skills](https://github.com/vercel-labs/skills) (Node)                                         |

skillz brings the [npx skills](https://github.com/vercel-labs/skills) workflow to the .NET SDK via `dnx`, so you can run it without a global install. The command surface deliberately mirrors what Node developers already know. skillz has no registry of its own: you point `dnx skillz add` at any git repository or local folder, and you discover skills to install from directories like [skills.sh](https://www.skills.sh/).

# Next steps

- [Get started](./getting-started.md): install skillz and add your first skill end to end.
- [Author a skill](./authoring-skills.md): scaffold a `SKILL.md`, write a strong description, and publish it.
