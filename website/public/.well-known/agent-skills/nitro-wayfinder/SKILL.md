---
name: nitro-wayfinder
description: Reproduce the wayfinder-on-nitro-agent-tasks planning workflow used for the Nitro service map. Use when the user wants to plan a large fuzzy feature the same way, invokes /wayfinder with nitro agent tasks as the tracker, or says 'plan this like the service map'. Composes /wayfinder, the sibling nitro-task and nitro-task-orchestrator skills, /batch-grill-me, /research, and /prototype; explains the operating rhythm, not the individual skills.
---

<!-- Ported command-wise from the wayfinder-br-workflow skill at ~/.claude/skills/wayfinder-br-workflow/SKILL.md. -->

# Wayfinder on nitro agent tasks: the operating rhythm

This is the concrete workflow that took the service map from "add a service map based on telemetry data" to a frozen spec plus an implementation-ready ticket graph. The component skills (/wayfinder, nitro-task, /batch-grill-me, /research) define their own mechanics; this file records how they compose, because the composition is where the value was.

## Roles of each piece

- **nitro agent tasks is the only memory.** Sessions die and compact; the tracker survives. Everything decided lives in exactly one closed ticket; the map issue only indexes it. Never rely on conversation context for anything a future session needs.
- **/wayfinder supplies the structure**: one map issue (label `wayfinder:map`), child tickets that are DECISIONS not build tasks, native nitro agent tasks dependency edges for the frontier, a fog section for what cannot be phrased sharply yet, and an out-of-scope section.
- **/batch-grill-me is the resolution engine for HITL tickets**: rounds of numbered questions covering the whole current frontier of that one decision, each with a recommended answer, waiting on the user between rounds.
- **/research and /prototype resolve the AFK tickets**: facts from primary sources, and cheap runnable artifacts when only execution can answer ("does this MV design work on the real database version?"). Findings land on throwaway `research/<name>` branches, linked from the ticket.

## Session grammar

The user drives with two words:

- **Charting session** (first invocation, loose idea): grill to pin the destination, then a breadth-first grill across the whole space to surface the initial decision tickets and the fog. Create map + tickets, wire dependencies in a second pass (issues need ids first), fire research subagents for every research ticket in parallel, stop. Chart, do not resolve.
- **"next" / "next session"**: load the map, claim the first unblocked decision ticket (or the named one), resolve exactly ONE ticket, then stop. One decision per session is a hard rule; the temptation to chain into the next decision is how context quality degrades.

## Resolving one decision ticket (the loop that repeats)

1. Claim the ticket in nitro agent tasks first (`nitro agent tasks update --actor "$ACTOR" <id> --status in_progress`) so parallel sessions skip it.
2. Zoom as needed: read the closed tickets this one depends on, in full, with `nitro agent tasks show <id> --output json`. The map gives one-line gists; the tickets hold the real contracts.
3. Run /batch-grill-me scoped to this decision. Per round: every currently-askable question, numbered, each with a recommendation. When a question needs a FACT (what does SigNoz do, what does the industry do about cardinality caps), never ask the user; dispatch a research subagent mid-round and let only the downstream questions wait for it. Feed findings back into the next round.
4. Communication discipline the user enforced: terse, on point, plain language, every question self-contained (context visible in the question itself, never "see my reasoning above"; compressed dialog-box questions without context get rejected as "what?").
5. When the decision lands, write the ANSWER as a resolution comment on the ticket, close it, and append one line to the map's Decisions-so-far.
6. Then do the graduation pass, which is the actual engine of progress: what did this answer unlock? Create the newly-statable tickets (create, then wire edges), promote fog entries that became sharp, close tickets the answer invalidated, and rule things out of scope explicitly. The hot/settled-split ticket only existed because resolving the pairing decision surfaced the multiple-children problem.
7. `nitro agent tasks sync --flush-only`. Git stays the user's.

## Prototype tickets

When a decision hinges on "does this actually work", resolve the ticket with a prototype instead of conversation: a subagent builds a minimal runnable artifact (for the service map: real ClickHouse 26.4 container, real DDL, scenario SQL) on a research branch, and its findings.md becomes evidence for the next grilling round. Prototypes are allowed to FAIL a design; the service-map prototype falsified the naive repair design and that failure produced the hot/settled split. Guard prototype agents against hanging queries (max execution time) and expect that subagents may not be able to write report files themselves; the orchestrator commits findings to the branch.

## Reaching the destination

When the map has no open decision tickets and the fog is empty, the way is clear. Then, in order:

1. Write the destination spec as a real file in the repo (docs/<feature>/...), assembled from the closed tickets' resolutions; every locked parameter and byte-level contract goes in, because implementation agents will treat it as authoritative.
2. Cut implementation tickets in nitro agent tasks, clearly prefixed (`[svc-map] impl:`), grouped under area milestones, wired into a dependency chain that starts at storage and ends at tests. Milestones depend on their children only. Beware the `--parent` flag: it creates blocking parent-child edges invisible to `nitro agent tasks dep cycles` and can deadlock the ready queue; group by prefix and label instead.
3. Cross-review the implementation tickets before building (blind peer review, adjudication to an empty disputed set, settlement applied back into the ticket texts). Escalate only genuine product decisions to the user; settle technical disputes with file:line evidence.
4. Hand off to execution (for the build phase itself see the sibling nitro-task-orchestrator skill).

## Failure modes actually hit, and the counters

- Compaction mid-effort: harmless BECAUSE everything lived in nitro agent tasks; the resummarized session reloaded the map and continued. Test this assumption by never writing load-bearing content only in chat.
- Rejected dialogs: re-issue the same AskUserQuestion once if the user says "ask again"; if the user answers with confusion instead of a choice, the question lacked context; rewrite it self-contained in plain prose.
- Fog pre-slicing: do not cut fog into ticket-sized pieces early; a fog patch may become several tickets or none once the frontier reaches it.
- Scope creep in tickets: a decision ticket that starts producing deliverables is a sign the map is done in that region; stop and hand off instead.
