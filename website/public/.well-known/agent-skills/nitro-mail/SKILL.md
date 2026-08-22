---
name: nitro-mail
description: >-
  Official skill for `nitro agent mail`, the local-first, SQLite-backed
  mailbox built into the Nitro CLI for agent-to-agent coordination. Use when
  registering a mail identity, sending or replying to messages, checking an
  inbox, reading/acking/archiving mail, searching or browsing threads, or
  waiting for new mail with `watch`.
license: MIT
domain: project-management
role: specialist
scope: operations
output-format: commands
triggers:
  - nitro agent mail
  - agent mailbox
  - agent coordination mail
  - mail thread
  - mail watch
metadata:
  version: 1.0.0
---

<!-- Sibling skill to nitro-task/SKILL.md; same structure and tone. -->
<!-- TOC: Critical Rules | Quick Workflow | Identity | Send/Reply/Broadcast | Inbox/Read/Ack/Archive | Threads/Search | Watch | TUI Warning | Etiquette | Storage | Troubleshooting -->

# nitro agent mail -- Nitro Agent Mailbox (Official Skill)

> **Non-invasive:** `nitro agent mail` never runs git commands. It shares the unified `.nitro/agents/` workspace with `nitro agent tasks` (see the sibling `nitro-task` skill) -- one database, `agents.db`, holding both tasks and mail.

## Critical Rules for Agents

