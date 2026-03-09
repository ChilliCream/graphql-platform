using Squadron;

namespace HotChocolate.Data.Projections.Spatial;

public class PostgisConfig : PostgreSqlDefaultOptions
{
    public override void Configure(ContainerResourceBuilder builder) =>
        builder
            .WaitTimeout(120)
            .Name("postgis")
            .Image("postgis/postgis:16-3.4-alpine")
            .Username("postgis")
            .Password(Guid.NewGuid().ToString("N")[12..])
            .InternalPort(5432);
}
