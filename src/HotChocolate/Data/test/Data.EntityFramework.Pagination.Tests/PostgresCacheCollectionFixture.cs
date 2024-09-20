using Squadron;

[CollectionDefinition(DefinitionName)]
public class PostgresCacheCollectionFixture : ICollectionFixture<PostgreSqlResource>
{
    internal const string DefinitionName = "PostgresSqlResource";
}
