using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using HotChocolate.StarWars;
using HotChocolate.Types;

namespace StarWars
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddGraphQLServer()
                    .AddStarWarsTypes()
                    .AddStarWarsRepositories()
                    .AddTypeExtension<SlowTypeExtension>()
                    .AddTypeExtension<SlowHumanTypeExtension>()
                .AddGraphQLServer("hello_world")
                    .AddQueryType(d => d
                        .Name("Query")
                        .Field("hello")
                        .Resolve("world"))
                    .AddApolloTracing()
                .AddGraphQLServer("filtering")
                    .AddQueryType<Query>()
                    .AddFiltering();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app
                .UseRouting()
                .UseEndpoints(endpoints =>
                {
                    endpoints.MapGraphQL();
                    endpoints.MapGraphQL("/hello", schemaName: "hello_world");
                    endpoints.MapGraphQL("/filtering", schemaName: "filtering");
                });
        }
    }

    [ExtendObjectType("Droid")]
    public class SlowTypeExtension
    {
        public async Task<string> SlowAsync()
        {
            await Task.Delay(600);
            return "droid";
        }
    }

    [ExtendObjectType("Human")]
    public class SlowHumanTypeExtension
    {
        public async Task<string> SlowAsync()
        {
            await Task.Delay(600);
            return "human";
        }
    }
}
