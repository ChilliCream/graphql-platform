using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace GreenDonut.Data.TestContext;

public class AnimalContext(string connectionString) : DbContext
{
    public DbSet<Owner> Owners { get; set; } = null!;
    public DbSet<Animal> Pets { get; set; } = null!;
    public DbSet<Dog> Dogs { get; set; } = null!;
    public DbSet<Cat> Cats { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseNpgsql(connectionString);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Owner>()
            .HasMany(o => o.Pets)
            .WithOne(t => t.Owner)
            .HasForeignKey(t => t.OwnerId)
            .HasPrincipalKey(o => o.Id);

        modelBuilder.Entity<Animal>()
            .HasDiscriminator<string>("AnimalType")
            .HasValue<Dog>("Dog")
            .HasValue<Cat>("Cat");

        base.OnModelCreating(modelBuilder);
    }
}

public class Owner
{
    public int Id { get; set; }

    [MaxLength(100)]
    public required string Name { get; set; }

    public List<Animal> Pets { get; set; } = [];
}

public abstract class Animal
{
    public int Id { get; set; }

    [MaxLength(100)]
    public required string Name { get; set; }

    public int OwnerId { get; set; }

    public Owner? Owner { get; set; }
}

public class Dog : Animal
{
    public bool IsBarking { get; set; }
}

public class Cat : Animal
{
    public bool IsPurring { get; set; }
}
