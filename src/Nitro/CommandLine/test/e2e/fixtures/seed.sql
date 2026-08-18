-- Deterministic task workspace fixture for e2e tapes.
--
-- Applied by run.sh with the sqlite3 CLI against a workspace database that
-- `nitro task init` already created (so the schema, PRAGMA user_version, and
-- the `config` prefix row come from the real binary, not from this file).
-- Every ID, timestamp, and actor here is hardcoded so recordings that read
-- this data are byte-stable across runs. See README.md in this directory for
-- the dataset shape and how to regenerate this file after a schema change.
--
-- All timestamps use the exact text format TaskStore/Microsoft.Data.Sqlite
-- writes for a DateTimeOffset ("yyyy-MM-dd HH:mm:ss.fffffff+00:00"), UTC, on a
-- fixed date (2026-01-01) so idx_tasks_updated_at ordering matches what a
-- real run would have produced.

BEGIN TRANSACTION;

-- tasks ----------------------------------------------------------------
-- acme-epic1: an epic with two children (acme-epic1.1, acme-epic1.2).
INSERT INTO tasks (
    id, title, description, design, acceptance_criteria, notes,
    status, priority, task_type, assignee, estimated_minutes, due_at, defer_until,
    created_at, created_by, updated_at, closed_at, close_reason, deleted_at, delete_reason
) VALUES (
    'acme-epic1', 'Launch v2 pricing page', '', '', '', '',
    'open', 1, 'epic', NULL, NULL, NULL, NULL,
    '2026-01-01 09:00:00.0000000+00:00', 'e2e-agent',
    '2026-01-01 09:00:00.0000000+00:00', NULL, '', NULL, ''
);

INSERT INTO tasks (
    id, title, description, design, acceptance_criteria, notes,
    status, priority, task_type, assignee, estimated_minutes, due_at, defer_until,
    created_at, created_by, updated_at, closed_at, close_reason, deleted_at, delete_reason
) VALUES (
    'acme-epic1.1', 'Draft new pricing copy', '', '', '', '',
    'in_progress', 2, 'task', 'alice', NULL, NULL, NULL,
    '2026-01-01 09:05:00.0000000+00:00', 'e2e-agent',
    '2026-01-02 10:00:00.0000000+00:00', NULL, '', NULL, ''
);

INSERT INTO tasks (
    id, title, description, design, acceptance_criteria, notes,
    status, priority, task_type, assignee, estimated_minutes, due_at, defer_until,
    created_at, created_by, updated_at, closed_at, close_reason, deleted_at, delete_reason
) VALUES (
    'acme-epic1.2', 'Wire up billing API', '', '', '', '',
    'open', 2, 'task', NULL, NULL, NULL, NULL,
    '2026-01-01 09:10:00.0000000+00:00', 'e2e-agent',
    '2026-01-03 12:00:00.0000000+00:00', NULL, '', NULL, ''
);

-- acme-a1b: standalone bug, blocked on acme-epic1.2 via a `blocks` edge below.
INSERT INTO tasks (
    id, title, description, design, acceptance_criteria, notes,
    status, priority, task_type, assignee, estimated_minutes, due_at, defer_until,
    created_at, created_by, updated_at, closed_at, close_reason, deleted_at, delete_reason
) VALUES (
    'acme-a1b', 'Checkout crashes on Safari', '', '', '', '',
    'open', 0, 'bug', NULL, NULL, NULL, NULL,
    '2026-01-02 08:00:00.0000000+00:00', 'e2e-agent',
    '2026-01-02 08:00:00.0000000+00:00', NULL, '', NULL, ''
);

-- acme-c3d: standalone feature, already closed.
INSERT INTO tasks (
    id, title, description, design, acceptance_criteria, notes,
    status, priority, task_type, assignee, estimated_minutes, due_at, defer_until,
    created_at, created_by, updated_at, closed_at, close_reason, deleted_at, delete_reason
) VALUES (
    'acme-c3d', 'Add dark mode toggle', '', '', '', '',
    'closed', 3, 'feature', NULL, NULL, NULL, NULL,
    '2026-01-01 07:00:00.0000000+00:00', 'e2e-agent',
    '2026-01-04 15:00:00.0000000+00:00', '2026-01-04 15:00:00.0000000+00:00', 'Shipped in v1.9',
    NULL, ''
);

-- acme-e5f: standalone chore, deferred.
INSERT INTO tasks (
    id, title, description, design, acceptance_criteria, notes,
    status, priority, task_type, assignee, estimated_minutes, due_at, defer_until,
    created_at, created_by, updated_at, closed_at, close_reason, deleted_at, delete_reason
) VALUES (
    'acme-e5f', 'Rotate staging API keys', '', '', '', '',
    'deferred', 4, 'chore', NULL, NULL, NULL, '2026-02-01 00:00:00.0000000+00:00',
    '2026-01-01 07:30:00.0000000+00:00', 'e2e-agent',
    '2026-01-05 09:00:00.0000000+00:00', NULL, '', NULL, ''
);

