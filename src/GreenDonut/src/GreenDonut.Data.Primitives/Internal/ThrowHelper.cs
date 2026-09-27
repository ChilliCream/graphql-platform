namespace GreenDonut.Data.Internal;

/// <summary>
/// Creates the exceptions thrown by the streaming page primitives, so their messages live in one
/// place.
/// </summary>
internal static class ThrowHelper
{
    public static ArgumentException StreamBatchPump_KeyNotRequested(object key)
        => new(
            $"'{key}' is not one of the keys this batch pump was created with.",
            "key");

    public static InvalidOperationException StreamBatchPump_RowForUnrequestedKey(object key)
        => new(
            $"The batch source produced a row for key '{key}', which is not one of the requested "
            + "keys.");

    public static InvalidOperationException StreamBatchPump_KeyAlreadyHasPage(object key)
        => new($"A page for key '{key}' was already created. Call CreatePage exactly once per key.");

    public static ArgumentException StreamBatchPump_DuplicateKey(object key)
        => new($"The requested keys contain a duplicate: '{key}'.", "keys");
}
