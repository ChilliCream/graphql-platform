using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using HotChocolate.Data.Neo4J;
using Neo4j.Driver;

namespace Neo4jDemo
{
    public class Startup
    {

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            IAuthToken authToken = AuthTokens.Basic("neo4j", "test123");
            IDriver driver = GraphDatabase.Driver("bolt://localhost:7687", authToken);

            var context = new Neo4JContext(Assembly.GetExecutingAssembly());
            var repository = new Neo4JRepository(driver, "neo4j", context);


            services
                .AddSingleton(driver)
                .AddSingleton(repository)
                .AddGraphQLServer()
                    .AddQueryType(d => d.Name("Query"))
                        .AddType<Schema.Queries>()
                    .AddMutationType(d => d.Name("Mutation"))
                        .AddType<Schema.Mutations>()
                .AddNeo4JProjections()
                .AddNeo4JFiltering()
                .AddNeo4JSorting();
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
                endpoints.MapGraphQL("/");
            });
        }
    }
}
