using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data;

public class InterfaceFieldFilterAndSortTests : IDisposable
{
    private readonly string _fileName = Guid.NewGuid().ToString("N") + ".db";

    [Fact]
    public async Task Interface_Field_Filter_With_Database()
    {
        // arrange
        await SeedAsync();

        var executor = await new ServiceCollection()
            .AddDbContext<AnimalDbContext>(
                b => b.UseSqlite("Data Source=" + _fileName))
            .AddGraphQL()
            .AddFiltering()
            .AddSorting()
            .AddProjections()
            .AddQueryType<AnimalQuery>()
            .AddObjectType<Dog>()
            .AddObjectType<Cat>()
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                animals(where: { name: { startsWith: "Dog" } }) {
                    name
                }
            }
            """);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Interface_Field_Sort_With_Database()
    {
        // arrange
        await SeedAsync();

        var executor = await new ServiceCollection()
            .AddDbContext<AnimalDbContext>(
                b => b.UseSqlite("Data Source=" + _fileName))
            .AddGraphQL()
            .AddFiltering()
            .AddSorting()
            .AddProjections()
            .AddQueryType<AnimalQuery>()
            .AddObjectType<Dog>()
            .AddObjectType<Cat>()
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                animals(order: { name: DESC }) {
                    name
                }
            }
            """);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Interface_Field_Filter_And_Sort_With_Database()
    {
        // arrange
        await SeedAsync();

        var executor = await new ServiceCollection()
            .AddDbContext<AnimalDbContext>(
                b => b.UseSqlite("Data Source=" + _fileName))
            .AddGraphQL()
            .AddFiltering()
            .AddSorting()
            .AddProjections()
            .AddQueryType<AnimalQuery>()
            .AddObjectType<Dog>()
            .AddObjectType<Cat>()
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                animals(
                    where: { name: { contains: "Cat" } }
                    order: { name: ASC }
                ) {
                    name
                }
            }
            """);

        // assert
        result.MatchSnapshot();
    }

    private async Task SeedAsync()
    {
        await using var context = new AnimalDbContext(
            new DbContextOptionsBuilder<AnimalDbContext>()
                .UseSqlite("Data Source=" + _fileName)
                .Options);

        await context.Database.EnsureCreatedAsync();

        context.Animals.AddRange(
            new Dog { Name = "Dog Charlie", IsBarking = true },
            new Cat { Name = "Cat Alice", IsPurring = true },
            new Dog { Name = "Dog Bob", IsBarking = false },
            new Cat { Name = "Cat Daisy", IsPurring = false });

        await context.SaveChangesAsync();
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();

        if (File.Exists(_fileName))
        {
            File.Delete(_fileName);
        }
    }

    public class AnimalQuery
    {
        [UseFiltering]
        [UseSorting]
        public IQueryable<Animal> GetAnimals(AnimalDbContext context)
            => context.Animals;
    }

    public class AnimalDbContext(DbContextOptions<AnimalDbContext> options) : DbContext(options)
    {
        public DbSet<Animal> Animals { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Animal>()
                .HasDiscriminator<string>("AnimalType")
                .HasValue<Dog>("Dog")
                .HasValue<Cat>("Cat");
        }
    }

    [InterfaceType]
    public abstract class Animal
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string Name { get; set; } = default!;
    }

    public class Dog : Animal
    {
        public bool IsBarking { get; set; }
    }

    public class Cat : Animal
    {
        public bool IsPurring { get; set; }
    }
}
