# Fixture agent workspace

`seed.sql`, `mail-seed.sql`, `agents-seed.sql`, and `memory-seed.sql` are a
deterministic dataset for the `nitro agent` e2e tapes: task data, mail data,
agent presence data, and memory data, applied to the same unified workspace
database. IDs, timestamps, and actors are all hardcoded so a recording that
reads this data is byte-stable across runs, wall-clock time, and machines.

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
4. Apply `seed.sql`, then `mail-seed.sql`, then `agents-seed.sql`, then
   `memory-seed.sql`, with the `sqlite3` CLI against
   `out/fixture/acme/.nitro/agents/agents.db`. `seed.sql`
   (`tasks`/`dependencies`/`labels`/`comments`/`events`/`child_counters`),
   `mail-seed.sql` (`agents`/`messages`/`message_recipients`), and
   `memory-seed.sql` (`memory_curated`/`memory_journal`) insert into disjoint
   tables, so their own order does not matter relative to each other;
   `agents-seed.sql` must run after `mail-seed.sql` since it updates two of
   the `agents` rows `mail-seed.sql` inserts (`alice`, `e2e-agent`) rather
   than inserting them itself, alongside two further agents of its own
   (`nora`, `wren`).
5. Guard: run `bin/nitro agent tasks list` inside `out/fixture/acme` and grep
   for `acme-epic1`, then run `bin/nitro agent mail inbox` (as `e2e-agent`)
   and grep for `Retro notes`, then run `bin/nitro agent list` and grep for
   `planner`, then run `bin/nitro agent memory recent --collection all` and
   grep for the id `e2emem00000000000000000001`. If any marker is missing,
   schema drift is failing fast here, with a pointer back to this file,
   instead of surfacing later as a confusing golden diff inside a tape's
   `Hide` block.

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

`agents-seed.sql` refreshes two of the three agents `mail-seed.sql` already
inserted and adds two of its own, to exercise the three states
`AgentStateResolver` derives from a row plus a fourth, already soft-deleted
agent:

| Name | State | Notes |
| --- | --- | --- |
| `alice` | Unreachable | login-only: no harness, no session, `endpoint_kind` stays `'none'` |
| `bob` | Offline | untouched here; stale `last_seen_at` (mail-seed.sql's own) |
| `e2e-agent` | Offline | `ended_at` set; still usable as the mail/task actor elsewhere in this fixture |
| `nora` | Online | a role, a `claude-code` harness, a live session, a non-`none` endpoint; no mail/task/memory participation |
| `wren` | (never shown) | `deleted_at` set on insert |

Online and Unreachable both require a `last_seen_at` inside the current
30-minute window (`AgentStateResolver.OnlineWindow`), a live, time-windowed
check with no host/PID escape hatch to pin it from a static SQL fixture the
way the old `agent_sessions` model's `Remote` state could; `alice`'s and
`nora`'s `last_seen_at` are set to SQLite's own `datetime('now')` instead of
a fixture-past timestamp, so they always land inside that window however
long after this file runs a tape actually records. The relative age that
produces is normalized by the `agents` (and `mail-send`) SCRUBS entry in
[`run.sh`](../run.sh) before the diff. The Online state specifically goes to
a fresh name, `nora`, rather than to `alice` or `bob`: both are mail-seed.sql
message participants, and `MailDetailView.AttributeHarness` renders either
one's name with a harness attribution wherever mail-board-flow.tape's own
fixed fixture happens to show it, which would change that flow's committed
golden even though this ticket touches only agents-flow and mail-send-flow.
`nora` and `wren` both sort after `e2e-agent` alphabetically, so neither
shifts mail-board-flow.tape's own agent-filter picker navigation either. See
`agents-seed.sql`'s own header for the full reasoning.

## The memory dataset

`memory-seed.sql` inserts two curated memories and one journal entry
directly into `memory_curated`/`memory_journal`, at the same fixed-past
`created_at`/`updated_at` timestamps `mail-seed.sql` uses for its messages,
so the Memory tab's age column is likewise always a fixed `yyyy-MM-dd`
string. Every id is a syntactically valid `MemoryId` (26 lowercase Crockford
base32 characters), the same check a popover load runs before it can render
a row. The Memory tab shows every entry in the workspace, so this file's
three rows sit alongside the three journal entries `agents-seed.sql` already
seeded for `nora`; see `memory-seed.sql`'s own header for the combined
counts memory-board-flow.tape asserts.

## Regenerating after a schema change

`seed.sql`/`mail-seed.sql`/`agents-seed.sql`/`memory-seed.sql` are plain
lists of `INSERT`/`UPDATE` statements against `TaskStoreSchema.Create`/
`MailStoreSchema.Create`/`AgentRegistrySchema.Create`/
`MemoryStoreSchema.Create`; there is no code generator. After changing any
of the four schemas (a new column, a new `NOT NULL` constraint, a renamed
table):

1. Bump `AgentDatabase.CurrentVersion` as usual for the production change.
2. Update every affected `INSERT` to match the new column list. For a new
   `NOT NULL` column, add a value to every affected `INSERT` (an empty
   string/`NULL` per the column's own default, unless the fixture should
   exercise the new column specifically).
3. Re-run `./run.sh help` (or any flow). The prepare-fixture step reapplies
   all four seed files from scratch every run, so a missed column surfaces
   immediately as a `sqlite3` constraint error, and the guard queries catch
   a renamed table or column before any tape records against stale data.
4. To inspect the seeded data directly:
   `sqlite3 out/fixture/acme/.nitro/agents/agents.db ".dump"`.
