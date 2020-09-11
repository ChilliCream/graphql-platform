using Data.Filters.SqlServer.Tests;
using HotChocolate.Data.Filters;
using Squadron;
using Xunit;

namespace HotChocolate.Data
{
    [CollectionDefinition(nameof(DatabaseCollection))]
    public class DatabaseCollection
        : ICollectionFixture<SqlServerResource<CustomSqlServerOptions>>,
          IClassFixture<SchemaCache>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }
}
