using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Voyager;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HotChocolate.Types.Spatial.Playground
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddGraphQL(
                    SchemaBuilder.New()
                        .AddDocumentFromFile("schema.graphql")
                        .AddResolver("Query", "citites", ctx => "")
                        .AddResolver("Query", "parcels", ctx => "")
                        .AddResolver("Query", "person", ctx => "")
                        .AddResolver("Person", "id", ctx => "")
                        .AddResolver("Person", "properties", ctx => "")
                        .AddResolver("Person", "type", ctx => "")
                        .AddResolver("City", "shape", ctx => "")
                        .AddResolver("City", "name", ctx => "")
                        .AddResolver("PersonProperties", "firstName", ctx => "")
                        .AddResolver("PersonProperties", "lastName", ctx => "")
                        .AddResolver("PersonProperties", "partOf", ctx => "")
                        .AddResolver("Record", "id", ctx => "")
                        .AddResolver("Record", "properties", ctx => "")
                        .AddResolver("Record", "type", ctx => "")
                        .AddResolver("RecordProperties", "relatedTo", ctx => "")
                        .AddResolver("RecordProperties", "right", ctx => "")
                        .AddResolver("CadastralParcel", "geometry", ctx => "")
                        .AddResolver("CadastralParcel", "id", ctx => "")
                        .AddResolver("CadastralParcel", "properties", ctx => "")
                        .AddResolver("CadastralParcel", "type", ctx => "")
                        .AddResolver("GeoJSONMultiSurface", "type", ctx => "")
                        .AddResolver("CadastralParcelProperties", "area", ctx => "")
                        .AddResolver("CadastralParcelProperties", "parcelNumber", ctx => "")
                        .AddResolver("CadastralParcelProperties", "records", ctx => "")
                        .ModifyOptions(x => x.RemoveUnreachableTypes = false)
                        .ModifyOptions(x => x.StrictValidation = false));
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseGraphQL()
                .UsePlayground()
                .UseVoyager();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Hello World!");
                });
            });
        }
    }
}
