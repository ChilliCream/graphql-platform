# Cross-Review: Pattern Buckets and Hybrid Approaches

**Reviewer**: Category Waves architect
**Date**: 2026-03-29

---

## Review of Approach: Pattern Buckets

### Summary

Pattern Buckets groups the 32 commands into 8 behavioral buckets (list, create/mutation, show/query, upload, download, subscription-validate, subscription-publish, special). Agents specialize by pattern type rather than domain category. Execution proceeds in 4 phases: simple patterns first, file I/O second, subscriptions third, special commands last.

### What's Better Than My Approach

1. **Agent specialization is a real advantage.** An agent doing all 8 list commands consecutively will be faster and more consistent than an agent switching between `ListWorkspaceCommand`, `CreateWorkspaceCommand`, and `ShowWorkspaceCommand` within the same wave. The repetition compounds -- by the third list command, the agent barely needs to think about the test skeleton. My approach asks agents to context-switch between pattern types within a category, which is less efficient per-command.

2. **The template idea is excellent.** Concrete `.work/templates/bucket-*.cs` files with ADAPT markers is a practical, low-overhead way to enforce consistency. My approach relies on agents reading existing reference implementations (e.g., "look at CreateApiCommandTests") which requires more interpretation. Explicit templates with fill-in-the-blank markers are harder to get wrong.

3. **Subscription infrastructure is centralized, not duplicated.** Pattern Buckets builds the `SubscriptionTestHelper` once and 10 commands use it. My approach says "add ToAsyncEnumerable to SchemaCommandTestHelper and reuse it" -- but in practice, if Wave E (MCP+OpenAPI) is being built by a different agent than Wave A, they might not know about the Schema helper. Buckets avoids this by having a single agent (or pair) own all subscription commands.

4. **Deferred complexity is well-reasoned.** Phases 1-2 (list, create, show, upload, download) can all be done with zero new infrastructure. Subscription complexity is isolated to Phase 3. My approach mixes simple and complex within the same wave (e.g., Wave A has both `DownloadSchemaCommand` and `PublishSchemaCommand`).

### What's Worse Than My Approach

1. **Cross-category context switching is underestimated.** The doc says "templates minimize domain knowledge needed" but this understates the problem. To write a test for `ListMcpFeatureCollectionCommand`, the agent needs to:
   - Read the command source to understand its specific options (`--api-id`, `--stage`, etc.)
   - Understand the `IMcpFeaturesClient` interface and its list method signature
   - Construct correct mock payloads with the right generated types (`IListMcpFeatureCollectionCommandQuery_...`)
   - Get the snapshot assertions right for MCP-specific output formatting

   These are all domain-specific. The template handles the skeleton but the agent still needs to understand each command's specifics. And it needs to switch between MCP, OpenAPI, PAT, Stage, and Workspace domains within a single bucket.

2. **Helper fragmentation is a real problem.** The approach acknowledges this but the mitigation ("each test file should be self-contained with its own private helpers") contradicts the established codebase convention. The existing tests use shared `*CommandTestHelper.cs` files per category. Going self-contained per file means MCP's list tests, create tests, and validate tests each independently build mock payloads for MCP feature collections. That's duplication the codebase explicitly avoids via helpers like `ApiCommandTestHelper.cs`.

3. **`UpdateMockCommand` in Bucket C (Show/Query) is wrong.** The doc puts `UpdateMockCommand` in the show/query bucket because it's "closest to show pattern." But `UpdateMockCommand` is a mutation command with file I/O (reads schema files, sends them to the server). It uses `PrintMutationErrorsAndExit` and has `ValidationError` error branches. Its test shape is mutation + file upload, not query. This misclassification would lead to incorrect test coverage.

4. **Phase boundaries create artificial waiting.** All 8 list commands must finish Phase 1 before any upload command starts Phase 2. But `ListWorkspaceCommand` and `UploadSchemaCommand` have zero dependencies on each other. My approach allows Wave A (schemas) and Wave B (workspaces) to run in parallel from the start.

5. **Implementation migration is hand-waved.** The doc mentions that "Phase 3 agents have dual mandate: fix commands to match target pattern AND write tests" and calls this "actually an advantage." But it doesn't detail which specific commands need migration, how many, or how the agent handles compilation failures from changing error handling in command source while also working across multiple categories. My approach lists every command needing migration per wave.

