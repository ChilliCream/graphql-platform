using Microsoft.EntityFrameworkCore;
using Squadron;

namespace HotChocolate.Data.Projections.Spatial;

public class DatabaseContext<T>(
    PostgreSqlResource<PostgisConfig> resource,
    string databaseName)
    : DbContext
    where T : class
{
    private bool _disposed;

    public DbSet<T> Data { get; set; } = default!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(
            resource.GetConnectionString(databaseName),
            o => o.UseNetTopologySuite());
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();

        if (!_disposed)
        {
            _disposed = true;
        }
    }
}
