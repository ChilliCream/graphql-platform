using System;
using Microsoft.EntityFrameworkCore;

namespace HotChocolate.Data.Sorting
{
    public class DatabaseContext<T> : DbContext
        where T : class
    {
        public DbSet<T> Data { get; set; } = default!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite($"DataSource=database_{Guid.NewGuid():N}.db");
        }
    }
}
