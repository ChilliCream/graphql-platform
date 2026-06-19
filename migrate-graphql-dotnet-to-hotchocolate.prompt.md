# Prompt: GraphQL.NET → Hot Chocolate Migration Guide (with working reference apps)

> Paste this whole file as the task prompt. It is self-contained, phase-structured, and
> resumable. **Before doing anything, read `MIGRATION_TASK_STATUS.md` (create it if missing,
> see Phase 0) and resume from the first incomplete checkbox.** Never restart from scratch
> if the status file shows completed phases.

---

## Mission

Produce a **thorough, accurate migration guide** in the Hot Chocolate website docs showing how to
migrate a **GraphQL.NET** project to **Hot Chocolate (v16, annotation-based / implementation-first)**.

The guide must be *proven*, not theoretical. You will:

1. **Research** GraphQL.NET's surface area and map every concept to its Hot Chocolate equivalent
   (parallel subagents).
2. **Build** a working GraphQL server using GraphQL.NET that exercises those features. Commit it.
3. **Write** the migration guide based on the real mapping.
4. **Apply** the guide yourself: port the reference server to Hot Chocolate, run it, prove it
   behaves identically. Commit it.
5. **Verify** the whole thing end-to-end.

You are the **orchestrator**. Delegate research and implementation to subagents. Keep your own
context clean for decisions and status tracking. For .NET implementation work prefer the
`dotnet-implementer` agent; for read-only fan-out research use `Explore` / `general-purpose`;
run independent subagents **in parallel** (multiple tool calls in one message).

---

## Hard constraints

- **Hot Chocolate target: latest v16**, annotation-based (implementation-first) style as the
  primary approach. Where GraphQL.NET's code-first `ObjectGraphType` maps more naturally to
  Hot Chocolate's fluent type API or schema-first, mention it but lead with annotation-based.
- **GraphQL.NET reference app must actually run** (server boots, schema introspects, queries
  resolve) before it is committed.
- **Ported Hot Chocolate app must produce an equivalent schema and equivalent query results.**
  "Equivalent" = same types/fields/operations and same data for a fixed set of test operations.
- **Do not mention "Claude" or any AI assistant** in commit messages, PR descriptions, code, or
  docs. Use conventional commit prefixes (`feat:`, `docs:`, `chore:`).
- **Branch policy:** the user asked to commit on the **current branch**. Check current branch
  first (`git rev-parse --abbrev-ref HEAD`). If it is `main`, **stop and confirm with the user**
  before committing to it — the global rule is "branch first on default branch." Otherwise commit
  on the current branch. Record the decision in the status file.
- **Docs style (from CLAUDE.md):** no em-dash sentences; use commas/periods/parentheses/colons.
  XML docs describe contract, not internals.
- **Code style:** file-scoped namespaces, 4-space indent, curly braces always.
- This is a documentation + example task. Keep the reference apps **small but representative**,
  not a kitchen sink. Every feature in the apps must appear in the guide and vice versa.

---

## Repository facts (verified)

- Migration guide goes here:
  `website/src/docs/hotchocolate/v16/migrating/migrate-from-graphql-dotnet.md`
- Register it in the sidebar at `website/src/docs/docs.json`, in the **v16 → `migrating`**
  section (the `items` array near line ~770, alongside `migrate-from-15-to-16`). Add as the
  first item:
  ```json
  { "path": "migrate-from-graphql-dotnet", "title": "Migrate from GraphQL.NET" }
  ```
- Existing migration guides in that folder are the **style reference** for tone, structure, and
  before/after code-block formatting. Read `migrate-from-15-to-16.md` and
  `migrate-from-12-to-13.md` before writing.
- Reference apps location: create `examples/migration/graphql-dotnet-to-hotchocolate/` with two
  projects:
  - `before-graphql-dotnet/` — the GraphQL.NET server
  - `after-hotchocolate/`     — the ported Hot Chocolate server
  Include a `README.md` and a shared `operations.http` (or `.graphql`) file of test operations
  used to prove equivalence. Do **not** add these to `src/All.slnx` unless the user asks.
- Website build: `cd website && yarn` then the docs dev server. .NET build: `dotnet build`.
- Sources of truth for GraphQL.NET: <https://graphql-dotnet.github.io/> and
  <https://github.com/graphql-dotnet/graphql-dotnet>. Use `WebFetch`/`WebSearch` and, for the
  newest package APIs, the `context7` MCP and `dotnet package search`.

---

## Status tracking & resumability protocol

**Create and maintain `MIGRATION_TASK_STATUS.md` at the repo root.** This is the single source of
truth for resumption. Rules:

- Update it **after every meaningful step** (not just phase boundaries): tick checkboxes, paste
  the commit SHA, note the current branch decision, record any blocking question.
