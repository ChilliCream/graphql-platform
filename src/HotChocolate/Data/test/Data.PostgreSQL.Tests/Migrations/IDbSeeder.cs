using Microsoft.EntityFrameworkCore;

namespace eShop.Catalog.Migrations;

public interface IDbSeeder<in TContext> where TContext : DbContext
{
    Task SeedAsync(TContext context);
}
