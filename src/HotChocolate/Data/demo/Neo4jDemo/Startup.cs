using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Data.Neo4J;
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
            public int? YearReleased { get; set; }
            public string Plot { get; set; }
            public double? ImdbRating { get; set; }

            //[Relationship("ACTED_IN", RelationshipDirection.Incoming)]
            //public List<Actor> Actors { get; set; }
        }

        public class Actor
        {
            public string Name { get; set; }

            //[Relationship("ACTED_IN", RelationshipDirection.Outgoing)]
            public List<Movie> ActedIn { get; set; }
        }


        public class Query
        {
            public async Task<Movie> GetMovies()
            {
                Movie movies;

                IDriver driver = GraphDatabase.Driver("bolt://localhost:7687", AuthTokens.Basic("neo4j", "test123"));
                IAsyncSession session = driver.AsyncSession(o => o.WithDatabase("neo4j"));

                try
                {
                    var cursor = await session.RunAsync(@"
                        MATCH (m:Movie)
                        RETURN m
                    ");

                    movies = await cursor.MapSingleAsync<Movie>();
                }
                finally
                {
                    await session.CloseAsync();
                }

                await driver.CloseAsync();

                return movies;
            }
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services
                //.AddSingleton(_ => CreateDriver())

                .AddGraphQLServer()
                .AddDocumentFromString(@"
                    type Query {
                        movies: Movie
                    }
                    type Movie {
                        title: String
                        yearReleased: Int
                        plot: String
                        imdbRating: Float
                    }
                ")
                .BindComplexType<Query>()
                .BindComplexType<Movie>();
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
