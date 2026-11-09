namespace HotChocolate.Data.ElasticSearch;

internal static class ThrowHelper
{
    public static GraphQLException Paging_SkipNotSupported()
        => new(ErrorBuilder.New()
            .SetMessage("ES|QL cannot skip rows: `skip`, `after` and `before` are not supported.")
            .SetCode("ELASTICSEARCH_SKIP_NOT_SUPPORTED")
            .Build());

    public static GraphQLException ListFilter_RequiresNested(string path)
        => new(ErrorBuilder.New()
            .SetMessage(
                "This filter on `{0}` needs the field to be mapped as nested and marked with "
                + "`ElasticSearchNestedAttribute`: an object mapping keeps the values of each field apart.",
                path)
            .SetCode("ELASTICSEARCH_LIST_FILTER_REQUIRES_NESTED")
            .Build());

    public static GraphQLException ListFilter_ReservedCharacterInPrefix(string value)
        => new(ErrorBuilder.New()
            .SetMessage("KQL cannot express this pattern on a nested field: `{0}`.", value)
            .SetCode("ELASTICSEARCH_LIST_FILTER_PATTERN_NOT_SUPPORTED")
            .Build());

    public static GraphQLException ListFilter_NotSupported(string condition)
        => new(ErrorBuilder.New()
            .SetMessage("This condition is not supported on a list: `{0}`.", condition)
            .SetCode("ELASTICSEARCH_LIST_FILTER_NOT_SUPPORTED")
            .Build());
}
