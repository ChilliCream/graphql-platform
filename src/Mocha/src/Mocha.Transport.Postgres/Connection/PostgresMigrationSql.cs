namespace Mocha.Transport.Postgres;

internal static class PostgresMigrationSql
{
    public static string Literal(string value)
        => "E'" + value.Replace("\\", "\\\\").Replace("'", "''") + "'";

    public static string Identifier(string identifier)
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

    public static string QualifiedIdentifier(string identifier)
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
