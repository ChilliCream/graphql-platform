#if !NET9_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Commands.ApiKeys;
using ChilliCream.Nitro.CommandLine.Commands.Apis;
using ChilliCream.Nitro.CommandLine.Commands.Clients;
using ChilliCream.Nitro.CommandLine.Commands.Environments;
using ChilliCream.Nitro.CommandLine.Commands.Fusion;
using ChilliCream.Nitro.CommandLine.Commands.Fusion.PublishCommand;
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
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Configuration;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ChilliCream.Nitro.CommandLine;

internal static class ServiceCollectionExtensions
{
#if !NET9_0_OR_GREATER
    [RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
    [RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
#endif
    public static IServiceCollection AddNitroCommands(this IServiceCollection services)
    {
        services.AddSingleton<NitroRootCommand>();

        // Top-level commands
        services.AddSingleton<ApiKeyCommand>();
        services.AddSingleton<ApiCommand>();
        services.AddSingleton<ClientCommand>();
        services.AddSingleton<EnvironmentCommand>();
        services.AddSingleton<FusionCommand>();
        services.AddSingleton<LaunchCommand>();
        services.AddSingleton<LoginCommand>();
        services.AddSingleton<LogoutCommand>();
        services.AddSingleton<McpCommand>();
        services.AddSingleton<MockCommand>();
        services.AddSingleton<OpenApiCommand>();
        services.AddSingleton<PersonalAccessTokenCommand>();
        services.AddSingleton<SchemaCommand>();
        services.AddSingleton<StageCommand>();
        services.AddSingleton<WorkspaceCommand>();

        // API key commands
        services.AddSingleton<CreateApiKeyCommand>();
        services.AddSingleton<DeleteApiKeyCommand>();
        services.AddSingleton<ListApiKeyCommand>();

        // API commands
        services.AddSingleton<CreateApiCommand>();
        services.AddSingleton<DeleteApiCommand>();
        services.AddSingleton<ListApiCommand>();
        services.AddSingleton<ShowApiCommand>();
        services.AddSingleton<SetApiSettingsApiCommand>();

        // Client commands
        services.AddSingleton<CreateClientCommand>();
        services.AddSingleton<DeleteClientCommand>();
        services.AddSingleton<DownloadClientCommand>();
        services.AddSingleton<ListClientCommand>();
        services.AddSingleton<ListClientPublishedVersionsCommand>();
        services.AddSingleton<ListClientVersionsCommand>();
        services.AddSingleton<PublishClientCommand>();
        services.AddSingleton<ShowClientCommand>();
        services.AddSingleton<UnpublishClientCommand>();
        services.AddSingleton<UploadClientCommand>();
        services.AddSingleton<ValidateClientCommand>();

        // Environment commands
        services.AddSingleton<CreateEnvironmentCommand>();
        services.AddSingleton<ListEnvironmentCommand>();
        services.AddSingleton<ShowEnvironmentCommand>();

        // Fusion commands
        services.AddSingleton<FusionComposeCommand>();
        services.AddSingleton<FusionDownloadCommand>();
        services.AddSingleton<FusionMigrateCommand>();
        services.AddSingleton<FusionPublishCommand>();
        services.AddSingleton<FusionRunCommand>();
        services.AddSingleton<FusionSettingsCommand>();
        services.AddSingleton<FusionSettingsSetCommand>();
        services.AddSingleton<FusionUploadCommand>();
        services.AddSingleton<FusionValidateCommand>();
        services.AddSingleton<FusionConfigurationPublishBeginCommand>();
        services.AddSingleton<FusionConfigurationPublishCancelCommand>();
        services.AddSingleton<FusionConfigurationPublishCommitCommand>();
        services.AddSingleton<FusionConfigurationPublishStartCommand>();
        services.AddSingleton<FusionConfigurationPublishValidateCommand>();

        // MCP commands
        services.AddSingleton<CreateMcpFeatureCollectionCommand>();
        services.AddSingleton<DeleteMcpFeatureCollectionCommand>();
        services.AddSingleton<ListMcpFeatureCollectionCommand>();
        services.AddSingleton<PublishMcpFeatureCollectionCommand>();
        services.AddSingleton<UploadMcpFeatureCollectionCommand>();
        services.AddSingleton<ValidateMcpFeatureCollectionCommand>();

        // Mock commands
        services.AddSingleton<CreateMockCommand>();
        services.AddSingleton<ListMockCommand>();
        services.AddSingleton<UpdateMockCommand>();

        // OpenAPI commands
        services.AddSingleton<CreateOpenApiCollectionCommand>();
        services.AddSingleton<DeleteOpenApiCollectionCommand>();
        services.AddSingleton<ListOpenApiCollectionCommand>();
        services.AddSingleton<PublishOpenApiCollectionCommand>();
        services.AddSingleton<UploadOpenApiCollectionCommand>();
        services.AddSingleton<ValidateOpenApiCollectionCommand>();

        // Personal access token commands
        services.AddSingleton<CreatePersonalAccessTokenCommand>();
        services.AddSingleton<ListPersonalAccessTokenCommand>();
        services.AddSingleton<RevokePersonalAccessTokenCommand>();

        // Schema commands
        services.AddSingleton<DownloadSchemaCommand>();
        services.AddSingleton<PublishSchemaCommand>();
        services.AddSingleton<UploadSchemaCommand>();
        services.AddSingleton<ValidateSchemaCommand>();

        // Stage commands
        services.AddSingleton<DeleteStageCommand>();
        services.AddSingleton<EditStagesCommand>();
        services.AddSingleton<ListStagesCommand>();

        // Workspace commands
        services.AddSingleton<CreateWorkspaceCommand>();
        services.AddSingleton<CurrentWorkspaceCommand>();
        services.AddSingleton<ListWorkspaceCommand>();
        services.AddSingleton<SetDefaultWorkspaceCommand>();
        services.AddSingleton<ShowWorkspaceCommand>();

        return services;
    }

    public static IServiceCollection AddNitroServices(this IServiceCollection services)
    {
        services.TryAddSingleton<IConfigurationService, ConfigurationService>();

        services.TryAddSingleton<ISessionService, SessionService>();

        services.TryAddSingleton<IFileSystem, FileSystem>();

        services.TryAddSingleton<IResultHolder, ResultHolder>();
        services.TryAddSingleton<IResultFormatter, JsonResultFormatter>();

        return services;
    }
}
