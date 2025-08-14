using ChilliCream.Nitro.CLI.Commands.Api;
using ChilliCream.Nitro.CLI.Commands.ApiKey;
using ChilliCream.Nitro.CLI.Commands.Environment;
using ChilliCream.Nitro.CLI.Commands.FusionConfiguration;
using ChilliCream.Nitro.CLI.Commands.Mock;
using ChilliCream.Nitro.CLI.Commands.PersonalAccessToken;
using ChilliCream.Nitro.CLI.Commands.Stages;
using ChilliCream.Nitro.CLI.Option;
using ChilliCream.Nitro.CLI.Results;

namespace ChilliCream.Nitro.CLI;

internal sealed class NitroRootCommand : Command
{
    public NitroRootCommand() : base("nitro")
    {
        AddGlobalOption(Opt<CloudUrlOption>.Instance);
        AddGlobalOption(Opt<ApiKeyOption>.Instance);
        AddGlobalOption(Opt<OutputFormatOption>.Instance);

        AddCommand(new ApiKeyCommand());
        AddCommand(new ApiCommand());
        AddCommand(new ClientCommand());
        AddCommand(new EnvironmentCommand());
        AddCommand(new LaunchCommand());
        AddCommand(new LoginCommand());
        AddCommand(new LogoutCommand());
        AddCommand(new SchemaCommand());
        AddCommand(new FusionConfigurationCommand());
        AddCommand(new StageCommand());
        AddCommand(new WorkspaceCommand());
        AddCommand(new MockCommand());
        AddCommand(new PersonalAccessTokenCommand());
    }
}
