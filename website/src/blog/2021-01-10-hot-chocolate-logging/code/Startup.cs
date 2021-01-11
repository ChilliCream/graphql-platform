using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace logging
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
            services
                .AddRouting()
                .AddGraphQLServer()
                .AddQueryType<Query>()
                .AddDiagnosticEventListener(sp =>
                    new ConsoleQueryLogger
                        (sp.GetApplicationService<ILogger<ConsoleQueryLogger>>()))
                .AddDiagnosticEventListener(sp =>
                    new MiniProfilerQueryLogger());
            services.AddMiniProfiler(options => { options.RouteBasePath = "/profiler"; });
        }

        public void Configure(IApplicationBuilder app,
            IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseRouting();
            app.UseMiniProfiler();
            app.UseEndpoints(endpoints => { endpoints.MapGraphQL(); });
        }
    }
}