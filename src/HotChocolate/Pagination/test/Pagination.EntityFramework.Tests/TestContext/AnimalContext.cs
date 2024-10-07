using System.ComponentModel.DataAnnotations;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;

namespace HotChocolate.Data.TestContext;

public class AnimalContext(string connectionString) : DbContext
{
    public DbSet<Owner> Owners { get; set; }
    public DbSet<Animal> Pets { get; set; }
    public DbSet<Dog> Dogs { get; set; }
    public DbSet<Cat> Cats { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseNpgsql(connectionString);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Owner>()
            .HasMany<Animal>(o => o.Pets)
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

    public List<Animal> Pets { get; set; } = new();
}

[InterfaceType]
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
