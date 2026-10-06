namespace Mocha.Transport.Postgres;

internal static partial class PostgresSchemaSql
{
    private static string Bootstrap(IReadOnlyPostgresSchemaOptions s)
        => $"""
            SELECT pg_advisory_xact_lock({LockId});
            CREATE SCHEMA IF NOT EXISTS {SchemaName(s)};

            CREATE TABLE IF NOT EXISTS {MigrationsTable(s)}
            (
                migration_id  text        NOT NULL PRIMARY KEY,
                applied_on    timestamptz NOT NULL DEFAULT (now() at time zone 'utc')
            );
            """;

    public static string InitialSchema(IReadOnlyPostgresSchemaOptions s) =>
        $"""
        CREATE SCHEMA IF NOT EXISTS {SchemaName(s)};

        CREATE SEQUENCE IF NOT EXISTS {TopologySequence(s)} AS bigint;

        CREATE TABLE IF NOT EXISTS {TopicTable(s)}
        (
            id          bigint      NOT NULL PRIMARY KEY DEFAULT nextval({TopologySequenceLiteral(s)}),
            updated     timestamptz NOT NULL DEFAULT (now() at time zone 'utc'),
            name        text        NOT NULL
        );

        CREATE UNIQUE INDEX IF NOT EXISTS {TopicUniqueIndex(s)} ON {TopicTable(s)} (name) INCLUDE (id);
        ALTER TABLE {TopicTable(s)} ADD CONSTRAINT {TopicUniqueConstraint(s)} UNIQUE USING INDEX {TopicUniqueIndex(s)};

        CREATE TABLE IF NOT EXISTS {QueueTable(s)}
        (
            id          bigint      NOT NULL PRIMARY KEY DEFAULT nextval({TopologySequenceLiteral(s)}),
            updated     timestamptz NOT NULL DEFAULT (now() at time zone 'utc'),
            name        text        NOT NULL
        );

        CREATE UNIQUE INDEX IF NOT EXISTS {QueueUniqueIndex(s)} ON {QueueTable(s)} (name) INCLUDE (id);
        ALTER TABLE {QueueTable(s)} ADD CONSTRAINT {QueueUniqueConstraint(s)} UNIQUE USING INDEX {QueueUniqueIndex(s)};

        CREATE TABLE IF NOT EXISTS {QueueSubscriptionTable(s)}
        (
            id              bigint      NOT NULL PRIMARY KEY DEFAULT nextval({TopologySequenceLiteral(s)}),
            updated         timestamptz NOT NULL DEFAULT (now() at time zone 'utc'),
            source_id       bigint      NOT NULL REFERENCES {TopicTable(s)} (id) ON DELETE CASCADE,
            destination_id  bigint      NOT NULL REFERENCES {QueueTable(s)} (id) ON DELETE CASCADE
        );

        CREATE UNIQUE INDEX IF NOT EXISTS {QueueSubscriptionUniqueIndex(s)}
            ON {QueueSubscriptionTable(s)} (source_id, destination_id);
        ALTER TABLE {QueueSubscriptionTable(s)}
            ADD CONSTRAINT {QueueSubscriptionUniqueConstraint(s)} UNIQUE USING INDEX {QueueSubscriptionUniqueIndex(s)};

        CREATE INDEX IF NOT EXISTS {QueueSubscriptionSourceIndex(s)}
            ON {QueueSubscriptionTable(s)} (source_id) INCLUDE (id, destination_id);
        CREATE INDEX IF NOT EXISTS {QueueSubscriptionDestinationIndex(s)}
            ON {QueueSubscriptionTable(s)} (destination_id) INCLUDE (id, source_id);

        CREATE TABLE IF NOT EXISTS {MessageTable(s)}
        (
            transport_message_id    uuid        NOT NULL PRIMARY KEY DEFAULT gen_random_uuid(),
            body                    bytea       NOT NULL,
            headers                 jsonb,
            queue_id                bigint      NOT NULL REFERENCES {QueueTable(s)} (id) ON DELETE CASCADE,
            sent_time               timestamptz NOT NULL DEFAULT (now() at time zone 'utc'),
            scheduled_time          timestamptz,
            expiration_time         timestamptz,
            delivery_count          int         NOT NULL DEFAULT 0,
            max_delivery_count      int         NOT NULL DEFAULT 10,
            last_delivered          timestamptz,
            consumer_id             uuid,
            error_reason            jsonb
        );

        CREATE INDEX IF NOT EXISTS {MessageQueueIndex(s)}
            ON {MessageTable(s)} (queue_id) INCLUDE (transport_message_id);
        """;

    private static string AddTransportIndex(IReadOnlyPostgresSchemaOptions s) =>
        $"""
        CREATE INDEX IF NOT EXISTS {MessageTransportQueueIndex(s)}
            ON {MessageTable(s)} (transport_message_id, queue_id);

        CREATE INDEX IF NOT EXISTS {MessageExpirationScheduledIndex(s)}
            ON {MessageTable(s)} (expiration_time, scheduled_time);

        CREATE INDEX IF NOT EXISTS {MessageSentTimeIndex(s)}
            ON {MessageTable(s)} (sent_time);
        """;

    private static string AddConsumerManagement(IReadOnlyPostgresSchemaOptions s) =>
        $"""
        CREATE TABLE IF NOT EXISTS {ConsumersTable(s)}
        (
            id              uuid        NOT NULL PRIMARY KEY,
            service_name    text        NOT NULL,
            created_at      timestamptz NOT NULL DEFAULT now(),
            updated_at      timestamptz NOT NULL DEFAULT now()
        );

        ALTER TABLE {QueueTable(s)}
            ADD COLUMN IF NOT EXISTS consumer_id uuid REFERENCES {ConsumersTable(s)}(id) ON DELETE CASCADE;

        CREATE INDEX IF NOT EXISTS {ConsumersUpdatedAtIndex(s)}
            ON {ConsumersTable(s)} (updated_at);

        CREATE INDEX IF NOT EXISTS {QueueConsumerIdIndex(s)}
            ON {QueueTable(s)} (consumer_id);
        """;
}
