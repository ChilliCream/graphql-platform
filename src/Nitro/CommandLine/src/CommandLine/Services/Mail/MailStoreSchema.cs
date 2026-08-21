namespace ChilliCream.Nitro.CommandLine.Services.Mail;

internal static class MailStoreSchema
{
    /// <summary>
    /// The complete schema. Statements are idempotent so applying them to an
    /// existing database is non-destructive.
    /// </summary>
    public const string Create =
        """
        CREATE TABLE IF NOT EXISTS messages (
            id TEXT PRIMARY KEY,
            thread_id TEXT NOT NULL,
            in_reply_to TEXT REFERENCES messages (id),
            sender TEXT NOT NULL REFERENCES agents (name),
            subject TEXT NOT NULL CHECK (length(subject) BETWEEN 1 AND 500),
            body TEXT NOT NULL,
            created_at TEXT NOT NULL
        );

        CREATE INDEX IF NOT EXISTS idx_messages_thread_id ON messages (thread_id);
        CREATE INDEX IF NOT EXISTS idx_messages_created_at ON messages (created_at);

        CREATE TABLE IF NOT EXISTS message_recipients (
            message_id TEXT NOT NULL REFERENCES messages (id) ON DELETE CASCADE,
            recipient TEXT NOT NULL REFERENCES agents (name),
            kind TEXT NOT NULL DEFAULT 'to' CHECK (kind IN ('to', 'cc')),
            ordinal INTEGER NOT NULL CHECK (ordinal >= 0),
            read_at TEXT,
            archived_at TEXT,
            PRIMARY KEY (message_id, recipient),
            UNIQUE (message_id, ordinal)
        );

        CREATE INDEX IF NOT EXISTS idx_message_recipients_recipient
            ON message_recipients (recipient);
        """;
}
