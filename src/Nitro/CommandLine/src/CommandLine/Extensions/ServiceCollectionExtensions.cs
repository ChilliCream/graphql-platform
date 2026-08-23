using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Configuration;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Memory;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ChilliCream.Nitro.CommandLine;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNitroServices(this IServiceCollection services)
    {
        services.AddHttpClient();

        services.TryAddSingleton<IConfigurationService, ConfigurationService>();

        services.TryAddSingleton<ISessionService, SessionService>();

        services.TryAddSingleton<IFileSystem, FileSystem>();

        services.TryAddSingleton<IEnvironmentVariableProvider, EnvironmentVariableProvider>();

        services.TryAddSingleton<IResultHolder, ResultHolder>();
        services.TryAddSingleton<IResultFormatter, JsonResultFormatter>();

        services.TryAddSingleton<IBrowserLauncher, SystemBrowserLauncher>();

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<AgentDatabase>();
        services.TryAddSingleton<IAgentRegistry, AgentRegistry>();
        services.TryAddSingleton<IGlobalConfigDirectoryProvider, GlobalConfigDirectoryProvider>();
        services.TryAddSingleton<INitroInstanceIdProvider, NitroInstanceIdProvider>();
        services.TryAddSingleton<IProcessInfoProvider, ProcessInfoProvider>();
        services.TryAddSingleton<IClaudeAncestorSessionResolver, ClaudeAncestorSessionResolver>();
        services.TryAddSingleton<IAgentSessionRegistry, AgentSessionRegistry>();
        services.TryAddSingleton<IClaudeSessionActivityReader, ClaudeSessionActivityReader>();
        services.TryAddSingleton<ITaskStore, TaskStore>();
        services.TryAddSingleton<IMailStore, MailStore>();
        services.TryAddSingleton<IGlobalMemoryDirectoryProvider, GlobalMemoryDirectoryProvider>();
        services.TryAddSingleton<IMemoryStore>(sp => new MemoryStore(
            sp.GetRequiredService<IFileSystem>(),
            sp.GetRequiredService<TimeProvider>(),
            sp.GetRequiredService<IGlobalMemoryDirectoryProvider>().GetDirectory()));

        return services;
    }
}
