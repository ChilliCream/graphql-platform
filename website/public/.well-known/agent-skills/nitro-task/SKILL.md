---
name: nitro-task
description: >-
  Official skill for `nitro agent tasks`, the local-first, dependency-aware issue
  tracker built into the Nitro CLI for coding agents. Use when creating tasks,
  triaging backlogs, managing dependencies, finding ready work, updating
  status, or syncing the task database to git via JSONL.
license: MIT
domain: project-management
role: specialist
scope: operations
output-format: commands
triggers:
  - nitro agent tasks
  - task tracker
  - task triage
  - backlog
  - dependencies
  - ready work
metadata:
  version: 1.0.0
---

<!-- Ported command-wise from the beads_rust skill at ~/.claude/skills/br/SKILL.md. -->
<!-- TOC: Critical Rules | Quick Workflow | Init and Workspace | Essential Commands | Dependencies | Sync | TUI Warning | Agent Mail | Troubleshooting -->

# nitro agent tasks -- Nitro Task Tracker (Official Skill)

> **Non-invasive:** `nitro agent tasks` NEVER runs git commands. Sync and commit are YOUR responsibility.

## Critical Rules for Agents

| Rule                                         | Why                                                                                                                                                                                                   |
| -------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Use `--output json`**                      | Structured output for parsing                                                                                                                                                                         |
| **NEVER run bare `nitro agent tasks board`** | Blocks the session in an interactive TUI; it errors on a non-TTY console but must not be attempted                                                                                                    |
| **NEVER run bare `nitro agent`**             | In an interactive terminal with a workspace found, it opens the same tabbed TUI (Tasks + Mail); it is safe on a non-TTY console (prints the group's usage guidance instead) but must not be attempted |
| **Sync is EXPLICIT**                         | `nitro agent tasks sync --flush-only` exports the DB to `tasks.jsonl` only                                                                                                                            |
| **Git is YOUR job**                          | `nitro agent tasks` only touches `.nitro/agents/` -- you must `git add .nitro/agents/tasks.jsonl && git commit`                                                                                       |
| **No cycles allowed**                        | `nitro agent tasks dep cycles` must return empty                                                                                                                                                      |
| **Resolve actor at runtime**                 | Use `ACTOR="${NITRO_TASK_ACTOR:-$(whoami)}"` and pass `--actor "$ACTOR"` (the CLI itself falls back to the OS user name if you omit `--actor`)                                                        |

## Quick Workflow

```bash
ACTOR="${NITRO_TASK_ACTOR:-$(whoami)}"

# 1. Find work
nitro agent tasks ready --output json

# 2. Claim it
nitro agent tasks update --actor "$ACTOR" <id> --status in_progress

# 3. Do work...

# 4. Complete
nitro agent tasks close --actor "$ACTOR" <id> --reason "Implemented X"

# 5. Sync to git (EXPLICIT!)
nitro agent tasks sync --flush-only
git add .nitro/agents/tasks.jsonl && git commit -m "feat: X (<id>)"
```

## Init and Workspace

`nitro agent tasks init` and `nitro agent mail init` are retired -- there is no per-feature init anymore and no alias for the old spellings; running either fails with "Unrecognized command or argument 'init'." Initialize (or migrate) the shared workspace once, from the workspace root:

```bash
nitro agent init                    # Create .nitro/agents/ (agents.db + tasks.jsonl) in the current directory
nitro agent init --prefix "app"     # Set an explicit task ID prefix (defaults to the current directory name)
nitro agent init --force            # Reinitialize an existing workspace (reapplies schema/prefix/.gitignore; never touches tasks.jsonl)
```

`agent init` also migrates a pre-unification layout it finds under the current directory, then leaves the old files for you to remove:

- A legacy `.nitro/tasks/tasks.db` is copied into the new `.nitro/agents/agents.db` table by table; the result reports `migratedTasks` and the human output tells you to `git rm -r .nitro/tasks` and `git add .nitro/agents`.
- A legacy `.nitro/tasks/tasks.jsonl` (or a committed `.nitro/agents/tasks.jsonl` from a fresh clone) is imported into the database; the result reports `importedCount`.
- A legacy `.nitro/mail/mail.db` was never released, so it is only reported, not migrated: "Found a legacy mail workspace at '.nitro/mail'. It was never released, so its data was not migrated." Delete it yourself with `rm -rf .nitro/mail`.

**Worktree limitation carries over:** workspace discovery walks up from the current directory looking for `.nitro/agents/`, so sibling git worktrees do NOT share a workspace by default. Run `nitro agent init` in a deliberate common ancestor directory that contains only the cooperating worktrees (not the whole filesystem), so the walk-up from any worktree finds that shared `.nitro/agents/`.

## Essential Commands

### Issue Lifecycle

```bash
ACTOR="${NITRO_TASK_ACTOR:-$(whoami)}"

nitro agent tasks create --actor "$ACTOR" "Title" --priority p1 --type task  # Create task
nitro agent tasks q --actor "$ACTOR" "Quick note"                 # Quick capture (ID only output)
nitro agent tasks show <id> --output json                         # Show task details
nitro agent tasks update --actor "$ACTOR" <id> --status in_progress  # Update status
nitro agent tasks update --actor "$ACTOR" <id> --priority p0       # Change priority
nitro agent tasks close --actor "$ACTOR" <id> --reason "Done"      # Close with reason
nitro agent tasks close --actor "$ACTOR" <id1> <id2> --reason "..." # Close multiple at once
nitro agent tasks reopen --actor "$ACTOR" <id>                     # Reopen closed task
```

### Create Options

```bash
nitro agent tasks create --actor "$ACTOR" "Title" \
  --priority p1 \             # 0-4 or p0-p4 (0/p0=critical, 4/p4=backlog)
  --type task \                # task, bug, feature, epic, chore, docs, question, or custom
  --assignee "user@..." \      # Optional assignee
  --label backend --label auth \ # Repeatable, one label per flag
  --description "..." \        # Detailed description
  --depends-on <id> \          # Dependency as 'id' or 'type:id'; repeatable
  --parent <parent-id>         # Parent task ID; new task becomes its child
```

### Update Options

```bash
nitro agent tasks update --actor "$ACTOR" <id> \
  --title "New title" \
  --priority p0 \
  --status in_progress \     # open, in_progress, blocked, deferred, closed, or custom
  --assignee "new@..." \
  --add-label reliability \
  --remove-label triage \
  --parent <parent-id> \
  --claim                    # Shorthand for --status in_progress --assignee <actor>
```

Bulk update (batch triage, multiple IDs in one call):

```bash
nitro agent tasks update --actor "$ACTOR" <id1> <id2> <id3> --priority p2 --add-label triage-reviewed --output json
```

### Querying (always use --output json for agents)

```bash
nitro agent tasks ready --output json                       # Actionable work (no blockers)
nitro agent tasks list --output json                         # All open+in_progress tasks
nitro agent tasks list --status open --output json           # Filter by status (repeatable)
nitro agent tasks list --priority p0-p1 --output json         # Filter by priority range
nitro agent tasks list --assignee alice --output json         # Filter by assignee
nitro agent tasks list --all --output json                    # Include closed and tombstoned tasks
nitro agent tasks blocked --output json                       # Show blocked tasks
nitro agent tasks search "keyword" --output json               # Full-text search (matches comments too)
nitro agent tasks show <id> --output json                      # Task details with dependencies, labels, comments
nitro agent tasks stale --output json                          # Open tasks not updated recently (default: 30 days)
nitro agent tasks stale --days 14 --output json                 # Override the staleness window
nitro agent tasks count --by status --output json               # Count with grouping
```

Note: there is no `--sort` flag -- default ordering is priority.

### Dependencies

```bash
nitro agent tasks dep add <id> <depends-on-id>                    # id depends on depends-on-id (type: blocks)
nitro agent tasks dep add <id> <depends-on-id> --type waits-for    # Explicit dependency type
nitro agent tasks dep remove <id> <depends-on-id>                  # Remove dependency
nitro agent tasks dep list <id> --output json                       # List dependencies/dependents for a task
nitro agent tasks dep tree <id> --output json                       # Show outgoing dependency tree
nitro agent tasks dep cycles --output json                          # Find circular deps (MUST be empty!)
```

**Critical:** `nitro agent tasks dep cycles` must return empty. Circular dependencies break the dependency graph and make `nitro agent tasks ready` unreliable. In text mode (no `--output json`), `dep add` prints a `Warning: dependency cycle: ...` line inline if the new edge introduces one.

### Labels

```bash
nitro agent tasks label add <id> backend auth       # Add multiple labels
nitro agent tasks label remove <id> urgent          # Remove label
nitro agent tasks label list <id>                   # List one task's labels (array of strings)
nitro agent tasks label list                        # All labels in use, with counts (array of {label, count})
```

### Comments

```bash
ACTOR="${NITRO_TASK_ACTOR:-$(whoami)}"
nitro agent tasks comment add --actor "$ACTOR" <id> "Triage note" --output json
nitro agent tasks comment list <id> --output json
```

### Sync (EXPLICIT -- never automatic)

```bash
nitro agent tasks sync --flush-only                 # Export DB to tasks.jsonl (before git commit)
nitro agent tasks sync --import-only                # Import tasks.jsonl to DB (after git pull)
nitro agent tasks sync --status                     # Check sync status
```

Note: `sync --status` exits 1 (not 0) if `tasks.jsonl` or the task database is missing, and also exits 1 when the two have diverged. Guard calls to it in `set -e` scripts, since the non-zero exit is expected in those cases, not a script failure.

Workflow after making changes:

```bash
nitro agent tasks sync --flush-only
git add .nitro/agents/tasks.jsonl && git commit -m "Update tasks"
```

Workflow after pulling:

```bash
git pull
nitro agent tasks sync --import-only
```

### System and Diagnostics

```bash
nitro agent tasks doctor --output json              # Full diagnostics (cycles, orphans, tombstoned edges)
nitro agent tasks stats --output json               # Project statistics
nitro agent tasks config list                       # Show all configuration
nitro agent tasks config get prefix                 # Get specific value
nitro agent tasks config set prefix "app"           # Set value
nitro agent tasks where                             # Show workspace location
nitro --version                              # Show version
nitro agent tasks lint --output json                # Lint tasks for quality problems (e.g. empty description)
```

## Priority Scale

| Priority | Meaning          | Use numbers or p-prefixed, not words |
| -------- | ---------------- | ------------------------------------ |
| 0 / p0   | Critical         | Immediate action required            |
| 1 / p1   | High             | Important, do soon                   |
| 2 / p2   | Medium (default) | Normal priority                      |
| 3 / p3   | Low              | When time permits                    |
| 4 / p4   | Backlog          | Future consideration                 |

`list` and `ready` also accept a priority range, e.g. `--priority p0-p1` or `--priority 0-1`.

## Issue Types

`task`, `bug`, `feature`, `epic`, `chore`, `docs`, `question`, or any custom string.

## Output Formats

| Flag            | Use case                                                                                                |
| --------------- | ------------------------------------------------------------------------------------------------------- |
| `--output json` | Default for agents -- full structured data. Array-returning commands wrap results as `{"items": [...]}` |
| (no flag)       | Human-readable terminal output with colors                                                              |

Note: `list`/`ready`/`search`/`blocked`/`stale` return the compact `TaskSnapshotResult` shape (no `labels`, `parent`, or `dependencies`); use `nitro agent tasks show <id> --output json` when you need full task state.

## TUI Warning

**CRITICAL:** Never run bare `nitro agent tasks board` in an agent session -- it launches an interactive TUI and blocks. It does error out cleanly on a non-TTY console (`agent tasks board requires an interactive terminal.`, exit 1), but treat it as forbidden rather than relying on that guard.

**CRITICAL:** Also never run bare `nitro agent` in an agent session. When the terminal is interactive and a workspace is found, it opens the same tabbed TUI (Tasks and Mail tabs, `[`/`]` to switch, the Mail tab title shows an unread count badge) starting on the Tasks tab. On a non-TTY console it does not launch anything -- it prints the usual "Required command was not provided." group guidance instead -- but treat it as forbidden rather than relying on that guard. `nitro agent tasks board` and `nitro agent mail board` remain the focused, single-tab entries into the same TUI.

Use the non-interactive commands instead:

```bash
nitro agent tasks ready --output json     # What to work on next
nitro agent tasks blocked --output json   # What is stuck and why
nitro agent tasks stats --output json     # Project health snapshot
nitro agent tasks dep cycles --output json # Graph health (must be empty)
```

The board's gestures (`/` to filter, `t` to change type, `e` to edit, `x`/`X` to close) have no non-interactive equivalent flags -- use `update`/`close`/`list --status` directly.

## Agent Mail Coordination

Multi-agent coordination uses `nitro agent mail` (see the sibling `nitro-mail` skill for the full command surface). Mail threads are keyed by generated `m-*` message IDs, not task IDs, so correlate a thread to a task through the subject line instead:

| Concept         | Value                                         |
| --------------- | --------------------------------------------- |
| Mail subject    | `[app-1a2] ...` (task ID as a subject prefix) |
| Commit messages | Include the task ID for traceability          |

```bash
ACTOR="${NITRO_TASK_ACTOR:-$(whoami)}"

# 1. Announce work
nitro agent mail send "other-agent" --actor "$ACTOR" \
  --subject "[app-1a2] Starting" --body "Claiming this now."

# 2. Do work...

# 3. Close the task
nitro agent tasks close --actor "$ACTOR" app-1a2 --reason "Completed"
```

## Session Ending Pattern

Before ending any work session:

```bash
git pull --rebase
nitro agent tasks sync --flush-only
git add .nitro/agents/tasks.jsonl && git commit -m "Update tasks"
git push
git status  # MUST show "up to date with origin"
```

## Standard Agent Workflow (Full)

```bash
ACTOR="${NITRO_TASK_ACTOR:-$(whoami)}"

# 1. Verify workspace
nitro agent tasks where
nitro agent tasks ready --output json
nitro agent tasks blocked --output json
nitro agent tasks list --status open --output json

# 2. Pick highest-priority ready work
nitro agent tasks show <id> --output json

# 3. Claim it
nitro agent tasks update --actor "$ACTOR" <id> --status in_progress --claim

# 4. Do work...

# 5. Close with evidence
nitro agent tasks close --actor "$ACTOR" <id> --reason "Implemented X in commit abc123"

# 6. Check queue impact
nitro agent tasks ready --output json
nitro agent tasks blocked --output json

# 7. Sync to git
nitro agent tasks sync --flush-only
git add .nitro/agents/tasks.jsonl && git commit -m "feat: X (<id>)"
git push
```

## Triage Decision Matrix

Classify each task into exactly one category:

| Classification        | Action                                         |
| --------------------- | ---------------------------------------------- |
| `implemented`         | Close with evidence (commit/PR/file/behavior)  |
| `out-of-scope`        | Close with explicit boundary reason            |
| `needs-clarification` | Comment with specific unanswered questions     |
| `actionable`          | Keep open, correct status/priority/labels/deps |

During large triage efforts, checkpoint every few updates:

```bash
nitro agent tasks ready --output json
nitro agent tasks blocked --output json
```

## Anti-Patterns

- Running `nitro agent tasks sync` without `--flush-only` or `--import-only`
- Forgetting sync before git commit
- Creating circular dependencies
- Running bare `nitro agent tasks board` (blocks session)
- Assuming auto-commit behavior (`nitro agent tasks` NEVER auto-commits)
- Inventing evidence for closure -- if unsure, comment instead
- Modifying unrelated tasks during triage
- Adding speculative dependencies

## Storage Layout

`nitro agent tasks` and `nitro agent mail` (see the sibling `nitro-mail` skill) share one workspace and one database:

```
.nitro/agents/
  agents.db       # SQLite database (primary storage, tasks AND mail, local-only, gitignored)
  agents.db-shm   # SQLite shared memory (WAL mode, gitignored)
  agents.db-wal   # SQLite write-ahead log (gitignored)
  tasks.jsonl     # JSONL export -- the source of truth in git (mail has no export; see nitro-mail)
  .gitignore      # Excludes agents.db, agents.db-wal, agents.db-shm
```

## Troubleshooting

```bash
nitro agent tasks doctor            # Full diagnostics
nitro agent tasks dep cycles        # Must be empty
nitro agent tasks config list       # Check settings
which nitro                  # Verify nitro is installed
```

**Database locked**: Check for other `nitro agent tasks` processes with `pgrep -f "nitro"`.

**Verbose debugging:**

```bash
NITRO_OUTPUT_FORMAT=json nitro agent tasks list   # Force JSON via env var instead of --output
```