6. **No quality gates.** Pattern Buckets has no per-command or per-phase completion checklist. It assumes the template + agent discipline is sufficient. The Hybrid approach has explicit 5-tier quality gates. My approach has the test-rule checklist from research.md. Pattern Buckets is silent on verification.

### Elements I'd Steal

- **The template files idea.** Creating `.work/templates/` with concrete test skeletons for list, create, delete, upload, download, subscription-validate, and subscription-publish patterns would accelerate any approach. These can be created ahead of time from existing reference tests and used regardless of whether work is organized by pattern or category.

- **The Phase 0 infrastructure step.** Pattern Buckets explicitly calls out a Phase 0 to create templates and verify infrastructure. My approach implicitly expects Wave A to establish patterns, but a dedicated setup phase would be cleaner.

---

## Review of Approach: Hybrid Dependency-Driven

### Summary

The Hybrid approach organizes work into 4 parallel streams based on client interface dependency chains, with each stream progressing from simple to complex commands. It front-loads implementation migration identification (Tier 1/2/3 analysis) and defines explicit quality gates. Streams are: A (Schema+Workspace+PAT+Standalone), B (MCP+OpenAPI), C (Client+Mock+Stage), D (Fusion).

### What's Better Than My Approach

1. **The 3-tier migration classification is the strongest analysis of the three approaches.** Tier 1 (subscription commands, 10), Tier 2 (non-subscription legacy, 11), Tier 3 (already compliant, 11) -- this is actionable intelligence. My approach flags migration needs per wave but doesn't systematically classify the full population. The Hybrid's analysis of exactly which commands are compliant vs. not, and what specific issues each has (missing `AssertHasAuthentication`, using `PrintMutationErrorsAndExit`, etc.) is thorough and correct.

2. **The 4-stream model maximizes parallelism from day one.** All 4 streams can launch simultaneously. My approach has 7 sequential phases where earlier phases must complete before later ones start. The Hybrid only has the soft constraint that Stream D might want to wait for subscription patterns from another stream. This means more total throughput.

3. **Quality gates are explicit and graduated.** Gate 1 (entry), Gate 2 (per-command), Gate 3 (subscription extension), Gate 4 (stream), Gate 5 (project) -- this is mature project management. My approach has the test-rule checklist but doesn't formalize verification tiers. Quality gates would catch the kind of subtle misses that hurt test suites (e.g., `client.VerifyAll()` forgotten, mode coverage gaps).

4. **"Fix inline, not as a separate phase" is pragmatic.** The Hybrid says "fix the command, then write the test, in the same unit of work." My approach says "each wave's first task is to update commands." The Hybrid's approach is better because:
   - The agent fixing the command is the same one who will test it -- they have full context
   - There's no handoff between "migrator" and "tester"
   - The fix can be verified immediately by the test

5. **Cross-client dependencies are explicitly mapped.** Section 1.3 identifies that MCP create, OpenAPI create, Mock create/update, and Stage edit all inject `IApisClient` as a secondary dependency. This means their tests need mock setup for two client interfaces. My approach doesn't call this out -- agents would discover it while reading command source, which wastes time.

6. **Stream A is well-composed.** Grouping Schema + Workspace + PAT + Standalone into one stream is smart -- these categories have zero dependency overlap and span the full complexity range (simple queries, standard mutations, subscription commands, special commands). This gives Stream A's agent a complete skills ladder.

### What's Worse Than My Approach

1. **Streams are too large for single agents.** Stream A has 15 commands (4 schema + 5 workspace + 3 PAT + 3 standalone). Stream C has 13 commands. If each stream is one agent, the serialization within a stream defeats the purpose. If each stream is multiple agents, the intra-stream coordination is undefined. My approach is clearer: each wave has a defined parallelism count (e.g., "4 agents, one per command").

2. **MCP+OpenAPI structural isomorphism is noted but under-exploited.** The Hybrid puts them in the same stream and says they're "structurally isomorphic" but doesn't explain how to exploit this. Should one agent do MCP first and another copy the pattern for OpenAPI? Should they be interleaved? My approach explicitly says "implement MCP first as the template, then copy patterns to OpenAPI."

