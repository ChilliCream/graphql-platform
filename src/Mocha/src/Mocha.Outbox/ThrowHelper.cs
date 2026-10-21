namespace Mocha.Outbox;

internal static class ThrowHelper
{
    public static InvalidOperationException MissingPreparedEnvelope()
        => new("The outbox dispatch pipeline did not produce a message envelope.");
}
