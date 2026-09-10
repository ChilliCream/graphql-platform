using CookieCrumble.Resources;

namespace HotChocolate.Data;

[CollectionDefinition(DefinitionName)]
public class PostgresCacheCollectionFixture : ICollectionFixture<PostgreSqlResource>
{
    internal const string DefinitionName = "PostgresSqlResource";
}
