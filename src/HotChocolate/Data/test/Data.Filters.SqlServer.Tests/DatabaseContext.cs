using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace HotChocolate.Data.Filters
{
    public class DatabaseContext<T> : DbContext
        where T : class
    {
        private readonly string _fileName;
        private bool _disposed;
        private Action<ModelBuilder>? _onModelCreating;

        public DatabaseContext(
            string fileName,
            Action<ModelBuilder>? onModelCreating = null)
        {
            _fileName = fileName;
            _onModelCreating = onModelCreating;
        }

        public DbSet<T> Data { get; set; } = default!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite($"Data Source={_fileName}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            _onModelCreating?.Invoke(modelBuilder);
        }

        public override async ValueTask DisposeAsync()
        {
            await base.DisposeAsync();

            if (!_disposed)
            {
                if (File.Exists(_fileName))
                {
                    try
                    {
                        File.Delete(_fileName);
                    }
                    catch
                    {
                        // we will ignore if we cannot delete it.
                    }
                }

                _disposed = true;
            }
        }
    }
}
