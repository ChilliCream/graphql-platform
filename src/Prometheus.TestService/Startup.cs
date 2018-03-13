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
               interface X {
                   a: String!
               }
                type Foo implements X
                {
                    a: String!
                    b(z: Boolean = true): String
                    c: Int
                }

                type Test {
                    z: String!
                }

                type Query {
                    getFoo: Foo 
                    getTest: Test
                }
                ",
                ConfigureResolvers
           );
        }

        private void ConfigureResolvers(IResolverBuilder builder)
        {
            builder.AddQueryType<Query>()
                .Add("Query", "getTest", () => 66)
                .AddType<FooXyz>("Foo")
                .Add("Foo", "b", () => "hello")
                .Add("Foo", "c", () => 1)
                .AddType("Test", new {
                    z = "world"
                });
        }
    }

    public class Query
    {
        public object GetFoo()
        {
            return new object();
        }
    }

    public class FooXyz
    {
        [GraphQLName("a")]
        public string GetA()
        {
            return "a";
        }
    }
}
