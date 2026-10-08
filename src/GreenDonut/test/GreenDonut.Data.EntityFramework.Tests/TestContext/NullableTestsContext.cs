using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace GreenDonut.Data.TestContext;

public class NullableTestsContext(
    Provider provider,
    string connectionString,
    IEnumerable<IInterceptor>? interceptors = null) : DbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        switch (provider)
        {
            case Provider.PostgreSql:
                optionsBuilder.UseNpgsql(connectionString);
                break;
            case Provider.SqlServer:
                optionsBuilder.UseSqlServer(connectionString);
                break;
            default:
                throw new InvalidOperationException();
        }

        if (interceptors is not null)
        {
            optionsBuilder.AddInterceptors(interceptors);
        }
    }

    public DbSet<Record> Records { get; set; } = null!;

    public DbSet<Item> Items { get; set; } = null!;
}

public class Record
{
    public Guid Id { get; set; }
    public DateOnly? Date { get; set; }
    public TimeOnly? Time { get; set; }
    public string? String { get; set; }
}

public class Item
{
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; }

    public int? DetailId { get; set; }

    public Detail? Detail { get; set; }
}

public class Detail
{
    public int Id { get; set; }

    public int Number { get; set; }

    public string Name { get; set; } = null!;
}

public enum Provider
{
    PostgreSql,
    SqlServer
}
