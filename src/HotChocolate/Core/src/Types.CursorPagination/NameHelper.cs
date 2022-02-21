namespace HotChocolate.Types.Pagination;

internal static class NameHelper
{
    public static string CreateConnectionName(NameString connectionName)
        => connectionName + "Connection";

    public static string CreateEdgeName(NameString connectionName)
        => connectionName + "Edge";
}
