using Data.Filters.SqlServer.Tests;
using Squadron;

namespace HotChocolate.Data
{
    public class DatabaseHelper
    {
        public static SqlServerResource<CustomSqlServerOptions> Resource { get; } =
            new SqlServerResource<CustomSqlServerOptions>();
    }
}
