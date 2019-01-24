using HotChocolate.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Server
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            // If you need dependency injection with your query object add your query type as a services.
            // services.AddSingleton<Query>();

            // enable InMemory messaging services for subscription support.
            // var inMemoryEventRegistry = new InMemoryEventRegistry();
            // services.AddSingleton<IEventRegistry>(inMemoryEventRegistry);
            // services.AddSingleton<IEventSender>(inMemoryEventRegistry);

            // Add GraphQL Services
            services.AddGraphQL(sp => Schema.Create(c =>
            {
                // enable for authorization support
                // c.RegisterAuthorizeDirectiveType();
                c.RegisterQueryType<Query>();
            }));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // enable this if you want tu support subscription
            // app.UseWebSockets();
            app.UseGraphQL();
            app.UseGraphiQL();
        }
    }
}
