using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Execution;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Data
{
    public class UseDbContextTests
    {
        [Fact]
        public async Task Execute_Queryable()
        {
            // arrange
            IServiceProvider services =
                new ServiceCollection()
                    .AddPooledDbContextFactory<BookContext>(
                        b => b.UseInMemoryDatabase("Data Source=books.db"))
                    .AddGraphQL()
                    .AddQueryType<Query>()
                    .Services
                    .BuildServiceProvider();

            IRequestExecutor executor =
                await services.GetRequiredService<IRequestExecutorResolver>()
                    .GetRequestExecutorAsync();

            IDbContextFactory<BookContext> contextFactory =
                services.GetRequiredService<IDbContextFactory<BookContext>>();

            await using (BookContext context = contextFactory.CreateDbContext())
            {
                await context.Authors.AddAsync(new Author { Name = "foo" });
                await context.SaveChangesAsync();
            }

            // act
            IExecutionResult result = await executor.ExecuteAsync("{ authors { name } }");

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Execute_Single()
        {
            // arrange
            IServiceProvider services =
                new ServiceCollection()
                    .AddPooledDbContextFactory<BookContext>(
                        b => b.UseInMemoryDatabase("Data Source=books.db"))
                    .AddGraphQL()
                    .AddQueryType<Query>()
                    .Services
                    .BuildServiceProvider();

            IRequestExecutor executor =
                await services.GetRequiredService<IRequestExecutorResolver>()
                    .GetRequestExecutorAsync();

            IDbContextFactory<BookContext> contextFactory =
                services.GetRequiredService<IDbContextFactory<BookContext>>();

            await using (BookContext context = contextFactory.CreateDbContext())
            {
                await context.Authors.AddAsync(new Author { Name = "foo" });
                await context.SaveChangesAsync();
            }

            // act
            IExecutionResult result = await executor.ExecuteAsync("{ author { name } }");

            // assert
            result.ToJson().MatchSnapshot();
        }
    }

    public class Query
    {
        [UseDbContext(typeof(BookContext))]
        public IQueryable<Author> GetAuthors([ScopedService]BookContext context) =>
            context.Authors;

        [UseDbContext(typeof(BookContext))]
        public async Task<Author> GetAuthor([ScopedService]BookContext context) =>
            await context.Authors.FirstOrDefaultAsync();
    }

    public class BookContext : DbContext
    {
        public BookContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<Book> Books { get; set; } = default!;

        public DbSet<Author> Authors { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Author>()
                .HasMany(t => t.Books)
                .WithOne(t => t.Author!)
                .HasForeignKey(t => t.AuthorId);
        }
    }

    public class Author
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public string? Name { get; set; }

        public virtual ICollection<Book> Books { get; set; } =
            new List<Book>();
    }

    public class Book
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int AuthorId { get; set; }

        [Required]
        public string? Title { get; set; }

        public virtual Author? Author { get; set; }
    }
}
