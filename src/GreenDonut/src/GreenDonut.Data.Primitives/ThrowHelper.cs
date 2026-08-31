namespace GreenDonut.Data;

internal static class ThrowHelper
{
    public static ArgumentException StreamPage_BackwardPaginationNotSupported(string parameterName)
        => new("StreamPage does not support the 'last' or 'before' paging arguments.", parameterName);
}
