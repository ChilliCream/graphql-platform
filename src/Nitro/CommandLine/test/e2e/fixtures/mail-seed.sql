-- Deterministic mail workspace fixture for the mail-board-flow e2e tape.
--
-- Applied by run.sh with the sqlite3 CLI against a workspace database that
-- `nitro agent mail init` already created (so the schema and PRAGMA
-- user_version come from the real binary, not from this file). Every ID,
-- timestamp, and actor here is hardcoded so a recording that reads this data
-- is byte-stable across runs.
--
-- All timestamps use the exact text format MailStore/Microsoft.Data.Sqlite
-- writes for a DateTimeOffset ("yyyy-MM-dd HH:mm:ss.fffffff+00:00"), UTC, on
-- a fixed date (2026-01-01). Every `created_at` here is far enough in the
-- past that MailAges.Format/MailInboxRow.FormatAge always land in their
-- "week or older" branch (a fixed "yyyy-MM-dd" string), independent of the
-- wall-clock date a recording actually runs on, so no SCRUBS entry is needed
-- for this flow the way mail-send-flow needs one for its live-created ids
-- and dates.

BEGIN TRANSACTION;

-- agents -----------------------------------------------------------------
-- e2e-agent is the actor the tape opens the board as (NITRO_MAIL_ACTOR).
INSERT INTO agents (name, registered_at, last_seen_at) VALUES
    ('e2e-agent', '2026-01-01 07:00:00.0000000+00:00', '2026-01-01 07:00:00.0000000+00:00'),
    ('alice', '2026-01-01 07:00:00.0000000+00:00', '2026-01-01 07:00:00.0000000+00:00'),
    ('bob', '2026-01-01 07:00:00.0000000+00:00', '2026-01-01 07:00:00.0000000+00:00');

-- messages -----------------------------------------------------------------
-- Three messages land in e2e-agent's inbox (they are a `to` recipient of
-- each); m-fix004 is e2e-agent's own reply on the m-fix003 thread, so it is
-- never listed in the inbox itself (QueryInboxAsync only returns messages
-- where the actor is a recipient), only visible via the thread toggle.
INSERT INTO messages (id, thread_id, in_reply_to, sender, subject, body, created_at) VALUES
    ('m-fix001', 'm-fix001', NULL, 'alice', 'Pricing page', 'Deploy started, will confirm once live.', '2026-01-01 09:00:00.0000000+00:00'),
    ('m-fix002', 'm-fix002', NULL, 'bob', 'Billing review', 'Needs review before merge.', '2026-01-01 10:00:00.0000000+00:00'),
    ('m-fix003', 'm-fix003', NULL, 'alice', 'Retro notes', 'Notes are attached, take a look.', '2026-01-01 08:00:00.0000000+00:00'),
    ('m-fix004', 'm-fix003', 'm-fix003', 'e2e-agent', 'Retro notes', 'Thanks, looks good to me.', '2026-01-01 08:30:00.0000000+00:00');

-- message_recipients ---------------------------------------------------------
-- m-fix001 and m-fix003 are unread for e2e-agent (unread styling); m-fix002
-- is already read (read_at set), so the board's initial selection (newest
-- first: m-fix002, m-fix001, m-fix003) starts on a read message and the
-- unread marker is visible only on the other two rows. m-fix002 also carries
-- a cc to alice, exercising a non-`to` recipient kind in the fixture.
INSERT INTO message_recipients (message_id, recipient, kind, ordinal, read_at, archived_at) VALUES
    ('m-fix001', 'e2e-agent', 'to', 0, NULL, NULL),
    ('m-fix002', 'e2e-agent', 'to', 0, '2026-01-02 08:00:00.0000000+00:00', NULL),
    ('m-fix002', 'alice', 'cc', 1, NULL, NULL),
    ('m-fix003', 'e2e-agent', 'to', 0, NULL, NULL),
    ('m-fix004', 'alice', 'to', 0, NULL, NULL);

COMMIT;
