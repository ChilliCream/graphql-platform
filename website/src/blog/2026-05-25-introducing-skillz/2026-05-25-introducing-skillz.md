---
path: "/blog/2026/05/25/introducing-skillz"
date: "2026-05-25"
title: "Introducing skillz: the .NET CLI for Agent Skills"
description: "skillz is a .NET CLI for installing, updating, and authoring Agent Skills, with dnx support for a one-shot workflow similar to npx skills."
tags: ["dotnet", "release", "products", "ai"]
author: Pascal Senn
authorUrl: https://github.com/pascalsenn
authorImageUrl: https://avatars.githubusercontent.com/u/14233220?v=4
featuredImage: "header.png"
---

# Introducing skillz: the .NET CLI for Agent Skills

Agent Skills package instructions for AI coding agents in a format that can move between tools. A skill starts as a directory with a `SKILL.md` file, YAML frontmatter, and Markdown instructions; when the task needs more context, the same folder can include scripts, references, and assets.

Because the format is portable, the same skill can teach Claude Code, Cursor, GitHub Copilot, Codex, or another compatible agent how your team designs GraphQL schemas, writes tests, reviews accessibility, deploys services, or handles a domain-specific workflow. Agents read the short description up front, then load the full skill and any deeper references only when the task calls for them.

Vercel's [skills.sh](https://skills.sh) made this workflow familiar in the JavaScript ecosystem with `npx skills`: one command installs the skill into the right place and the agent can pick it up. `skillz` brings the same flow to .NET.

```bash
dnx skillz add ChilliCream/agent-skills --skill graphql-schema-design
```

With the .NET 10 SDK, `dnx` runs a tool from NuGet without installing it globally. The first run downloads the package into the NuGet cache, and later runs reuse it, giving .NET teams the same one-shot workflow without a global tool install, a PATH shim, or a Node dependency just to install a few Markdown files.

## `/graphql-schema-design`

The first ChilliCream skill to try with `skillz` is `graphql-schema-design`.

Schema design is exactly where plausible output can become expensive later. In GraphQL, a schema that mirrors entities, models mutations as generic updates, or exposes unbounded arrays instead of connections may look fine in a diff, but it becomes harder to evolve and harder for clients to use.

`graphql-schema-design` makes the agent behave more like a schema reviewer than a code generator. It helps design new schemas, evolve existing ones, and review schema diffs before the implementation starts.

It focuses on the parts of GraphQL schema design that tend to matter later:

- client-first query and mutation shape
- naming that stays understandable as the schema grows
- connections and pagination for lists that can grow
- mutation payloads and domain errors
- schema evolution, deprecations, and breaking-change avoidance
- platform conventions where they are relevant

The skill stays at the contract level: it works on SDL and design feedback, and once the schema is approved, implementation continues in the normal code workflow.

## What skillz does

`dnx skillz add` installs skills from a source into the agents on your machine. A source can be a GitHub `owner/repo`, a full Git URL, a GitLab project, or a local directory.

```bash
dnx skillz add ChilliCream/agent-skills --skill graphql-schema-design
dnx skillz list
dnx skillz update
dnx skillz remove graphql-schema-design
```

By default, installs are project-scoped and recorded in `skills-lock.json` in the working directory. That gives a repository a shared set of skills instead of relying on every developer to configure their agent by hand.

For personal skills you want everywhere, use the global scope:

```bash
dnx skillz add ChilliCream/agent-skills --skill graphql-schema-design --global
```

`skillz` detects supported agents on your machine and installs the skill where each agent expects it. The package currently supports 55+ AI coding agents, including Claude Code, Cursor, GitHub Copilot, Codex, Continue, Gemini CLI, and others. By default, skills are symlinked from a canonical location so updates happen in one place. If an agent or sandbox cannot follow symlinks, use `--copy`.

Useful flags:

```bash
skillz add <source> --agent claude-code
skillz add <source> --skill graphql-schema-design
skillz add <source> --all --yes
skillz add <source> --copy
skillz add <source> --json
```

## What a skill looks like

The Agent Skills format is deliberately small. At minimum, a skill is a folder with a `SKILL.md` file:

```text
graphql-schema-design/
  SKILL.md
  references/
    naming.md
    nullability.md
    mutations.md
```

The required frontmatter fields are `name` and `description`.

```markdown
---
name: graphql-schema-design
description: GraphQL schema design and review for queries, mutations, nullability, naming, pagination, errors, and schema evolution.
---

# GraphQL Schema Design

Instructions for the agent to follow when this skill is activated.
```

The description matters because agents use it to decide when to activate the skill. The Markdown body should contain the core procedure. Detailed checklists, examples, and references can live in separate files so they are only loaded when needed.

## Authoring your own skills

If you want to package your team's conventions as a skill, start with:

```bash
dnx skillz init my-skill
```

That creates a valid `SKILL.md` scaffold with the required frontmatter. From there, write the instructions, add references if the skill needs them, commit the folder, and install it from GitHub, GitLab, a Git URL, or a local path. Private GitHub and GitLab repositories work as long as your Git credentials can access them.

The package is available on [NuGet](https://www.nuget.org/packages/skillz). If you are building skills for your own stack, we would like to hear what patterns you are packaging. Join us on [Slack](https://slack.chillicream.com) if you want to compare notes.
