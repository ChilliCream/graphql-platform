---
title: "Authoring Skills"
description: "Author agent skills with Skillz: scaffold a folder with dnx skillz init, write the SKILL.md frontmatter and instructions, and publish via git for installs."
---

Package your team's conventions once, and any agent can pick them up. A skill is a folder with a `SKILL.md` file that tells an agent how to do something the way your team does it: your release checklist, your code review rules, your API client patterns. You write it once, push it to a git repo, and your teammates install it with one command. From then on, every agent they run loads that knowledge on demand, no copy-pasting prompts into each session.

This page teaches you to scaffold a skill, write a correct `SKILL.md`, add supporting files, and publish it so others can install it with [dnx skillz add](./installing-skills.md). The skill format is the open [Agent Skills](https://agentskills.io/home) standard, so what you write here works across [more than 50 agents](./index.md), not only the one you author it in.

# Scaffold a skill

> Prerequisites: skillz installed. See [Getting Started](./getting-started.md) to install the CLI before running the commands below.

To create a new skill in its own folder, run `dnx skillz init` with a name.

```bash
dnx skillz init my-skill
```

```text
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

This creates `my-skill/SKILL.md`. If everything worked, you have a folder named `my-skill` with one file inside it.

To scaffold a skill in the current directory instead, run `dnx skillz init` with no name. skillz derives the skill name from the folder you are in and writes `SKILL.md` next to your other files.

```bash
dnx skillz init
```

```text
Initialized skill: my-skill

Created:
  SKILL.md

Next steps:
  1. Edit SKILL.md to define your skill instructions
  2. Update the name and description in the frontmatter

Publishing:
  GitHub: Push to a repo, then skillz add <owner>/<repo>
  URL:    Host the file, then skillz add https://example.com/my-skill/SKILL.md
```

`dnx skillz init` never overwrites an existing skill. If `SKILL.md` is already present, it leaves your file untouched and tells you so.

```text
Skill already exists at my-skill/SKILL.md
```

## The generated template

`dnx skillz init` writes a spec-compliant starting point: the two required frontmatter fields, then a body with a heading and the sections most skills need.

```markdown
---
name: my-skill
description: A brief description of what this skill does
---

# my-skill

Instructions for the agent to follow when this skill is activated.

## When to use

Describe when this skill should be used.

## Instructions

1. First step
2. Second step
3. Additional steps as needed
```

Replace the placeholder `description`, then fill in the body. The next sections explain each part.

# Structure a SKILL.md

A `SKILL.md` has two parts: YAML frontmatter between `---` fences, and a Markdown body below it. The frontmatter is metadata the agent reads to decide whether your skill is relevant. The body is the instructions the agent follows once it decides to use the skill.

The "Required frontmatter" and "Optional frontmatter" subsections below are reference material: field-by-field constraint tables you can scan when you fill in the frontmatter. For the complete, authoritative field list see the [Reference](./reference.md).

## Required frontmatter

Every skill needs `name` and `description`. skillz skips any skill that is missing either field, so getting these right is the difference between a skill that installs and one that silently disappears.

| Field         | Constraints                                                                                                                                                                                                                                               |
| ------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `name`        | 1 to 64 characters. Lowercase letters, digits, and hyphens only. No leading or trailing hyphen. No consecutive hyphens. Should match the skill's folder name. Must not contain XML tags, and must not contain the reserved words `anthropic` or `claude`. |
| `description` | 1 to 1024 characters. Must say BOTH what the skill does AND when to use it. Include specific trigger keywords that match the tasks where the skill applies. Must not contain XML tags.                                                                    |

The `description` is the single most important field you write. At startup the agent loads only your `name` and `description` (not the body), then matches the user's request against that description to decide whether to read the rest. A vague description means the agent never triggers your skill. Front-load what the skill does, then state the conditions that should activate it, and name the tools, file types, or commands involved.

```markdown
---
name: release-checklist
description: Runs our release checklist before publishing a NuGet package. Use when cutting a release, bumping a version, or preparing a package for nuget.org.
---
```

These constraints are the union of two authorities: the open spec at [agentskills.io/specification](https://agentskills.io/specification) (which adds the "match the folder name" and "no consecutive hyphens" rules) and the [Anthropic platform rules](https://platform.claude.com/docs/en/agents-and-tools/agent-skills/overview) (which add the reserved-word and XML-tag prohibitions). Honoring both keeps your skill portable across every agent.

## Optional frontmatter

Add these fields only when you need them. The open spec at [agentskills.io/specification](https://agentskills.io/specification) is the authoritative field list; the most useful fields are below.

| Field           | Purpose                                                                                                                                                                     |
| --------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `license`       | A license name or a reference to a bundled license file. Keep it short.                                                                                                     |
| `compatibility` | Up to 500 characters describing environment needs (intended product, system packages, network access). Most skills do not need it. Example: `Requires git, docker, and jq`. |
| `metadata`      | A string-to-string map for client-defined properties the spec does not cover, such as `author` or `version`. Use distinctive key names to avoid collisions.                 |
| `allowed-tools` | A space-separated list of pre-approved tools, for example `Bash(git:*) Read`.                                                                                               |

For the exhaustive list of frontmatter fields and their constraints, see the [Reference](./reference.md).

> [!WARNING]
> **`allowed-tools` is experimental.** Support varies between agents, and `dnx skillz init` does not emit it. Treat it as a hint that some agents honor and others ignore, not a security boundary.

## The instructions body

The body is plain Markdown the agent follows when your skill activates. There are no format restrictions, so write it the way you would brief a new teammate: step-by-step instructions, input and output examples, and the edge cases people get wrong.

Keep `SKILL.md` focused. The recommendation from both [Anthropic](https://www.anthropic.com/engineering/equipping-agents-for-the-real-world-with-agent-skills) and the open spec is to keep the body under roughly 500 lines and move long detail into supporting files (see the next section). A tight body loads fast and keeps the agent on the steps that matter.

# Add supporting files

A skill folder can hold more than `SKILL.md`. Three conventional subdirectories cover most needs.

```text
my-skill/
├── SKILL.md          # Required: frontmatter + instructions
├── references/       # Docs loaded on demand (REFERENCE.md, API.md, ...)
├── scripts/          # Code the agent runs
└── assets/           # Templates, data files, schemas
```

skillz copies or symlinks the whole folder as a unit, so every supporting file ships with the skill. (A few build artifacts are excluded automatically, including `.git`, `node_modules`, and `__pycache__`.)

These directories exist because of [progressive disclosure](./index.md#how-skills-load-progressive-disclosure): the agent loads your skill in stages so a large skill costs almost nothing until it is needed.

- At startup the agent loads only `name` and `description`.
- When the skill triggers, the agent reads the `SKILL.md` body.
- A file under `references/`, `scripts/`, or `assets/` loads only when the body points the agent at it.

This is why moving detail out of `SKILL.md` and into `references/` is free: that material never enters the context window until the agent actually follows a link to it. Reference your supporting files with relative paths one level deep (for example `references/api.md`), and avoid deep nesting chains. For more on the three-stage model, see the [Introduction](./index.md#how-skills-load-progressive-disclosure) and [Anthropic's overview](https://platform.claude.com/docs/en/agents-and-tools/agent-skills/overview).

# How skillz names skills on install

When someone installs your skill, skillz derives the on-disk directory name by sanitizing the `name` field. It lowercases the name, replaces every run of characters outside `a-z`, `0-9`, `.`, and `_` with a single hyphen, and trims leading and trailing dots and hyphens.

For a clean lowercase-hyphen name like `release-checklist`, the on-disk name is identical, with no surprises. A name with spaces or uppercase letters gets rewritten (`My Skill` becomes `my-skill`), which can mismatch what you and your teammates expect on disk and during removal. Pick a clean lowercase-hyphen name that already satisfies the [required frontmatter rules](#required-frontmatter), and the install name matches your folder name exactly.

# Publish and install

A skill is a folder in a git repo, so publishing is committing and pushing. Anywhere skillz can reach with git works.

To publish, commit your skill folder and push it to GitHub, GitLab, or any git remote. Then anyone installs it with `dnx skillz add` pointed at the repo. The forms below show what an installer runs against your published skill; for the full add workflow, including expected output, scoping, and agent targeting, see [Installing Skills](./installing-skills.md).

```bash
# install every skill in the repo
dnx skillz add my-org/my-skills

# install only one skill from a multi-skill repo
dnx skillz add my-org/my-skills@release-checklist

# install a single SKILL.md hosted at a URL
dnx skillz add https://example.com/my-skill/SKILL.md
```

Private repositories work with no extra configuration. skillz shells out to your own `git`, so it uses your existing credentials (SSH agent keys, a git credential helper, or `gh auth`). If you can `git clone` the repo, skillz can install from it. When authentication fails, see [Troubleshooting](./troubleshooting.md).

To keep a work-in-progress skill out of default discovery, set `metadata.internal` to `true` in the frontmatter. skillz hides internal skills unless the installer explicitly opts in (by filtering for the skill by name, or by setting the `INSTALL_INTERNAL_SKILLS` environment variable).

```markdown
---
name: experimental-skill
description: An in-progress skill. Use when testing new conventions before rollout.
metadata:
  internal: "true"
---
```

# Validate a skill

To check a skill against the open spec before you publish, use the reference validator `skills-ref` from the [agentskills/agentskills](https://github.com/agentskills/agentskills) repository. It validates your frontmatter and naming conventions.

```bash
skills-ref validate ./my-skill
```

It reports a clean pass when the skill is valid, or names the offending field and rule when it is not (for example, an empty `description` or a `name` that breaks the naming rules).

Running it after `dnx skillz init` and again before you push catches a malformed `name` or an empty `description` while it is still fast to fix, rather than discovering that skillz skipped your skill on install.

# Next steps

- [Installing Skills](./installing-skills.md): install your published skill, target specific agents, and choose project or global scope.
- [Reference](./reference.md): the complete command and flag surface, including `dnx skillz init`.
