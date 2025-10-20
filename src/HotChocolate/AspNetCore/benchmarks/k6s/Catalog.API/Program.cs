var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddDbContextPool<CatalogContext>(o => o.UseNpgsql(builder.Configuration.GetConnectionString("catalog-db")))
    .AddMigration<CatalogContext, CatalogContextSeed>();

builder.Services
    .AddScoped<BrandService>()
    .AddScoped<ProductService>();

builder
    .AddGraphQL()
    .AddCatalogTypes()
    .AddPagingArguments()
    .AddQueryContext()
    .AddSorting()
    .AddFiltering();

var app = builder.Build();

app.MapGraphQL();

app.RunWithGraphQLCommands(args);
