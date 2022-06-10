using Squadron;
using Xunit;

namespace HotChocolate.Data.Filters.Spatial;

[CollectionDefinition("Postgres")]
public class PostgreSqlFixture : ICollectionFixture<PostgreSqlResource<PostgisConfig>>
{
}
