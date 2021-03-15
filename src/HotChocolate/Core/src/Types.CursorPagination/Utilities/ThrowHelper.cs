using static HotChocolate.Types.Properties.CursorResources;

namespace HotChocolate.Utilities
{
    internal static class ThrowHelper
    {
        public static GraphQLException ConnectionMiddleware_MaxPageSize() =>
            new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage("The maximum allowed items per page were exceeded.")
                    .SetCode("PAGINATION_MAX_ITEMS")
                    .Build());

        public static SchemaException PagingObjectFieldDescriptorExtensions_InvalidType() =>
            new SchemaException(
                SchemaErrorBuilder.New()
                    .SetMessage(PagingObjectFieldDescriptorExtensions_SchemaTypeNotValid)
                    .SetCode(ErrorCodes.Types.SchemaTypeInvalid)
                    .Build());
    }
}
