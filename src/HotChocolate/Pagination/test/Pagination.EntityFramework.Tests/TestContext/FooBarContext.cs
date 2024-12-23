using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace HotChocolate.Data.TestContext;

public class FooBarContext(string connectionString) : DbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(connectionString);
    }

    public DbSet<Foo> Foos { get; set; }

    public DbSet<Bar> Bars { get; set; }

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
    public string Name { get; set; } = default!;

    public int? BarId { get; set; }

    public Bar? Bar { get; set; }
}

public class Bar
{
    public int Id { get; set; }

    [MaxLength(100)]
    public string? Description { get; set; }

    [MaxLength(100)]
    public string SomeField1 { get; set; } = default!;

    [MaxLength(100)]
    public string? SomeField2 { get; set; }
}
