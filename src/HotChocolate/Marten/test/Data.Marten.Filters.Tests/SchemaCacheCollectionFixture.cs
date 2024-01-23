using HotChocolate.Data.Filters;

namespace HotChocolate.Data;

[CollectionDefinition(DefinitionName)]
public class SchemaCacheCollectionFixture : ICollectionFixture<SchemaCache>
{
    internal const string DefinitionName = "SchemaCacheDatabase";
}
