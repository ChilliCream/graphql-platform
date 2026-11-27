namespace Mocha.Outbox;

internal static class ThrowHelper
{
    public static InvalidOperationException EnvelopeNotSet()
        => new("Envelope is not set");
}
