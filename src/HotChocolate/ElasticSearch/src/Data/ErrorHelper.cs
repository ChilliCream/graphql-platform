namespace HotChocolate.Data.ElasticSearch;

internal static class ErrorHelper
{
    public static IError Paging_SkipNotSupported()
        => ErrorBuilder.New()
            .SetMessage(ElasticSearchResources.ErrorHelper_Paging_SkipNotSupported)
            .SetCode("ELASTICSEARCH_SKIP_NOT_SUPPORTED")
            .Build();

    public static IError ListFilter_RequiresNested(string path)
        => ErrorBuilder.New()
            .SetMessage(ElasticSearchResources.ErrorHelper_ListFilter_RequiresNested, path)
            .SetCode("ELASTICSEARCH_LIST_FILTER_REQUIRES_NESTED")
            .Build();

    public static IError ListFilter_ReservedCharacterInPrefix(string value)
        => ErrorBuilder.New()
            .SetMessage(ElasticSearchResources.ErrorHelper_ListFilter_ReservedCharacter, value)
            .SetCode("ELASTICSEARCH_LIST_FILTER_PATTERN_NOT_SUPPORTED")
            .Build();

    public static IError ListFilter_NotSupported(string condition)
        => ErrorBuilder.New()
            .SetMessage(ElasticSearchResources.ErrorHelper_ListFilter_NotSupported, condition)
            .SetCode("ELASTICSEARCH_LIST_FILTER_NOT_SUPPORTED")
            .Build();
}
