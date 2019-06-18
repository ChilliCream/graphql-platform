using System.Security.Claims;
using System.Threading.Tasks;
using HotChocolate;
using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Authorization;
using HotChocolate.AspNetCore.Voyager;
using HotChocolate.Subscriptions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using StarWars.Data;
using StarWars.Types;

namespace StarWars
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            // Add the custom services like repositories etc ...
            services.AddSingleton<CharacterRepository>();
            services.AddSingleton<ReviewRepository>();

            services.AddSingleton<Query>();
            services.AddSingleton<Mutation>();
            services.AddSingleton<Subscription>();

            // Add in-memory event provider
            var eventRegistry = new InMemoryEventRegistry();
            services.AddSingleton<IEventRegistry>(eventRegistry);
            services.AddSingleton<IEventSender>(eventRegistry);

            // Add GraphQL Services
            services.AddGraphQL(sp => SchemaBuilder.New()
                .AddServices(sp)
                .AddQueryType<QueryType>()
                .AddMutationType<MutationType>()
                .AddSubscriptionType<SubscriptionType>()
                .AddType<HumanType>()
                .AddType<DroidType>()
                .AddType<EpisodeType>()
                .AddDirectiveType<AuthorizeDirectiveType>()
                /*
                Run query:
                query {
                  droid (id:1) {
                    id
                    name
                  }
                  __schema {
                    queryType {
                      name
                      description
                    }
                  }
                }

                Results:
                {
                  "data": {
                    "droid": {
                      "id": "I see you! I have eat your results...",
                      "name": "I see you! I have eat your results..."
                    },
                    "__schema": {
                      "queryType": {
                        "name": "Query",
                        "description": null
                      }
                    }
                  }
                }

                Please notice that introspection results are not corrupted.
                Middlerware is not executing for introspection fields.

                 */
                .Use(next => context =>
                {
                    context.Result = "I see you! I have eat your results...";
                    return Task.CompletedTask;
                })
                .Create());

            // Add Authorization Policies
            services.AddAuthorization(options =>
            {
                options.AddPolicy("HasCountry", policy =>
                    policy.RequireAssertion(context =>
                        context.User.HasClaim(c =>
                            (c.Type == ClaimTypes.Country))));
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseWebSockets();
            app.UseGraphQL();
            app.UseGraphiQL();
            app.UsePlayground();
            app.UseVoyager();

            /*
            Note: comment app.UseGraphQL("/graphql"); and uncomment this
            section in order to simulare a user that has a country claim and
            passes the configured authorization rule.
            app.UseGraphQL(new QueryMiddlewareOptions
            {
                Path = "/graphql",
                OnCreateRequest = (ctx, builder, ct) =>
                {
                    var identity = new ClaimsIdentity("abc");
                    identity.AddClaim(new Claim(ClaimTypes.Country, "us"));
                    ctx.User = new ClaimsPrincipal(identity);
                    builder.SetProperty(nameof(ClaimsPrincipal), ctx.User);
                    return Task.CompletedTask;
                }
            });
            */
        }
    }
}
