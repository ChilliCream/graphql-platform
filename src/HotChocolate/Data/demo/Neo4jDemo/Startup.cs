using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using HotChocolate;

namespace Neo4jDemo
{
    public class Startup
    {
        // Book.cs
        public class Book
        {
            public string Title { get; set; }

            public string Author { get; set; }

        }

        // Query.cs
        public class Query
        {
            public Book GetBook() => new Book { Title = "C# in depth", Author = "Jon Skeet" };
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddGraphQLServer()
                .AddDocumentFromString(@"
                    type Query {
                      book(id: String): Book
                    }

                    type Book {
                      title: String
                      author: String
                    }
                ")
                .BindComplexType<Query>();
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
                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Hello World!");
                });
                endpoints.MapGraphQL();
            });
        }
    }
}
