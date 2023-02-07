using HotChocolate.Data.Raven.Filters;

namespace HotChocolate.Data.Raven.Paging;

[CollectionDefinition(DefinitionName)]
public class SchemaCacheCollectionFixture : ICollectionFixture<SchemaCache>
{
    internal const string DefinitionName = "SchemaCacheDatabase";
}
