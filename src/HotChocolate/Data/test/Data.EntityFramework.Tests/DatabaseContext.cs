using Microsoft.EntityFrameworkCore;

namespace HotChocolate.Data;

public class DatabaseContext<T>(DbContextOptions options) : DbContext(options)
    where T : class
{
    public DbSet<T> Data { get; set; } = null!;
}
