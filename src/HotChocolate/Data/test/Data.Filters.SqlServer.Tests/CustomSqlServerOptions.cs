using Squadron;

namespace Data.Filters.SqlServer.Tests
{
    public class CustomSqlServerOptions : SqlServerDefaultOptions
    {
        public override void Configure(ContainerResourceBuilder builder)
        {
            base.Configure(builder);
            builder.WaitTimeout(60);
        }
    }
}
