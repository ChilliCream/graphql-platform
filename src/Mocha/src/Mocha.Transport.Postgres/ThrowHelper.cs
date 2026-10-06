namespace Mocha.Transport.Postgres;

internal static class ThrowHelper
{
    public static InvalidOperationException MigrationConnectionMustBeOpen()
        => new("Transport migration requires an open PostgreSQL connection.");

    public static InvalidOperationException MigrationTransactionNotSupported(Exception? innerException)
        => new("Transport migration requires a connection without an active or ambient transaction.", innerException);
}
