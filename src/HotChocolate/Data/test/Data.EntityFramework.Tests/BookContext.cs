using Microsoft.EntityFrameworkCore;

namespace HotChocolate.Data;

public class BookContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<Book> Books { get; set; } = default!;

    public DbSet<Author> Authors { get; set; } = default!;

    public DbSet<SingleOrDefaultAuthor> SingleOrDefaultAuthors { get; set; } = default!;

    public DbSet<ZeroAuthor> ZeroAuthors { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.Entity<Author>()
            .HasMany(t => t.Books)
            .WithOne(t => t.Author!)
            .HasForeignKey(t => t.AuthorId);
}
