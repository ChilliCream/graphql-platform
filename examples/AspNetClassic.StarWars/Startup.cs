using System;
using HotChocolate;
using HotChocolate.AspNetClassic;
using HotChocolate.AspNetClassic.Authorization;
using HotChocolate.AspNetClassic.GraphiQL;
using HotChocolate.AspNetClassic.Playground;
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
            services.AddGraphQL(sp => Schema.Create(c =>
            {
                c.RegisterServiceProvider(sp);

                // Adds the authorize directive and
                // enable the authorization middleware.
                c.RegisterAuthorizeDirectiveType();

                c.RegisterQueryType<QueryType>();
                c.RegisterMutationType<MutationType>();

                c.RegisterType<HumanType>();
                c.RegisterType<DroidType>();
                c.RegisterType<EpisodeType>();
            }));

            // Add Authorization Policy
            //services.AddAuthorization(options =>
            //{
            //    options.AddPolicy("HasCountry", policy =>
            //        policy.RequireAssertion(context =>
            //            context.User.HasClaim(c =>
            //                (c.Type == ClaimTypes.Country))));
            //});

            return services.BuildServiceProvider();
        }

        public void Configuration(IAppBuilder appBuilder)
        {
            IServiceProvider services = ConfigureServices();

            appBuilder
                .UseGraphQL(services, new PathString("/graphql"))
                .UseGraphiQL(new PathString("/graphql"))
                .UsePlayground(new PathString("/graphql"));
        }
    }
}
