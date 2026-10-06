using System.Text;
using static Mocha.Transport.Postgres.PostgresMigrationSql;

namespace Mocha.Transport.Postgres;

internal static class PostgresSchemaMigrator
{
    internal const int LockId = 958913715;

    internal static (string Id, string Sql)[] GetMigrations(IReadOnlyPostgresSchemaOptions options)
        =>
        [
            ("2026-03-06_InitialSchema", PostgresSchemaSql.InitialSchema(options)),
            ("2026-03-06_AddTransportIndex", PostgresSchemaSql.AddTransportIndex(options)),
            ("2026-03-06_AddConsumerManagement", PostgresSchemaSql.AddConsumerManagement(options))
        ];

    public static string GenerateBody(IReadOnlyPostgresSchemaOptions options)
    {
        var history = QualifiedIdentifier(options.MigrationsTable);
        var builder = new StringBuilder();
        builder.Append($"SELECT pg_advisory_xact_lock({LockId});\n");
        builder.Append(
            $"""
            CREATE SCHEMA IF NOT EXISTS {Identifier(options.Schema)};

            CREATE TABLE IF NOT EXISTS {history}
            (
                migration_id  text        NOT NULL PRIMARY KEY,
                applied_on    timestamptz NOT NULL DEFAULT (now() at time zone 'utc')
            );
            """);
        builder.Append('\n');

        foreach (var (id, sql) in GetMigrations(options))
        {
            var indentedSql = string.Join("\n", sql.ReplaceLineEndings("\n").Split('\n')
                .Select(line => line.Length == 0 ? line : "        " + line));
            var body =
                $"""
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM {history} WHERE migration_id = {Literal(id)}) THEN
                {indentedSql}
                        INSERT INTO {history} (migration_id) VALUES ({Literal(id)});
                    END IF;
                END;
                """;
            var delimiter = "$mocha_migration$";
            for (var suffix = 1; body.Contains(delimiter, StringComparison.Ordinal); suffix++)
            {
                delimiter = $"$mocha_migration_{suffix}$";
            }

            builder.Append("\nDO ").Append(delimiter).Append('\n');
            builder.Append(body).Append('\n');
            builder.Append(delimiter).Append(";\n");
        }

        return builder.ToString().ReplaceLineEndings("\n");
    }
}
