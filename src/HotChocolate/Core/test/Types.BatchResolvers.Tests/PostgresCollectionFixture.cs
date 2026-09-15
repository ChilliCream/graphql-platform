using Squadron;

namespace HotChocolate.Types.BatchResolvers;

[CollectionDefinition(DefinitionName)]
public sealed class PostgresCollectionFixture : ICollectionFixture<PostgreSqlResource>
{
    public const string DefinitionName = "BatchResolversPostgreSql";
}
