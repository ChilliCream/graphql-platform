---
path: "/blog/2026/06/05/introducing-skillz"
date: "2026-06-05"
title: "Introducing skillz: the .NET CLI for Agent Skills"
description: "skillz is a .NET CLI for installing, updating, and authoring Agent Skills, with dnx support for a one-shot workflow the way npx skills works in JavaScript."
tags: ["dotnet", "release", "products", "ai"]
author: Pascal Senn
authorUrl: https://github.com/pascalsenn
authorImageUrl: https://avatars.githubusercontent.com/u/14233220?v=4
featuredImage: "header.png"
---

Over the past year, everyone who has worked with coding agents has probably had their <span class="wow-wobble" aria-label="wow"><em><span>w</span><span>o</span><span>w</span></em></span> moment. Mine came when I pasted an error message (`unterminated string`) from an HTTP response into Codex. We knew the issue had something to do with the parser, but had no idea how it was even possible. Yet five minutes later, Codex pointed me to this code in the HTTP middleware:

```csharp
const int bufferSize = 4096;
var buffer = new byte[bufferSize];
while (await stream.ReadAsync(buffer) == bufferSize)
{
    // process buffer
}
// handle remaining bytes in buffer
```

Do you see it? The code reads from the stream in a loop until it gets back fewer bytes than the buffer size - that's the signal that the stream has ended. But, as so often in software engineering, the devil is in the details. The documentation for `ReadAsync` says:

```text
//Returns:
//  A task that represents the asynchronous read operation. The value of its ValueTask<int> property
//  contains the total number of bytes read into the destination. The result value can be less than
//  the number of bytes allocated in destination if that many bytes are not currently available, or
//  it can be 0 (zero) if the end of the memory stream has been reached.
```

Which makes sense! When a client delivers bytes too slowly to fill a whole buffer, `ReadAsync` can return less than the buffer size even if there are more bytes to come. And of course this _never_ happens in a test case - but out in the wild, with real clients and real network conditions, Murphy's law is lurking at every turn.

That was my <span class="wow-wobble" aria-label="wow"><em><span>w</span><span>o</span><span>w</span></em></span> moment. It probably saved us a day of debugging and a lot of head scratching.

Since then, it's been a bumpy ride. Working with agents can be amazing, but it can also be incredibly frustrating - especially when you spend time explaining exactly what you want, the agent finally gets it, the task wraps up, and then it forgets everything. Next session, you start from scratch.

Anthropic's answer to this is [Skills](https://platform.claude.com/docs/en/agents-and-tools/agent-skills/overview): a way to package your workflows, conventions, and best practices into a format agents can understand and load on demand. Write a skill once, and any compatible agent can pick it up when a matching task comes up. No more re-explaining the same context every session. The concept proved so useful that other coding agents quickly followed suit.

The catch: every agent looks for skills in its own place. Claude uses `.claude`, Codex uses `.agents`, Windsurf uses `.windsurf`, and so on. You end up with the same skill files scattered across multiple directories, kept in sync by hand. And since there's a new _"best model yet"_ every other week - promptly followed by everyone switching tools out of FOMO - skills go stale fast and become a genuine pain to maintain.

That's where `dnx skillz` comes in. It's a CLI for installing, updating, and authoring Agent Skills. You manage skills like any other project dependency, and `skillz` handles putting them where each agent expects to find them. It runs one-shot via `dnx` - the same way `npx` runs packages from npm - so there's nothing to install globally. It also uses symlinks, so you maintain one canonical version of each skill and every agent picks it up from there. [Install .NET 10 to use `dnx`.](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)

`dnx skillz` is heavily inspired by Vercel's `npx skills`, which does the same for JavaScript. They even have a [registry of skills](https://www.skills.sh/) that anyone can publish to. We wanted to bring that same experience to .NET (just without the phoning-home telemetry 🤫), so we built `skillz` as a NuGet package. You can check out [the source code here](https://github.com/ChilliCream/skillz).

There's now [an official standard for skills](https://agentskills.io) and more agents are adopting it. But `dnx skillz` goes beyond just copying files into the right directory - it also supports installing skills from GitHub, GitLab, local directories, and more. Private repositories work too, as long as your git credentials can reach them.

```bash
dnx skillz add ChilliCream/agent-skills --skill graphql-schema-design
```

That command pulls the `graphql-schema-design` skill from the `ChilliCream/agent-skills` repository on GitHub, while `skillz` itself is a NuGet package.

If you are working with Aspire you can try out their skills too:

```bash
dnx skillz add microsoft/aspire-skills
```

or add the .NET-specific ones:

