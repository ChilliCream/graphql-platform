using Microsoft.EntityFrameworkCore;

namespace GreenDonut.Data.TestContext;

public class NullableTestsContext(Provider provider, string connectionString) : DbContext
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
    }

    public DbSet<Record> Records { get; set; } = null!;
}

public class Record
{
    public Guid Id { get; set; }
    public DateOnly? Date { get; set; }
    public TimeOnly? Time { get; set; }
    public string? String { get; set; }
}

public enum Provider
{
    PostgreSql,
    SqlServer
}
