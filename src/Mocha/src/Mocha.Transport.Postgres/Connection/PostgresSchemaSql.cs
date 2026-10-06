namespace Mocha.Transport.Postgres;

internal static partial class PostgresSchemaSql
{
    private const int LockId = 958913715;

    public static string GenerateMigrationsSql(IReadOnlyPostgresSchemaOptions s)
        => $"""
            {Bootstrap(s)}

            {Migration(s, "2026-03-06_InitialSchema", InitialSchema(s))}

            {Migration(s, "2026-03-06_AddTransportIndex", AddTransportIndex(s))}

            {Migration(s, "2026-03-06_AddConsumerManagement", AddConsumerManagement(s))}

            """.ReplaceLineEndings("\n");

    private static string Migration(IReadOnlyPostgresSchemaOptions s, string id, string sql)
    {
        var migrationHistory = MigrationsTable(s);
        var migrationIdLiteral = Literal(id);

        return DoBlock(
            $"""
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM {migrationHistory} WHERE migration_id = {migrationIdLiteral}) THEN
            {Indent(sql, 8)}
                    INSERT INTO {migrationHistory} (migration_id) VALUES ({migrationIdLiteral});
                END IF;
            END;
            """);
    }

    private static string DoBlock(string body)
    {
        var delimiter = "$mocha_migration$";
        var suffix = 0;
        while (body.Contains(delimiter, StringComparison.Ordinal))
        {
            delimiter = $"$mocha_migration_{++suffix}$";
        }

        return $"""
            DO {delimiter}
            {body}
            {delimiter};
            """;
    }

    private static string Indent(string sql, int spaces)
    {
        var indentation = new string(' ', spaces);
        return string.Join("\n", sql.ReplaceLineEndings("\n").Split('\n')
            .Select(line => line.Length == 0 ? line : indentation + line));
    }

    private static string SchemaName(IReadOnlyPostgresSchemaOptions options)
        => Identifier(options.Schema);

    private static string TopicTable(IReadOnlyPostgresSchemaOptions options)
        => QualifiedIdentifier(options.TopicTable);

    private static string QueueTable(IReadOnlyPostgresSchemaOptions options)
        => QualifiedIdentifier(options.QueueTable);

    private static string QueueSubscriptionTable(IReadOnlyPostgresSchemaOptions options)
        => QualifiedIdentifier(options.QueueSubscriptionTable);

    private static string MessageTable(IReadOnlyPostgresSchemaOptions options)
        => QualifiedIdentifier(options.MessageTable);

    private static string ConsumersTable(IReadOnlyPostgresSchemaOptions options)
        => QualifiedIdentifier(options.ConsumersTable);

    private static string MigrationsTable(IReadOnlyPostgresSchemaOptions options)
        => QualifiedIdentifier(options.MigrationsTable);

    private static string TopologySequence(IReadOnlyPostgresSchemaOptions options)
        => QualifiedIdentifier(options.TopologySequence);

    private static string TopologySequenceLiteral(IReadOnlyPostgresSchemaOptions options)
        => Literal(TopologySequence(options));

    private static string TopicUniqueIndex(IReadOnlyPostgresSchemaOptions options)
        => Identifier(options.TablePrefix + "topic_uqx");

    private static string TopicUniqueConstraint(IReadOnlyPostgresSchemaOptions options)
        => Identifier(options.TablePrefix + "unique_topic");

    private static string QueueUniqueIndex(IReadOnlyPostgresSchemaOptions options)
        => Identifier(options.TablePrefix + "queue_uqx");

    private static string QueueUniqueConstraint(IReadOnlyPostgresSchemaOptions options)
        => Identifier(options.TablePrefix + "unique_queue");

    private static string QueueSubscriptionUniqueIndex(IReadOnlyPostgresSchemaOptions options)
        => Identifier(options.TablePrefix + "queue_subscription_uqx");

    private static string QueueSubscriptionUniqueConstraint(IReadOnlyPostgresSchemaOptions options)
        => Identifier(options.TablePrefix + "unique_queue_subscription");

    private static string QueueSubscriptionSourceIndex(IReadOnlyPostgresSchemaOptions options)
        => Identifier(options.TablePrefix + "queue_subscription_source_ndx");

    private static string QueueSubscriptionDestinationIndex(IReadOnlyPostgresSchemaOptions options)
        => Identifier(options.TablePrefix + "queue_subscription_dest_ndx");

    private static string MessageQueueIndex(IReadOnlyPostgresSchemaOptions options)
        => Identifier(options.TablePrefix + "message_queue_ndx");

    private static string MessageTransportQueueIndex(IReadOnlyPostgresSchemaOptions options)
        => Identifier(options.TablePrefix + "message_transport_queue_ndx");

    private static string MessageExpirationScheduledIndex(IReadOnlyPostgresSchemaOptions options)
        => Identifier(options.TablePrefix + "message_expiration_scheduled_ndx");

    private static string MessageSentTimeIndex(IReadOnlyPostgresSchemaOptions options)
        => Identifier(options.TablePrefix + "message_sent_time_ndx");

    private static string ConsumersUpdatedAtIndex(IReadOnlyPostgresSchemaOptions options)
        => Identifier(options.TablePrefix + "consumers_updated_at_ndx");

    private static string QueueConsumerIdIndex(IReadOnlyPostgresSchemaOptions options)
        => Identifier(options.TablePrefix + "queue_consumer_id_ndx");

    private static string Literal(string value)
        => "E'" + value.Replace("\\", "\\\\").Replace("'", "''") + "'";

    private static string Identifier(string identifier)
    {
        if (string.IsNullOrEmpty(identifier) || identifier.Any(char.IsControl))
        {
            throw ThrowHelper.InvalidSqlIdentifier(identifier);
        }

        if (identifier[0] == '"')
        {
            if (identifier.Length < 3 || identifier[^1] != '"')
            {
                throw ThrowHelper.InvalidSqlIdentifier(identifier);
            }

            for (var i = 1; i < identifier.Length - 1; i++)
            {
                if (identifier[i] == '"' && (++i >= identifier.Length - 1 || identifier[i] != '"'))
                {
                    throw ThrowHelper.InvalidSqlIdentifier(identifier);
                }
            }

            return identifier;
        }

        for (var i = 0; i < identifier.Length; i++)
        {
            var c = identifier[i];
            if (!(char.IsAsciiLetter(c) || c == '_' || c >= 128
                || (i > 0 && (char.IsAsciiDigit(c) || c == '$'))))
            {
                throw ThrowHelper.InvalidSqlIdentifier(identifier);
            }
        }

        // Preserve PostgreSQL's folding of unquoted ASCII identifiers.
        return "\"" + string.Create(identifier.Length, identifier, static (span, value) =>
        {
            for (var i = 0; i < value.Length; i++)
            {
                span[i] = value[i] is >= 'A' and <= 'Z' ? (char)(value[i] + ('a' - 'A')) : value[i];
            }
        }) + "\"";
    }

    private static string QualifiedIdentifier(string identifier)
    {
        var quoted = false;
        for (var i = 0; i < identifier.Length; i++)
        {
            if (identifier[i] == '"')
            {
                quoted = !quoted;
            }
            else if (identifier[i] == '.' && !quoted)
            {
                return Identifier(identifier[..i]) + "." + Identifier(identifier[(i + 1)..]);
            }
        }

        return Identifier(identifier);
    }
}
