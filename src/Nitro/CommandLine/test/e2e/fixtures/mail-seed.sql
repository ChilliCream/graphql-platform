-- Deterministic mail workspace fixture for the mail-board-flow (and the
-- Mail-tab portions of board-flow/agent-root-flow) e2e tapes.
--
-- Applied by run.sh with the sqlite3 CLI against the same unified agent
-- workspace database seed.sql seeds, that `nitro agent init` already
-- created (so the schema and PRAGMA user_version come from the real binary,
-- not from this file). Every ID, timestamp, and actor here is hardcoded so
-- a recording that reads this data is byte-stable across runs.
--
-- All timestamps use the exact text format MailStore/Microsoft.Data.Sqlite
-- writes for a DateTimeOffset ("yyyy-MM-dd HH:mm:ss.fffffff+00:00"), UTC, on
-- a fixed date (2026-01-01). Every `created_at` here is far enough in the
-- past that MailAges.Format/MailInboxRow.FormatAge always land in their
-- "week or older" branch (a fixed "yyyy-MM-dd" string), independent of the
-- wall-clock date a recording actually runs on, so no SCRUBS entry is needed
-- for this flow the way mail-send-flow needs one for its live-created ids
-- and dates.
--
-- m-fix005 and m-fix006 back the Sent and Workspace mailboxes: m-fix005 is
-- e2e-agent's own unreplied thread root (sent, never appears in anyone's
-- inbox, and otherwise unreachable in the TUI at all -- the gap the Sent
-- mailbox exists to close), and m-fix006 is a message between alice and bob
-- that never involves e2e-agent, visible only in the Workspace mailbox.

BEGIN TRANSACTION;

-- agents -----------------------------------------------------------------
-- e2e-agent backs the identity-dependent mail command fixtures.
INSERT INTO agents (name, registered_at, last_seen_at) VALUES
    ('e2e-agent', '2026-01-01 07:00:00.0000000+00:00', '2026-01-01 07:00:00.0000000+00:00'),
    ('alice', '2026-01-01 07:00:00.0000000+00:00', '2026-01-01 07:00:00.0000000+00:00'),
    ('bob', '2026-01-01 07:00:00.0000000+00:00', '2026-01-01 07:00:00.0000000+00:00');

-- messages -----------------------------------------------------------------
-- Three messages land in e2e-agent's inbox (they are a `to` recipient of
-- each); m-fix004 is e2e-agent's own reply on the m-fix003 thread, so it is
-- never listed in the inbox itself (QueryInboxAsync only returns messages
-- where the actor is a recipient), only visible via the thread toggle --
-- but it is the newest message e2e-agent ever sent, so it is the first row
-- the Sent mailbox shows. m-fix005 is a second, older sent message from
-- e2e-agent that never got a reply, so its thread_id is its own id; unlike
-- m-fix004 it has no reply to be reached through, so Sent is the only place
-- it is reachable at all. m-fix006 is older still and sent by alice to bob,
-- with e2e-agent neither its sender nor a recipient, so it appears only in
-- the Workspace mailbox.
INSERT INTO messages (id, thread_id, in_reply_to, sender, subject, body, created_at) VALUES
    ('m-fix001', 'm-fix001', NULL, 'alice', 'Pricing page', 'Deploy started, will confirm once live.', '2026-01-01 09:00:00.0000000+00:00'),
    ('m-fix002', 'm-fix002', NULL, 'bob', 'Billing review', 'Needs review before merge.', '2026-01-01 10:00:00.0000000+00:00'),
    ('m-fix003', 'm-fix003', NULL, 'alice', 'Retro notes', 'Notes are attached, take a look.', '2026-01-01 08:00:00.0000000+00:00'),
    ('m-fix004', 'm-fix003', 'm-fix003', 'e2e-agent', 'Retro notes', 'Thanks, looks good to me.', '2026-01-01 08:30:00.0000000+00:00'),
    ('m-fix005', 'm-fix005', NULL, 'e2e-agent', 'Standup notes', 'No blockers from my side.', '2026-01-01 07:30:00.0000000+00:00'),
    ('m-fix006', 'm-fix006', NULL, 'alice', 'Deploy window', 'Let''s ship after lunch.', '2026-01-01 06:30:00.0000000+00:00');

-- message_recipients ---------------------------------------------------------
-- m-fix001 and m-fix003 are unread for e2e-agent (unread styling); m-fix002
-- is already read (read_at set), so the board's initial selection (newest
-- first: m-fix002, m-fix001, m-fix003) starts on a read message and the
-- unread marker is visible only on the other two rows. m-fix002 also carries
-- a cc to alice, exercising a non-`to` recipient kind in the fixture.
-- m-fix005's recipient is bob (Sent mailbox's second "To " peer, alongside
-- m-fix004's "To alice"); m-fix006's recipient is bob too, with alice as
-- sender, so e2e-agent has no row on it at all.
INSERT INTO message_recipients (message_id, recipient, kind, ordinal, read_at, archived_at) VALUES
    ('m-fix001', 'e2e-agent', 'to', 0, NULL, NULL),
    ('m-fix002', 'e2e-agent', 'to', 0, '2026-01-02 08:00:00.0000000+00:00', NULL),
    ('m-fix002', 'alice', 'cc', 1, NULL, NULL),
    ('m-fix003', 'e2e-agent', 'to', 0, NULL, NULL),
    ('m-fix004', 'alice', 'to', 0, NULL, NULL),
    ('m-fix005', 'bob', 'to', 0, NULL, NULL),
    ('m-fix006', 'bob', 'to', 0, NULL, NULL);

COMMIT;
