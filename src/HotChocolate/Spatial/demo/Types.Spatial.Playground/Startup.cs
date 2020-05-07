using HotChocolate;
using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Voyager;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Types.Spatial.Playground
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddGraphQL(sp => SchemaBuilder.New()
                .AddServices(sp)
                /*.AddDocumentFromFile("schema.graphql")
                .ModifyOptions(x => x.RemoveUnreachableTypes = false)
                .ModifyOptions(x => x.StrictValidation = false)
                .Use(next => context =>
                {
                    context.Result = "";
                    return next(context);
                })*/
                //.AddType<GeoQueries>()
                // .AddType<GeoMutations>()
                .AddSpatialTypes()
                .AddType<GeoQueries>()
                .Create());
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
