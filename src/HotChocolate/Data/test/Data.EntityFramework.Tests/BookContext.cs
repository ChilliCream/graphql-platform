using Microsoft.EntityFrameworkCore;

namespace HotChocolate.Data;

public class BookContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<Book> Books { get; set; }

    public DbSet<Author> Authors { get; set; }

    public DbSet<SingleOrDefaultAuthor> SingleOrDefaultAuthors { get; set; }

    public DbSet<ZeroAuthor> ZeroAuthors { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.Entity<Author>()
            .HasMany(t => t.Books)
            .WithOne(t => t.Author)
            .HasForeignKey(t => t.AuthorId);
}
