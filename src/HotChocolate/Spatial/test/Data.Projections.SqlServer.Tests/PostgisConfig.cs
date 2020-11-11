using System;
using Squadron;

namespace HotChocolate.Data.Projections.Spatial
{
    public class PostgisConfig : PostgreSqlDefaultOptions
    {
        public override void Configure(ContainerResourceBuilder builder)
        {
            builder
                .WaitTimeout(120)
                .Name("postgis")
                .Image("postgis/postgis:latest")
                .Username("postgis")
                .Password(Guid.NewGuid().ToString("N").Substring(12))
                .InternalPort(5432);
        }
    }
}
