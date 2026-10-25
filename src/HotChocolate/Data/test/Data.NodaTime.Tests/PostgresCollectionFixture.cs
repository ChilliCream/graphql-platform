using CookieCrumble.Resources;

namespace HotChocolate.Data.NodaTime;

[CollectionDefinition(DefinitionName)]
public sealed class PostgresCollectionFixture : ICollectionFixture<PostgreSqlResource>
{
    internal const string DefinitionName = "PostgresSqlResource";
}
