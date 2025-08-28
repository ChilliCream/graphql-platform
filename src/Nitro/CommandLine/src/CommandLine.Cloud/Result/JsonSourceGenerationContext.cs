using System.Text.Json.Serialization;
using ChilliCream.Nitro.CommandLine.Cloud.Commands.ApiKey;
using ChilliCream.Nitro.CommandLine.Cloud.Commands.FusionConfiguration;
using ChilliCream.Nitro.CommandLine.Cloud.Commands.Mock.Component;
using ChilliCream.Nitro.CommandLine.Cloud.Commands.PersonalAccessToken;

namespace ChilliCream.Nitro.CommandLine.Cloud.Results;

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
internal partial class JsonSourceGenerationContext : JsonSerializerContext;
