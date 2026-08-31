namespace GreenDonut.Data;

internal static class ThrowHelper
{
    public static InvalidOperationException StreamPage_EnumerationCanOnlyOccurOnce()
        => new("A streamed page can only be enumerated once.");

    public static ArgumentException StreamPage_BackwardPaginationNotSupported(string parameterName)
        => new("StreamPage does not support the 'last' or 'before' paging arguments.", parameterName);
}
