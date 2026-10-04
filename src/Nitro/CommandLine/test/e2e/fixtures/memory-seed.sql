-- Deterministic memory fixture for the memory-board-flow e2e tape (the
-- Memory tab's table, its kind filter, and the Enter entry popover).
--
-- Applied by run.sh after mail-seed.sql and agents-seed.sql, against the
-- same unified agent workspace database seed.sql already seeds (schema and
-- PRAGMA user_version come from the real binary, not from this file).
-- Every id, timestamp, and actor here is hardcoded so a recording that
-- reads this data is byte-stable across runs.
--
-- Every id is a syntactically valid MemoryId (exactly 26 lowercase
-- Crockford base32 characters: 0-9 and a-z minus i, l, o, u), the same
-- check MemoryStore runs via MemoryId.Require before a popover load can
-- render a row; a human-readable id like 'mem-curated-1' fails that check
-- and surfaces as an "Invalid memory id" ExitException.
--
-- All timestamps use the exact text format MemoryStore/Microsoft.Data.
-- Sqlite writes for a DateTimeOffset ("yyyy-MM-dd HH:mm:ss.fffffff+00:00"),
-- UTC, on the same fixed date (2026-01-01) mail-seed.sql uses, far enough
-- in the past that AgentAges.Format/MailAges.Format always land in their
-- "week or older" branch, independent of the wall-clock date a recording
-- actually runs on, so this flow needs no SCRUBS entry either.
--
-- Two curated memories (e2emem...0001, a fact; e2emem...0002, a decision)
-- and one journal entry (e2emem...0003). The Memory tab shows every entry
-- in the workspace, not just this file's own: agents-seed.sql (applied
-- just before this file, to the same shared fixture) already seeds three
-- journal entries for `nora`, so the table's default "All" view totals six
-- rows ("Memory (6)"), and the Journal filter totals four ("Journal (4)"),
-- with this file's own entry sorting third among them (its created_at ties
-- one of nora's, broken by id: "e2emem..." sorts before "e2enramemj...").
-- See memory-board-flow.tape's own header for the full row-order reasoning.
-- Neither curated row's updated_at differs from its created_at, so the
-- popover's optional "Updated" field never renders for either.

BEGIN TRANSACTION;

INSERT INTO memory_curated (id, type, body, created_at, updated_at, created_by) VALUES
    ('e2emem00000000000000000001', 'fact', 'Ops runbook lives at docs/ops.md.', '2026-01-01 09:00:00.0000000+00:00', '2026-01-01 09:00:00.0000000+00:00', 'alice'),
    ('e2emem00000000000000000002', 'decision', 'Ship database migrations before app code.', '2026-01-01 10:00:00.0000000+00:00', '2026-01-01 10:00:00.0000000+00:00', 'bob');

INSERT INTO memory_journal (id, body, created_at, created_by) VALUES
    ('e2emem00000000000000000003', 'Follow up on the flaky checkout test.', '2026-01-01 08:00:00.0000000+00:00', 'e2e-agent');

COMMIT;
