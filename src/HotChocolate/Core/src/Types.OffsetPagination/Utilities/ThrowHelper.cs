namespace HotChocolate.Utilities
{
    internal static class ThrowHelper
    {
        public static GraphQLException OffsetPagingHandler_MaxPageSize() =>
            new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage("The maximum allowed items per page were exceeded.")
                    .SetCode("PAGINATION_MAX_ITEMS")
                    .Build());
    }
}
