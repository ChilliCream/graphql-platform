using static Mocha.Transport.Postgres.PostgresMigrationSql;

namespace Mocha.Transport.Postgres;

/// <summary>
/// Contains the SQL migration scripts for the PostgreSQL messaging transport schema.
/// </summary>
internal static class PostgresSchemaSql
{
    public static string InitialSchema(IReadOnlyPostgresSchemaOptions s) =>
        $"""
        CREATE SCHEMA IF NOT EXISTS {Identifier(s.Schema)};

        CREATE SEQUENCE IF NOT EXISTS {QualifiedIdentifier(s.TopologySequence)} AS bigint;

        CREATE TABLE IF NOT EXISTS {QualifiedIdentifier(s.TopicTable)}
        (
            id          bigint      NOT NULL PRIMARY KEY DEFAULT nextval({Literal(QualifiedIdentifier(s.TopologySequence))}),
            updated     timestamptz NOT NULL DEFAULT (now() at time zone 'utc'),
            name        text        NOT NULL
        );

        CREATE UNIQUE INDEX IF NOT EXISTS {Identifier(s.TablePrefix + "topic_uqx")} ON {QualifiedIdentifier(s.TopicTable)} (name) INCLUDE (id);
        ALTER TABLE {QualifiedIdentifier(s.TopicTable)} ADD CONSTRAINT {Identifier(s.TablePrefix + "unique_topic")} UNIQUE USING INDEX {Identifier(s.TablePrefix + "topic_uqx")};

        CREATE TABLE IF NOT EXISTS {QualifiedIdentifier(s.QueueTable)}
        (
            id          bigint      NOT NULL PRIMARY KEY DEFAULT nextval({Literal(QualifiedIdentifier(s.TopologySequence))}),
            updated     timestamptz NOT NULL DEFAULT (now() at time zone 'utc'),
            name        text        NOT NULL
        );

        CREATE UNIQUE INDEX IF NOT EXISTS {Identifier(s.TablePrefix + "queue_uqx")} ON {QualifiedIdentifier(s.QueueTable)} (name) INCLUDE (id);
        ALTER TABLE {QualifiedIdentifier(s.QueueTable)} ADD CONSTRAINT {Identifier(s.TablePrefix + "unique_queue")} UNIQUE USING INDEX {Identifier(s.TablePrefix + "queue_uqx")};

        CREATE TABLE IF NOT EXISTS {QualifiedIdentifier(s.QueueSubscriptionTable)}
        (
            id              bigint      NOT NULL PRIMARY KEY DEFAULT nextval({Literal(QualifiedIdentifier(s.TopologySequence))}),
            updated         timestamptz NOT NULL DEFAULT (now() at time zone 'utc'),
            source_id       bigint      NOT NULL REFERENCES {QualifiedIdentifier(s.TopicTable)} (id) ON DELETE CASCADE,
            destination_id  bigint      NOT NULL REFERENCES {QualifiedIdentifier(s.QueueTable)} (id) ON DELETE CASCADE
        );

        CREATE UNIQUE INDEX IF NOT EXISTS {Identifier(s.TablePrefix + "queue_subscription_uqx")}
            ON {QualifiedIdentifier(s.QueueSubscriptionTable)} (source_id, destination_id);
        ALTER TABLE {QualifiedIdentifier(s.QueueSubscriptionTable)}
            ADD CONSTRAINT {Identifier(s.TablePrefix + "unique_queue_subscription")} UNIQUE USING INDEX {Identifier(s.TablePrefix + "queue_subscription_uqx")};

        CREATE INDEX IF NOT EXISTS {Identifier(s.TablePrefix + "queue_subscription_source_ndx")}
            ON {QualifiedIdentifier(s.QueueSubscriptionTable)} (source_id) INCLUDE (id, destination_id);
        CREATE INDEX IF NOT EXISTS {Identifier(s.TablePrefix + "queue_subscription_dest_ndx")}
            ON {QualifiedIdentifier(s.QueueSubscriptionTable)} (destination_id) INCLUDE (id, source_id);

        CREATE TABLE IF NOT EXISTS {QualifiedIdentifier(s.MessageTable)}
        (
            transport_message_id    uuid        NOT NULL PRIMARY KEY DEFAULT gen_random_uuid(),
            body                    bytea       NOT NULL,
            headers                 jsonb,
            queue_id                bigint      NOT NULL REFERENCES {QualifiedIdentifier(s.QueueTable)} (id) ON DELETE CASCADE,
            sent_time               timestamptz NOT NULL DEFAULT (now() at time zone 'utc'),
            scheduled_time          timestamptz,
            expiration_time         timestamptz,
            delivery_count          int         NOT NULL DEFAULT 0,
            max_delivery_count      int         NOT NULL DEFAULT 10,
            last_delivered          timestamptz,
            consumer_id             uuid,
            error_reason            jsonb
        );

        CREATE INDEX IF NOT EXISTS {Identifier(s.TablePrefix + "message_queue_ndx")}
            ON {QualifiedIdentifier(s.MessageTable)} (queue_id) INCLUDE (transport_message_id);
        """;

    public static string AddTransportIndex(IReadOnlyPostgresSchemaOptions s) =>
        $"""
        CREATE INDEX IF NOT EXISTS {Identifier(s.TablePrefix + "message_transport_queue_ndx")}
            ON {QualifiedIdentifier(s.MessageTable)} (transport_message_id, queue_id);

        CREATE INDEX IF NOT EXISTS {Identifier(s.TablePrefix + "message_expiration_scheduled_ndx")}
            ON {QualifiedIdentifier(s.MessageTable)} (expiration_time, scheduled_time);

        CREATE INDEX IF NOT EXISTS {Identifier(s.TablePrefix + "message_sent_time_ndx")}
            ON {QualifiedIdentifier(s.MessageTable)} (sent_time);
        """;

    public static string AddConsumerManagement(IReadOnlyPostgresSchemaOptions s) =>
        $"""
        CREATE TABLE IF NOT EXISTS {QualifiedIdentifier(s.ConsumersTable)}
        (
            id              uuid        NOT NULL PRIMARY KEY,
            service_name    text        NOT NULL,
            created_at      timestamptz NOT NULL DEFAULT now(),
            updated_at      timestamptz NOT NULL DEFAULT now()
        );

        ALTER TABLE {QualifiedIdentifier(s.QueueTable)}
            ADD COLUMN IF NOT EXISTS consumer_id uuid REFERENCES {QualifiedIdentifier(s.ConsumersTable)}(id) ON DELETE CASCADE;

        CREATE INDEX IF NOT EXISTS {Identifier(s.TablePrefix + "consumers_updated_at_ndx")}
            ON {QualifiedIdentifier(s.ConsumersTable)} (updated_at);

        CREATE INDEX IF NOT EXISTS {Identifier(s.TablePrefix + "queue_consumer_id_ndx")}
            ON {QualifiedIdentifier(s.QueueTable)} (consumer_id);
        """;
}
