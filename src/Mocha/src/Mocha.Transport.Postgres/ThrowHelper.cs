namespace Mocha.Transport.Postgres;

internal static class ThrowHelper
{
    public static InvalidOperationException MigrationConnectionMustBeOpen()
        => new("Transport migration requires an open PostgreSQL connection.");

    public static InvalidOperationException MigrationTransactionNotSupported(Exception? innerException)
        => new("Transport migration requires a connection without an active or ambient transaction.", innerException);

    public static ArgumentException InvalidSqlIdentifier(string identifier)
        => new($"'{identifier}' is not a valid PostgreSQL identifier.", nameof(identifier));
}
