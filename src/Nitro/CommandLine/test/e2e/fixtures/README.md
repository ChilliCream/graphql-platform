# Fixture agent workspace

`seed.sql`, `mail-seed.sql`, and `agents-seed.sql` are a deterministic
dataset for the `nitro agent` e2e tapes: task data, mail data, and agent
presence data, applied to the same unified workspace database. IDs,
timestamps, and actors are all hardcoded so a recording that reads this data
is byte-stable across runs, wall-clock time, and machines.

## How run.sh uses it

Before recording any flow, `run.sh` prepares `out/fixture/acme/` on the host
(not inside the VHS container):

1. `rm -rf` + `mkdir -p out/fixture/acme` for a clean slate.
2. Run the freshly published `bin/nitro agent init` inside it. This creates
   the real `.nitro/agents/agents.db` unified schema (via `AgentDatabase.InitializeAsync`,
   composing `TaskStoreSchema` and `MailStoreSchema`) and sets the `acme`
   task ID prefix (via `AgentWorkspace.NormalizePrefix`, derived from the
   `acme` directory name, matching `TasksCommandTestBase`).
3. Assert `PRAGMA user_version` on the fresh database equals
   `AgentDatabase.CurrentVersion`, read straight out of `AgentDatabase.cs` so
   the guard cannot drift out of sync with a hardcoded number. Since the
   expected value comes from the same source tree the fixture binary was
   just published from, a mismatch here means the published `bin/nitro` is
   stale relative to that tree (rebuild with `REBUILD=1` or `--update`), not
   that the seed files are out of date. Seed drift against a real schema
   change is instead caught by the `sqlite3` apply step failing on a missing
   column and by the `FIXTURE_*_MARKER` guard queries below.
4. Apply `seed.sql`, then `mail-seed.sql`, then `agents-seed.sql`, with the
   `sqlite3` CLI against `out/fixture/acme/.nitro/agents/agents.db`.
   `seed.sql` (`tasks`/`dependencies`/`labels`/`comments`/`events`/
   `child_counters`) and `mail-seed.sql` (`agents`/`messages`/
   `message_recipients`) insert into disjoint tables, so their own order
   does not matter; `agents-seed.sql` (`agent_sessions`) must run after
   `mail-seed.sql` since its one row's `agent_name` references a name
   `mail-seed.sql` inserts.
5. Guard: run `bin/nitro agent tasks list` inside `out/fixture/acme` and grep
   for `acme-epic1`, then run `bin/nitro agent mail inbox` (as `e2e-agent`)
   and grep for `Retro notes`, then run `bin/nitro agent list` and grep for
   `bob  remote`. If any marker is missing, schema drift is failing fast
   here, with a pointer back to this file, instead of surfacing later as a
   confusing golden diff inside a tape's `Hide` block.

A tape only ever `cp -r`s the prepared `out/fixture/acme` directory into its
own throwaway `/tmp/work`; no task- or mail-mutating command inside a tape's
`Hide` block is relied on to produce IDs, so recordings stay independent of
the wall-clock-seeded ID scheme in `CreateTaskCommand`/`TaskStore.CreateIdSuffix`
and `MailStore.CreateMessageIdAsync`.

## The task dataset

One epic with two children, a few standalone tasks spanning statuses,
priorities, and types, a blocking dependency, a label, and a comment:

| ID | Type | Status | Priority | Notes |
| --- | --- | --- | --- | --- |
| `acme-epic1` | epic | open | P1 | parent of the two tasks below |
| `acme-epic1.1` | task | in_progress | P2 | assignee `alice`, label `content` |
| `acme-epic1.2` | task | open | P2 | blocked on `acme-epic1.1`, has a comment from `bob` |
| `acme-a1b` | bug | open | P0 | blocked on `acme-epic1.2` |
| `acme-c3d` | feature | closed | P3 | closed with a reason |
| `acme-e5f` | chore | deferred | P4 | deferred to `2026-02-01` |

Every `NOT NULL` column in `TaskStoreSchema.Create` is populated on every
inserted row (empty string / matching default where a command would leave it
unset). `events` rows mirror what each corresponding command would have
written, even though no task command besides `task stats` (`COUNT(*)`) reads
that table today.

All timestamps are UTC, fixed on `2026-01-01` (or shortly after, to give
`updated_at`/dependency ordering a realistic spread), written in the exact
text format `Microsoft.Data.Sqlite` persists for a `DateTimeOffset`:
`yyyy-MM-dd HH:mm:ss.fffffff+00:00`. That format keeps `idx_tasks_updated_at`
lexicographic ordering correct, the same reason `TaskDates.Parse` requires an
explicit offset.

`child_counters` seeds `('acme-epic1', 2)` so a tape that creates a further
child of `acme-epic1` mints `acme-epic1.3`, continuing on from the two
children already in the fixture instead of colliding with them.

## The mail dataset

Three agents (`e2e-agent`, `alice`, `bob`) and four messages. `e2e-agent` is
the actor the mail tapes run as: it is a `to` recipient of `m-fix001`,
`m-fix002`, and `m-fix003`, so its inbox shows exactly those three
(`m-fix004` is `e2e-agent`'s own reply on the `m-fix003` thread and only
surfaces via the thread toggle). `m-fix002` is already read; `m-fix001` and
`m-fix003` are unread. Every `created_at` is far enough in the past that the
mail board's age column always renders a fixed `yyyy-MM-dd` string,
independent of the wall-clock date a recording actually runs on.

## The agents dataset

One `agent_sessions` row: `bob` bound to a `claude-code` session whose `host`
is the fixed string `e2e-remote-host-fixture`, deliberately foreign to
whatever the recording machine's own instance id resolves to
(`NitroInstanceIdProvider`). `AgentSessionRegistry.ListAsync` renders any
session row on a foreign host as `Remote` unconditionally, with no PID
liveness check at all, unlike `Online`/`Unreachable`, both of which require a
real, live process on the CURRENT host and so cannot be pinned from a static
SQL fixture. `alice` and `e2e-agent` have no session row and so render
`Offline`, the default for zero live sessions. See `agents-seed.sql`'s own
header for the full reasoning.

## Regenerating after a schema change

`seed.sql`/`mail-seed.sql`/`agents-seed.sql` are plain lists of `INSERT`
statements against `TaskStoreSchema.Create`/`MailStoreSchema.Create`/
`AgentSessionSchema.Create`; there is no code generator. After changing any
of the three schemas (a new column, a new `NOT NULL` constraint, a renamed
table):

1. Bump `AgentDatabase.CurrentVersion` as usual for the production change.
2. Update every affected `INSERT` to match the new column list. For a new
   `NOT NULL` column, add a value to every affected `INSERT` (an empty
   string/`NULL` per the column's own default, unless the fixture should
   exercise the new column specifically).
3. Re-run `./run.sh help` (or any flow). The prepare-fixture step reapplies
   both seed files from scratch every run, so a missed column surfaces
   immediately as a `sqlite3` constraint error, and the two guard queries
   catch a renamed table or column before any tape records against stale
   data.
4. To inspect the seeded data directly:
   `sqlite3 out/fixture/acme/.nitro/agents/agents.db ".dump"`.
