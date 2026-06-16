---
title: "Troubleshooting"
description: "Troubleshoot skillz errors: each section maps the exact message the CLI prints to its cause and a copy-pasteable fix for installing and updating skills."
---

This page maps the errors and surprises you hit when running skillz to their cause and fix. Each section starts with the exact message skillz prints (where one exists), explains why it happens, and gives you a copy-pasteable way out.

If you are new to skillz, start with [Getting Started](./getting-started.md). For the full command and flag surface, see the [Reference](./reference.md). For what each agent identifier means and where skills land, see [Installing Skills](./installing-skills.md).

# "No valid skills found. Skills require a SKILL.md with name and description."

You ran `dnx skillz add <source>` and skillz reached the source but found nothing to install:

```text
$ dnx skillz add ./my-skills --yes --agent claude-code
# exit 1

Source: /home/you/my-skills
No valid skills found. Skills require a SKILL.md with name and description.
```

Cause: a skill is a directory that contains a `SKILL.md` file with both `name` and `description` in its YAML frontmatter. skillz silently skips any folder that is missing the file, missing either field, or whose `name`/`description` collapses to empty. By default skillz only looks at the source root (or the subpath you pointed it at), so a `SKILL.md` buried in a nested directory is not discovered.

To see exactly what skillz finds without installing anything, run `--list`:

```bash
dnx skillz add owner/repo --list
```

```text
Source: https://github.com/owner/repo.git
Found 2 skill(s)

Available Skills
  alpha
    the alpha skill
  beta
    the beta skill

Use --skill <name> to install specific skills
```

If `--list` reports `Found 0 skill(s)`, work through these in order:

| Check                                                     | Fix                                                                                                                    |
| --------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------- |
| Is your path or subpath pointing at the right folder?     | Point at the directory that contains `SKILL.md`, or add the repo subpath: `dnx skillz add owner/repo/skills/my-skill`. |
| Does the `SKILL.md` have both `name:` and `description:`? | Add the missing field. Both must be non-empty. See [Authoring Skills](./authoring-skills.md).                          |
| Are the skills in nested directories?                     | Add `--full-depth` to scan nested directories: `dnx skillz add owner/repo --full-depth --list`.                        |

> `--full-depth` widens discovery so skillz scans nested directories for skills. It does not change how much of the repository is cloned. Clones are always shallow regardless of this flag.

# "Invalid agents: &lt;name&gt;"

You passed an `--agent` value that skillz does not recognize:

```text
$ dnx skillz add ./my-skills --yes --agent bogus
# exit 1

Source: /home/you/my-skills
Found 1 skill(s)

┌─Invalid agents───────────────────────────────────────────────────────────────┐
│ Invalid agents: bogus                                                        │
│                                                                              │
│ Valid agents: adal, aider-desk, amp, antigravity, augment, bob, claude-code, │
│ cline, codearts-agent, codebuddy, codemaker, codestudio, codex,              │
│ command-code, continue, cortex, crush, cursor, deepagents, devin, dexto,     │
│ droid, firebender, forgecode, gemini-cli, github-copilot, goose,             │
│ hermes-agent, iflow-cli, junie, kilo, kimi-cli, kiro-cli, kode, mcpjam,      │
│ mistral-vibe, mux, neovate, openclaw, opencode, openhands, pi, pochi, qoder, │
│ qwen-code, replit, roo, rovodev, tabnine-cli, trae, trae-cn, universal,      │
│ warp, windsurf, zencoder                                                     │
└──────────────────────────────────────────────────────────────────────────────┘
```

Cause: the value is misspelled or uses the wrong case. Agent identifiers are case-sensitive, and several differ from the product's marketing name. The most common mistakes:

| You typed                   | Use instead      |
| --------------------------- | ---------------- |
| `copilot`, `github_copilot` | `github-copilot` |
| `Claude-Code`, `claude`     | `claude-code`    |
| `gemini`                    | `gemini-cli`     |
| `roo-code`                  | `roo`            |

The error panel always prints every valid identifier, so copy the correct one from the list. The same `Invalid agents: <name>` message appears (without the panel) from `dnx skillz list` and `dnx skillz remove`. For the full table of identifiers and where each one installs skills, see [Installing Skills](./installing-skills.md) and the [Reference](./reference.md).

# Cloning a private repository fails with "Authentication failed"

`dnx skillz add` reports an authentication error and stops before installing.

