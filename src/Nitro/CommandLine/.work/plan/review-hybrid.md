# Cross-Review from the Hybrid Dependency-Driven Architect

Reviewer: Hybrid approach author
Documents reviewed:
1. `approach-category-waves.md` (Category Waves)
2. `approach-pattern-buckets.md` (Pattern Buckets)

---

## Review: Category Waves

### What It Gets Right

**Schema-first ordering is well-reasoned.** The insight that schema commands are already guidelines-compliant and should go first is correct and matches my own analysis. This avoids the cold-start problem -- you get a working subscription test pattern before tackling the messier commands.

**Risk identification is thorough.** The three highest-risk items (`EditStagesCommand`, `FusionValidateCommand`, `FusionConfigurationPublishValidateCommand`) are accurately identified. The medium-risk section covering the 10 subscription commands needing migration is also on point.

**The MCP+OpenAPI pairing in Wave E is the right call.** These are structural twins and must be done together. Both my approach and this one agree on this.

**Subscription mock code examples are concrete and usable.** The `ToAsyncEnumerable` pattern with inline examples for success, failure, multi-step, and empty subscription scenarios gives implementers something copy-pasteable. This is more actionable than my approach's prose description.

### What It Gets Wrong

**Sequential wave ordering kills parallelism.** The approach defines 7 phases where each phase largely depends on the prior one. Phase 1 must complete before Phase 2 starts. Phase 5 depends on Phase 4. This is a 7-step waterfall disguised as parallel work. Only Phases 2 (Workspace + Stages) and 5 (Client + Fusion) have true internal parallelism.

In practice, there is **no real dependency** between Waves A through I. Schema tests don't produce artifacts that workspace tests need. The "subscription pattern" is simple enough to establish in any stream independently. The approach conflates "pedagogical ordering" (learn patterns first) with "execution dependency" (can't build X without Y). These are not the same thing when you have multiple agents -- you can teach each agent the pattern upfront rather than making them wait for Wave A to demonstrate it.

**Implementation migration is an afterthought.** The approach identifies that ~20 commands need `PrintMutationErrorsAndExit` migration, but treats it as a "pre-requisite" checkbox before each wave. There's no strategy for how to batch these migrations, who does them, or how to verify they don't break existing behavior. My approach integrates migration as an inline step within each stream's phasing.

**Wave sizing is unbalanced.** Wave E (MCP + OpenAPI) has 12 commands and an estimated 154-188 tests. Wave I (Standalone) has 3 commands and 16-24 tests. This creates a massive bottleneck at Phase 4 while Phase 7 is trivial. Better to redistribute: spread standalone commands into earlier phases, or start MCP/OpenAPI earlier.

**No quality gates between phases.** The approach has no formal "what must be true before moving from Phase N to Phase N+1." It's implicit that tests pass, but there's no checklist for implementation compliance, no gate for subscription infrastructure readiness, no definition of "done" beyond "tests exist."

**The test count estimates are inflated.** 500-620 new test methods for 32 commands is ~16-19 tests per command on average. Based on the completed reference implementations (e.g., `ListApiCommandTests` has 11, `CreateApiCommandTests` has 27), the realistic average is closer to 12-15 for non-subscription commands and 18-25 for subscription commands, yielding ~400-500 total. The inflated estimates risk over-scoping.

### What I Would Steal

**The concrete `ToAsyncEnumerable` code examples.** My approach describes the need for subscription mocking infrastructure but doesn't provide copy-paste code. Category Waves gives you the exact pattern with four scenarios. I'd lift this into my approach.

**The per-wave test count estimates.** Even if slightly inflated, having per-category estimates helps with capacity planning. My approach doesn't estimate test counts at all.

---

## Review: Pattern Buckets

### What It Gets Right

**The core insight is strong.** The observation that "the test patterns ARE the primary complexity" is accurate. An agent that writes 8 list command tests back-to-back will be faster and more consistent than one that writes a list test, then a create test, then a subscription test, all within the same category. Pattern specialization reduces context-switching overhead.

**Template-first methodology is the right approach for this codebase.** The idea of maintaining `.work/templates/` with concrete, annotated test files that agents copy and adapt is excellent. This codebase has extremely repetitive test patterns -- the difference between `ListApiCommandTests` and `ListMcpFeatureCollectionCommand` tests really is just type names and argument paths. Templates would eliminate an entire class of mistakes (missed mandatory test cases, wrong assertion methods, inconsistent naming).

**Subscription commands are correctly isolated into their own phase.** By deferring all 10 subscription commands to Phase 3, the approach ensures that 22 of 32 commands can be tested without solving the subscription problem at all. This de-risks the project timeline significantly.

**The test matrices for subscription commands (Section 5) are the best artifact across all three approaches.** The concrete table showing every test case with mutation setup, subscription events, and expected outcome is exactly what an implementer needs. Neither my approach nor Category Waves provides this level of specificity.

**Honest cons section.** The approach doesn't hide its weaknesses -- it acknowledges cross-category context switching, helper fragmentation, and the hybrid command problem. This intellectual honesty makes the pros more credible.

### What It Gets Wrong

**Cross-category context switching is worse than acknowledged.** The approach says "templates minimize domain knowledge needed" but this underestimates the real cost. When writing `CreateMcpFeatureCollectionCommand` tests, the agent needs to:
1. Read the command implementation to understand its options, prompts, and error branches
2. Find the `.graphql` file to enumerate mutation error types
3. Understand the `IApisClient` dependency for API selection prompts
4. Understand the `IMcpClient` mock interface and its method signatures

