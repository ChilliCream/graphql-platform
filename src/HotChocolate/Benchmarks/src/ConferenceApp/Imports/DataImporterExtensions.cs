using HotChocolate.ConferencePlanner.Data;
using HotChocolate.Execution.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.ConferencePlanner.Imports
{
    public static class ImportRequestExecutorBuilderExtensions
    {
        public static IRequestExecutorBuilder EnsureDatabaseIsCreated(
            this IRequestExecutorBuilder builder) =>
            builder.ConfigureSchemaAsync(async (services, builder, ct) =>
            {
                var factory =
                    services.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
                using var dbContext = factory.CreateDbContext();

                if (await dbContext.Database.EnsureCreatedAsync(ct))
                {
                    var importer = new DataImporter();
                    await importer.LoadDataAsync(dbContext);
                }
            });
    }
}