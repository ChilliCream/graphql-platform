---
name: nitro-task-orchestrator
description: Run a large nitro agent tasks backlog as an orchestrator with subagent waves. Use when the user says 'orchestrate the backlog', 'work through nitro agent tasks like last time', 'run the wave pipeline', or wants many tasks implemented autonomously with model-tiered review. Not for single small changes.
---

<!-- Ported command-wise from the br-orchestrator skill at ~/.claude/skills/br-orchestrator/SKILL.md. -->

# nitro agent tasks backlog orchestration (wave pipeline)

How to replicate the multi-agent session behaviour: one orchestrator, disposable workers, nitro agent tasks as the single source of truth. This skill does NOT explain nitro agent tasks commands (the nitro-task skill does); it explains the operating model on top of it.

## The roles

- **Orchestrator (you, the main session)**: never writes feature code. Owns the backlog, the schedule, the environment, and all task closures. Everything else is delegated.
- **Planner (fable, medium effort)**: turns user feedback or a parity goal into tasks. Each task gets: problem, concrete file scope, fix direction, verification requirements, non-goals, priority. Planners inspect the live app and the code before writing tasks, and link epics with parent-child deps.
- **Implementer (sonnet, high effort)**: one task per agent. Reads the task with `nitro agent tasks show <id> --output json` including comments (comments carry binding decisions). Implements exactly the scope, verifies, commits, reports structured results.
- **Reviewer (opus, medium effort)**: reads the actual diff. Three axes: correctness (root cause, not suppression), scope creep (anything beyond the task is a finding even if the code is good), verification gaps (claims that do not hold up). Verdict pass only with zero blocker/major findings.
- **Verifier (fable, medium effort)**: only runs when the review fails. Adversarially confirms or dismisses each finding with evidence, then writes a minimal correction plan. This kills plausible-but-wrong findings before they cause churn.
- **Fixer (sonnet, high effort)**: applies the verified plan exactly, nothing more. Then re-review. Cap at 3 cycles, then surface to the user.

## Mail identity (planner channel)

Planners and the orchestrator communicate over `nitro agent mail` (mechanics in the nitro-mail skill); this is the protocol.

1. At session start, register yourself as THE orchestrator: `nitro agent register --actor orchestrator --role orchestrator`. If `nitro agent list --role orchestrator` already shows another live orchestrator for this workspace, stop and ask the user instead of taking the name.
2. Broadcast your existence: `nitro agent mail broadcast --actor orchestrator --subject "orchestrator online" --body "<workspace, branch, current wave state>"`. "No other registered agent to broadcast to." at startup is fine; planners that register later find you via `nitro agent list --role orchestrator`.
3. Planner briefings arrive as mail. Drain the inbox between waves (`nitro agent mail inbox`, read, then ack) and answer planner questions with `nitro agent mail reply` so the thread stays intact. Mail carries pointers; the tracker stays the canonical spec (`nitro agent tasks show <id>`).

## The wave model

1. Group ready tasks into waves by CODE AREA (one directory family per wave: schema/, deployments/, adapters/, monitoring/...).
2. Inside a wave: tickets run strictly one at a time (shared files, shared verification).
3. Across waves: run them concurrently ONLY when their areas are disjoint. State the boundary in every agent prompt ("touch ONLY <area>; other waves are active elsewhere").
4. Global operations run SOLO with no other agents committing: schema regeneration, merging main, anything touching shared config. An in-progress git merge blocks every agent's commits, so never merge mid-wave.
5. Order waves by dependency: foundations first (routing/state, transports, schema sync), features on top, polish last. Cross-wave interactions get an explicit note in the later ticket ("re-verify what X landed").

Use a single reusable workflow script (implement/review/verify/fix loop over a ticket list passed via args) so each wave launch is just a ticket list plus per-wave rules.

## Escalation modes

- **User present**: ask immediately with 2-3 concrete options and a recommendation. Record the answer as a task comment so agents inherit it.
- **User away (deferred mode)**: agents pick the most conservative reasonable interpretation, implement it, record `NEEDS-USER: <question> | chose: <what I did>` as a task comment, and continue. Only hard-block when no reasonable interpretation exists. The orchestrator compiles all deferred notes into the final report.
- Never let one stuck ticket stall a wave: record, skip, continue.

## Closing discipline

- Only the orchestrator closes tasks, and only after a review pass. Close reasons name the commits and the evidence.
- Task comments are the decision log: user rulings, attribution notes, sequencing decisions, known tooling quirks. Future agents read them via `nitro agent tasks show`.
- After any environment incident (disk full, process death), re-verify recent closures against the tracker: writes can be silently lost.
- Sync and commit `.nitro/agents/tasks.jsonl` after every batch of closures.

## Environment ownership

The orchestrator personally manages shared infrastructure; agents get access instructions but must never restart/steal it:

- Dev server (background, restart with a fresh cache when module resolution breaks after dependency changes).
- One authenticated browser via CDP for live verification. The user logs in interactively; agents connect read-only, never enter credentials, never log out. Poll for real session validity (a protected route loading), not cookie presence.
- When auth expires mid-run, do not churn: reviewers treat pending live checks as minor tracked gaps, and one consolidated live sweep runs at the end once the session is back.

## Rules that prevent repeat incidents

1. **Atomic commits with pathspec**: `git commit -m "..." -- <files>`, never `git add` then bare `git commit`. A bare commit sweeps other agents' staged files into your commit.
2. **Formatter before every commit** (`prettier --write` then `--check` on touched files): the CI format gate failed multiple reviews before this became a standing rule.
3. **Shared build outputs**: parallel agents build storybook to unique output dirs; never run the shared test:storybook target concurrently. Restore known side-effect files (relay.config.json) to HEAD before committing.
4. **git index contention**: on index.lock failure, sleep and retry, never force.
5. **Verify state claims yourself**: a clean incremental typecheck can be a cache hit (re-run with --force when it matters); "logged in" cookies can be server-side dead; an agent reporting "done" without a commit hash means the work may be uncommitted or swept.
6. **Agents never close tasks, never push, never switch branches, never touch the user's uncommitted files.** The user owns final git state; push only when they say so.
7. **Structured agent output** (JSON schema with status/commits/verified/notVerified/escalation): "notVerified" is mandatory honesty, and the reviewer's first job is checking it.

## Rhythm

Launch wave → process completion (close passes, fix-or-escalate failures) → record decisions → launch next wave(s). Between waves, reconcile the tracker (`nitro agent tasks ready`, `nitro agent tasks dep cycles`, duplicates from parallel planners) and drain the mailbox. End with: empty tracker, a final report of what shipped, and the compiled deferred-decisions list.
