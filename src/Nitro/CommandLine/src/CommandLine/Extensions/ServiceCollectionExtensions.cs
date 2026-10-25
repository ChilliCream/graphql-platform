using ChilliCream.Nitro.CommandLine.Commands.Agent.Hooks.Opencode;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Configuration;
using ChilliCream.Nitro.CommandLine.Services.Hook;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Memory;
using ChilliCream.Nitro.CommandLine.Services.Notify;
using ChilliCream.Nitro.CommandLine.Services.Preferences;
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

        services.TryAddSingleton<IStandardInputReader, StandardInputReader>();

        services.TryAddSingleton<IResultHolder, ResultHolder>();
        services.TryAddSingleton<IResultFormatter, JsonResultFormatter>();

        services.TryAddSingleton<IBrowserLauncher, SystemBrowserLauncher>();

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<AgentDatabase>();
        services.TryAddSingleton<IAgentStore, AgentStore>();
        services.TryAddSingleton<IGlobalConfigDirectoryProvider, GlobalConfigDirectoryProvider>();
        services.TryAddSingleton<IClaudeSessionFileReader, ClaudeSessionFileReader>();
        services.TryAddKeyedSingleton<IClaudeSessionFileReader, VentiPeerRegistryReader>(AgentSessionHarness.Venti);
        services.TryAddSingleton<ICodexHarnessVersionResolver, CodexHarnessVersionResolver>();
        services.TryAddSingleton<IActingActorResolver, ActingActorResolver>();
        services.TryAddSingleton<ITakeoverLedger, TakeoverLedger>();
        services.TryAddSingleton<IAgentDeliveryLedger, AgentDeliveryLedger>();
        services.TryAddSingleton<IPingLeaseStore, PingLeaseStore>();
        services.TryAddSingleton<IClaudePeerClient, ClaudePeerClient>();
        services.TryAddSingleton<IOpencodeServerClient, OpencodeServerClient>();
        services.TryAddSingleton<IPingSessionExecutor, PingSessionExecutor>();
        services.TryAddSingleton<IMailWakeBatchStore, MailWakeBatchStore>();
        services.TryAddSingleton<IAgentPingGateStore, AgentPingGateStore>();
        services.TryAddSingleton<ISessionGateCoordinator, SessionGateCoordinator>();
        services.TryAddSingleton<IMailNudge, MailNudge>();
        services.TryAddSingleton<IActorWakeDispatcher, ActorWakeDispatcher>();
        services.TryAddSingleton<IMailWakeDaemonLeaderStore, MailWakeDaemonLeaderStore>();
        services.TryAddSingleton(MailWakeDaemonPolicy.Default);

        // Inert until a caller invokes IMailWakeDaemonCoordinator.StartAsync.
        services.TryAddSingleton<IMailWakeDaemonCoordinator, MailWakeDaemonCoordinator>();
        services.TryAddKeyedSingleton<IClaudeHookHandler>(
            AgentSessionHarness.ClaudeCode,
            static (provider, _) => CreateClaudeHookHandler(
                provider, AgentSessionHarness.ClaudeCode, provider.GetRequiredService<IClaudeSessionFileReader>()));
        services.TryAddKeyedSingleton<IClaudeHookHandler>(
            AgentSessionHarness.Venti,
            static (provider, _) => CreateClaudeHookHandler(
                provider,
                AgentSessionHarness.Venti,
                provider.GetRequiredKeyedService<IClaudeSessionFileReader>(AgentSessionHarness.Venti)));
        services.TryAddSingleton<ICodexQueueClient, CodexQueueClient>();
        services.TryAddSingleton<ICodexForeignNotifyRunner, CodexForeignNotifyRunner>();
        services.TryAddSingleton<ICodexHookHandler, CodexHookHandler>();
        services.TryAddSingleton<IOpencodeHookHandler, OpencodeHookHandler>();
        services.TryAddSingleton<ILaunchDescriptorResolver, LaunchDescriptorResolver>();
        services.TryAddSingleton<IClaudeSettingsPathResolver, ClaudeSettingsPathResolver>();
        services.TryAddKeyedSingleton<IClaudeSettingsPathResolver, VentiSettingsPathResolver>(
            AgentSessionHarness.Venti);
        services.TryAddSingleton<IClaudeHooksSidecarStore, ClaudeHooksSidecarStore>();
        services.TryAddKeyedSingleton<IClaudeHooksInstallerService>(
            AgentSessionHarness.ClaudeCode,
            static (provider, _) => CreateClaudeHooksInstallerService(
                provider, provider.GetRequiredService<IClaudeSettingsPathResolver>(), ClaudeHooksTemplate.Claude));
        services.TryAddKeyedSingleton<IClaudeHooksInstallerService>(
            AgentSessionHarness.Venti,
            static (provider, _) => CreateClaudeHooksInstallerService(
                provider,
                provider.GetRequiredKeyedService<IClaudeSettingsPathResolver>(AgentSessionHarness.Venti),
                ClaudeHooksTemplate.Venti));
        services.TryAddSingleton<ICodexPathResolver, CodexPathResolver>();
        services.TryAddSingleton<ICodexHooksSidecarStore, CodexHooksSidecarStore>();
        services.TryAddSingleton<ICodexHooksInstallerService, CodexHooksInstallerService>();
        services.TryAddSingleton<IOpencodePathResolver, OpencodePathResolver>();
        services.TryAddSingleton<IOpencodeHooksSidecarStore, OpencodeHooksSidecarStore>();
        services.TryAddSingleton<IOpencodeHooksInstallerService, OpencodeHooksInstallerService>();
        services.TryAddSingleton<IOpencodeVersionResolver, OpencodeVersionResolver>();
        services.TryAddSingleton<ITaskStore, TaskStore>();
        services.TryAddSingleton<IMailStore, MailStore>();
        services.TryAddSingleton<IMemoryStore, MemoryStore>();
        services.TryAddSingleton<IBoardPreferencesStore, BoardPreferencesStore>();

        return services;
    }

    private static ClaudeHookHandler CreateClaudeHookHandler(
        IServiceProvider provider,
        string harness,
        IClaudeSessionFileReader sessionFileReader)
        => new(
            provider.GetRequiredService<IFileSystem>(),
            provider.GetRequiredService<TimeProvider>(),
            provider.GetRequiredService<IAgentStore>(),
            provider.GetRequiredService<IAgentDeliveryLedger>(),
            provider.GetRequiredService<IMailStore>(),
            harness,
            sessionFileReader);

    private static ClaudeHooksInstallerService CreateClaudeHooksInstallerService(
        IServiceProvider provider,
        IClaudeSettingsPathResolver pathResolver,
        ClaudeHooksTemplate template)
        => new(
            provider.GetRequiredService<IFileSystem>(),
            pathResolver,
            provider.GetRequiredService<ILaunchDescriptorResolver>(),
            provider.GetRequiredService<IClaudeHooksSidecarStore>(),
            provider.GetRequiredService<TimeProvider>(),
            template);
}
