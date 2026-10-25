using Microsoft.EntityFrameworkCore;

namespace HotChocolate.Data.Spatial.Filters;

public class DatabaseContext<T> : DbContext
    where T : class
{
    private readonly PostgisResource _resource;
    private readonly string _databaseName;
    private bool _disposed;

    public DatabaseContext(PostgisResource resource, string databaseName)
    {
        _resource = resource;
        _databaseName = databaseName;
    }

    public DbSet<T> Data { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(
            _resource.GetConnectionString(_databaseName),
            o =>
                o.UseNetTopologySuite());
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
