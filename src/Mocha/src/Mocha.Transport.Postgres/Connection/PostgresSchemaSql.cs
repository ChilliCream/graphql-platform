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
        var migrationIdLiteral = Literal(id);

        return DoBlock(
            $"""
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM {s.MigrationsTable} WHERE migration_id = {migrationIdLiteral}) THEN
            {Indent(sql, 8)}
                    INSERT INTO {s.MigrationsTable} (migration_id) VALUES ({migrationIdLiteral});
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
            suffix++;
            delimiter = $"$mocha_migration_{suffix}$";
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
        return indentation + sql.ReplaceLineEndings("\n" + indentation);
    }

    private static string Literal(string value)
        => "E'" + value.Replace("\\", "\\\\").Replace("'", "''") + "'";
}
