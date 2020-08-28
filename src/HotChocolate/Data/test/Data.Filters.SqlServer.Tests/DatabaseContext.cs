using System;
using Microsoft.EntityFrameworkCore;
using Squadron;

namespace HotChocolate.Data.Filters
{
    public class DatabaseContext<T> : DbContext
        where T : class
    {
        private readonly SqlServerResource _resource;

        public DatabaseContext(SqlServerResource resource)
        {
            _resource = resource;
        }

        public DbSet<T> Data { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var connectionString = _resource.CreateConnectionString("database_" + Guid.NewGuid());
            optionsBuilder.UseSqlServer(connectionString);
        }
    }
}
