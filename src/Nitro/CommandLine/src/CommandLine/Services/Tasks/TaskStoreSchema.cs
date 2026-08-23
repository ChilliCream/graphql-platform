namespace ChilliCream.Nitro.CommandLine.Services.Tasks;

internal static class TaskStoreSchema
{
    public const int CurrentVersion = 1;

    /// <summary>
    /// The complete schema. Statements are idempotent so applying them to an
    /// existing database is non-destructive.
    /// </summary>
    public const string Create =
        """
        CREATE TABLE IF NOT EXISTS tasks (
            id TEXT PRIMARY KEY,
            title TEXT NOT NULL CHECK (length(title) <= 500),
            description TEXT NOT NULL DEFAULT '',
            design TEXT NOT NULL DEFAULT '',
            acceptance_criteria TEXT NOT NULL DEFAULT '',
            notes TEXT NOT NULL DEFAULT '',
            status TEXT NOT NULL DEFAULT 'open',
            priority INTEGER NOT NULL DEFAULT 2 CHECK (priority BETWEEN 0 AND 4),
            task_type TEXT NOT NULL DEFAULT 'task',
            assignee TEXT,
            estimated_minutes INTEGER,
            due_at TEXT,
            defer_until TEXT,
            created_at TEXT NOT NULL,
            created_by TEXT NOT NULL DEFAULT '',
            updated_at TEXT NOT NULL,
            closed_at TEXT,
            close_reason TEXT NOT NULL DEFAULT '',
            deleted_at TEXT,
            delete_reason TEXT NOT NULL DEFAULT ''
        );

        CREATE INDEX IF NOT EXISTS idx_tasks_status ON tasks (status);
        CREATE INDEX IF NOT EXISTS idx_tasks_priority ON tasks (priority);
        CREATE INDEX IF NOT EXISTS idx_tasks_assignee ON tasks (assignee);
        CREATE INDEX IF NOT EXISTS idx_tasks_updated_at ON tasks (updated_at);

        CREATE TABLE IF NOT EXISTS dependencies (
            task_id TEXT NOT NULL REFERENCES tasks (id) ON DELETE CASCADE,
            depends_on_id TEXT NOT NULL,
            dependency_type TEXT NOT NULL DEFAULT 'blocks',
            created_at TEXT NOT NULL,
            created_by TEXT NOT NULL DEFAULT '',
            PRIMARY KEY (task_id, depends_on_id)
        );

        CREATE INDEX IF NOT EXISTS idx_dependencies_depends_on ON dependencies (depends_on_id);

        CREATE TABLE IF NOT EXISTS labels (
            task_id TEXT NOT NULL REFERENCES tasks (id) ON DELETE CASCADE,
            label TEXT NOT NULL,
            PRIMARY KEY (task_id, label)
        );

        CREATE INDEX IF NOT EXISTS idx_labels_label ON labels (label);

        CREATE TABLE IF NOT EXISTS comments (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            task_id TEXT NOT NULL REFERENCES tasks (id) ON DELETE CASCADE,
            author TEXT NOT NULL,
            text TEXT NOT NULL,
            created_at TEXT NOT NULL
        );

        CREATE INDEX IF NOT EXISTS idx_comments_task ON comments (task_id);

        CREATE TABLE IF NOT EXISTS events (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            task_id TEXT NOT NULL REFERENCES tasks (id) ON DELETE CASCADE,
            event_type TEXT NOT NULL,
            actor TEXT NOT NULL DEFAULT '',
            old_value TEXT,
            new_value TEXT,
            comment TEXT,
            created_at TEXT NOT NULL
        );

        CREATE INDEX IF NOT EXISTS idx_events_task ON events (task_id);

        CREATE TABLE IF NOT EXISTS config (
            key TEXT PRIMARY KEY,
            value TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS child_counters (
            parent_id TEXT PRIMARY KEY REFERENCES tasks (id) ON DELETE CASCADE,
            last_child INTEGER NOT NULL DEFAULT 0
        );
        """;
}