3. **The stream boundaries feel arbitrary in places.** Stream A groups schemas with workspaces, PAT, and standalone commands. These share nothing -- different client interfaces, different error types, different command patterns. The grouping exists to balance stream sizes, not because of logical dependency. My approach groups by actual shared infrastructure (client interface + domain).

4. **No subscription test pattern detail.** The Hybrid says "Verify `IAsyncEnumerable<T>` mock utility exists in test infrastructure; add if missing" and provides one code snippet. But it doesn't define the test matrix for subscription commands (what states to test, how to mock multi-step sequences, validate vs publish differences). Both Pattern Buckets and my approach provide detailed subscription test matrices with specific test cases per state.

5. **"Needs verification" is a gap.** Tier 3 lists 11 commands as "compliant" but then says "needs verification" for 8 of them. In practice, this means the agent starts writing tests, discovers the command uses `PrintMutationErrorsAndExit`, stops to fix it, then resumes testing. The Hybrid optimistically classifies commands to reduce the apparent migration scope, but the actual work is the same.

6. **Standalone commands in Stream A are misplaced.** `LaunchCommand`, `LoginCommand`, and `LogoutCommand` have nothing in common with schema or workspace commands. Including them in Stream A means the stream's "done" depends on solving `SystemBrowser.Open()` mockability and browser auth flow testing -- which are unique, unsolved problems. These special cases should be deferred (as both my approach and Pattern Buckets do), not mixed into the earliest stream.

### Elements I'd Steal

- **The 3-tier migration classification.** Systematically categorizing all 32 commands as Tier 1 (subscription migration), Tier 2 (non-subscription migration), or Tier 3 (test-ready) should be adopted by any approach. It lets agents immediately know if they need to fix implementation before writing tests.

- **Quality gates.** The 5-gate system is overdue. My approach references the test-rule checklist from research.md but doesn't formalize the verification process. Adopting explicit gates per command and per wave would improve reliability.

- **Cross-client dependency mapping.** Section 1.3's explicit identification of which commands inject multiple client interfaces is valuable. This should be part of any approach's pre-work analysis.

- **"Fix inline" strategy.** The command migration and test writing should be a single atomic unit of work per command, not separate phases.

---

## Comparative Summary

| Dimension | Pattern Buckets | Hybrid | Category Waves (Mine) |
|-----------|----------------|--------|----------------------|
| **Grouping logic** | Behavioral pattern | Client interface + phased complexity | Domain category |
| **Agent specialization** | High (one pattern type) | Medium (mixed within stream) | Low (mixed within category) |
| **Parallelism** | 4 phases, moderate | 4 streams, high from start | 7 phases, moderate |
| **Migration handling** | Vague ("dual mandate") | 3-tier classification, inline fix | Listed per wave, pre-requisite phase |
| **Subscription depth** | Good test matrices | Light treatment | Good test matrices + mock examples |
| **Quality gates** | None explicit | 5-tier, excellent | Checklist reference only |
| **Template/reuse** | Explicit template files | No templates | Reference implementation only |
| **Risk identification** | Minimal | Moderate | Detailed with mitigations |
| **Codebase fit** | Fights helper convention | Fits helper convention | Fits helper convention |
| **Feasibility** | Good for simple, risky for subscription | Good overall | Good overall |

### My Honest Assessment

**Pattern Buckets** has the best agent-efficiency model (specialization + templates) but the worst codebase fit (fights the established `*CommandTestHelper.cs` per-category convention) and under-specifies migration and verification.

**Hybrid** has the best project management (quality gates, migration tiers, parallelism from day 1) but is light on subscription testing strategy and has some stream composition issues.

**Category Waves (mine)** has good codebase fit and detailed risk analysis but is overly sequential (7 phases) and misses the agent-specialization insight that Pattern Buckets nails.

### If I Were Building the Final Plan

I'd take:
- **Pattern Buckets'** template files and Phase 0 infrastructure setup
- **Hybrid's** 3-tier migration classification, quality gates, cross-client dependency mapping, and "fix inline" strategy
- **Category Waves'** grouping logic (domain category), MCP+OpenAPI twin exploitation, subscription test matrices, and risk analysis

The ideal plan is category-grouped (for codebase fit and helper sharing) with explicit templates per pattern type (for agent efficiency), 3-tier migration pre-classification (for planning accuracy), quality gates (for verification), and within-category pattern ordering (simple commands first, subscriptions last).