Cause: skillz never prompts for credentials. It shells out to your own `git`, which uses your existing setup (SSH agent keys, a git credential helper, `gh` auth, or `~/.netrc`). When git cannot authenticate, skillz surfaces the failure instead of hanging. Any credentials embedded in a URL are redacted from the output, so secrets never appear in errors or in the lock file.

To fix it, confirm your machine can reach the repository, then retry the same `dnx skillz add` command.

For SSH sources (`git@github.com:owner/repo.git`):

```bash
ssh -T git@github.com
# Hi <you>! You've successfully authenticated, but GitHub does not provide shell access.
```

For HTTPS sources, authenticate with the GitHub CLI or configure a git credential helper. These are one-time setup commands whose output varies by environment:

```bash
gh auth login                         # uses the gh credential helper
# or
git config --global credential.helper store
```

After `gh auth login` completes, it confirms the authenticated account.

Once `git clone <repo>` succeeds on its own, `dnx skillz add <repo>` will succeed too.

# The clone times out

A large repository can exceed the default clone budget and skillz aborts the fetch.

Cause: skillz caps each clone at 5 minutes (300000 ms). The clone is always shallow (latest commit only), but a very large repository or a slow connection can still run past the limit.

To raise the timeout, set `SKILLZ_CLONE_TIMEOUT_MS` in milliseconds before running the command:

```bash
SKILLZ_CLONE_TIMEOUT_MS=600000 dnx skillz add owner/big-repo --agent claude-code   # 10 minutes
```

With the longer budget the clone finishes and the install completes with the usual `Installed 1 skill(s)` panel.

Alternatively, clone the repository yourself and point skillz at the local copy. This skips the network step entirely:

```bash
git clone --depth 1 https://github.com/owner/big-repo.git
dnx skillz add ./big-repo --agent claude-code
```

Pointing skillz at the local clone installs the skill without touching the network, and prints the same `Installed 1 skill(s)` panel.

# A skill does not appear for one agent

`dnx skillz add` reports success, the skill shows up in `dnx skillz list`, but one of your agents does not see it.

Cause: agents that keep skills in their own directory (for example `.claude/skills` for Claude Code or `.windsurf/skills` for Windsurf) only get a link when that agent's directory already exists in your project. skillz will not create a config directory for an agent you do not use. When the directory is missing, skillz skips the link for that agent but still materializes the skill in the shared `.agents/skills` store, so the skill is installed; it is only the per-agent link that is missing.

Pick whichever fix matches your intent:

| Goal                                                           | Fix                                                                                                                           |
| -------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------- |
| You do use that agent in this project                          | Create its directory, then re-run the install: `mkdir -p .claude && dnx skillz add owner/repo --agent claude-code`.           |
| You want a self-contained copy regardless of agent directories | Add `--copy` to write the skill straight into each agent's directory: `dnx skillz add owner/repo --agent claude-code --copy`. |
| You meant a different agent                                    | Re-run with the correct `--agent` value. See [Installing Skills](./installing-skills.md).                                     |

After the directory exists, re-running the install links the skill into `.claude/skills`. The Installation Summary then lists `Symlinked:  Claude Code` instead of skipping that agent.

Agents that share the universal `.agents/skills` store (Cursor, Codex, GitHub Copilot, Gemini CLI, and others) always see the skill because their directory is the canonical store.

# Symlinks are not created (Windows)

On Windows, skillz copies skills into agent directories instead of linking them.

Cause: creating a symlink on Windows requires either an elevated process or Developer Mode. When skillz cannot create the link, it automatically falls back to copying the skill so the install still succeeds. The skill works either way; the only difference is that copies do not share a single editable source of truth the way symlinks do.

To get symlinks instead of copies, choose one:

- Enable Developer Mode (Settings, "For developers", "Developer Mode"), then re-run the command.
- Run your terminal elevated ("Run as administrator") and re-run the command.

If you prefer copies and want skillz to copy without attempting a symlink first, pass `--copy` explicitly:

```bash
dnx skillz add owner/repo --agent claude-code --copy
```

The output shows the skill was copied rather than linked: the Installation Summary lists `Copied:  Claude Code`, and the skill is written directly into `.claude/skills`.

# "skillz update" cannot check a skill or hits a rate limit

`dnx skillz update` reports that one or more skills could not be checked:

```text
$ dnx skillz update -g

Checking for skill updates...


1 skill(s) cannot be checked automatically:
  * legacy (Private or deleted repo)
    To update: skillz add https://github.com/owner/repo -g -y
```

