namespace HotChocolate.Data.ElasticSearch;

internal static class ThrowHelper
{
    public static GraphQLException Paging_SkipNotSupported()
        => new(ErrorHelper.Paging_SkipNotSupported());

    public static GraphQLException ListFilter_RequiresNested(string path)
        => new(ErrorHelper.ListFilter_RequiresNested(path));

    public static GraphQLException ListFilter_ReservedCharacterInPrefix(string value)
        => new(ErrorHelper.ListFilter_ReservedCharacterInPrefix(value));

    public static GraphQLException ListFilter_NotSupported(string condition)
        => new(ErrorHelper.ListFilter_NotSupported(condition));
}
