# Copy proposals: /platform/agentic-coding

Three options per section, distilled from six writer stances (3x Haiku, 3x Sonnet)
plus the current copy. Voice contract: calm, plain, technical; no hype; no em dashes;
only real symbols and verifiable claims. Pick one option per section (mixing is fine,
headlines and bodies were written to be swappable).

## Hero

### Option 1 (refined current) ACCEPTED

**H1:** Consistently good code, from any agent.
**Lead:** Agents are strong at filling a known pattern and weak at inventing architecture. The platform gives your agent the pattern to fill, your conventions as checked-in skills, and feedback it can act on before the merge, so what comes back is best-practice code.
**Gradient:** best-practice code.
**Caption:** One command teaches your agent the platform.

### Option 2 (review-outcome)

**H1:** Code agents that write like your team.
**Lead:** Agents are good at filling a known pattern and bad at inventing structure. The platform gives your agent the shape to fill, your conventions as versioned skills, and CI feedback it can act on before the merge. What comes back is code you can review in seconds.
**Gradient:** review in seconds.
**Caption:** Setup is one command.

### Option 3 (mechanism-led)

**H1:** The same good code from every agent.
**Lead:** Your agent already knows how to fill a pattern. The platform supplies the rest: one shape per recurring problem, your conventions installed as skills, and CI that flags a breaking change while the agent can still fix it. What comes back is code that reads like your team wrote it.
**Gradient:** your team wrote it.
**Caption:** Works with any agent that reads Agent Skills or speaks MCP.

## Directory

### Option 1 (refined current, contrast removed) ACCEPTED

**H2:** Bring the agent you already use.
**Body:** Every agent plugs into the platform the same two ways: skills teach it your conventions, MCP lets it call your API. Which agent you run is a preference; the quality of the code that comes back is the same.

### Option 2 (named-roster)

**H2:** Use the agent you already have.
**Body:** Claude, Codex, Copilot, Cursor, Windsurf, Gemini, Cline, and many more all plug in the same two ways: skills teach them your conventions, MCP lets them call your API. Switching agents changes nothing about the code that comes back.

### Option 3 (two-interfaces)

**H2:** Two interfaces, every agent.
**Body:** Anything that reads Agent Skills or speaks MCP works here. Skills carry your conventions into the agent, MCP lets it query your running API. Claude, Codex, Copilot, Cursor, Windsurf, Gemini, Cline, and many more already do both.

## Review

### Option 1 (refined current)

**H2:** Keep the time your agent saves you.
**Body:** Writing code is cheap now; reviewing it is the expensive part, and a slow review gives the saved time right back. Because every change is the same small, uniform shape, a review is a glance, and the time your agent saved you stays saved.

### Option 2 (diff-reading)

**H2:** Review is the expensive part now.
**Body:** Writing code is cheap now, and a slow review hands the saved time right back. Because every change fills the same small set of patterns, a diff reads the same way every time: same shape, same places to check, no surprise structure to reverse-engineer first.

### Option 3 (uniformity-first) ACCEPTED

**H2:** Review stays fast because changes stay uniform.
**Body:** Every change an agent produces on this platform is the same small, uniform shape, so review stays a glance instead of an investigation. That matters because writing code is cheap now; reviewing it is where your time actually goes.

## Skills (reframed: platform enables agentic work, skills are helpers)

### Option 1 (head start)

**H2:** Skills give your agent a head start.
**Body:** The platform is what makes agent output dependable; the patterns and the feedback do the heavy lifting before anything is installed. Skills are helpers on top: packaged conventions and workflows an agent picks up in one install instead of a long prompt. We ship a starting set, and your own conventions belong in the same place.
**Cards:**

- **Schema design and review** (`graphql-schema-design`): Proposes SDL in design mode and audits schema diffs in review mode, following the team's conventions.
- **Frontend prototype with mock data** (`prototype-feature`): Builds a clickable, local-only prototype with realistic mock data before any schema or backend work.
- **Prototype to backend contract** (`prototype-to-contract`): Turns an accepted prototype into colocated GraphQL fragments and a contract for the backend.

### Option 2 (working agentically) ACCEPTED

