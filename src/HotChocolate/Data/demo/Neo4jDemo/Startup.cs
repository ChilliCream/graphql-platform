using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using HotChocolate.Data.Neo4J.Language;
using Neo4j.Driver;

namespace Neo4jDemo
{
    public class Startup
    {
        public class Movie
        {
            public string Title { get; set; }
            public string YearReleased { get; set; }
            public string Plot { get; set; }
            public double ImdbRating { get; set; }

            [Relationship("ACTED_IN", RelationshipDirection.Incoming)]
            public List<Actor> Actors { get; set; }
        }

        public class Actor
        {
            public string Name { get; set; }

            [Relationship("ACTED_IN", RelationshipDirection.Outgoing)]
            public List<Movie> ActedIn { get; set; }
        }

        public class Query
        {
            public async Task<List<Person>> GetPeople()
            {
                List<Person> people = new();
                //Node node = Cypher.Node("Person").Named("p");
                //StatementBuilder statement = Cypher.Match(book);

                IDriver driver = GraphDatabase.Driver("bolt://localhost:7687", AuthTokens.Basic("neo4j", "test123"));
                IAsyncSession session = driver.AsyncSession(o => o.WithDatabase("neo4j"));
                try
                {
                    IResultCursor cursor = await session.RunAsync("MATCH (p:Person) RETURN p { .name} ");
                    var test = await cursor.ToListAsync();
                    people = await cursor.ToListAsync(record => new Person() {Name = record["name"].As<string>()});

                }
                finally
                {
                    await session.CloseAsync();
                }

                await driver.CloseAsync();
                return people;
            }
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services
                //.AddSingleton(_ => CreateDriver())
                .AddGraphQLServer();
                .AddQueryType<Query>();
        }

        private IDriver CreateDriver()
        {
            return GraphDatabase.Driver("bolt://localhost:7687", AuthTokens.Basic("neo4j", "password"));
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
