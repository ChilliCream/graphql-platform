using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using HotChocolate;
using Neo4j.Driver;

namespace Neo4jDemo
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
              services
                .AddSingleton(_ => GraphDatabase.Driver("bolt://localhost:7687", AuthTokens.Basic("neo4j", "pass1")))
                .AddGraphQLServer()
                .AddQueryType(d => d.Name("Query"))
                    .AddType<SpeakerQueries>()
                    .AddFiltering();    
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGraphQL();

                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("GraphQL Server Launched!");
                });
            });
        }
    }
}
