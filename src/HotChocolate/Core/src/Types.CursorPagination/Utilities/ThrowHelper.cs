using System.Reflection;
using static HotChocolate.Types.Properties.CursorResources;

namespace HotChocolate.Utilities
{
    internal static class ThrowHelper
    {
        public static SchemaException UsePagingAttribute_NodeTypeUnknown(
            MemberInfo member) =>
            new SchemaException(
                SchemaErrorBuilder.New()
                    .SetMessage("The UsePaging attribute needs a valid node schema type.")
                    .SetCode("PAGINATION_SCHEMA_TYPE_INVALID")
                    .SetExtension(nameof(member), member)
                    .Build());

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
                    .SetCode("PAGINATION_SCHEMA_TYPE_INVALID")
                    .Build());
    }
}
