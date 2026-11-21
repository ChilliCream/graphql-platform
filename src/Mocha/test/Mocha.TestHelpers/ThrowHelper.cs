namespace Mocha.TestHelpers;

internal static class ThrowHelper
{
    public static InvalidOperationException LogStreamCompleted()
        => new("The log stream completed before a matching record was collected.");
}
