using System;
using HotChocolate;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Spatial.Demo
{
    public class Startup
    {
        private const string CONNECTION_STRING =
            "Database=opensgid;Host=opensgid.agrc.utah.gov;Username=agrc;Password=agrc";

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddPooledDbContextFactory<ApplicationDbContext>(
                    options => options
                        .UseNpgsql(CONNECTION_STRING, o => o.UseNetTopologySuite())
                        .LogTo(Console.WriteLine))
                .AddGraphQLServer()
                .AddFiltering()
                .AddProjections()
                .AddSpatialTypes()
                .AddSpatialProjections()
                .AddSpatialFiltering()
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
