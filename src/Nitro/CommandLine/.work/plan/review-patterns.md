# Cross-Review: Category Waves & Hybrid Approaches

**Reviewer**: Pattern Buckets architect
**Date**: 2026-03-29

---

## Review of Category Waves Approach

### What It Gets Right

**1. Schema commands first is the correct call.**
Category Waves puts schema commands in Wave A because they're already guidelines-compliant and establish the subscription pattern. This is correct. My Pattern Buckets approach buries the subscription infrastructure in Phase 3, which means agents writing simple list/create tests in Phase 1 don't validate subscription mocking early. Category Waves smokes out the hardest pattern (subscriptions) immediately. This is a significant advantage.

**2. MCP + OpenAPI as structural twins.**
Wave E pairs MCP and OpenAPI and calls them "identical twins." This observation is dead-on, and the pairing is natural. In Pattern Buckets, MCP and OpenAPI commands are scattered across Buckets A, B, D, F, and G -- the structural isomorphism is lost. An agent writing `ListMcpFeatureCollectionCommand` tests in Bucket A has no reason to coordinate with the agent writing `CreateMcpFeatureCollectionCommand` tests in Bucket B, even though they share client interface knowledge and test helper infrastructure. Category Waves preserves this domain cohesion.

**3. Risk identification is thorough and honest.**
The risk section calls out 10 specific items ranked by severity. `EditStagesCommand`, `FusionValidateCommand`, and `FusionConfigurationPublishValidateCommand` are correctly identified as the three hardest commands. The risk mitigation strategies are practical (e.g., "dedicate a senior agent" for EditStagesCommand, "test each phase independently" for FusionValidateCommand). My Pattern Buckets approach lumps `EditStagesCommand` into "Bucket H: Special" without this level of analysis.

**4. Implementation migration is called out per-wave.**
Each wave explicitly lists which commands need `PrintMutationErrorsAndExit` migration. This makes the work visible. Pattern Buckets mentions migration as a concern for Buckets F/G but doesn't systematically catalog which non-subscription commands also need migration (Bucket B has several).

### What It Gets Wrong

**1. 9 waves with 7 phases is too many sequential gates.**
The approach creates Waves A through I with Phases 1-7. Each phase "depends" on the previous one in some way (Wave A establishes patterns for Wave E, Wave G must complete before Wave H, etc.). In practice, Phases 2-5 could all run in parallel -- the ordering is partly artificial. The document claims "sequential with overlap" but the actual dependency chain is: A -> everything else (for subscription pattern) and G -> H (for Fusion publish flow). That's only 2 real dependencies, not 7 phases.

**2. Parallelism within waves is overstated.**
Wave E claims "up to 12 agents (6 MCP + 6 OpenAPI)." But within MCP, you have 6 commands: create, delete, list, upload, validate, publish. The validate and publish subscription commands can't start until the test helper is created and the subscription pattern is proven. The create and delete commands need `PrintMutationErrorsAndExit` migration first. Realistically, you get 2-3 agents running in parallel within a category, not 6.

**3. No template strategy.**
Category Waves relies on agents looking at completed test suites (API, Client, Environment) as reference implementations. But it doesn't propose creating explicit templates that agents copy. This means each agent for, say, `ListStagesCommand` has to mentally extract the list pattern from `ListApiCommandTests`, understand what to change, and apply it. Templates make this mechanical rather than cognitive. Pattern Buckets' template approach is more efficient here.

**4. Test helper creation is a serialization bottleneck.**
The approach says "the first command in each wave creates the test helper class." This creates a dependency: all other commands in the wave must wait for (or work around) the helper. In practice, agents either block or create inline mocks and then refactor. Either way, it adds friction. Pattern Buckets has the same problem but acknowledges it (recommending self-contained test files with private helpers).

**5. Wave D (PAT + Mock) is an awkward pairing.**
PAT and Mock are combined into a single wave because both are "small, independent categories." But they share nothing -- different client interfaces, different error types, different domain. The pairing is purely for scheduling convenience. This weakens the "category coherence" argument that justifies category-based grouping in the first place.

### What I Would Steal

