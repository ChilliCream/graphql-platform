using Microsoft.EntityFrameworkCore;

namespace HotChocolate.Data;

public class DatabaseContext<T> : DbContext
    where T : class
{
    public DatabaseContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<T> Data { get; set; } = default!;
}
