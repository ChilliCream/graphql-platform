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
using ChilliCream.Nitro.CommandLine.Commands.Status;
using ChilliCream.Nitro.CommandLine.Commands.Workspaces;
using ChilliCream.Nitro.CommandLine.Helpers;

#if !NET9_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace ChilliCream.Nitro.CommandLine;

/// <summary>
/// The root command of the Nitro CLI.
/// </summary>
#if !NET9_0_OR_GREATER
[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
#endif
internal sealed class NitroRootCommand : RootCommand
{
    public NitroRootCommand()
    {
        Description = "Nitro CLI";

        Subcommands.Add(new ApiKeyCommand());
        Subcommands.Add(new ApiCommand());
        Subcommands.Add(new ClientCommand());
        Subcommands.Add(new EnvironmentCommand());
        Subcommands.Add(new FusionCommand());
        Subcommands.Add(new LaunchCommand());
        Subcommands.Add(new LoginCommand());
        Subcommands.Add(new LogoutCommand());
        Subcommands.Add(new McpCommand());
        Subcommands.Add(new MockCommand());
        Subcommands.Add(new OpenApiCommand());
        Subcommands.Add(new PersonalAccessTokenCommand());
        Subcommands.Add(new SchemaCommand());
        Subcommands.Add(new StageCommand());
        Subcommands.Add(new StatusCommand());
        Subcommands.Add(new WorkspaceCommand());

        CommandExamples.Install(this);
    }
}
