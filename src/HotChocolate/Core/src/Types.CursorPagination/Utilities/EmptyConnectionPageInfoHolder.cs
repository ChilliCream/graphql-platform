namespace HotChocolate.Types.Pagination.Utilities;

internal static class EmptyConnectionPageInfoHolder
{
    public static readonly ConnectionPageInfo Empty = new(false, false, null, null);
}
