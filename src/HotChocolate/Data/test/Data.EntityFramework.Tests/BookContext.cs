using Microsoft.EntityFrameworkCore;

namespace HotChocolate.Data
{
    public class BookContext : DbContext
    {
        public BookContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<Book> Books { get; set; } = default!;

        public DbSet<Author> Authors { get; set; } = default!;

        public DbSet<SingleOrDefaultAuthor> SingleOrDefaultAuthors { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Author>()
                .HasMany(t => t.Books)
                .WithOne(t => t.Author!)
                .HasForeignKey(t => t.AuthorId);
        }
    }
}
