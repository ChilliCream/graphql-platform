using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Voyager;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types.Filters.Conventions;
using HotChocolate.Types.Spatial.Filters.Expressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HotChocolate.Types.Spatial.Playground
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddGraphQL(sp => SchemaBuilder.New()
                .AddServices(sp)
                .AddConvention<IFilterConvention>(
                    new FilterConvention(x => x.UseSpatialFilters())
                )
                .AddSpatialTypes()
                .AddQueryType<GeoFilterType>()
                .Create(),
                new QueryExecutionOptions
                {
                    IncludeExceptionDetails = true
                });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app
                .UseWebSockets()
                .UseGraphQL("/graphql")
                .UsePlayground("/graphql", "/graphql")
                .UseVoyager("/graphql");
        }
    }
}
