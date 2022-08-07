using Squadron;

namespace HotChocolate.Data.Spatial.Filters;

[CollectionDefinition("Postgres")]
public class PostgreSqlFixture : ICollectionFixture<PostgreSqlResource<PostgisConfig>>
{
}
