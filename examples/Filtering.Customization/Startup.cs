using HotChocolate;
using HotChocolate.AspNetCore;
using HotChocolate.Types;
using HotChocolate.Types.Filters.Conventions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NetTopologySuite.Geometries;

namespace Filtering.Customization
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            // Add GraphQL Services
            services.AddGraphQL(sp => SchemaBuilder.New()
                .AddServices(sp)
                .AddConvention<IFilterConvention>(new FilterConvention(x => x.UseGeometryFilter()))
                .AddQueryType(d => d.Name("Query"))
                .AddType(new InputObjectType<Geometry>(x => x.BindFieldsExplicitly().Field("Area").Type<StringType>()))
                .AddType(new ObjectType<Geometry>(x => x.BindFieldsExplicitly().Field(x => x.Area)))
                .AddType<TouristAttractionQueries>()
                .Create());
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app
                .UseRouting()
                .UseWebSockets()
                .UsePlayground()
                .UseGraphQL("/graphql");
        }
    }
}
