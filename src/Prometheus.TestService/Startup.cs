using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Prometheus;
using Prometheus.AspNet;
using Prometheus.Execution;
using Prometheus.Resolvers;

namespace GraphQL.TestService
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<ISchema>(sp => CreateSchema());
            services.AddSingleton<IDocumentExecuter, DocumentExecuter>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseGraphQL();
        }

        private ISchema CreateSchema()
        {
            return Schema.Create(
               @"
                interface Z {
                    a: String!
                }

                type X implements Z
                {
                    a: String!
                }

                type Query {
                    c: Z!
                }
                ",
                ConfigureResolvers
           );
        }

        private void ConfigureResolvers(IResolverBuilder builder)
        {
            builder.AddQueryType<Query>();
        }
    }

    public class Query
    {
        public object c()
        {
            return new Y();
        }
    }

    [GraphQLName("X")]
    public class Y
    {
        public string a { get; set; } = "foo";
    }
}
