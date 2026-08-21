namespace ChilliCream.Nitro.CommandLine.Services.Memory;

/// <summary>
/// The schema of the disposable per-scope FTS5 search index at
/// <c>.local/index.db</c>: one metadata row recording the fingerprint the
/// index was built from, one row per curated memory with the fields
/// <c>search</c> filters against, a tag junction table for repeatable
/// <c>--tag</c> filtering, and an FTS5 virtual table over the body text.
/// </summary>
internal static class MemoryIndexSchema
{
    public const string Create = """
        CREATE TABLE meta (
            key TEXT PRIMARY KEY,
            value TEXT NOT NULL
        );

        CREATE TABLE curated (
            id TEXT PRIMARY KEY,
            type TEXT NOT NULL,
            created_at TEXT NOT NULL,
            updated_at TEXT NOT NULL,
            created_by TEXT NOT NULL,
            promoted_from TEXT,
            path TEXT NOT NULL
        );

        CREATE TABLE curated_tags (
            id TEXT NOT NULL,
            tag TEXT NOT NULL
        );

        CREATE INDEX idx_curated_tags_id ON curated_tags (id);
        CREATE INDEX idx_curated_tags_tag ON curated_tags (tag);

        CREATE VIRTUAL TABLE curated_fts USING fts5(id UNINDEXED, body);
        """;

    public const string FingerprintKey = "fingerprint";
}