- **Schema commands first**: Front-loading subscription pattern validation is better than deferring it to Phase 3.
- **Explicit migration catalog per wave**: Listing which commands need `PrintMutationErrorsAndExit` migration, per wave, makes the work plannable.
- **Risk-ranked items**: The tiered risk identification (highest/medium/lower) is more actionable than my pros/cons section.

---

## Review of Hybrid Dependency-Driven Approach

### What It Gets Right

**1. The dependency analysis is the most rigorous of the three approaches.**
Section 1 maps every command to its client interface, identifies cross-client dependencies (MCP/OpenAPI/Mock/Stage all inject `IApisClient`), and flags the MCP/OpenAPI isomorphism. This is analysis, not just listing. Pattern Buckets classifies by behavioral pattern but doesn't analyze the dependency graph. Category Waves groups by domain but doesn't trace cross-domain dependencies.

**2. The three-tier implementation readiness classification is excellent.**
Tier 1 (subscription commands needing full migration), Tier 2 (non-subscription commands needing `PrintMutationErrorsAndExit` migration), Tier 3 (already compliant -- test immediately). This is the most honest accounting of what needs to happen before tests can be written. Pattern Buckets doesn't distinguish between compliant and non-compliant commands within a bucket. Category Waves identifies migration needs per wave but doesn't classify by migration complexity.

**3. Quality gates are well-structured.**
Five progressive gates (stream entry, per-command, subscription extension, stream completion, project completion) with specific checkboxes. This is operationally useful -- an agent knows exactly what "done" means at each level. Neither Pattern Buckets nor Category Waves has explicit quality gates. I should have included something like this.

**4. "Fix + test in same unit of work" is pragmatic.**
The Hybrid approach says implementation fixes are done inline -- fix the command, then write the test. This avoids the problem of having separate "migration phase" and "testing phase" where commands are in a half-migrated state. Pattern Buckets risks this in Phase 3 where subscription commands may need migration AND tests.

**5. The 4-stream model is simple.**
Stream A (schema + workspace + PAT + standalone), Stream B (MCP + OpenAPI), Stream C (client + mock + stage), Stream D (fusion). Four parallel streams, each with internal phasing. This is easier to reason about than Pattern Buckets' 8 buckets with 4 phases or Category Waves' 9 waves with 7 phases.

### What It Gets Wrong

**1. Stream A is overloaded and unfocused.**
Stream A combines schema (4 commands), workspace (5 commands), PAT (3 commands), AND standalone (3 commands) -- 15 commands total with 4 different client interfaces. The stated rationale is "independent client interfaces with no cross-dependencies." But that argument applies to ANY combination of categories. The grouping is arbitrary -- there's no reason PAT tests benefit from being in the same stream as schema tests. This creates a mega-stream where the agent working on `LogoutCommand` has zero context from the agent working on `ValidateSchemaCommand`.

**2. The dependency-driven framing is weaker than it appears.**
The approach title emphasizes "dependency chains," but the actual stream assignments are mostly domain-based with some pragmatic bundling. Stream B is MCP + OpenAPI (domain grouping). Stream C is client + mock + stage (convenience grouping). Stream D is fusion (domain grouping). The dependency analysis in Section 1 identifies cross-client dependencies but doesn't meaningfully use them for stream design. For example, the fact that mock and stage commands inject `IApisClient` doesn't make them natural neighbors of client commands (which use `IClientsClient`).

**3. Stream D has a deferred-start caveat that undermines parallelism.**
The document says Stream D "should start after at least one subscription command has been successfully tested in another stream, so the subscription test patterns are proven." This creates a dependency between streams that contradicts the stated principle of "no stream depends on another stream's output." In practice, Stream D's simple commands (fusion download, settings set) could start immediately. Only fusion validate/publish need the subscription pattern. The stream should split the work internally rather than blocking the whole stream.

**4. No template strategy (same gap as Category Waves).**
The Hybrid approach provides detailed quality gates but no templates. An agent implementing `ListMcpFeatureCollectionCommand` gets a quality checklist but no starting skeleton. Quality gates tell you what to verify, not how to produce the artifact. Templates tell you how to produce it. You need both.

**5. The "51 tasks" count is confusing.**
The appendix says "51 tasks covers 32 commands plus implementation fixes counted as separate tasks." But the streams are organized to do fix + test as one unit of work. The task count inflates the apparent scope without adding clarity. The document should just say "32 commands, some requiring implementation fixes as prerequisites."

