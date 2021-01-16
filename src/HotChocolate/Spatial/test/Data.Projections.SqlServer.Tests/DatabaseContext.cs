using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Squadron;

namespace HotChocolate.Data.Projections.Spatial
{
    public class DatabaseContext<T> : DbContext where T : class
    {
        private readonly PostgreSqlResource<PostgisConfig> _resource;
        private readonly string _databaseName;
        private bool _disposed;

        public DatabaseContext(PostgreSqlResource<PostgisConfig> resource, string databaseName)
        {
            _resource = resource;
            _databaseName = databaseName;
        }

        public DbSet<T> Data { get; set; } = default!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql(
                _resource.GetConnectionString(_databaseName),
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
}