- Each phase has an explicit **Done-when** gate. Do not tick a phase complete until its gate is met.
- Store intermediate research artifacts under `.work/migration/` (e.g. `research-*.md`,
  `feature-map.md`) so a fresh agent can rebuild context without re-researching.
- On resume: read this status file, read `.work/migration/feature-map.md` if present, then jump
  to the first unchecked item. **Never redo committed work.**

Template to write in Phase 0:

```markdown
# Migration Task Status

Branch: <name>   (decision: <committed here / branched to X / awaiting user>)
Last updated: <date>

## Phase 0 — Setup            [ ]
## Phase 1 — Research         [ ]
## Phase 2 — GraphQL.NET app  [ ]   commit: ____
## Phase 3 — Migration guide  [ ]
## Phase 4 — Port + verify    [ ]   commit: ____
## Phase 5 — Final review     [ ]

### Notes / blockers
-

### Test operations (equivalence set)
-
```

---

## Phase 0 — Setup

- [ ] `git rev-parse --abbrev-ref HEAD`; apply the **Branch policy** above; record decision.
- [ ] Create `MIGRATION_TASK_STATUS.md` from the template.
- [ ] Create `.work/migration/` for research artifacts.
- [ ] Read the two reference guides (`migrate-from-15-to-16.md`, `migrate-from-12-to-13.md`) and
      note the house style in the status file.

**Done-when:** status file exists, branch decision recorded, house style noted.

---

## Phase 1 — Research (parallel subagents)

Spawn research subagents **in parallel**, one per dimension. Each returns a structured markdown
section: *GraphQL.NET concept → API/example → Hot Chocolate v16 equivalent → API/example →
gotchas/behavior differences*. Each agent writes its result to
`.work/migration/research-<dimension>.md` AND returns a summary.

Dimensions (assign one subagent each, adjust as needed):

1. **Schema & type definition** — `Schema`, `ObjectGraphType<T>`, code-first vs schema-first
   (`Schema.For`/SDL) vs type-first; Hot Chocolate annotation-based `[ObjectType]`, `[QueryType]`,
   fluent `ObjectType<T>`, schema-first. Lead with annotation-based.
2. **Resolvers, fields & arguments** — `Field`/`FieldAsync`, `Resolve`, `IResolveFieldContext`,
   `QueryArgument`/`QueryArguments`, `GetArgument<T>`; Hot Chocolate resolver methods, `[Argument]`,
   parameter injection, `IResolverContext`.
3. **Scalars, enums, input types, interfaces, unions** — custom scalars, `EnumerationGraphType`,
   `InputObjectGraphType`, `InterfaceGraphType`, `UnionGraphType`; HC `[EnumType]`, input objects,
   `[InterfaceType]`, `[UnionType]`, built-in scalar differences.
4. **Mutations & subscriptions** — mutation conventions, `AddSubscription`, message transports;
   HC mutations, mutation conventions/errors, `[Subscribe]`, in-memory + topic event sources.
5. **DataLoaders & N+1** — GraphQL.NET `IDataLoaderContextAccessor`/`DataLoaderDocumentListener`;
   HC GreenDonut, source-generated `[DataLoader]`, batch/group/cache loaders.
6. **DI, server hosting & middleware** — `AddGraphQL`, `IDocumentExecuter`,
   `GraphQL.Server.Transports.AspNetCore`, `app.UseGraphQL`, UI middleware (GraphiQL/Playground/
   Altair); HC `AddGraphQLServer`, `MapGraphQL`, Nitro/Banana Cake Pop, request/field middleware,
   scoped services.
7. **Validation, errors, auth & advanced** — validation rules, complexity/depth, `ExecutionError`/
   error handling, `IValidationRule`, authorization (`AuthorizeAttribute`/policies); HC validation,
   cost analysis, `IError`/error filters, `[Authorize]`, global state, DI scoping. Also note
   Relay/connection pagination mapping (`Connection`/`ConnectionType` → HC `[UsePaging]`).

After all return, **you** synthesize `.work/migration/feature-map.md`: one master table/section
set covering every concept, plus a short list of **behavioral differences** that will trip up
migrators (default nullability, naming conventions, async-by-default, schema casing, error shape).

**Done-when:** `feature-map.md` exists and covers schema, resolvers/args, scalars/enums/inputs/
interfaces/unions, mutations, subscriptions, dataloaders, hosting/UI, middleware, validation,
errors, auth, and pagination. Tick Phase 1.

---

## Phase 2 — Build the GraphQL.NET reference server

Delegate to `dotnet-implementer`. Build `before-graphql-dotnet/` as a runnable ASP.NET Core app
using **current GraphQL.NET packages** (verify versions via `dotnet package search GraphQL` /
context7). It must exercise the feature set from `feature-map.md`, kept minimal but representative.
Suggested domain: a small **books/authors** API so relations exercise DataLoaders.

