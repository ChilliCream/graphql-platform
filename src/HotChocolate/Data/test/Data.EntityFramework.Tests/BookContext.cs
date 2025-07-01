using Microsoft.EntityFrameworkCore;

namespace HotChocolate.Data;

public class BookContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<Book> Books { get; set; } = null!;

    public DbSet<Author> Authors { get; set; } = null!;

    public DbSet<SingleOrDefaultAuthor> SingleOrDefaultAuthors { get; set; } = null!;

    public DbSet<ZeroAuthor> ZeroAuthors { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.Entity<Author>()
            .HasMany(t => t.Books)
            .WithOne(t => t.Author!)
            .HasForeignKey(t => t.AuthorId);
}
