namespace HotChocolate.Data.ElasticSearch;

internal static class ThrowHelper
{
    public static GraphQLException Paging_SkipNotSupported()
        => new(ErrorBuilder.New()
            .SetMessage(ElasticSearchResources.ThrowHelper_Paging_SkipNotSupported)
            .SetCode("ELASTICSEARCH_SKIP_NOT_SUPPORTED")
            .Build());

    public static GraphQLException ListFilter_RequiresNested(string path)
        => new(ErrorBuilder.New()
            .SetMessage(ElasticSearchResources.ThrowHelper_ListFilter_RequiresNested, path)
            .SetCode("ELASTICSEARCH_LIST_FILTER_REQUIRES_NESTED")
            .Build());

    public static GraphQLException ListFilter_ReservedCharacterInPrefix(string value)
        => new(ErrorBuilder.New()
            .SetMessage(ElasticSearchResources.ThrowHelper_ListFilter_ReservedCharacter, value)
            .SetCode("ELASTICSEARCH_LIST_FILTER_PATTERN_NOT_SUPPORTED")
            .Build());

    public static GraphQLException ListFilter_NotSupported(string condition)
        => new(ErrorBuilder.New()
            .SetMessage(ElasticSearchResources.ThrowHelper_ListFilter_NotSupported, condition)
            .SetCode("ELASTICSEARCH_LIST_FILTER_NOT_SUPPORTED")
            .Build());
}
