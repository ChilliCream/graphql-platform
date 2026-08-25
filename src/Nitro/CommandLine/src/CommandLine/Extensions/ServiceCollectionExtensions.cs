using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Configuration;
using ChilliCream.Nitro.CommandLine.Services.Hook;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Memory;
using ChilliCream.Nitro.CommandLine.Services.Notify;
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
        services.TryAddSingleton<ICodexAncestorSessionResolver, CodexAncestorSessionResolver>();
        services.TryAddSingleton<ICopilotAncestorSessionResolver, CopilotAncestorSessionResolver>();
        services.TryAddSingleton<IClaudeHarnessVersionResolver, ClaudeHarnessVersionResolver>();
        services.TryAddSingleton<ICodexHarnessVersionResolver, CodexHarnessVersionResolver>();
        services.TryAddSingleton<ICopilotHarnessVersionResolver, CopilotHarnessVersionResolver>();
        services.TryAddSingleton<IAgentSessionRegistry, AgentSessionRegistry>();
        services.TryAddSingleton<ISessionDeliveryLedger, SessionDeliveryLedger>();
        services.TryAddSingleton<IPingLeaseStore, PingLeaseStore>();
        services.TryAddSingleton<IPingWorkerLauncher, PingWorkerLauncher>();
        services.TryAddSingleton<IClaudePeerClient, ClaudePeerClient>();
        services.TryAddSingleton<IPingSessionExecutor, PingSessionExecutor>();
        services.TryAddSingleton<IMailWakeBatchStore, MailWakeBatchStore>();
        services.TryAddSingleton<ISessionPingGateStore, SessionPingGateStore>();
        services.TryAddSingleton<ISessionGateCoordinator, SessionGateCoordinator>();
        services.TryAddSingleton<IActorWakeDispatcher, ActorWakeDispatcher>();
        services.TryAddSingleton<IMailWakeReceiptObserver, MailWakeReceiptObserver>();
        services.TryAddSingleton<INotifier, Notifier>();
        services.TryAddSingleton<IClaudeHookHandler, ClaudeHookHandler>();
        services.TryAddSingleton<ICodexQueueClient, CodexQueueClient>();
        services.TryAddSingleton<ICodexForeignNotifyRunner, CodexForeignNotifyRunner>();
        services.TryAddSingleton<ICodexHookHandler, CodexHookHandler>();
        services.TryAddSingleton<ICopilotHookHandler, CopilotHookHandler>();
        services.TryAddSingleton<IClaudeSessionActivityReader, ClaudeSessionActivityReader>();
        services.TryAddSingleton<ILaunchDescriptorResolver, LaunchDescriptorResolver>();
        services.TryAddSingleton<IClaudeSettingsPathResolver, ClaudeSettingsPathResolver>();
        services.TryAddSingleton<IClaudeHooksSidecarStore, ClaudeHooksSidecarStore>();
        services.TryAddSingleton<IClaudeHooksInstallerService, ClaudeHooksInstallerService>();
        services.TryAddSingleton<ICodexPathResolver, CodexPathResolver>();
        services.TryAddSingleton<ICodexHooksSidecarStore, CodexHooksSidecarStore>();
        services.TryAddSingleton<ICodexHooksInstallerService, CodexHooksInstallerService>();
        services.TryAddSingleton<ICopilotPathResolver, CopilotPathResolver>();
        services.TryAddSingleton<ICopilotHooksSidecarStore, CopilotHooksSidecarStore>();
        services.TryAddSingleton<ICopilotHooksInstallerService, CopilotHooksInstallerService>();
        services.TryAddSingleton<ICopilotExtensionPathResolver, CopilotExtensionPathResolver>();
        services.TryAddSingleton<ICopilotExtensionInstallerService, CopilotExtensionInstallerService>();
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
