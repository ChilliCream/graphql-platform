# Command Test Migration Progress

This document tracks migration of Nitro command tests to the guideline in `COMMAND_TESTING_GUIDELINES.md`.

## Status Legend

- `done`: command test suite added and reviewed against the checklist
- `in-progress`: currently being implemented or reviewed
- `not-started`: not migrated yet
- `n/a`: command container/root command with no direct command behavior to test

## Current Focus

- Scope baseline established.
- Existing baseline suites (reference):
  - `api-key create`
  - `api-key delete`
  - `api-key list`
- API command migration wave completed (`api create|delete|list|show|set-settings`).
- Schema command implementations were aligned to `COMMAND_IMPLEMENTATION_GUIDELINES.md` (typed mutation errors, auth assertions, activity fail/success semantics) and are ready for test-suite migration.

## Command Inventory

| Command                        | Source File                                                                                                       | Status      | Notes          |
| ------------------------------ | ----------------------------------------------------------------------------------------------------------------- | ----------- | -------------- |
| api-key (group)                | src/Nitro/CommandLine/src/CommandLine/Commands/ApiKeys/ApiKeyCommand.cs                                           | n/a         | Group command  |
| api-key create                 | src/Nitro/CommandLine/src/CommandLine/Commands/ApiKeys/CreateApiKeyCommand.cs                                     | done        | Existing suite |
| api-key delete                 | src/Nitro/CommandLine/src/CommandLine/Commands/ApiKeys/DeleteApiKeyCommand.cs                                     | done        | Existing suite |
| api-key list                   | src/Nitro/CommandLine/src/CommandLine/Commands/ApiKeys/ListApiKeyCommand.cs                                       | done        | Existing suite |
| api (group)                    | src/Nitro/CommandLine/src/CommandLine/Commands/Apis/ApiCommand.cs                                                 | n/a         | Group command  |
| api create                     | src/Nitro/CommandLine/src/CommandLine/Commands/Apis/CreateApiCommand.cs                                           | done        | Wave 1         |
| api delete                     | src/Nitro/CommandLine/src/CommandLine/Commands/Apis/DeleteApiCommand.cs                                           | done        | Wave 1         |
| api list                       | src/Nitro/CommandLine/src/CommandLine/Commands/Apis/ListApiCommand.cs                                             | done        | Wave 1         |
| api show                       | src/Nitro/CommandLine/src/CommandLine/Commands/Apis/ShowApiCommand.cs                                             | done        | Wave 1         |
| api set-settings               | src/Nitro/CommandLine/src/CommandLine/Commands/Apis/SetApiSettingsCommand.cs                                      | done        | Wave 1         |
| client (group)                 | src/Nitro/CommandLine/src/CommandLine/Commands/Clients/ClientCommand.cs                                           | n/a         | Group command  |
| client create                  | src/Nitro/CommandLine/src/CommandLine/Commands/Clients/CreateClientCommand.cs                                     | done        | Wave 4         |
| client delete                  | src/Nitro/CommandLine/src/CommandLine/Commands/Clients/DeleteClientCommand.cs                                     | done        | Wave 4         |
| client download                | src/Nitro/CommandLine/src/CommandLine/Commands/Clients/DownloadClientCommand.cs                                   | not-started |                |
| client list                    | src/Nitro/CommandLine/src/CommandLine/Commands/Clients/ListClientCommand.cs                                       | done        | Wave 2         |
| client list-published-versions | src/Nitro/CommandLine/src/CommandLine/Commands/Clients/ListClientPublishedVersionsCommand.cs                      | not-started |                |
| client list-versions           | src/Nitro/CommandLine/src/CommandLine/Commands/Clients/ListClientVersionsCommand.cs                               | not-started |                |
| client publish                 | src/Nitro/CommandLine/src/CommandLine/Commands/Clients/PublishClientCommand.cs                                    | not-started |                |
| client show                    | src/Nitro/CommandLine/src/CommandLine/Commands/Clients/ShowClientCommand.cs                                       | done        | Wave 2         |
| client unpublish               | src/Nitro/CommandLine/src/CommandLine/Commands/Clients/UnpublishClientCommand.cs                                  | not-started |                |
| client upload                  | src/Nitro/CommandLine/src/CommandLine/Commands/Clients/UploadClientCommand.cs                                     | not-started |                |
| client validate                | src/Nitro/CommandLine/src/CommandLine/Commands/Clients/ValidateClientCommand.cs                                   | not-started |                |
| environment (group)            | src/Nitro/CommandLine/src/CommandLine/Commands/Environments/EnvironmentCommand.cs                                 | n/a         | Group command  |
| environment create             | src/Nitro/CommandLine/src/CommandLine/Commands/Environments/CreateEnvironmentCommand.cs                           | done        | Wave 3         |
| environment list               | src/Nitro/CommandLine/src/CommandLine/Commands/Environments/ListEnvironmentCommand.cs                             | done        | Wave 3         |
| environment show               | src/Nitro/CommandLine/src/CommandLine/Commands/Environments/ShowEnvironmentCommand.cs                             | done        | Wave 3         |
| fusion (group)                 | src/Nitro/CommandLine/src/CommandLine/Commands/Fusion/FusionCommand.cs                                            | n/a         | Group command  |
| fusion compose                 | src/Nitro/CommandLine/src/CommandLine/Commands/Fusion/FusionComposeCommand.cs                                     | done        |                |
| fusion download                | src/Nitro/CommandLine/src/CommandLine/Commands/Fusion/FusionDownloadCommand.cs                                    | not-started |                |
| fusion migrate                 | src/Nitro/CommandLine/src/CommandLine/Commands/Fusion/FusionMigrateCommand.cs                                     | done        |                |
| fusion publish                 | src/Nitro/CommandLine/src/CommandLine/Commands/Fusion/FusionPublishCommand.cs                                     | not-started |                |
| fusion run                     | src/Nitro/CommandLine/src/CommandLine/Commands/Fusion/FusionRunCommand.cs                                         | not-started |                |
| fusion settings (group)        | src/Nitro/CommandLine/src/CommandLine/Commands/Fusion/FusionSettingsCommand.cs                                    | n/a         | Group command  |
| fusion settings set            | src/Nitro/CommandLine/src/CommandLine/Commands/Fusion/FusionSettingsSetCommand.cs                                 | not-started |                |
| fusion upload                  | src/Nitro/CommandLine/src/CommandLine/Commands/Fusion/FusionUploadCommand.cs                                      | not-started |                |
| fusion validate                | src/Nitro/CommandLine/src/CommandLine/Commands/Fusion/FusionValidateCommand.cs                                    | not-started |                |
| fusion publish begin           | src/Nitro/CommandLine/src/CommandLine/Commands/Fusion/PublishCommand/FusionConfigurationPublishBeginCommand.cs    | not-started |                |
| fusion publish cancel          | src/Nitro/CommandLine/src/CommandLine/Commands/Fusion/PublishCommand/FusionConfigurationPublishCancelCommand.cs   | not-started |                |
| fusion publish commit          | src/Nitro/CommandLine/src/CommandLine/Commands/Fusion/PublishCommand/FusionConfigurationPublishCommitCommand.cs   | not-started |                |
| fusion publish start           | src/Nitro/CommandLine/src/CommandLine/Commands/Fusion/PublishCommand/FusionConfigurationPublishStartCommand.cs    | not-started |                |
| fusion publish validate        | src/Nitro/CommandLine/src/CommandLine/Commands/Fusion/PublishCommand/FusionConfigurationPublishValidateCommand.cs | not-started |                |
| launch                         | src/Nitro/CommandLine/src/CommandLine/Commands/Launch/LaunchCommand.cs                                            | not-started |                |
| login                          | src/Nitro/CommandLine/src/CommandLine/Commands/Login/LoginCommand.cs                                              | not-started |                |
| logout                         | src/Nitro/CommandLine/src/CommandLine/Commands/Logout/LogoutCommand.cs                                            | not-started |                |
| mcp (group)                    | src/Nitro/CommandLine/src/CommandLine/Commands/Mcp/McpCommand.cs                                                  | n/a         | Group command  |
| mcp create                     | src/Nitro/CommandLine/src/CommandLine/Commands/Mcp/CreateMcpFeatureCollectionCommand.cs                           | not-started |                |
| mcp delete                     | src/Nitro/CommandLine/src/CommandLine/Commands/Mcp/DeleteMcpFeatureCollectionCommand.cs                           | not-started |                |
| mcp list                       | src/Nitro/CommandLine/src/CommandLine/Commands/Mcp/ListMcpFeatureCollectionCommand.cs                             | not-started |                |
| mcp publish                    | src/Nitro/CommandLine/src/CommandLine/Commands/Mcp/PublishMcpFeatureCollectionCommand.cs                          | not-started |                |
| mcp upload                     | src/Nitro/CommandLine/src/CommandLine/Commands/Mcp/UploadMcpFeatureCollectionCommand.cs                           | not-started |                |
| mcp validate                   | src/Nitro/CommandLine/src/CommandLine/Commands/Mcp/ValidateMcpFeatureCollectionCommand.cs                         | not-started |                |
| mock (group)                   | src/Nitro/CommandLine/src/CommandLine/Commands/Mocks/MockCommand.cs                                               | n/a         | Group command  |
| mock create                    | src/Nitro/CommandLine/src/CommandLine/Commands/Mocks/CreateMockCommand.cs                                         | not-started |                |
| mock list                      | src/Nitro/CommandLine/src/CommandLine/Commands/Mocks/ListMockCommand.cs                                           | not-started |                |
| mock update                    | src/Nitro/CommandLine/src/CommandLine/Commands/Mocks/UpdateMockCommand.cs                                         | not-started |                |
| openapi (group)                | src/Nitro/CommandLine/src/CommandLine/Commands/OpenApi/OpenApiCommand.cs                                          | n/a         | Group command  |
| openapi create                 | src/Nitro/CommandLine/src/CommandLine/Commands/OpenApi/CreateOpenApiCollectionCommand.cs                          | not-started |                |
| openapi delete                 | src/Nitro/CommandLine/src/CommandLine/Commands/OpenApi/DeleteOpenApiCollectionCommand.cs                          | not-started |                |
| openapi list                   | src/Nitro/CommandLine/src/CommandLine/Commands/OpenApi/ListOpenApiCollectionCommand.cs                            | not-started |                |
| openapi publish                | src/Nitro/CommandLine/src/CommandLine/Commands/OpenApi/PublishOpenApiCollectionCommand.cs                         | not-started |                |
| openapi upload                 | src/Nitro/CommandLine/src/CommandLine/Commands/OpenApi/UploadOpenApiCollectionCommand.cs                          | not-started |                |
| openapi validate               | src/Nitro/CommandLine/src/CommandLine/Commands/OpenApi/ValidateOpenApiCollectionCommand.cs                        | not-started |                |
| pat (group)                    | src/Nitro/CommandLine/src/CommandLine/Commands/PersonalAccessTokens/PersonalAccessTokenCommand.cs                 | n/a         | Group command  |
| pat create                     | src/Nitro/CommandLine/src/CommandLine/Commands/PersonalAccessTokens/CreatePersonalAccessTokenCommand.cs           | not-started |                |
| pat list                       | src/Nitro/CommandLine/src/CommandLine/Commands/PersonalAccessTokens/ListPersonalAccessTokenCommand.cs             | not-started |                |
| pat revoke                     | src/Nitro/CommandLine/src/CommandLine/Commands/PersonalAccessTokens/RevokePersonalAccessTokenCommand.cs           | not-started |                |
| schema (group)                 | src/Nitro/CommandLine/src/CommandLine/Commands/Schemas/SchemaCommand.cs                                           | n/a         | Group command  |
| schema download                | src/Nitro/CommandLine/src/CommandLine/Commands/Schemas/DownloadSchemaCommand.cs                                   | not-started |                |
| schema publish                 | src/Nitro/CommandLine/src/CommandLine/Commands/Schemas/PublishSchemaCommand.cs                                    | not-started |                |
| schema upload                  | src/Nitro/CommandLine/src/CommandLine/Commands/Schemas/UploadSchemaCommand.cs                                     | not-started |                |
| schema validate                | src/Nitro/CommandLine/src/CommandLine/Commands/Schemas/ValidateSchemaCommand.cs                                   | not-started |                |
| stage (group)                  | src/Nitro/CommandLine/src/CommandLine/Commands/Stages/StageCommand.cs                                             | n/a         | Group command  |
| stage delete                   | src/Nitro/CommandLine/src/CommandLine/Commands/Stages/DeleteStageCommand.cs                                       | not-started |                |
| stage edit                     | src/Nitro/CommandLine/src/CommandLine/Commands/Stages/EditStagesCommand.cs                                        | not-started |                |
| stage list                     | src/Nitro/CommandLine/src/CommandLine/Commands/Stages/ListStagesCommand.cs                                        | not-started |                |
| workspace (group)              | src/Nitro/CommandLine/src/CommandLine/Commands/Workspaces/WorkspaceCommand.cs                                     | n/a         | Group command  |
| workspace create               | src/Nitro/CommandLine/src/CommandLine/Commands/Workspaces/CreateWorkspaceCommand.cs                               | not-started |                |
| workspace current              | src/Nitro/CommandLine/src/CommandLine/Commands/Workspaces/CurrentWorkspaceCommand.cs                              | not-started |                |
| workspace list                 | src/Nitro/CommandLine/src/CommandLine/Commands/Workspaces/ListWorkspaceCommand.cs                                 | not-started |                |
| workspace set-default          | src/Nitro/CommandLine/src/CommandLine/Commands/Workspaces/SetDefaultWorkspaceCommand.cs                           | not-started |                |
| workspace show                 | src/Nitro/CommandLine/src/CommandLine/Commands/Workspaces/ShowWorkspaceCommand.cs                                 | not-started |                |

