using System;
using HotChocolate;
using HotChocolate.AspNetClassic;
using HotChocolate.AspNetClassic.Voyager;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Owin;
using Owin;
using StarWars.Data;
using StarWars.Types;

namespace StarWars
{
    public class Startup
    {
        public IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            // Add the custom services like repositories etc ...
            services.AddSingleton<CharacterRepository>();
            services.AddSingleton<ReviewRepository>();

            services.AddSingleton<Query>();
            services.AddSingleton<Mutation>();

            // Add GraphQL Services
            services.AddGraphQL(sp => SchemaBuilder.New()
                .AddServices(sp)

                // Adds the authorize directive and
                // enable the authorization middleware.
                .AddAuthorizeDirectiveType()

                .AddQueryType<QueryType>()
                .AddMutationType<MutationType>()
                .AddType<HumanType>()
                .AddType<DroidType>()
                .AddType<EpisodeType>()
                .Create(),
                new QueryExecutionOptions
                {
                    TracingPreference = TracingPreference.Always
                });

            // Add Authorization Policy
            //services.AddAuthorization(options =>
            //{
            //    options.AddPolicy("HasCountry", policy =>
            //        policy.RequireAssertion(context =>
            //            context.User.HasClaim(c =>
            //                (c.Type == ClaimTypes.Country))));
            //});

            /*
            Note: Intercept and enrich query requests

            services.AddQueryRequestInterceptor((ctx, builder, ct) =>
            {
                var identity = new ClaimsIdentity("abc");
                identity.AddClaim(new Claim(ClaimTypes.Country, "us"));
                ctx.User = new ClaimsPrincipal(identity);
                builder.SetProperty(nameof(ClaimsPrincipal), ctx.User);
                return Task.CompletedTask;
            });
            */

            return services.BuildServiceProvider();
        }

        public void Configuration(IAppBuilder appBuilder)
        {
            IServiceProvider services = ConfigureServices();

            appBuilder
                .UseGraphQL(services, new PathString("/graphql"))
                .UseGraphiQL(new PathString("/graphql"))
                .UsePlayground(new PathString("/graphql"))
                .UseVoyager(new PathString("/graphql"));
            ;
        }
    }
}