| Rule                                              | Why                                                                                                                                                                                                          |
| ------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **Use `--output json`**                           | Structured output for parsing; array-returning commands wrap results as `{"items": [...]}`                                                                                                                   |
| **NEVER run bare `nitro agent mail board`**       | Blocks the session in an interactive TUI; it errors on a non-TTY console (`agent mail board requires an interactive terminal.`, exit 1) but must not be attempted                                            |
| **NEVER run bare `nitro agent`**                  | In an interactive terminal with a workspace found, it opens the same tabbed TUI (Tasks + Mail); it is safe on a non-TTY console (prints the group's usage guidance instead) but must not be attempted        |
| **Register yourself at session start**            | Registration lives at the `nitro agent` root now (`nitro agent register`, not under `mail`); it sets an optional `--role` and is what makes you show up as a real (non-implicit) agent in `nitro agent list` |
| **An unknown recipient no longer fails the send** | `send` to a name that was never registered succeeds, creates an implicit mailbox for it, and prints `note: '<name>' has never registered.`; only an INVALID name (see below) is a hard failure               |
| **Resolve actor at runtime**                      | `--actor` is per invocation; set `NITRO_MAIL_ACTOR` to persist an identity for the session (falls back to `NITRO_TASK_ACTOR`, then the OS user name)                                                         |
| **Agent names are strict**                        | Only lowercase letters, digits, `-`, and `_`; anything else is rejected outright (never silently stripped)                                                                                                   |
| **`ack`/`archive` batches are all-or-nothing**    | One unknown or foreign message ID fails the whole batch, nothing partially applied                                                                                                                           |

## Quick Workflow

```bash
export NITRO_MAIL_ACTOR="${NITRO_TASK_ACTOR:-$(whoami)}"

# 1. Make sure the workspace exists and you're registered (init is shared with nitro agent tasks)
nitro agent init                            # no-op error if already initialized; add --force to reinit
nitro agent register                        # register at the root, not under mail; add --role "backend" to set one

# 2. Check what's new
nitro agent mail inbox --unread --output json

# 3. Read and act
nitro agent mail read "m-abc123"
nitro agent mail ack "m-abc123"             # mark read without printing
nitro agent mail archive "m-abc123"         # done with it

# 4. Reply or start a thread
nitro agent mail reply "m-abc123" --body "On it."
nitro agent mail send "other-agent" --subject "[task-id] Starting" --body "..."
```

## Identity: init, register, whoami, list

There is no `nitro agent mail init` -- it was retired along with `nitro agent tasks init`, with no alias for the old spelling; running it fails with "Unrecognized command or argument 'init'." Initialize the shared workspace with `nitro agent init` instead (see the `nitro-task` skill's Init and Workspace section for the full migration behavior, including how a legacy `.nitro/mail/mail.db` is reported but not migrated).

Agent identity commands (`register`/`whoami`/`list`) live at the `nitro agent` root, not under `mail` -- `mail` no longer has `register`, `whoami`, or `agents` subcommands; typing any of them under `mail` fails with "Unrecognized command or argument":

```bash
nitro agent init                             # Create .nitro/agents/ (agents.db + tasks.jsonl) in the current directory
nitro agent init --force                     # Reinitialize an existing workspace
nitro agent register                         # Register the resolved actor with no role (idempotent upsert)
nitro agent register --role "backend"        # Register (or re-register) with a role, normalized lowercase like agent names
nitro agent register --actor "claude-1"      # Register a specific name for this call only
nitro agent whoami                           # Print the resolved actor and whether it is registered
nitro agent list --output json               # List agents: {name, role, implicit, registeredAt, lastSeenAt}
nitro agent list --role "backend"            # Filter to agents with that role
nitro agent list --stale                     # Only agents not seen in the last 30 days
```

- Re-registering **always** sets the role to whatever `--role` resolves to for that call, including clearing it back to empty if you omit `--role` on a re-registration that previously had one -- it is not a merge/patch.
- **Implicit agents**: sending mail (or being an unregistered recipient of a send/broadcast, see below) creates an `implicit: true` row for that name so mail can reference it, but it is not "registered" -- `whoami`'s `registered` field and `list`'s human-readable output both distinguish implicit rows from a real `nitro agent register`. An implicit agent has no role; the first real `register` call for that name clears the implicit flag.
- Sending mail bumps your own `lastSeenAt` (`agent register`'s underlying `TouchAsync`) but never touches your role -- only an explicit `nitro agent register --role ...` call changes it.

**Actor resolution order**: `--actor` > `NITRO_MAIL_ACTOR` > `NITRO_TASK_ACTOR` > OS user name, then normalized (lowercased; rejected outright if empty or containing anything outside `[a-z0-9-_]`). The normalized result IS the agent's mail address -- there is no separate display name.

**Workspace discovery** walks up from the current directory looking for `.nitro/agents/`, the same as `nitro agent tasks`. **Worktree limitation**: each git worktree resolves its own directory tree, so sibling worktrees do NOT share a workspace by default. Workaround: run `nitro agent init` in a deliberate common ancestor directory that contains only the cooperating worktrees (not the whole filesystem), so the walk-up from any worktree finds that shared `.nitro/agents/`.

## Send, Reply, Broadcast

```bash
nitro agent mail send "agent-a" --subject "Status" --body "All good."
nitro agent mail send "agent-a" "agent-b" --cc "agent-c" --subject "Status" --body-file notes.txt
nitro agent mail reply "m-abc123" --body "On it."
nitro agent mail broadcast --subject "Heads up" --body "Deploying at 5pm."
nitro agent mail broadcast --role "backend" --subject "Heads up" --body "Deploying at 5pm."
```

- `send <recipients>...`: one or more recipient names, repeatable `--cc`. Exactly one of `--body` or `--body-file` is required (enforced at parse time). Sending to yourself is allowed.
- **A recipient that was never registered no longer fails the send.** It gets an implicit mailbox and the send still succeeds; human-readable output prints one `note: '<name>' has never registered.` line per such name below the `Sent '...'` confirmation, and the JSON DTO's `unregistered` array lists them. The only recipient-related hard failure now is an **invalid** name: `MailAgentName.Normalize` rejects anything outside lowercase letters, digits, `-`, and `_`, exit code 1, before the send touches anything.
- `reply <message-id>`: recipients are computed automatically -- the original sender plus its `to`/`cc`, minus you, **all flattened into `to`** (a reply never has `cc`). Subject is inherited from the thread. Replying to a message where you are the only possible recipient (e.g. your own solo message) fails with "would leave no recipients." Replying to a message you neither sent nor received fails.
- `broadcast`: sends to every **registered** agent except yourself (implicit agents are not included); fails with "No other registered agent to broadcast to." if there is none. `--role <role>` narrows that to agents whose role matches (normalized case-insensitively for the comparison, though the CLI's own "no match" error message echoes back whatever you typed, unnormalized -- a known cosmetic quirk); fails with "No registered agent with role '<role>' to broadcast to." if none match. `--role` here filters recipients by their role, not your own.
- All three accept `--actor` and `--output json`. `send`/`broadcast` JSON DTO: `{id, threadId, inReplyTo, from, to[], cc[], subject, createdAt, unregistered[]}`; `reply`'s omits `unregistered` (a reply's recipients are always drawn from an existing thread, so they already exist). Recipients are in first-occurrence order after dedupe.
- `--body-file` reads the file verbatim (no trimming); an empty body is a user error.
- **Known minor quirk**: if a send fails partway through (a rare failure inside the message-write step itself, not the common validation failures above), the sender may still end up registered/touched even though no message was written -- tracked as a minor, not something to route around.

## Inbox, Read, Ack, Archive

```bash
nitro agent mail inbox --output json                          # Newest first, archived excluded by default
nitro agent mail inbox --unread --output json
nitro agent mail inbox --from "agent-a" --since "2026-01-01T00:00:00Z" --output json
nitro agent mail inbox --all --output json                    # Include archived
nitro agent mail read "m-abc123"                               # Prints headers + body, marks read for you
nitro agent mail read "m-abc123" --thread                      # Whole thread, oldest first, marks all read
nitro agent mail ack "m-abc123" "m-def456"                     # Mark read without printing (batch, all-or-nothing)
nitro agent mail archive "m-abc123" "m-def456"                 # Archive (batch, all-or-nothing)
```

- Read state and archive state are **per-recipient**: only your own copy of a message is affected. Reading a message you only sent (never received) is allowed but does not mark anything read, since you have no recipient copy. `archive`/`ack` require you to actually be a recipient -- attempting to archive a message where you are only the sender fails with `'<actor>' is not a recipient of: <id>.`
- `read`'s human-readable `From:` header shows the sender's role in parentheses when they have one, e.g. `From: agent-a (backend)`; senders with no role print just the name. This is display-only -- the JSON DTO (see below) does not carry the role.
- Reading or acking an already-read message still succeeds and bumps its read timestamp (idempotent, not a no-op).
- `inbox` JSON DTO per row: `{id, threadId, from, subject, createdAt, read, archived}`. `read` JSON DTO: `{id, threadId, inReplyTo, from, to[], cc[], subject, body, createdAt, read, archived}` (an array of those under `--thread`).
- `--limit` on any command that has it must be a positive integer (`--limit 0` is rejected at parse time); omit it for unlimited.

## Threads and Search

```bash
nitro agent mail threads --output json                    # Threads you sent or received in, last activity first
nitro agent mail threads --limit 10
nitro agent mail search "deploy" --output json            # Matches subject, body, AND sender, case-insensitively
nitro agent mail search "agent-a" --output json           # Sender-name matches work too
```

- Both include archived messages -- archiving is an inbox display state, not deletion.
- `search` is scoped to messages you sent or received; it never surfaces another agent's private mail.
- `threads` JSON DTO per row: `{threadId, subject, participants[], messageCount, unreadCount, lastActivityAt}`.
- `search` JSON DTO per row: same shape as an `inbox` row: `{id, threadId, from, subject, createdAt, read, archived}`.

## Watch

```bash
nitro agent mail watch                       # Blocks until new mail arrives, then prints it and exits 0
nitro agent mail watch --timeout 30          # Exit 1 with empty stdout and a stderr line if nothing arrives in time
nitro agent mail watch --output json         # Prints a {"items": [...]} array even for a single message
```

- Polls roughly once per second. The baseline is your inbox at the moment `watch` starts: messages already unread when it started do NOT trigger (check those with `inbox --unread` first). When one or more new messages arrive, all of them print oldest first, then it exits 0.
- **`watch` never marks anything read.** Follow up with `read`/`ack` explicitly.
- On timeout: exit code 1, stdout stays empty in both output modes, stderr gets one line (`Timed out waiting for new mail.`). Without `--timeout` it waits until cancelled (Ctrl+C).

## TUI Warning

**CRITICAL:** Never run bare `nitro agent mail board` in an agent session -- it launches an interactive TUI and blocks. It does error out cleanly on a non-TTY console (`agent mail board requires an interactive terminal.`, exit 1), but treat it as forbidden rather than relying on that guard.

**CRITICAL:** Also never run bare `nitro agent` in an agent session. When the terminal is interactive and a workspace is found, it opens the same tabbed TUI (Tasks and Mail tabs, `[`/`]` to switch, the Mail tab title shows an unread count badge) starting on the Tasks tab. On a non-TTY console it does not launch anything -- it prints the usual "Required command was not provided." group guidance instead -- but treat it as forbidden rather than relying on that guard. `nitro agent mail board` and `nitro agent tasks board` remain the focused, single-tab entries into the same TUI.

Use the non-interactive commands instead (`inbox`, `threads`, `search`, `watch`). If a human is at the board, the key bindings are: `u` toggle read/unread, `a` archive, `r` reply, `c` compose, `Shift+R` refresh, `t` toggle thread view, `f`/`F` cycle the inbox/unread/archived filter.

## Coordination Etiquette

- **Ack what you act on.** If you read a message and are going to act on it, `ack` or `read` it so other agents watching the thread see it was seen.
- **Archive what is done.** Once a thread's ask is resolved, `archive` your copy -- it stays searchable and in `threads` (which always includes archived) and `inbox --all`, just out of the default inbox view.
- **Broadcast sparingly.** It goes to every registered agent except you; prefer `send`/`reply` to the specific agents who need it.
- **Use the `[task-id] ...` subject convention** when a message correlates to a tracked task (see `nitro-task`'s Agent Mail Coordination section) -- there is no separate thread-to-task linkage field in v1.

## Storage Layout

`nitro agent mail` shares the unified workspace with `nitro agent tasks` (see the sibling `nitro-task` skill) -- one database, `agents.db`, holding both tasks and mail:

```
.nitro/agents/
  agents.db       # SQLite database (primary storage, tasks AND mail, local-only, gitignored)
  agents.db-shm   # SQLite shared memory (WAL mode, gitignored)
  agents.db-wal   # SQLite write-ahead log (gitignored)
  tasks.jsonl     # JSONL export of tasks -- committed to git (see nitro-task)
  .gitignore      # Excludes agents.db, agents.db-wal, agents.db-shm
```

There is no JSONL export and no `sync` command for mail -- mail is not synced through git, by design (see the epic's non-goals). Only `tasks.jsonl` is committed.

## Troubleshooting

```bash
nitro agent whoami                           # Confirm resolved actor and registration
nitro agent list --output json               # Confirm the recipient you're sending to is actually registered (not just implicit)
which nitro                                  # Verify nitro is installed
```

**"note: '<name>' has never registered."**: not an error -- the send succeeded and created an implicit mailbox for that name. If that is unexpected, double check the recipient's spelling; if it is expected (a new agent's first message hasn't landed yet), no action needed.

**"Invalid agent name '...'"**: the name (yours or a recipient's) contains something other than lowercase letters, digits, `-`, or `_`. Fix the spelling; nothing is auto-corrected.

**Message not readable/archivable**: you are neither the sender nor a recipient of that message ID, or (for `archive`) you sent it but never received it.

**Verbose debugging:**

```bash
NITRO_OUTPUT_FORMAT=json nitro agent mail inbox   # Force JSON via env var instead of --output
```
