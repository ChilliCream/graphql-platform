using Microsoft.EntityFrameworkCore;

namespace HotChocolate.Data.Migrations;

public interface IDbSeeder<in TContext> where TContext : DbContext
{
    Task SeedAsync(TContext context);
}
