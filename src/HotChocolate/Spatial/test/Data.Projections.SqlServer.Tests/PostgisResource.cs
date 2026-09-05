using CookieCrumble.Resources;
using Testcontainers.PostgreSql;

namespace HotChocolate.Data.Projections.Spatial;

public sealed class PostgisResource : PostgreSqlResource
{
    protected override PostgreSqlBuilder Configure(PostgreSqlBuilder builder)
        => builder.WithImage("postgis/postgis:16-3.4-alpine");
}
