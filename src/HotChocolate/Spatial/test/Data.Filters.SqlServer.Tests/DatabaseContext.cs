using System.IO;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Squadron;

namespace HotChocolate.Spatial.Data.Filters
{
    public class DatabaseContext<T> : DbContext
        where T : class
    {
        private readonly PostgreSqlResource<PostgisConfig> _resource;
        private bool _disposed;

        public DatabaseContext(PostgreSqlResource<PostgisConfig> resource)
        {
            _resource = resource;
        }

        public DbSet<T> Data { get; set; } = default!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql(
                _resource.ConnectionString,
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
}
