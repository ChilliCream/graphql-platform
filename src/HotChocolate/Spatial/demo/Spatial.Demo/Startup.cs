using HotChocolate;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Filters.Expressions;
using HotChocolate.Data.Filters;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Spatial.Demo
{
    public class Startup
    {
        public static readonly ILoggerFactory loggerFactory =
            LoggerFactory.Create(x => x.AddConsole());

        private const string connectionString =
            "Database=opensgid;Host=opensgid.agrc.utah.gov;Username=agrc;Password=agrc";

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContextPool<ApplicationDbContext>(
                    options =>
                        options.UseNpgsql(
                                connectionString,
                                o =>
                                    o.UseNetTopologySuite())
                            .UseLoggerFactory(loggerFactory)
                )
                .AddGraphQLServer()
                .AddSpatialTypes()
                .AddFiltering(
                    x => x
                        .AddDefaults()
                        .AddSpatialOperations()
                        .BindSpatialTypes()
                        .Provider(
                            new QueryableFilterProvider(
                                p => p.AddSpatialHandlers().AddDefaultFieldHandlers())))
                .AddQueryType<Query>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints => endpoints.MapGraphQL());
        }
    }
}
