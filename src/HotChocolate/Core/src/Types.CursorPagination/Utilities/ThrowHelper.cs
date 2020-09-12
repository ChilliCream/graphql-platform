using System.Reflection;

namespace HotChocolate.Utilities
{
    internal static class ThrowHelper
    {
        public static SchemaException UsePagingAttribute_NodeTypeUnknown(
            MemberInfo member) =>
            new SchemaException(
                SchemaErrorBuilder.New()
                    .SetMessage("The UsePaging attribute needs a valid node schema type.")
                    .SetCode("ATTR_USEPAGING_SCHEMATYPE_INVALID")
                    .SetExtension(nameof(member), member)
                    .Build());
    }
}
