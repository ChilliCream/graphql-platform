namespace Mocha.Transport.Postgres;

internal static partial class PostgresSchemaSql
{
    private static string Bootstrap(IReadOnlyPostgresSchemaOptions s)
        => $"""
            SELECT pg_advisory_xact_lock({LockId});
            CREATE SCHEMA IF NOT EXISTS {s.Schema};

            CREATE TABLE IF NOT EXISTS {s.MigrationsTable}
            (
                migration_id  text        NOT NULL PRIMARY KEY,
                applied_on    timestamptz NOT NULL DEFAULT (now() at time zone 'utc')
            );
            """;

    public static string InitialSchema(IReadOnlyPostgresSchemaOptions s) =>
        $"""
        CREATE SCHEMA IF NOT EXISTS {s.Schema};

        CREATE SEQUENCE IF NOT EXISTS {s.TopologySequence} AS bigint;

        CREATE TABLE IF NOT EXISTS {s.TopicTable}
        (
            id          bigint      NOT NULL PRIMARY KEY DEFAULT nextval({Literal(s.TopologySequence)}),
            updated     timestamptz NOT NULL DEFAULT (now() at time zone 'utc'),
            name        text        NOT NULL
        );

        CREATE UNIQUE INDEX IF NOT EXISTS {TopicUniqueIndex(s)} ON {s.TopicTable} (name) INCLUDE (id);
        ALTER TABLE {s.TopicTable} ADD CONSTRAINT {TopicUniqueConstraint(s)} UNIQUE USING INDEX {TopicUniqueIndex(s)};

        CREATE TABLE IF NOT EXISTS {s.QueueTable}
        (
            id          bigint      NOT NULL PRIMARY KEY DEFAULT nextval({Literal(s.TopologySequence)}),
            updated     timestamptz NOT NULL DEFAULT (now() at time zone 'utc'),
            name        text        NOT NULL
        );

        CREATE UNIQUE INDEX IF NOT EXISTS {QueueUniqueIndex(s)} ON {s.QueueTable} (name) INCLUDE (id);
        ALTER TABLE {s.QueueTable} ADD CONSTRAINT {QueueUniqueConstraint(s)} UNIQUE USING INDEX {QueueUniqueIndex(s)};

        CREATE TABLE IF NOT EXISTS {s.QueueSubscriptionTable}
        (
            id              bigint      NOT NULL PRIMARY KEY DEFAULT nextval({Literal(s.TopologySequence)}),
            updated         timestamptz NOT NULL DEFAULT (now() at time zone 'utc'),
            source_id       bigint      NOT NULL REFERENCES {s.TopicTable} (id) ON DELETE CASCADE,
            destination_id  bigint      NOT NULL REFERENCES {s.QueueTable} (id) ON DELETE CASCADE
        );

        CREATE UNIQUE INDEX IF NOT EXISTS {QueueSubscriptionUniqueIndex(s)}
            ON {s.QueueSubscriptionTable} (source_id, destination_id);
        ALTER TABLE {s.QueueSubscriptionTable}
            ADD CONSTRAINT {QueueSubscriptionUniqueConstraint(s)} UNIQUE USING INDEX {QueueSubscriptionUniqueIndex(s)};

        CREATE INDEX IF NOT EXISTS {QueueSubscriptionSourceIndex(s)}
            ON {s.QueueSubscriptionTable} (source_id) INCLUDE (id, destination_id);
        CREATE INDEX IF NOT EXISTS {QueueSubscriptionDestinationIndex(s)}
            ON {s.QueueSubscriptionTable} (destination_id) INCLUDE (id, source_id);

        CREATE TABLE IF NOT EXISTS {s.MessageTable}
        (
            transport_message_id    uuid        NOT NULL PRIMARY KEY DEFAULT gen_random_uuid(),
            body                    bytea       NOT NULL,
            headers                 jsonb,
            queue_id                bigint      NOT NULL REFERENCES {s.QueueTable} (id) ON DELETE CASCADE,
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
            ON {s.MessageTable} (queue_id) INCLUDE (transport_message_id);
        """;

