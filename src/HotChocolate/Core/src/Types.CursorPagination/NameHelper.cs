using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Pagination;

internal static class NameHelper
{
    public static string CreateConnectionName(
        INamingConventions namingConventions,
        string connectionName)
        => namingConventions.GetTypeName(connectionName + "Connection", TypeKind.Object);

    public static string CreateEdgeName(
        INamingConventions namingConventions,
        string connectionName)
        => namingConventions.GetTypeName(connectionName + "Edge", TypeKind.Object);
}