**H2:** Skills give your agent a head start.
**Body:** Prototype a feature, derive the contract, evolve the schema: on this platform those are workflows an agent can run, not rituals a developer performs. Skills package that working knowledge so any agent starts productive, and your own conventions ship the same way, reviewed like code.
**Cards:** (same as option 1)

### Option 3 (conventions install)

**H2:** Conventions your agent installs, not prompts you repeat.
**Body:** Instead of re-explaining the house rules in every prompt, install them once. A skill is a small reviewed file that tells any agent how the team works here; ours cover common workflows out of the box, and yours go in the same repo.
**Cards:** (same as option 1)

## Patterns tile

### Option 1 (refined current) ACCEPTEDJ

**H:** One pattern per problem.
**Body:** A query is an attributed static method, a DataLoader is a keyed batch function, an event handler is one interface. Agents fill a known shape instead of inventing structure, so two features written weeks apart come back looking the same.

### Option 2 (symbol-roster)

**H:** Every problem has a named shape.
**Body:** A query is a [QueryType] method, a mutation a [MutationType], a batch loader a [DataLoader]. An event handler is IEventHandler<T> or IBatchEventHandler<T>, a saga is Saga<TState>. Agents fill a known shape instead of inventing structure, so two features written weeks apart come back looking the same.

### Option 3 (consistency-outcome)

**H:** Known shapes, consistent output.
**Body:** Queries are [QueryType] methods, batching is [DataLoader], authorization is [Authorize], pagination is [UseConnection], filtering is [UseFiltering]. The agent fills the shape instead of designing structure, so its second feature looks like its first, and like the one your teammate's agent wrote.

## Feedback tile

### Option 1 (refined current)ACCEPTED

**H:** Feedback before the merge.
**Body:** A schema-first, strongly typed stack turns most bad edits into compile errors. nitro checks the rest in CI against the client registry, so a risky change comes back as feedback while the agent can still fix it.

### Option 2 (worked-example)

**H:** A breaking change is CI feedback.
**Body:** A schema-first, strongly typed stack turns most bad edits into compile errors. nitro checks the rest in CI against the client registry: an agent patch that removes Product.price comes back "breaking: published clients affected" while the agent can still fix it, for example by deprecating the field instead.

### Option 3 (two-layer)

**H:** Two layers catch bad edits.
**Body:** Most bad edits never build: the schema-first, strongly typed stack turns them into compile errors. nitro covers the rest in CI, checking every schema change on the PR against the client registry, so a change that would break published clients comes back as feedback while the agent can still fix it.

## MCP

### Option 1 (refined current) ACCEPTED

**H2:** Your API is a tool, too.
**Body:** The same server that shapes the code agents write also serves them at runtime. AddMcp() and MapGraphQLMcp() expose your operations as MCP tools at /graphql/mcp, so an agent can query the running API while it works instead of guessing at your data.

### Option 2 (objection-first)

**H2:** Stop guessing at the running API.
**Body:** An agent that cannot see your API guesses at your data. AddMcp() and MapGraphQLMcp() expose your GraphQL operations as MCP tools at /graphql/mcp, so any agent that speaks MCP can query the running API and work from real responses.

### Option 3 (one-server-both-roles)

**H2:** One server, design time and runtime.
**Body:** The platform shapes the code an agent writes and serves that agent while it works. With AddMcp() and MapGraphQLMcp(), your GraphQL operations become MCP tools at /graphql/mcp, so the agent can inspect real data through the running API instead of inferring it from source.

## Closing (reframed: no skill counting)

### Option 1 (no checklist) ACCEPTED

**H2:** Point your agent at the platform.
**Body:** The patterns, the feedback, and the checks are already in place; whatever agent your team uses writes against them. One command installs the helpers, and your conventions ride along the same way.
**Checklist:** none, closes on the command alone.

### Option 2 (platform behavior, 3 items)

**H2:** Ready when your agent is.
**Body:** Nothing on this page is configuration you have to build. It is how the platform behaves once your agent shows up.
**Checklist:**

- Any agent that reads Agent Skills or speaks MCP plugs in.
- A schema change that breaks a published client fails CI with "breaking: published clients affected".
- Diffs come back small and uniform, so review stays a glance.

### Option 3 (shortest)

**H2:** One command from here.
**Body:** Install the helpers, point your agent at the platform, and review the code it writes in a glance.
**Checklist:** none, closes on the command alone.