Cause: `update` checks for newer versions by calling the GitHub API, and unauthenticated calls are rate-limited. It also cannot diff every kind of source. Skills installed from a local path, a generic git URL, a well-known HTTP source, or a private or deleted repository have no checkable remote tree, so they are reported as "cannot be checked automatically" with a manual refresh hint.

To raise the rate limit, give skillz a GitHub token and re-run. skillz reads `GITHUB_TOKEN`, then `GH_TOKEN`, then falls back to `gh auth token`:

```bash
export GITHUB_TOKEN=ghp_...   # or: gh auth login
dnx skillz update
```

With the token in place the rate limit clears and the check completes:

```text
Checking for skill updates...

All skills are up to date.
```

For sources that can never be checked automatically, re-install with `dnx skillz add` to refresh them. The `update` output prints the exact command to run for each one:

```bash
dnx skillz add owner/repo/skills/my-skill -y
```

The refresh re-installs the skill from its source and prints the usual `Installed 1 skill(s)` panel.

> `dnx skillz update` only checks for and reports available updates. It never applies them. Whatever it finds, it prints the precise `skillz add ...` command for you to run (prefix it with `dnx` when you use `dnx`). The aliases `dnx skillz upgrade` and `dnx skillz check` behave identically. See the [Reference](./reference.md).

# "... is newer than this skillz supports"

A command refuses to write to a lock file and reports that the on-disk version is newer than skillz supports.

Cause: the lock file (`skills-lock.json` in your project, or the global `.skill-lock.json`) was written by a newer version of skillz than the one you are running. skillz reads newer lock files but refuses to modify them, so it does not corrupt state written by a version it does not understand.

To fix it, update skillz to the latest release:

```bash
dotnet tool update -g skillz
```

A successful upgrade reports the version change:

```text
Tool 'skillz' was successfully updated from version 0.1.1 to version 0.1.2.
```

If you no longer need the existing lock state and want to start fresh, delete the lock file and re-install your skills. The project lock lives at `skills-lock.json` in your working directory; the global lock lives at `~/.local/share/skillz/.skill-lock.json` (or under `$XDG_DATA_HOME/skillz` if that variable is set).

> [!WARNING]
> **Deleting a lock file discards update tracking.** skillz uses the lock file to know what is installed and to check for updates. After deleting it, run `dnx skillz add` again for each skill so skillz can rebuild the lock. The installed skill folders themselves are not removed by deleting the lock.

# Bundled binary assets are missing from a skill

A skill installs, but a binary file it ships (an image, a model, a compiled tool) is a tiny placeholder or absent.

Cause: skillz disables Git LFS while cloning, so only LFS pointer files are fetched, not the large binaries they track. This keeps clones fast and shallow, but it means LFS-tracked assets do not arrive with the skill.

To fix it, change how the skill stores its assets:

- Store skill assets without Git LFS so they travel as ordinary files in the repository.
- Vendor the assets directly into the skill folder (commit the real files alongside `SKILL.md`).

If you author the skill yourself, see [Authoring Skills](./authoring-skills.md) for how to lay out bundled resources.

# "Failed to remove" a skill

`dnx skillz remove` reports that it could not remove a skill:

```text
$ dnx skillz remove "My Skill" --yes
# exit 1

Failed to remove 1 skill(s)
  My Skill: ...
```

Cause: skillz removes a skill by the sanitized, lowercase, hyphenated name it uses on disk (for example `my-skill`). A folder created outside skillz whose name does not match that sanitized form is not something skillz will delete automatically, so it reports the failure rather than falsely claiming success. skillz also refuses to delete a real non-empty directory or file it did not create, which protects your own data.

To fix it, delete the folder yourself from the skills directory. For a project install that is `./.agents/skills/<folder>`; for the corresponding agent directory it is, for example, `./.claude/skills/<folder>`. For a global install the canonical store is `~/.agents/skills/<folder>`.

```bash
rm -rf "./.agents/skills/My Skill"
```

Run `dnx skillz list` afterward to confirm the skill is gone. The listing no longer shows the removed skill:

```text
$ dnx skillz list

No project skills found.
Try listing global skills with -g
```

# Next steps

- [Reference](./reference.md): every command, flag, exit code, and environment variable in one place.
- [Installing Skills](./installing-skills.md): sources, scopes, agent targeting, and the symlink versus copy model.
- Still stuck? Ask in the [ChilliCream Slack](https://slack.chillicream.com/).
