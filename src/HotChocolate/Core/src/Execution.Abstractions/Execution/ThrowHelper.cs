namespace HotChocolate.Execution;

internal static class ThrowHelper
{
    public static InvalidOperationException JsonValueFormatter_IncrementalObjectResultDataRequired()
        => new("An incremental object result must include data.");
}
