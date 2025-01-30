using Squadron;

namespace GreenDonut.Data;

[CollectionDefinition(DefinitionName)]
public class PostgresCacheCollectionFixture : ICollectionFixture<PostgreSqlResource>
{
    internal const string DefinitionName = "PostgresSqlResource";
}
