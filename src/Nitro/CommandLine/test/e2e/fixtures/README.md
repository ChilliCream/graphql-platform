# Fixture task workspace

`seed.sql` is a deterministic dataset for the `nitro task` e2e tapes. IDs,
timestamps, and actors are all hardcoded so a recording that reads this data
is byte-stable across runs, wall-clock time, and machines.

## How run.sh uses it

Before recording any flow, `run.sh` prepares `out/fixture/acme/` on the host
(not inside the VHS container):

1. `rm -rf` + `mkdir -p out/fixture/acme` for a clean slate.
2. Run the freshly published `bin/nitro task init` inside it, with
   `NITRO_TASK_ACTOR=e2e-agent` set. This creates the real `.nitro/tasks/`
   schema (via `TaskStore.InitializeAsync`/`TaskStoreSchema`) and sets the
   `acme` task ID prefix (via `TaskWorkspace.NormalizePrefix`, derived from the
   `acme` directory name, matching `TasksCommandTestBase`).
3. Apply `seed.sql` with the `sqlite3` CLI against
   `out/fixture/acme/.nitro/tasks/tasks.db`.
4. Guard: run `bin/nitro task list` inside `out/fixture/acme` and grep for
   `acme-epic1`. If the marker is missing, schema drift is failing fast here,
   with a pointer back to this file, instead of surfacing later as a
   confusing golden diff inside a tape's `Hide` block.

A tape only ever `cp -r`s the prepared `out/fixture/acme` directory into its
own throwaway `/tmp/work`; no task-mutating command inside a tape's `Hide`
block is relied on to produce IDs, so recordings stay independent of the
wall-clock-seeded ID scheme in `CreateTaskCommand`/`TaskStore.CreateIdSuffix`.

## The dataset

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

## Regenerating after a schema change

`seed.sql` is a plain list of `INSERT` statements against
`TaskStoreSchema.Create`; there is no code generator. After changing the
schema (a new column, a new `NOT NULL` constraint, a renamed table):

1. Bump `TaskStoreSchema.CurrentVersion` as usual for the production change.
2. Update every `INSERT` in `seed.sql` to match the new column list. For a new
   `NOT NULL` column, add a value to every affected `INSERT` (an empty
   string/`NULL` per the column's own default, unless the fixture should
   exercise the new column specifically).
3. Re-run `./run.sh help` (or any flow). The prepare-fixture step reapplies
   `seed.sql` from scratch every run, so a missed column surfaces immediately
   as a `sqlite3` constraint error, and the `task list` guard catches a
   renamed table or column before any tape records against stale data.
4. To inspect the seeded data directly:
   `sqlite3 out/fixture/acme/.nitro/tasks/tasks.db ".dump"`.