Must include, at minimum:
- Query type with object types, scalars, enums, an interface or union, list + nested fields.
- Arguments + an input object.
- At least one **mutation** and one **subscription**.
- A **DataLoader** resolving authors-for-books (demonstrate N+1 fix).
- A GraphQL **UI** middleware endpoint and a plain POST endpoint.
- Seeded in-memory data (no external DB needed; deterministic for equivalence tests).

Then:
- [ ] Build and **run** it; confirm it boots and introspects.
- [ ] Execute the equivalence **test operations** (queries, the mutation, ideally the
      subscription) and capture outputs into `.work/migration/before-results.md`. Record the exact
      operation set in the status file's "Test operations" section.
- [ ] Write the example `README.md` (how to run, endpoints, sample operations).
- [ ] **Commit** (`feat: add GraphQL.NET reference server for migration guide`). Record SHA.

**Done-when:** app runs, test operations produce captured results, committed, SHA in status file.

---

## Phase 3 — Write the migration guide

Delegate drafting to a docs-focused subagent (or the `docs-writer` skill), but **you** own final
accuracy review against `feature-map.md` and the running apps. Write to
`website/src/docs/hotchocolate/v16/migrating/migrate-from-graphql-dotnet.md` and register it in
`docs.json` (see Repository facts).

Required structure (match house style of existing guides):
1. **Intro** — who this is for, what changes conceptually (code-first GraphTypes → annotation-based
   POCO+resolvers), and the "mental model" shift.
2. **Package & hosting changes** — NuGet packages, `AddGraphQLServer`/`MapGraphQL` vs
   `AddGraphQL`/`UseGraphQL`, UI (Nitro vs GraphiQL/Playground).
3. **Section per concept** from the feature map, each with **before (GraphQL.NET) / after
   (Hot Chocolate)** code blocks pulled from (or consistent with) the real reference apps:
   types, resolvers & arguments, scalars/enums, input objects, interfaces/unions, mutations,
   subscriptions, DataLoaders, middleware, validation/cost, errors, authorization, pagination.
4. **Behavioral differences & gotchas** — nullability defaults, naming/casing, async-by-default,
   error shape, schema-first option.
5. **Checklist / summary table** mapping GraphQL.NET API → Hot Chocolate API.

Constraints: every code sample must reflect what actually compiles in the apps. No em-dashes.

**Done-when:** guide written, registered in `docs.json`, every concept from the map covered,
samples consistent with the committed reference app. Tick Phase 3.

---

## Phase 4 — Apply the guide: port to Hot Chocolate & verify

Delegate to `dotnet-implementer` / `framework-author`. Build `after-hotchocolate/` by **following
your own guide** (this validates the guide). Use latest Hot Chocolate v16 packages, annotation-based.

- [ ] Implement the equivalent schema and resolvers, DataLoader, mutation, subscription, UI.
- [ ] Build and **run** it; introspect the schema.
- [ ] Run the **same** equivalence test operations; capture into
      `.work/migration/after-results.md`.
- [ ] **Diff before vs after**: schema shape (allowing for documented naming/nullability
      differences) and query results (data must match). Record any intentional differences and
      make sure the guide explains them.
- [ ] If the port reveals the guide is wrong/incomplete, **fix the guide**, then re-port. Loop
      until guide and app agree.
- [ ] Write the example `README.md`; update the shared operations file if needed.
- [ ] **Commit** (`feat: add Hot Chocolate port + docs: migration guide` or split docs/app
      commits). Record SHA.

**Done-when:** HC app runs, results match the GraphQL.NET app for the test set (modulo documented
diffs), guide and app are consistent, committed, SHA in status file.

---

## Phase 5 — Final review

- [ ] Re-read the guide top to bottom for accuracy and house style (no em-dashes, correct paths,
      working sidebar link). Optionally run the website dev server to confirm it renders and the
      nav entry appears.
- [ ] Confirm `git status` is clean and both commits are on the intended branch.
- [ ] Summarize for the user: what was researched, the two app locations, the guide path, commit
      SHAs, the equivalence test results, and any open follow-ups (e.g. features intentionally
      omitted).
- [ ] Update `MIGRATION_TASK_STATUS.md` final state.

**Done-when:** all phases ticked, summary delivered.

---

## Definition of done (overall)

- `feature-map.md` covers the full GraphQL.NET surface mapped to Hot Chocolate v16.
- A runnable GraphQL.NET reference app and a runnable Hot Chocolate port both exist and are committed.
- The two apps produce equivalent results for a fixed, recorded operation set.
- The migration guide is written, registered in the sidebar, accurate against the apps, and in
  house style.
- No AI-assistant references anywhere. Branch policy honored. Status file reflects reality.

## Resume instructions (read first on any restart)

1. `git rev-parse --abbrev-ref HEAD` and `git log --oneline -5`.
2. Read `MIGRATION_TASK_STATUS.md` and `.work/migration/feature-map.md`.
3. Jump to the first unchecked checkbox. Trust committed work; do not redo it.
