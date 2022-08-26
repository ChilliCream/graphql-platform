using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace HotChocolate.AspNetCore.Tests.Utilities.Logging;

public static class TestLoggerExtensions
{
    public static IServiceCollection AddTestLogging(this IServiceCollection services,
        ITestOutputHelper? outputHelper)
    {
        if (outputHelper is null)
        {
            return services;
        }

        return services.AddLogging(cfg =>
        {
            cfg.AddTestLogger(outputHelper);
        });
    }

    public static IWebHostBuilder ConfigureTestLogging(this IWebHostBuilder hostBuilder, ITestOutputHelper outputHelper)
    {
        return hostBuilder.ConfigureLogging(builder =>
        {
            builder.AddTestLogger(outputHelper);
        });
    }

    public static void AddTestLogger(this ILoggingBuilder builder, ITestOutputHelper testOutputHelper)
    {
        builder.Services.TryAddSingleton(testOutputHelper);

        builder.Services
            .TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, TestLoggerProvider>());
    }
}