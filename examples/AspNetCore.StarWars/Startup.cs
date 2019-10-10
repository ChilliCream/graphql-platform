using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using HotChocolate;
using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Grpc;
using HotChocolate.AspNetCore.Voyager;
using HotChocolate.Execution.Configuration;
using HotChocolate.Subscriptions;
using Microsoft.AspNetCore.Http;
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
            services.AddHttpContextAccessor();

            // Add the custom services like repositories etc ...
            services.AddSingleton<CharacterRepository>();
            services.AddSingleton<ReviewRepository>();

            // Add in-memory event provider
            services.AddInMemorySubscriptionProvider();

            // Add GraphQL Services
            services.AddGraphQL(sp => SchemaBuilder.New()
                .AddServices(sp)

                // Adds the authorize directive and
                // enable the authorization middleware.
                .AddAuthorizeDirectiveType()

                .AddQueryType<QueryType>()
                .AddMutationType<MutationType>()
                .AddSubscriptionType<SubscriptionType>()
                .AddType<HumanType>()
                .AddType<DroidType>()
                .AddType<EpisodeType>()
                .Create(),
                new QueryExecutionOptions
                {
                    TracingPreference = TracingPreference.Always
                });

            // Add gRPC Services
            services.AddGrpc(options =>
            {
                options.EnableDetailedErrors = true;
            });

            // TODO: Move to test client
            // Add test for gRPC to GraphQL
            services.AddHostedService<TestGrpcToGraphqlHostedService>();

            // Add Authorization Policy
            services.AddAuthorization(options =>
            {
                options.AddPolicy("HasCountry", policy =>
                    policy.RequireAssertion(context =>
                        context.User.HasClaim(c =>
                            (c.Type == ClaimTypes.Country))));
            });

            /*
            Note: uncomment this
            section in order to simulate a user that has a country claim and
            passes the configured authorization rule.

            services.AddQueryRequestInterceptor((ctx, builder, ct) =>
            {
                var identity = new ClaimsIdentity("abc");
                identity.AddClaim(new Claim(ClaimTypes.Country, "us"));
                ctx.User = new ClaimsPrincipal(identity);
                builder.SetProperty(nameof(ClaimsPrincipal), ctx.User);
                return Task.CompletedTask;
            });
            */
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // gRPC clients need to use HTTPS to call the server - https://docs.microsoft.com/en-us/aspnet/core/tutorials/grpc/grpc-start?view=aspnetcore-3.0&tabs=visual-studio#run-the-service
            app.UseHttpsRedirection();

            app.UseRouting();

            app
                .UseWebSockets()
                // TODO: When use gRPC services via endpoints.MapGrpcService<GraphqlGrpcService>() than not working routing for GraphQL/GraphiQL/Playground/Voyager
                // More info here: https://hotchocolategraphql.slack.com/archives/CD9TNKT8T/p1570112005410200
                .UseGraphQL("/graphql")
                .UseGraphiQL("/graphql")
                .UsePlayground("/graphql")
                .UseVoyager("/graphql");

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGrpcService<GraphqlGrpcService>();
            });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync($"{nameof(StarWars)} - HotChocolate GraphQL API Server");
                });
            });
        }
    }
}
