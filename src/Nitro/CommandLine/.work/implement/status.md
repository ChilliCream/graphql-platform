# Implementation Status — Nitro CLI Test Migration

**Last updated**: 2026-03-29
**Branch**: `tte/add-nitro-cli-tests`

---

## Overall Progress

**Previously completed (test files confirmed exist):**

### API Keys (3/3 done)

- ✅ api-key create
- ✅ api-key delete
- ✅ api-key list

### APIs (5/5 done)

- ✅ api create
- ✅ api delete
- ✅ api list
- ✅ api show
- ✅ api set-settings

### Clients (10/12 done)

- ✅ client create
- ✅ client delete
- ✅ client download
- ✅ client list
- ✅ client list-published-versions
- ✅ client list-versions
- ✅ client show
- ✅ client unpublish
- ✅ client upload
- ❌ **client validate** — MISSING (needs code migration + tests)
- ❌ **client publish** — MISSING (needs code migration + tests)

### Environments (3/3 done)

- ✅ environment create
- ✅ environment list
- ✅ environment show

### Fusion (9/14 done)

- ✅ fusion compose
- ✅ fusion download
- ✅ fusion migrate
- ✅ fusion settings set
- ✅ fusion upload
- ✅ fusion publish begin
- ✅ fusion publish cancel
- ✅ fusion publish commit
- ✅ fusion publish start
- ❌ **fusion validate** — MISSING (needs code migration + tests)
- ❌ **fusion publish** (main) — MISSING (complex multi-activity, no subscription)
- ❌ **fusion run** — MISSING (process management, special)
- ❌ **fusion publish validate** — MISSING (needs code migration + tests)

### MCP (4/6 done)

- ✅ mcp create
- ✅ mcp delete
- ✅ mcp list
- ✅ mcp upload
- ❌ **mcp validate** — MISSING (needs code migration + tests)
- ❌ **mcp publish** — MISSING (needs code migration + tests)

### Mocks (3/3 done)

- ✅ mock create
- ✅ mock list
- ✅ mock update

### OpenAPI (4/6 done)

- ✅ openapi create
- ✅ openapi delete
- ✅ openapi list
- ✅ openapi upload
- ❌ **openapi validate** — MISSING (needs code migration + tests)
- ❌ **openapi publish** — MISSING (needs code migration + tests)

### Personal Access Tokens (3/3 done)

- ✅ pat create
- ✅ pat list
- ✅ pat revoke

### Schemas (4/4 done)

- ✅ schema download
- ✅ schema upload
- ✅ schema validate
- ✅ schema publish

### Stages (3/3 done)

- ✅ stage delete
- ✅ stage edit
- ✅ stage list

### Workspaces (5/5 done)

- ✅ workspace create
- ✅ workspace current
- ✅ workspace list
- ✅ workspace set-default
- ✅ workspace show

---

## Remaining Work (13 commands)

### Code migrations required before tests:

**ValidateClientCommand** (`src/CommandLine/Commands/Clients/ValidateClientCommand.cs`):

- Uses `console.PrintMutationErrorsAndExit(validationRequest.Errors)` → replace with typed switch + activity.Fail() + stderr
- Uses `console.PrintMutationErrors(errors)` in subscription handler → replace with per-error stderr writes

**PublishClientCommand** (`src/CommandLine/Commands/Clients/PublishClientCommand.cs`):

- Same issues as ValidateClientCommand

**ValidateMcpFeatureCollectionCommand** (`src/CommandLine/Commands/Mcp/ValidateMcpFeatureCollectionCommand.cs`):

- Uses `console.PrintMutationErrorsAndExit(validationRequest.Errors)` → replace with typed switch + activity.Fail() + stderr
- Uses `console.PrintMutationErrors(errors)` in subscription handler → replace with per-error stderr writes

**PublishMcpFeatureCollectionCommand** (`src/CommandLine/Commands/Mcp/PublishMcpFeatureCollectionCommand.cs`):

- Same issues as ValidateMcpFeatureCollectionCommand

**ValidateOpenApiCollectionCommand** (`src/CommandLine/Commands/OpenApi/ValidateOpenApiCollectionCommand.cs`):

- Same issues

**PublishOpenApiCollectionCommand** (`src/CommandLine/Commands/OpenApi/PublishOpenApiCollectionCommand.cs`):

- Same issues

**FusionValidateCommand** (`src/CommandLine/Commands/Fusion/FusionValidateCommand.cs`):

- `ValidateAsync()` method uses `console.PrintMutationErrorsAndExit(result.Errors)` → replace with typed switch
- Subscription handler uses `console.PrintMutationErrors(v.Errors)` → replace with per-error stderr writes
- Note: Complex two-path command (archive vs source schema files)

**FusionConfigurationPublishValidateCommand** (`src/CommandLine/Commands/Fusion/PublishCommand/FusionConfigurationPublishValidateCommand.cs`):

- Uses `console.PrintMutationErrorsAndExit(result.Errors)` → replace
- Uses `console.PrintMutationErrors(failed.Errors)` in subscription → replace
- Special: some subscription states use `throw Exit(...)` — keep those

### No code migration needed (just write tests):

**FusionPublishCommand** (`src/CommandLine/Commands/Fusion/FusionPublishCommand.cs`):

- Complex multi-activity command (begin + claim + download + compose + upload)
- No subscription
- Multiple execution paths (archive file vs source schema files vs source identifiers)

**FusionRunCommand** (`src/CommandLine/Commands/Fusion/FusionRunCommand.cs`):

- Process management (special handling needed)

**LaunchCommand** (`src/CommandLine/Commands/Launch/LaunchCommand.cs`):

- Simple browser open

**LoginCommand** (`src/CommandLine/Commands/Login/LoginCommand.cs`):

- Browser-based auth + workspace selection

**LogoutCommand** (`src/CommandLine/Commands/Logout/LogoutCommand.cs`):

- Simple session clear

---

## Reference Patterns

**For subscription command code migration:** See `ValidateSchemaCommand.cs` — fully migrated reference
**For subscription tests:** See `ValidateSchemaCommandTests.cs` and `PublishSchemaCommandTests.cs`
**For general test pattern:** See `CreateApiCommandTests.cs`
**For subscription mock helper:** Use `ToAsyncEnumerable<T>` helper if it exists, otherwise inline

---

## Agent Assignments (Active)

| Agent | Commands                                                                                                                 | Status                |
| ----- | ------------------------------------------------------------------------------------------------------------------------ | --------------------- |
| A     | launch, logout, login                                                                                                    | dispatched 2026-03-29 |
| B     | client validate (migrate+test), client publish (migrate+test)                                                            | dispatched 2026-03-29 |
| C     | mcp validate (migrate+test), mcp publish (migrate+test), openapi validate (migrate+test), openapi publish (migrate+test) | dispatched 2026-03-29 |
| D     | fusion validate (migrate+test), fusion publish validate (migrate+test), fusion publish (tests), fusion run (tests)       | dispatched 2026-03-29 |