**6. Phase numbering within streams is potentially confusing at scale.**
Stream A has Phase A1 and A2. Stream B has B1, B2, B3. Stream C has C1, C2, C3. Stream D has D1, D2, D3. That's 11 sub-phases across 4 streams. When communicating progress ("we're in Phase C2"), it's not immediately clear what that means without looking up the document. Pattern Buckets' flat bucket naming (Bucket A: List, Bucket B: Create) is easier to communicate.

### What I Would Steal

- **Three-tier implementation readiness classification**: Tier 1 (subscription migration), Tier 2 (simple migration), Tier 3 (test immediately). This should be overlaid onto any approach.
- **Quality gates with explicit checklists**: Gates 1-5 with specific checkboxes. This is operational infrastructure that any approach benefits from.
- **"Fix + test as one unit of work"**: Avoids half-migrated limbo state.
- **Cross-client dependency analysis**: Knowing that MCP/OpenAPI/Mock/Stage all share `IApisClient` mock patterns is useful regardless of stream design.

---

## Comparative Summary

| Dimension | Category Waves | Hybrid | Pattern Buckets |
|---|---|---|---|
| **Organizing principle** | Domain category | Client interface dependency | Behavioral test pattern |
| **Number of work units** | 9 waves, 7 phases | 4 streams, 11 sub-phases | 8 buckets, 4 phases |
| **Max parallelism** | ~12 agents (Wave E) | ~4 streams, ~3-4 agents each | ~8 agents (one per bucket in Phase 1) |
| **Template strategy** | None (reference impl only) | None (quality gates only) | Explicit per-bucket templates |
| **Migration tracking** | Per-wave listing | Three-tier classification | Mentioned but not systematically cataloged |
| **Subscription handling** | Front-loaded (Wave A) | Deferred to Phase 3 per stream | Deferred to Phase 3 globally |
| **MCP/OpenAPI coherence** | Excellent (Wave E together) | Good (Stream B together) | Lost (scattered across buckets) |
| **Quality gates** | None explicit | Five-tier gates with checklists | None explicit |
| **Risk analysis** | Thorough, ranked | Moderate | Pros/cons only |
| **Infrastructure needs** | Minimal, per-wave helpers | Minimal, shared subscription util | Per-bucket infrastructure, subscription factory |
| **Complexity to explain** | Medium | Medium-high | Low-medium |

### Strongest elements per approach

- **Category Waves**: Front-loads subscription pattern (Wave A schemas), preserves MCP/OpenAPI twin structure, thorough risk analysis
- **Hybrid**: Three-tier readiness classification, quality gates, cross-client dependency analysis, fix+test integration
- **Pattern Buckets**: Template-driven agent workflow, clean bucket naming, behavioral pattern specialization, deferred complexity

### Each approach's blind spot

- **Category Waves**: No template strategy, overstated parallelism, too many sequential phases
- **Hybrid**: Stream A is overloaded, dependency framing doesn't fully justify stream design, deferred-start for Stream D breaks own rules
- **Pattern Buckets**: Scatters MCP/OpenAPI across buckets, defers subscription infrastructure too late, doesn't catalog which commands need migration per bucket

---

## Recommendations for the Final Plan

If I were synthesizing one plan from all three, I would:

1. **Use Pattern Buckets' behavioral grouping as the primary work structure** -- agents specialize by pattern, get templates, produce consistent output
2. **Overlay Hybrid's three-tier readiness classification** onto each bucket -- so agents know which commands in their bucket need migration vs. test-only
3. **Steal Category Waves' "schema first" sequencing** -- run schema commands (which span buckets D, F, G) as Phase 0 to validate subscription infrastructure early, rather than deferring all subscriptions to Phase 3
4. **Add Hybrid's quality gates** to every bucket -- the five-tier gate system is the best operational framework of the three
5. **Preserve MCP/OpenAPI co-assignment** -- even in pattern-based grouping, ensure the same agent handles both `ListMcpFeatureCollectionCommand` and `ListOpenApiCollectionCommand` within Bucket A, so the structural isomorphism is exploited
6. **Create explicit templates** per bucket (Pattern Buckets' contribution) -- neither alternative has this