## Per-Command Completion Checklist

When marking a command as `done`, validate all items from `COMMAND_TESTING_GUIDELINES.md`:

- Before adding or migrating tests, update the command implementation to follow the conventions in `src/Nitro/CommandLine/src/CommandLine/Commands/COMMAND_IMPLEMENTATION_GUIDELINES.md`.
- Test names follow the naming conventions from `COMMAND_TESTING_GUIDELINES.md`
  - `<ConditionOrInput>_Return<Outcome>[_<Mode>]`
  - Mode suffixes use `_Interactive`, `_NonInteractive`, `_JsonOutput`
  - Exception tests use `ClientThrowsException_ReturnsError_<Mode>` and `ClientThrowsAuthorizationException_ReturnsError_<Mode>`
  - Mutation branch errors use `MutationReturns<BranchName>Error_ReturnsError_<Mode>`
- Help output snapshot
- Interactive, non-interactive, and JSON mode coverage
- Auth/session prerequisite coverage
- Parser-level required-option test (single consolidated case where applicable)
- ExecuteAsync custom validation branches
- Input combination branches (options/session/prompt permutations)
- Cursor coverage in all modes for list commands exposing `--cursor`
- NitroClientException in all three modes
- NitroClientAuthorizationException in all three modes
- Mutation typed error coverage in all three modes (3 separate theories + shared public MemberData)
- Branch/end-state mapping complete
