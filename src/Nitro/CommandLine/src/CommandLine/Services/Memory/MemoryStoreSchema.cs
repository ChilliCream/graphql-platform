namespace ChilliCream.Nitro.CommandLine.Services.Memory;

/// <summary>
/// Creates missing memory tables, indexes, and search-maintenance triggers in the
/// workspace database.
/// </summary>
internal static class MemoryStoreSchema
{
    public const string Create =
        """
        CREATE TABLE IF NOT EXISTS memory_journal (
            id TEXT PRIMARY KEY,
            body TEXT NOT NULL,
            created_at TEXT NOT NULL,
            created_by TEXT NOT NULL
        );

        CREATE INDEX IF NOT EXISTS idx_memory_journal_created_at ON memory_journal (created_at);

        CREATE TABLE IF NOT EXISTS memory_curated (
            id TEXT PRIMARY KEY,
            type TEXT NOT NULL,
            body TEXT NOT NULL,
            created_at TEXT NOT NULL,
            updated_at TEXT NOT NULL,
            created_by TEXT NOT NULL,
            -- No foreign key against memory_journal: promotion copies the
            -- entry rather than referencing it, so forgetting the journal
            -- entry must never cascade into the curated memory it produced.
            promoted_from TEXT
        );

        CREATE INDEX IF NOT EXISTS idx_memory_curated_updated_at ON memory_curated (updated_at);
        CREATE INDEX IF NOT EXISTS idx_memory_curated_type ON memory_curated (type);
        CREATE UNIQUE INDEX IF NOT EXISTS idx_memory_curated_promoted_from
            ON memory_curated (promoted_from) WHERE promoted_from IS NOT NULL;

        CREATE TABLE IF NOT EXISTS memory_curated_tags (
            id TEXT NOT NULL REFERENCES memory_curated (id) ON DELETE CASCADE,
            tag TEXT NOT NULL,
            PRIMARY KEY (id, tag)
        );

        CREATE INDEX IF NOT EXISTS idx_memory_curated_tags_tag ON memory_curated_tags (tag);

        CREATE VIRTUAL TABLE IF NOT EXISTS memory_curated_fts USING fts5(
            id UNINDEXED,
            body,
            content = 'memory_curated',
            content_rowid = 'rowid'
        );

        -- Curated-memory writes update the external-content search index.
        CREATE TRIGGER IF NOT EXISTS memory_curated_fts_insert AFTER INSERT ON memory_curated
        BEGIN
            INSERT INTO memory_curated_fts (rowid, id, body) VALUES (new.rowid, new.id, new.body);
        END;

        CREATE TRIGGER IF NOT EXISTS memory_curated_fts_delete AFTER DELETE ON memory_curated
        BEGIN
            INSERT INTO memory_curated_fts (memory_curated_fts, rowid, id, body)
            VALUES ('delete', old.rowid, old.id, old.body);
        END;

        CREATE TRIGGER IF NOT EXISTS memory_curated_fts_update AFTER UPDATE ON memory_curated
        BEGIN
            INSERT INTO memory_curated_fts (memory_curated_fts, rowid, id, body)
            VALUES ('delete', old.rowid, old.id, old.body);
            INSERT INTO memory_curated_fts (rowid, id, body) VALUES (new.rowid, new.id, new.body);
        END;
        """;
}
