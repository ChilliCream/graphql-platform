namespace HotChocolate.Types.Pagination;

internal static class NameHelper
{
    public static string CreateConnectionName(string connectionName)
        => connectionName + "Connection";

    public static string CreateEdgeName(string connectionName)
        => connectionName + "Edge";
}
