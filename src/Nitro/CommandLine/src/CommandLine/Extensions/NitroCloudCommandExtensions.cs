using System.CommandLine.Builder;
using ChilliCream.Nitro.CommandLine.Commands.ApiKeys;
using ChilliCream.Nitro.CommandLine.Commands.Apis;
using ChilliCream.Nitro.CommandLine.Commands.Clients;
using ChilliCream.Nitro.CommandLine.Commands.Environments;
using ChilliCream.Nitro.CommandLine.Commands.Fusion;
using ChilliCream.Nitro.CommandLine.Commands.Launch;
using ChilliCream.Nitro.CommandLine.Commands.Login;
using ChilliCream.Nitro.CommandLine.Commands.Logout;
using ChilliCream.Nitro.CommandLine.Commands.Mocks;
using ChilliCream.Nitro.CommandLine.Commands.OpenApi;
using ChilliCream.Nitro.CommandLine.Commands.PersonalAccessTokens;
using ChilliCream.Nitro.CommandLine.Commands.Schemas;
using ChilliCream.Nitro.CommandLine.Commands.Stages;
using ChilliCream.Nitro.CommandLine.Commands.Workspaces;
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Configuration;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine;

public static class NitroCloudCommandExtensions
{
    public static CommandLineBuilder AddNitroCloudConfiguration(this CommandLineBuilder builder)
    {
        builder.AddService<IConfigurationService, ConfigurationService>()
            .AddSession()
            .AddResult()
            .AddApiClient()
            .AddSessionMiddleware()
            .AddResultMiddleware();

        return builder;
    }

    public static void AddNitroCloudCommands(this Command command)
    {
        command.AddCommand(new ApiKeyCommand());
        command.AddCommand(new ApiCommand());
        command.AddCommand(new ClientCommand());
        command.AddCommand(new EnvironmentCommand());
        command.AddCommand(new FusionCommand());
        command.AddCommand(new LaunchCommand());
        command.AddCommand(new LoginCommand());
        command.AddCommand(new LogoutCommand());
        command.AddCommand(new MockCommand());
        command.AddCommand(new OpenApiCommand());
        command.AddCommand(new PersonalAccessTokenCommand());
        command.AddCommand(new SchemaCommand());
        command.AddCommand(new StageCommand());
        command.AddCommand(new WorkspaceCommand());
    }

    internal static void AddNitroCloudDefaultOptions(this Command command)
    {
        command.AddGlobalOption(Opt<CloudUrlOption>.Instance);
        command.AddGlobalOption(Opt<ApiKeyOption>.Instance);
        command.AddGlobalOption(Opt<OutputFormatOption>.Instance);
    }
}
