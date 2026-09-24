-- Deterministic agent-presence fixture for the agents-flow e2e tape (the
-- Agents tab's table, its Enter popover, and `nitro agent list`'s shape).
--
-- Applied by run.sh after mail-seed.sql, which already inserts alice, bob,
-- and e2e-agent as bare `agents` rows (name/registered_at/started_at/
-- last_seen_at only, every other column left at its schema default: no
-- role, no harness, endpoint_kind 'none'). This file refreshes alice's
-- last_seen_at in place and inserts two further agents, nora and wren.
--
-- alice, bob, and e2e-agent are also mail-seed.sql's message participants,
-- and MailDetailView.AttributeHarness renders any of them with a non-empty
-- `harness` as "name (harness)" wherever mail-board-flow.tape's fixed
-- fixture happens to show that name (bob as m-fix002's sender, alice as
-- its cc, in the flow's own final frame). Giving either of them a harness
-- would therefore change mail-board-flow's committed golden even though
-- this ticket touches only agents-flow and mail-send-flow, so the one
-- Online state here (a harness, a live session, a non-'none' endpoint)
-- goes to nora, a name with no mail or task participation at all, instead
-- of reusing one of the three. nora and wren both sort after "e2e-agent"
-- alphabetically, so neither shifts mail-board-flow.tape's own agent-filter
-- picker navigation, which counts on "bob" staying its third entry
-- (All agents, alice, bob, e2e-agent, ...).
--
-- alice is the one login-only agent: harness/session_id stay NULL and
-- endpoint_kind stays 'none' (mail-seed.sql's row default); only her
-- last_seen_at is refreshed to SQLite's own datetime('now') rather than a
-- hardcoded past timestamp, so she resolves to Unreachable (recently seen,
-- no endpoint), not Offline, however long after this file runs the
-- agents-flow tape actually records (see run.sh's own `agents` SCRUBS
-- entry for the one piece of her rendering that stays genuinely relative:
-- the formatted age itself). mail-send-flow.tape's own
-- `register --actor alice --role reviewer` overwrites her role in its own
-- throwaway copy of this fixture and never touches harness/session, so
-- that flow's `nitro agent list` step still shows her without one.
--
-- bob is left untouched here (mail-send-flow.tape's `register --actor bob`,
-- no --role, only touches last_seen_at itself and reports his role as is,
-- so leaving it empty here keeps that flow's existing "Actor 'bob'."
-- output unchanged); his stale last_seen_at also happens to make him
-- Offline in the Agents tab, alongside e2e-agent, for a different reason
-- than either.
--
-- e2e-agent is the one ended agent: ended_at is set to a fixed past
-- timestamp. AgentStateResolver checks ended_at before last_seen_at at
-- all, so this row renders Offline regardless of how recently it was
-- seen; its last_seen_at is left untouched (mail-seed.sql's fixed
-- 2026-01-01 value) since it plays no further role here. Ending it does
-- not affect its use as the mail/task actor elsewhere in this fixture:
-- mail send only rejects an unknown or deleted recipient, never an ended
-- one, and no task command reads agent state at all.
--
-- nora is the one Online agent, with a role: a harness, a session id, and
-- a non-'none' endpoint_kind (AgentStateResolver.Resolve only returns
-- Online when none of "ended", "deleted", or "endpoint_kind = none" hold,
-- alongside a last_seen_at inside its 30-minute online window). Her
-- last_seen_at is likewise computed with datetime('now'). She has no mail,
-- task, or memory participation at all, so agents-flow.tape's own popover
-- walkthrough opens an empty show-more list ("Nothing here yet.",
-- AgentPopoverListMode.EmptyMessage) rather than a populated one; that is
-- still a real exercise of the show-more mechanism, and it is the only
-- name here that can carry a harness without perturbing mail-board-flow.
--
-- wren is the one deleted agent: deleted_at is set on insert, so every
-- non-deleted read excludes her outright, including the mail Workspace
-- mailbox's own agent-filter picker.

BEGIN TRANSACTION;

UPDATE agents SET
    last_seen_at = strftime('%Y-%m-%d %H:%M:%S', 'now') || '.0000000+00:00'
WHERE name = 'alice';

UPDATE agents SET
    ended_at = '2026-01-02 00:00:00.0000000+00:00'
WHERE name = 'e2e-agent';

INSERT INTO agents (
    name, role, harness, harness_version, session_id, cwd, workspace_path,
    registered_at, started_at, last_seen_at, endpoint_kind, endpoint_addr
) VALUES (
    'nora', 'planner', 'claude-code', '2.4.1', 'e2e-online-session',
    '/tmp/work/acme', '/tmp/work/acme',
    '2026-01-01 07:00:00.0000000+00:00', '2026-01-01 07:00:00.0000000+00:00',
    strftime('%Y-%m-%d %H:%M:%S', 'now') || '.0000000+00:00',
    'claude-peer', 'peer://e2e-online-session'
);

INSERT INTO agents (name, registered_at, started_at, last_seen_at, deleted_at) VALUES (
    'wren',
    '2026-01-01 07:00:00.0000000+00:00',
    '2026-01-01 07:00:00.0000000+00:00',
    '2026-01-01 07:00:00.0000000+00:00',
    '2026-01-01 09:00:00.0000000+00:00'
);

COMMIT;