```bash
dnx skillz add dotnet/skills
```

## Installing

There are two ways to run `skillz`:

```bash
# one-shot, no install (needs the .NET 10 SDK)
dnx skillz add <source>

# persistent tool on your PATH (.NET SDK 8.0+)
dotnet tool install -g skillz
skillz add <source>
```

`dnx` ships with the .NET 10 SDK and runs a tool straight from NuGet, the way `npx` runs a package from npm. The first run downloads `skillz` into the NuGet cache, and later runs reuse it. No global install and no PATH entry to manage.

## What skillz does

`skillz add` installs skills from a source for the agents on your machine. A source can be a GitHub `owner/repo`, a full git URL, a GitLab project, or a local directory.

```bash
dnx skillz add ChilliCream/agent-skills --skill graphql-schema-design
dnx skillz list
dnx skillz update
dnx skillz remove graphql-schema-design
```

By default, installs are project-scoped, recorded in `skills-lock.json` in your working directory. The whole repository shares one set of skills, so nobody has to install and configure the same skills by hand.

For personal skills you want everywhere, add `--global`:

```bash
dnx skillz add ChilliCream/agent-skills --skill graphql-schema-design --global
```

Skills are symlinked from one canonical location, so a single update reaches every agent. If an agent or sandbox can't follow symlinks, use `--copy`.

Common flags:

```bash
dnx skillz add <source> --agent claude-code    # target one agent (repeatable)
dnx skillz add <source> --skill <name>         # pick a single skill from the source
dnx skillz add <source> --all                  # install everything, no prompts
dnx skillz add <source> --copy                 # copy files instead of symlinking
dnx skillz list --json                         # machine-readable output
```

## Authoring your own skills

To package your team's conventions as a skill, start with:

```bash
dnx skillz init my-skill
```

That scaffolds a valid `SKILL.md` with the required frontmatter. From there, write the instructions, add references if you need them, and commit the folder. In ChilliCream we have an internal repository on GitHub with skills for our teams to use, and we also publish some of those skills publicly in the `ChilliCream/agent-skills` repository.

## graphql-schema-design

The first ChilliCream skill we're shipping is `graphql-schema-design`.

Schema mistakes are cheap to make and expensive to fix once clients depend on them. It's easy to write a GraphQL schema that mirrors your database tables, treats every mutation as a generic update, and returns unbounded arrays instead of connections. That schema looks fine in a diff, but it's harder to evolve and harder for clients to use.

`graphql-schema-design` turns the agent into a schema reviewer rather than a code generator. It helps you design new schemas, evolve existing ones, and review schema diffs. It brings the best practices from the GraphQL ecosystem - the connection specification, mutation payload pattern, error conventions, naming rules, and more - directly to your fingertips via `/graphql-schema-design`.

It focuses on the decisions that are easy to get wrong and costly to walk back:

- naming that holds up as the schema grows
- connections and pagination for lists that can get large
- mutation payloads and domain errors
- schema evolution, deprecations, and avoiding breaking changes
- client-first query and mutation shape
- Relay and platform conventions

That skill is available now, and more are on the way. We are building skills for backend patterns in Hot Chocolate v16, DataLoader best practices, and more. If you have a skill you want to see built, let us know on [Slack](https://slack.chillicream.com).

`skillz` is on [NuGet](https://www.nuget.org/packages/skillz), and the source is on [GitHub](https://github.com/ChilliCream/skillz). If you're building skills for your own stack, we'd love to see what you build. Come find us on [Slack](https://slack.chillicream.com).

<style>
.wow-wobble { font-weight: 500; font-style: normal; }
.wow-wobble em { display: inline-flex; font-style: italic; }
.wow-wobble em > span {
  display: inline-block;
  background-image: linear-gradient(90deg, #e24b4a, #ef9f27, #639922, #1d9e75, #378add, #7f77dd, #d4537e, #e24b4a);
  background-size: 800% auto;
  -webkit-background-clip: text; background-clip: text; color: transparent;
  animation: wow-slide 6s linear infinite, wow-bob 1.8s ease-in-out infinite;
}
.wow-wobble em > span:nth-child(1) { background-position: 0% center; animation-delay: 0s, 0s; }
.wow-wobble em > span:nth-child(2) { background-position: 33% center; animation-delay: 0s, .2s; }
.wow-wobble em > span:nth-child(3) { background-position: 66% center; animation-delay: 0s, .4s; }

@keyframes wow-slide { to { background-position: 200% center; } }
@keyframes wow-bob { 0%,100% { transform: translateY(0); } 50% { transform: translateY(-4px); } }
</style>
