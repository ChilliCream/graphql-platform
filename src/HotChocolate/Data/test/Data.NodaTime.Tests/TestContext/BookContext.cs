using Microsoft.EntityFrameworkCore;

namespace HotChocolate.Data.NodaTime.TestContext;

public sealed class BookContext(string connectionString) : DbContext
{
    public DbSet<Author> Authors { get; set; } = null!;

    public DbSet<Book> Books { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(connectionString, o => o.UseNodaTime());
    }
}
