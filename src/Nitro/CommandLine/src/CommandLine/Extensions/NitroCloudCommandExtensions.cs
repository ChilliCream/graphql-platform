#if !NET9_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using System.CommandLine.Builder;

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
using ChilliCream.Nitro.CommandLine.Options;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Configuration;
using ChilliCream.Nitro.CommandLine.Services.Sessions;

namespace ChilliCream.Nitro.CommandLine;

#if !NET9_0_OR_GREATER
[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
#endif
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
        command.AddCommand(new McpCommand());
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
