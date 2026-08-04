using Squadron;

namespace GreenDonut.Data;

[CollectionDefinition(DefinitionName)]
public class SqlServerCacheCollectionFixture : ICollectionFixture<SqlServerResource>
{
    internal const string DefinitionName = "SqlServerResource";
}
