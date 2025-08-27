using System.CommandLine.Builder;
using ChilliCream.Nitro.CommandLine.Cloud.Commands.Api;
using ChilliCream.Nitro.CommandLine.Cloud.Commands.ApiKey;
using ChilliCream.Nitro.CommandLine.Cloud.Commands.Environment;
using ChilliCream.Nitro.CommandLine.Cloud.Commands.FusionConfiguration;
using ChilliCream.Nitro.CommandLine.Cloud.Commands.Mock;
using ChilliCream.Nitro.CommandLine.Cloud.Commands.PersonalAccessToken;
using ChilliCream.Nitro.CommandLine.Cloud.Commands.Stages;
using ChilliCream.Nitro.CommandLine.Cloud.Option;
using ChilliCream.Nitro.CommandLine.Cloud.Option.Binders;
using ChilliCream.Nitro.CommandLine.Cloud.Results;

namespace ChilliCream.Nitro.CommandLine.Cloud;

public static class NitroCloudCommandExtensions
{
    public static CommandLineBuilder AddNitroCloud(this CommandLineBuilder builder)
    {
        builder.Command.AddNitroCloudCommands();

        builder.AddService<IConfigurationService, ConfigurationService>()
            .AddSession()
            .AddResult()
            .AddApiClient()
            .AddSessionMiddleware()
            .AddResultMiddleware();

        return builder;
    }

    internal static void AddNitroCloudCommands(this Command command)
    {
        command.AddCommand(new ApiKeyCommand());
        command.AddCommand(new ApiCommand());
        command.AddCommand(new ClientCommand());
        command.AddCommand(new EnvironmentCommand());
        command.AddCommand(new LaunchCommand());
        command.AddCommand(new LoginCommand());
        command.AddCommand(new LogoutCommand());
        command.AddCommand(new SchemaCommand());
        command.AddCommand(new FusionConfigurationCommand());
        command.AddCommand(new StageCommand());
        command.AddCommand(new WorkspaceCommand());
        command.AddCommand(new MockCommand());
        command.AddCommand(new PersonalAccessTokenCommand());
    }

    internal static void AddNitroCloudDefaultOptions(this Command command)
    {
        command.AddGlobalOption(Opt<CloudUrlOption>.Instance);
        command.AddGlobalOption(Opt<ApiKeyOption>.Instance);
        command.AddGlobalOption(Opt<OutputFormatOption>.Instance);
    }
}
