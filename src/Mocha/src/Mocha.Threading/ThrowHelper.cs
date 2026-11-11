namespace Mocha.Threading;

internal static class ThrowHelper
{
    public static ArgumentOutOfRangeException DelayMustNotBeNegative(string parameterName, TimeSpan delay)
        => new(parameterName, delay, "The delay must not be negative.");
}
