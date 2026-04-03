using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Configuration;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ChilliCream.Nitro.CommandLine;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNitroServices(this IServiceCollection services)
    {
        services.TryAddSingleton<IConfigurationService, ConfigurationService>();

        services.TryAddSingleton<ISessionService, SessionService>();

        services.TryAddSingleton<IFileSystem, FileSystem>();

        services.TryAddSingleton<IEnvironmentVariableProvider, EnvironmentVariableProvider>();

        services.TryAddSingleton<IResultHolder, ResultHolder>();
        services.TryAddSingleton<IResultFormatter, JsonResultFormatter>();

        return services;
    }
}
