using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

namespace CookieCrumble.Resources;

public class SqlServerResource : ContainerResource<MsSqlContainer>
{
    public string ConnectionString => Container.GetConnectionString();

    public string GetConnectionString(string database)
        => new SqlConnectionStringBuilder(ConnectionString) { InitialCatalog = database }
            .ConnectionString;

    protected override MsSqlContainer Build()
        => Configure(new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04"))
            .Build();

    protected virtual MsSqlBuilder Configure(MsSqlBuilder builder) => builder;
}
