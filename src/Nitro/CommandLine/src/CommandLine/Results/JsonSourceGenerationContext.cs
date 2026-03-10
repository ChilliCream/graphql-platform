using System.Text.Json.Serialization;
using ChilliCream.Nitro.CommandLine.Commands.ApiKeys;
using ChilliCream.Nitro.CommandLine.Commands.ApiKeys.Components;
using ChilliCream.Nitro.CommandLine.Commands.Apis.Components;
using ChilliCream.Nitro.CommandLine.Commands.Clients.Components;
using ChilliCream.Nitro.CommandLine.Commands.Environments.Components;
using ChilliCream.Nitro.CommandLine.Commands.Fusion.PublishCommand;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Components;
using ChilliCream.Nitro.CommandLine.Commands.Mocks.Components;
using ChilliCream.Nitro.CommandLine.Commands.OpenApi.Components;
using ChilliCream.Nitro.CommandLine.Commands.PersonalAccessTokens;
using ChilliCream.Nitro.CommandLine.Commands.PersonalAccessTokens.Components;
using ChilliCream.Nitro.CommandLine.Commands.Stages.Components;
using ChilliCream.Nitro.CommandLine.Commands.Workspaces.Components;

namespace ChilliCream.Nitro.CommandLine.Results;

[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(ClientDetailPrompt.ClientDetailPromptResult))]
[JsonSerializable(typeof(ApiDetailPrompt.ApiDetailPromptResult))]
[JsonSerializable(typeof(ApiKeyDetailPrompt.ApiKeyDetailPromptResult))]
[JsonSerializable(typeof(CreateApiKeyCommand.CreateApiKeyResult))]
[JsonSerializable(typeof(EnvironmentDetailPrompt.EnvironmentDetailPromptResult))]
[JsonSerializable(typeof(StageDetailPrompt.StageDetailPromptResult))]
[JsonSerializable(typeof(WorkspaceDetailPrompt.WorkspaceDetailPromptResult))]
[JsonSerializable(typeof(MockSchemaDetailPrompt.MockSchemaDetailPromptResult))]
[JsonSerializable(typeof(PersonalAccessTokenDetailPrompt.PersonalAccessTokenDetailPromptResult))]
[JsonSerializable(typeof(CreatePersonalAccessTokenCommand.CreatePersonalAccessTokenCommandResult))]
[JsonSerializable(typeof(FusionConfigurationPublishBeginCommand.FusionConfigurationPublishBeginCommandResult))]
[JsonSerializable(typeof(PaginatedListResult<ApiDetailPrompt.ApiDetailPromptResult>))]
[JsonSerializable(typeof(PaginatedListResult<ApiKeyDetailPrompt.ApiKeyDetailPromptResult>))]
[JsonSerializable(typeof(PaginatedListResult<ClientDetailPrompt.ClientDetailPromptResult>))]
[JsonSerializable(typeof(PaginatedListResult<EnvironmentDetailPrompt.EnvironmentDetailPromptResult>))]
[JsonSerializable(typeof(PaginatedListResult<MockSchemaDetailPrompt.MockSchemaDetailPromptResult>))]
[JsonSerializable(typeof(PaginatedListResult<PersonalAccessTokenDetailPrompt.PersonalAccessTokenDetailPromptResult>))]
[JsonSerializable(typeof(PaginatedListResult<StageDetailPrompt.StageDetailPromptResult>))]
[JsonSerializable(typeof(PaginatedListResult<WorkspaceDetailPrompt.WorkspaceDetailPromptResult>))]
[JsonSerializable(typeof(OpenApiCollectionDetailPrompt.OpenApiCollectionDetailPromptResult))]
[JsonSerializable(typeof(PaginatedListResult<OpenApiCollectionDetailPrompt.OpenApiCollectionDetailPromptResult>))]
[JsonSerializable(typeof(McpFeatureCollectionDetailPrompt.McpFeatureCollectionDetailPromptResult))]
[JsonSerializable(typeof(PaginatedListResult<McpFeatureCollectionDetailPrompt.McpFeatureCollectionDetailPromptResult>))]
internal partial class JsonSourceGenerationContext : JsonSerializerContext;
