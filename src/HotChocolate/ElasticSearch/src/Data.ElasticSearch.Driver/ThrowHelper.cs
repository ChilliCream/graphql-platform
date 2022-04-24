using System;
using System.Globalization;
using HotChocolate.Data.ElasticSearch.Filters;
using HotChocolate.Data.Filters;

namespace HotChocolate.Data.ElasticSearch;

internal static class ThrowHelper
{
    public static GraphQLException PagingTypeNotSupported(Type type)
    {
        return new GraphQLException(
            ErrorBuilder.New()
                .SetMessage(
                    ElasticSearchResources.Paging_SourceIsNotSupported,
                    type.FullName ?? type.Name)
                .SetCode(ErrorCodes.Data.NoPaginationProviderFound)
                .Build());
    }

    public static InvalidOperationException Filtering_ElasticSearchCombinator_QueueEmpty(
        ElasticSearchFilterCombinator combinator) =>
        new(string.Format(
            CultureInfo.CurrentCulture,
            ElasticSearchResources.Filtering_ElasticSearchCombinator_QueueEmpty,
            combinator.GetType()));

    public static InvalidOperationException Filtering_ElasticSearchCombinator_InvalidCombinator(
        ElasticSearchFilterCombinator combinator,
        FilterCombinator operation) =>
        new(string.Format(
            CultureInfo.CurrentCulture,
            ElasticSearchResources.Filtering_ElasticSearchCombinator_InvalidCombinator,
            combinator.GetType(),
            operation.ToString()));

    public static InvalidOperationException Filtering_WrongValueProvided(IFilterField field) =>
        new(string.Format(
            CultureInfo.CurrentCulture,
            ElasticSearchResources.Filtering_WrongValueProvided,
            field.Type.RuntimeType.Name,
            field.Coordinate.ToString()));
}