    private static string AddTransportIndex(IReadOnlyPostgresSchemaOptions s) =>
        $"""
        CREATE INDEX IF NOT EXISTS {MessageTransportQueueIndex(s)}
            ON {s.MessageTable} (transport_message_id, queue_id);

        CREATE INDEX IF NOT EXISTS {MessageExpirationScheduledIndex(s)}
            ON {s.MessageTable} (expiration_time, scheduled_time);

        CREATE INDEX IF NOT EXISTS {MessageSentTimeIndex(s)}
            ON {s.MessageTable} (sent_time);
        """;

    private static string AddConsumerManagement(IReadOnlyPostgresSchemaOptions s) =>
        $"""
        CREATE TABLE IF NOT EXISTS {s.ConsumersTable}
        (
            id              uuid        NOT NULL PRIMARY KEY,
            service_name    text        NOT NULL,
            created_at      timestamptz NOT NULL DEFAULT now(),
            updated_at      timestamptz NOT NULL DEFAULT now()
        );

        ALTER TABLE {s.QueueTable}
            ADD COLUMN IF NOT EXISTS consumer_id uuid REFERENCES {s.ConsumersTable}(id) ON DELETE CASCADE;

        CREATE INDEX IF NOT EXISTS {ConsumersUpdatedAtIndex(s)}
            ON {s.ConsumersTable} (updated_at);

        CREATE INDEX IF NOT EXISTS {QueueConsumerIdIndex(s)}
            ON {s.QueueTable} (consumer_id);
        """;

    private static string TopicUniqueIndex(IReadOnlyPostgresSchemaOptions options)
        => options.TablePrefix + "topic_uqx";

    private static string TopicUniqueConstraint(IReadOnlyPostgresSchemaOptions options)
        => options.TablePrefix + "unique_topic";

    private static string QueueUniqueIndex(IReadOnlyPostgresSchemaOptions options)
        => options.TablePrefix + "queue_uqx";

    private static string QueueUniqueConstraint(IReadOnlyPostgresSchemaOptions options)
        => options.TablePrefix + "unique_queue";

    private static string QueueSubscriptionUniqueIndex(IReadOnlyPostgresSchemaOptions options)
        => options.TablePrefix + "queue_subscription_uqx";

    private static string QueueSubscriptionUniqueConstraint(IReadOnlyPostgresSchemaOptions options)
        => options.TablePrefix + "unique_queue_subscription";

    private static string QueueSubscriptionSourceIndex(IReadOnlyPostgresSchemaOptions options)
        => options.TablePrefix + "queue_subscription_source_ndx";

    private static string QueueSubscriptionDestinationIndex(IReadOnlyPostgresSchemaOptions options)
        => options.TablePrefix + "queue_subscription_dest_ndx";

    private static string MessageQueueIndex(IReadOnlyPostgresSchemaOptions options)
        => options.TablePrefix + "message_queue_ndx";

    private static string MessageTransportQueueIndex(IReadOnlyPostgresSchemaOptions options)
        => options.TablePrefix + "message_transport_queue_ndx";

    private static string MessageExpirationScheduledIndex(IReadOnlyPostgresSchemaOptions options)
        => options.TablePrefix + "message_expiration_scheduled_ndx";

    private static string MessageSentTimeIndex(IReadOnlyPostgresSchemaOptions options)
        => options.TablePrefix + "message_sent_time_ndx";

    private static string ConsumersUpdatedAtIndex(IReadOnlyPostgresSchemaOptions options)
        => options.TablePrefix + "consumers_updated_at_ndx";

    private static string QueueConsumerIdIndex(IReadOnlyPostgresSchemaOptions options)
        => options.TablePrefix + "queue_consumer_id_ndx";
}
