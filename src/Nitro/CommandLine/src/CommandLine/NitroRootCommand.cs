using ChilliCream.Nitro.CommandLine.Commands.ApiKeys;
using ChilliCream.Nitro.CommandLine.Commands.Apis;
using ChilliCream.Nitro.CommandLine.Commands.Clients;
using ChilliCream.Nitro.CommandLine.Commands.Environments;
using ChilliCream.Nitro.CommandLine.Commands.Fusion;
using ChilliCream.Nitro.CommandLine.Commands.Launch;
using ChilliCream.Nitro.CommandLine.Commands.Login;
using ChilliCream.Nitro.CommandLine.Commands.Logout;
using ChilliCream.Nitro.CommandLine.Commands.Mcp;
using ChilliCream.Nitro.CommandLine.Commands.Mocks;
using ChilliCream.Nitro.CommandLine.Commands.OpenApi;
using ChilliCream.Nitro.CommandLine.Commands.PersonalAccessTokens;
using ChilliCream.Nitro.CommandLine.Commands.Schemas;
using ChilliCream.Nitro.CommandLine.Commands.Stages;
using ChilliCream.Nitro.CommandLine.Commands.Workspaces;
using static ChilliCream.Nitro.CommandLine.CommandLineResources;

namespace ChilliCream.Nitro.CommandLine;

/// <summary>
/// The root command of the Nitro CLI.
/// </summary>
internal sealed class NitroRootCommand : RootCommand
{
    public NitroRootCommand(
        ApiKeyCommand apiKeyCommand,
        ApiCommand apiCommand,
        ClientCommand clientCommand,
        EnvironmentCommand environmentCommand,
        FusionCommand fusionCommand,
        LaunchCommand launchCommand,
        LoginCommand loginCommand,
        LogoutCommand logoutCommand,
        McpCommand mcpCommand,
        MockCommand mockCommand,
        OpenApiCommand openApiCommand,
        PersonalAccessTokenCommand personalAccessTokenCommand,
        SchemaCommand schemaCommand,
        StageCommand stageCommand,
        WorkspaceCommand workspaceCommand)
        : base("nitro")
    {
        Description = RootCommand_Description;

        Subcommands.Add(apiKeyCommand);
        Subcommands.Add(apiCommand);
        Subcommands.Add(clientCommand);
        Subcommands.Add(environmentCommand);
        Subcommands.Add(fusionCommand);
        Subcommands.Add(launchCommand);
        Subcommands.Add(loginCommand);
        Subcommands.Add(logoutCommand);
        Subcommands.Add(mcpCommand);
        Subcommands.Add(mockCommand);
        Subcommands.Add(openApiCommand);
        Subcommands.Add(personalAccessTokenCommand);
        Subcommands.Add(schemaCommand);
        Subcommands.Add(stageCommand);
        Subcommands.Add(workspaceCommand);
    }
}
