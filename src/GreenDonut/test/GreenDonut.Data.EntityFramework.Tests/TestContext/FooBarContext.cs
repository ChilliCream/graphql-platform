using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace GreenDonut.Data.TestContext;

public class FooBarContext(string connectionString) : DbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(connectionString);
    }

    public DbSet<Foo> Foos { get; set; } = null!;

    public DbSet<Bar> Bars { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Foo>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Bar)
                .WithMany()
                .HasForeignKey("BarId");
        });

        modelBuilder.Entity<Bar>(entity =>
        {
            entity.HasKey(e => e.Id);
        });
    }
}

public class Foo
{
    public int Id { get; set; }

    [MaxLength(100)]
    public string Name { get; set; } = null!;

    public int? BarId { get; set; }

    public Bar? Bar { get; set; }
}

public class Bar
{
    public int Id { get; set; }

    [MaxLength(100)]
    public string? Description { get; set; }

    [MaxLength(100)]
    public string SomeField1 { get; set; } = null!;

    [MaxLength(100)]
    public string? SomeField2 { get; set; }
}