This is domain-specific work that can't be templated away. An agent doing all MCP commands together amortizes this domain learning cost. An agent doing MCP create, then OpenAPI create, then PAT create, then Mock create pays the context-switch cost 4 times.

**Helper fragmentation is a real problem, not a theoretical one.** The approach says "each test file should be self-contained with its own private helpers" but this contradicts the established codebase pattern. The existing test suites use shared `*CommandTestHelper.cs` files (e.g., `ApiCommandTestHelper.cs`) precisely because payload factories are reused across commands within a category. If Bucket A (List) creates an `McpCommandTestHelper` with list page factories, and Bucket B (Create) later needs MCP creation payloads, do they extend the same helper? Create a second one? Inline? The approach doesn't resolve this.

**`mock update` in Bucket C (Show/Query) is a categorization error.** `UpdateMockCommand` is a mutation that takes file I/O inputs, updates a resource, and has typed error branches. It belongs in Bucket B (Create/Mutation) or a hybrid of B+D. Placing it in Show/Query because it "reads something" mischaracterizes its test surface. The test suite for `UpdateMockCommand` will look nothing like `ShowWorkspaceCommand` tests.

**Bucket H is a dumping ground.** Four commands with nothing in common: `FusionSettingsSetCommand` (simple mutation), `EditStagesCommand` (complex interactive), `FusionRunCommand` (process management), and the 4-command fusion publish flow (stateful pipeline). Calling these "Special" and deferring them to Phase 4 means the hardest commands in the entire project get the least structure. The fusion publish flow alone is 4 commands with state dependencies -- it deserves its own phased plan, not a line item in "Special."

**Phase 0 proposes unnecessary infrastructure.** A `FileSystemTestHelper` and `SubscriptionTestHelper` as shared utilities add abstraction overhead that isn't justified. The `IFileSystem` mock setup is 2-3 lines of code -- a helper method adds indirection without meaningful benefit. The `ToAsyncEnumerable` helper is similarly trivial. Inline these patterns rather than creating shared classes that become coupling points.

**No implementation migration strategy.** Like Category Waves, this approach identifies that subscription commands need migration but doesn't integrate the migration work into the phasing. "Accept this" is not a strategy. Phase 3 agents having a "dual mandate" (fix commands AND write tests) means those agents are doing fundamentally different work (production code changes vs test writing) and may produce inconsistent fixes across the 10 subscription commands.

**Counting error: 32 commands not 51.** The Bucket assignments account for some commands twice or miss others. Let me verify: Bucket A (8) + Bucket B (9) + Bucket C (3) + Bucket D (5) + Bucket E (3) + Bucket F (5) + Bucket G (5) + Bucket H (4 + 4 fusion publish = 8) = 46. But we only have 32 commands. Some commands appear in multiple buckets (e.g., workspace commands appear in both A and B), and the fusion publish flow (5 commands) in Bucket H doesn't fully match the count in research.md. The counting is loose.

### What I Would Steal

**The template methodology.** This is the single best idea across all three approaches and one that my hybrid approach completely lacks. Concrete, annotated template files per pattern type would dramatically accelerate implementation and ensure consistency. I would create templates organized by pattern (list, create, delete, upload, download, validate-subscription, publish-subscription) but have them executed within my dependency-driven stream assignments.

**The subscription test matrices.** The concrete table in Section 5 showing (Test, Mutation setup, Subscription Events, Expected outcome) for both validate and publish commands is exactly what implementers need. My approach describes the tests in prose; Pattern Buckets gives you a decision table you can execute mechanically.

**Phase boundary clarity.** The numbered phases with clear agent assignments ("Agent 1: Bucket A, Agent 2: Bucket B") make resource planning concrete. My approach has "streams" that are conceptually parallel but doesn't assign them to specific execution units.

---

## Comparative Summary

| Dimension | Category Waves | Pattern Buckets | Hybrid (Mine) |
|---|---|---|---|
| **Parallelism** | Low -- 7 sequential phases | Medium -- 4 phases, 2-3 parallel | High -- 4 fully independent streams |
| **Implementation migration** | Acknowledged, not integrated | Acknowledged, not integrated | Integrated inline per stream |
| **Subscription handling** | Schema-first then per-category | Isolated Phase 3, all 10 together | Per-stream, last within each stream |
| **Agent specialization** | By domain (category expert) | By pattern (list expert, create expert) | By dependency chain (client-interface expert) |
| **Template/scaffold support** | None | Explicit templates per pattern | None |
| **Test matrices** | Subscription mock examples | Full decision tables | Prose coverage requirements |
| **Helper strategy** | Per-category shared helpers | Self-contained per test file | Per-client-interface helpers (on demand) |
| **Risk handling** | Good risk identification | Honest pros/cons | Front-loads simple, defers complex |
| **Quality gates** | None formal | Phase boundaries | 5 explicit gates |
| **Biggest weakness** | Sequential bottleneck | Helper fragmentation + context switching | No templates, no test count estimates |

### If I Were Starting Over

I would keep my hybrid stream structure (the parallelism advantage is real) but incorporate:
1. Pattern Buckets' **template files** -- one per behavioral pattern, executed within stream context
2. Pattern Buckets' **subscription test matrices** -- concrete decision tables, not prose
3. Category Waves' **subscription mock code examples** -- copy-paste ready
4. Category Waves' **per-wave test count estimates** -- for capacity planning

The core weakness of my approach is that it optimizes for parallelism and dependency awareness but doesn't provide enough concrete scaffolding for the agents actually writing tests. Templates + decision tables fill that gap.
