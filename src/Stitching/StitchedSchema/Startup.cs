using System;
using System.IO;
using System.Threading.Tasks;
using HotChocolate;
using HotChocolate.AspNetCore;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Stitching;
using HotChocolate.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Demo.Stitching
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            // Setup the clients that shall be used to access the remote endpoints.
            services.AddHttpClient("customer", client => client.BaseAddress = new Uri("http://127.0.0.1:5050"));
            services.AddHttpClient("contract", client => client.BaseAddress = new Uri("http://127.0.0.1:5051"));

            services.AddSingleton<IQueryResultSerializer, JsonQueryResultSerializer>();

            services.AddStitchedSchema(builder => builder
                .AddSchemaFromHttp("customer")
                .AddSchemaFromHttp("contract")
                .AddExtensionsFromFile("./Extensions.graphql")
                .AddSchemaConfiguration(c =>
                {
                    // custom resolver that depends on data from a remote schema.
                    c.BindResolver(context =>
                    {
                        var obj = context.Parent<OrderedDictionary>();
                        return obj["name"] + "_" + obj["id"];
                    })
                    .To("Customer", "foo");
                }));
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseGraphQL();
            app.UsePlayground();
        }
    }
}