-- dependencies -----------------------------------------------------------
-- Parent-child edges, as `task create --parent` would have recorded them.
INSERT INTO dependencies (task_id, depends_on_id, dependency_type, created_at, created_by)
VALUES ('acme-epic1.1', 'acme-epic1', 'parent-child', '2026-01-01 09:05:00.0000000+00:00', 'e2e-agent');

INSERT INTO dependencies (task_id, depends_on_id, dependency_type, created_at, created_by)
VALUES ('acme-epic1.2', 'acme-epic1', 'parent-child', '2026-01-01 09:10:00.0000000+00:00', 'e2e-agent');

-- A real blocking edge, so `task show acme-epic1.2` lists a dependency and
-- `task show acme-epic1.1` lists a block, independent of the parent-child pair.
INSERT INTO dependencies (task_id, depends_on_id, dependency_type, created_at, created_by)
VALUES ('acme-epic1.2', 'acme-epic1.1', 'blocks', '2026-01-01 09:10:00.0000000+00:00', 'e2e-agent');

-- acme-a1b is open but not actionable: it is blocked on the still-open
-- acme-epic1.2, so `task show acme-a1b` renders a "Blocked by" line.
INSERT INTO dependencies (task_id, depends_on_id, dependency_type, created_at, created_by)
VALUES ('acme-a1b', 'acme-epic1.2', 'blocks', '2026-01-02 08:00:00.0000000+00:00', 'e2e-agent');

-- labels -------------------------------------------------------------------
INSERT INTO labels (task_id, label) VALUES ('acme-epic1.1', 'content');

-- comments -------------------------------------------------------------------
INSERT INTO comments (task_id, author, text, created_at)
VALUES ('acme-epic1.2', 'bob', 'Needs design review before starting.', '2026-01-03 12:00:00.0000000+00:00');

-- events -------------------------------------------------------------------
-- Not read by any task command besides `task stats` (COUNT(*)); included so
-- the audit log looks like real command output would have produced.
INSERT INTO events (task_id, event_type, actor, old_value, new_value, comment, created_at) VALUES
    ('acme-epic1', 'created', 'e2e-agent', NULL, NULL, NULL, '2026-01-01 09:00:00.0000000+00:00'),
    ('acme-epic1.1', 'created', 'e2e-agent', NULL, NULL, NULL, '2026-01-01 09:05:00.0000000+00:00'),
    ('acme-epic1.1', 'dependency_added', 'e2e-agent', NULL, 'parent-child:acme-epic1', NULL, '2026-01-01 09:05:00.0000000+00:00'),
    ('acme-epic1.2', 'created', 'e2e-agent', NULL, NULL, NULL, '2026-01-01 09:10:00.0000000+00:00'),
    ('acme-epic1.2', 'dependency_added', 'e2e-agent', NULL, 'parent-child:acme-epic1', NULL, '2026-01-01 09:10:00.0000000+00:00'),
    ('acme-epic1.2', 'dependency_added', 'e2e-agent', NULL, 'blocks:acme-epic1.1', NULL, '2026-01-01 09:10:00.0000000+00:00'),
    ('acme-a1b', 'created', 'e2e-agent', NULL, NULL, NULL, '2026-01-02 08:00:00.0000000+00:00'),
    ('acme-a1b', 'dependency_added', 'e2e-agent', NULL, 'blocks:acme-epic1.2', NULL, '2026-01-02 08:00:00.0000000+00:00'),
    ('acme-c3d', 'created', 'e2e-agent', NULL, NULL, NULL, '2026-01-01 07:00:00.0000000+00:00'),
    ('acme-c3d', 'closed', 'e2e-agent', NULL, NULL, 'Shipped in v1.9', '2026-01-04 15:00:00.0000000+00:00'),
    ('acme-e5f', 'created', 'e2e-agent', NULL, NULL, NULL, '2026-01-01 07:30:00.0000000+00:00'),
    ('acme-e5f', 'deferred', 'e2e-agent', NULL, '2026-02-01 00:00:00.0000000+00:00', NULL, '2026-01-05 09:00:00.0000000+00:00'),
    ('acme-epic1.1', 'label_added', 'e2e-agent', NULL, 'content', NULL, '2026-01-02 09:00:00.0000000+00:00'),
    ('acme-epic1.1', 'status_changed', 'e2e-agent', 'open', 'in_progress', NULL, '2026-01-02 10:00:00.0000000+00:00'),
    ('acme-epic1.2', 'commented', 'bob', NULL, NULL, NULL, '2026-01-03 12:00:00.0000000+00:00');

-- child_counters -------------------------------------------------------------
-- Two children already allocated under acme-epic1 (epic1.1, epic1.2); the
-- next `task create --parent acme-epic1` would mint acme-epic1.3.
INSERT INTO child_counters (parent_id, last_child) VALUES ('acme-epic1', 2);

COMMIT;
