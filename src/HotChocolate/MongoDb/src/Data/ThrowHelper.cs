using System.Globalization;
using HotChocolate.Data.Filters;
using HotChocolate.Data.MongoDb.Filters;

namespace HotChocolate.Data.MongoDb;

internal static class ThrowHelper
{
    public static GraphQLException PagingTypeNotSupported(Type type)
    {
        return new GraphQLException(
            ErrorBuilder.New()
                .SetMessage(
                    MongoDbResources.Paging_SourceIsNotSupported,
                    type.FullName ?? type.Name)
                .SetCode(ErrorCodes.Data.NoPaginationProviderFound)
                .Build());
    }

    public static InvalidOperationException Filtering_MongoDbCombinator_QueueEmpty(
        MongoDbFilterCombinator combinator) =>
        new(string.Format(
            CultureInfo.CurrentCulture,
            MongoDbResources.Filtering_MongoDbCombinator_QueueEmpty,
            combinator.GetType()));

    public static InvalidOperationException Filtering_MongoDbCombinator_InvalidCombinator(
        MongoDbFilterCombinator combinator,
        FilterCombinator operation) =>
        new(string.Format(
            CultureInfo.CurrentCulture,
            MongoDbResources.Filtering_MongoDbCombinator_InvalidCombinator,
            combinator.GetType(),
            operation.ToString()));
}
