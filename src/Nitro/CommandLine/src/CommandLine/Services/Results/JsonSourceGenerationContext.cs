using System.Text.Json.Serialization;
using ChilliCream.Nitro.CommandLine.Commands.Agent;
using ChilliCream.Nitro.CommandLine.Commands.ApiKeys;
using ChilliCream.Nitro.CommandLine.Commands.ApiKeys.Components;
using ChilliCream.Nitro.CommandLine.Commands.Apis.Components;
using ChilliCream.Nitro.CommandLine.Commands.Clients;
using ChilliCream.Nitro.CommandLine.Commands.Clients.Components;
using ChilliCream.Nitro.CommandLine.Commands.Environments.Components;
using ChilliCream.Nitro.CommandLine.Commands.Fusion.PublishCommand;
using ChilliCream.Nitro.CommandLine.Commands.Mail;
using ChilliCream.Nitro.CommandLine.Commands.Memory;
using ChilliCream.Nitro.CommandLine.Commands.Mcp.Components;
using ChilliCream.Nitro.CommandLine.Commands.Mocks.Components;
using ChilliCream.Nitro.CommandLine.Commands.OpenApi.Components;
using ChilliCream.Nitro.CommandLine.Commands.PersonalAccessTokens;
using ChilliCream.Nitro.CommandLine.Commands.PersonalAccessTokens.Components;
using ChilliCream.Nitro.CommandLine.Commands.Stages.Components;
using ChilliCream.Nitro.CommandLine.Commands.Tasks;
using ChilliCream.Nitro.CommandLine.Commands.Workspaces.Components;
using ChilliCream.Nitro.CommandLine.Services.Memory;
using ChilliCream.Nitro.CommandLine.Services.Tasks;

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
[JsonSerializable(typeof(PaginatedListResult<ListClientVersionsCommand.ClientVersionResult>))]
[JsonSerializable(typeof(PaginatedListResult<ListClientPublishedVersionsCommand.ClientPublishedVersionResult>))]
[JsonSerializable(typeof(PaginatedListResult<EnvironmentDetailPrompt.EnvironmentDetailPromptResult>))]
[JsonSerializable(typeof(PaginatedListResult<MockSchemaDetailPrompt.MockSchemaDetailPromptResult>))]
[JsonSerializable(typeof(PaginatedListResult<PersonalAccessTokenDetailPrompt.PersonalAccessTokenDetailPromptResult>))]
[JsonSerializable(typeof(PaginatedListResult<StageDetailPrompt.StageDetailPromptResult>))]
[JsonSerializable(typeof(PaginatedListResult<WorkspaceDetailPrompt.WorkspaceDetailPromptResult>))]
[JsonSerializable(typeof(OpenApiCollectionDetailPrompt.OpenApiCollectionDetailPromptResult))]
[JsonSerializable(typeof(PaginatedListResult<OpenApiCollectionDetailPrompt.OpenApiCollectionDetailPromptResult>))]
[JsonSerializable(typeof(McpFeatureCollectionDetailPrompt.McpFeatureCollectionDetailPromptResult))]
[JsonSerializable(typeof(PaginatedListResult<McpFeatureCollectionDetailPrompt.McpFeatureCollectionDetailPromptResult>))]
[JsonSerializable(typeof(ListResult<TaskSummaryResult>))]
[JsonSerializable(typeof(ListResult<TaskBlockedResult>))]
[JsonSerializable(typeof(TaskDetailResult))]
[JsonSerializable(typeof(CountTaskCommand.TaskTotalCountResult))]
[JsonSerializable(typeof(ListResult<TaskCount>))]
[JsonSerializable(typeof(TaskStats))]
[JsonSerializable(typeof(TaskDependenciesResult))]
[JsonSerializable(typeof(TaskDependencyTreeNode))]
[JsonSerializable(typeof(ListResult<TaskCycleResult>))]
[JsonSerializable(typeof(ListResult<TaskLabelCount>))]
[JsonSerializable(typeof(ListResult<TaskComment>))]
[JsonSerializable(typeof(GetTaskConfigCommand.TaskConfigValueResult))]
[JsonSerializable(typeof(ListResult<TaskConfigEntry>))]
[JsonSerializable(typeof(WhereTaskCommand.TaskWorkspaceLocationResult))]
[JsonSerializable(typeof(ListResult<TaskEpicStatus>))]
[JsonSerializable(typeof(TaskSnapshotResult))]
[JsonSerializable(typeof(ListResult<TaskSnapshotResult>))]
[JsonSerializable(typeof(AddTaskDependencyCommand.TaskDependencyAddedResult))]
[JsonSerializable(typeof(RemoveTaskDependencyCommand.TaskDependencyRemovedResult))]
[JsonSerializable(typeof(AddTaskLabelCommand.TaskLabelAddResult))]
[JsonSerializable(typeof(RemoveTaskLabelCommand.TaskLabelRemovedResult))]
[JsonSerializable(typeof(TaskComment))]
[JsonSerializable(typeof(TaskConfigEntry))]
[JsonSerializable(typeof(InitAgentCommand.AgentWorkspaceInitResult))]
[JsonSerializable(typeof(DoctorTaskCommand.TaskDoctorResult))]
[JsonSerializable(typeof(ListResult<TaskLintFinding>))]
[JsonSerializable(typeof(RegisterAgentCommand.AgentRegisterResult))]
[JsonSerializable(typeof(WhoamiAgentCommand.AgentWhoamiResult))]
[JsonSerializable(typeof(ListResult<ListAgentCommand.AgentListRowResult>))]
[JsonSerializable(typeof(MailMessageResult))]
[JsonSerializable(typeof(MailSendResult))]
[JsonSerializable(typeof(ListResult<MailInboxRowResult>))]
[JsonSerializable(typeof(MailMessageDetailResult))]
[JsonSerializable(typeof(ListResult<MailMessageDetailResult>))]
[JsonSerializable(typeof(MailIdsResult))]
[JsonSerializable(typeof(ListResult<MailThreadRowResult>))]
[JsonSerializable(typeof(MemoryRecordResult))]
[JsonSerializable(typeof(ListResult<MemoryRecordResult>))]
[JsonSerializable(typeof(MemoryRecordDetailResult))]
[JsonSerializable(typeof(MemoryScopeConflictResult))]
[JsonSerializable(typeof(WhereMemoryCommand.MemoryLocationResult))]
[JsonSerializable(typeof(ListResult<WhereMemoryCommand.MemoryLocationResult>))]
[JsonSerializable(typeof(MemoryContextResult))]
[JsonSerializable(typeof(MemoryTagCount))]
[JsonSerializable(typeof(ListResult<MemoryTagCount>))]
[JsonSerializable(typeof(MemoryIndexRebuildResult))]
[JsonSerializable(typeof(ListResult<MemoryIndexRebuildResult>))]
internal partial class JsonSourceGenerationContext : JsonSerializerContext;
