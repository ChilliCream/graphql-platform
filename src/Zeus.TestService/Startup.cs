using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Zeus;
using Zeus.AspNet;
using Zeus.Execution;
using Zeus.Resolvers;

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
                   a: String
               }
                type Foo
                {
                    a: String!
                    b: String
                    c: Int
                }

                type Query {
                    getFoo: Foo 
                }
                ",
                c => c.Add("Query", "getFoo", () => "1")
                   .Add("Foo", "a", () => "2")
                   .Add("Foo", "b", () => "3")
                   .Add("Foo", "c", () => "4")
           );
        }


    }

    public class PersonResolver
        : IResolver
    {
        public Task<object> ResolveAsync(IResolverContext context, CancellationToken cancellationToken)
        {
            BatchedQuery b = new BatchedQuery();
            b.PersonId = "X";
            context.RegisterQuery(b);
            return Task.FromResult<object>(new Func<object>(() => b.Person));
        }
    }

    public class BatchedQuery
        : IBatchedQuery
    {
        public string PersonId { get; set; }

        public string Person { get; set; }

        public Task ExecuteAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
