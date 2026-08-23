# Codex CLI 0.149.0 hooks fixtures (spike S1, perles-net-k3j.1)

Captured live on 2026-08-23 against `codex-cli 0.149.0`
(`/home/pascal/.codex/packages/standalone/releases/0.149.0-x86_64-unknown-linux-musl/bin/codex`).
See the perles-net-k3j.1 findings comment for the full write-up and
everything that could not be verified.

## Correction to the plan's premise (important)

`.work/mail-notify-plan.md` assumes Codex hook event names are snake_case
on the wire (`session_start`, `user_prompt_submit`). That is WRONG. The
live `hook_event_name` field value is PascalCase, the same convention
Claude Code uses: `"SessionStart"`, `"UserPromptSubmit"`, `"SessionEnd"`
(also `"PreToolUse"`, `"PostToolUse"`, `"PermissionRequest"`,
`"PreCompact"`, `"PostCompact"`, `"SubagentStart"`, `"SubagentStop"` per
the JSON-schema `const` strings embedded in the compiled binary, though
only the first five in that combined list were seen live this spike). The
`hooks.json` config keys are also PascalCase (`"SessionStart"`, etc, see
`~/.codex/hooks.json` and the pre-existing `herdr` `SessionStart`
registration). The only place snake_case appears is Codex's internal,
undocumented `config.toml` `[hooks.state."<path>:<event>:<i>:<j>"]` trust
bookkeeping key, which lower-snake-cases the event name for its own
identifier, e.g. `hooks.json:session_start:0:0`. That internal key format
is NOT the wire schema and should not be used as one.

Headline facts these fixtures demonstrate:

- **Context injection works, it is not input-only.** Both `SessionStart`
  and `UserPromptSubmit` hook responses support
  `{"hookSpecificOutput": {"hookEventName": "...", "additionalContext":
  "..."}}` (same shape as Claude Code) and the injected text was
  independently confirmed to reach the model: see
  `evidence.additional-context-injection-confirmed.txt`. The plan's
  fallback framing ("if hooks are input-only, the Codex digest rides
  exclusively on notify+queue") does not apply; Layer A's turn-boundary
  digest can ride `additionalContext` on Codex too, matching the Claude
  design.
- Payload field names observed live: `session_id`, `transcript_path`,
  `cwd`, `hook_event_name`, `model`, `permission_mode`, `source` (
  `SessionStart` only, value `"startup"` for a fresh session), `turn_id`
  and `prompt` (`UserPromptSubmit` only), `reason` (`SessionEnd` only,
  value `"other"` observed; other values not exercised this spike).
- Hook **trust is established silently, non-interactively, with a
  one-run delay**: the first `codex exec` invocation after a new/changed
  `hooks.json` entry silently records a `trusted_hash` in
  `config.toml`'s `[hooks.state]` table but does not run that hook on
  that same invocation; from the next invocation onward the hook runs,
  still with no prompt, no stderr message, no exit-code signal. There is
  no interactive confirmation step in `codex exec` at all (searched the
  transcript and stdout/stderr for "trust", found nothing). This means a
  first-run adapter install will silently miss its first turn-boundary
  event; the installer/doctor should account for that (e.g. a synthetic
  warm-up invocation, or documenting the one-run lag).
- `hooks.json` supports multiple independent hook-group entries per
  event (an array of `{"hooks": [...]}` groups); ours coexisted cleanly
  alongside the pre-existing `herdr` `SessionStart` hook without needing
  to touch herdr's entry.
- `SessionEnd` hook timeouts are silently clamped to 3s regardless of the
  configured value (`clamping SessionEnd hook timeout to 3s in
  ~/.codex/hooks.json`, observed even for an untrusted, not-yet-running
  hook, so this validation happens at config-load time, independent of
  trust).
- No `SessionEndHookSpecificOutputWire` (or `PreCompact`/`PostCompact`/
  `SubagentStop`) type exists in the compiled binary's embedded JSON
  schema, unlike `SessionStart`/`UserPromptSubmit`/`PreToolUse`/
  `PostToolUse`/`PermissionRequest`/`SubagentStart` which all have a
  dedicated `*HookSpecificOutputWire` type. This is static (not live)
  evidence that `SessionEnd` has no structured response contract
  (fire-and-forget, consistent with the session already ending); not
  independently exercised live since there is no observable effect to
  test for a hook whose session has already ended.

## Operational risk finding: hooks.json is process-wide, not per-invocation

`~/.codex/hooks.json` applies to every `codex` process reading that
`CODEX_HOME`, including other agents' concurrently-running sessions, not
just the one that installed it. Installing a tee/capture hook during this
spike transiently captured `PreToolUse`/`PostToolUse`/`SessionStart`
payloads from a real, unrelated, concurrently-running agent session
(different `cwd`, different actor) that happened to be active on this
machine at the same time. Those captures were discarded, not committed;
only payloads from sessions this spike itself started are in this
directory. Any future adapter/installer work should treat this as a
correctness and privacy constraint: a per-`CODEX_HOME` hook cannot be
scoped to "this one session", and a tee-style capture mechanism used for
debugging in production must not write concurrent unrelated sessions'
tool calls to a shared/committed location.

## What could not be determined this spike

- `PreToolUse`/`PostToolUse`/`PermissionRequest`/`SubagentStart`/
  `PreCompact`/`PostCompact`/`SubagentStop` payload and response shapes:
  observed only via the contaminated capture (discarded, see above) and
  via static strings in the compiled binary (field names `tool_name`,
  `tool_input`, `tool_response`, `tool_use_id` were visible in the
  contaminated `PreToolUse`/`PostToolUse` payloads before being
  discarded, and independently corroborated by the `omnigent` package's
  Codex hook integration installed on this machine, which documents the
  same field names and the same `permissionDecision` /
  `permissionDecisionReason` / `additionalContext` response contract for
  those events). Not committed as a clean fixture; would need a
  dedicated, isolated capture run (ideally with no other concurrent
  Codex sessions on the machine) to do safely, out of this spike's
  scope.
- Whether a `SessionEnd` hook's response (if any were returned) has any
  effect. Static evidence above says no; not exercised live.
- `notify` (the separate `config.toml`-level program invoked on
  approval/queue events) is a different mechanism from `hooks.json` and
  was not touched or captured this spike; that is S2 (`perles-net-k3j.2`)
  and S4's scope per the plan's Phase 0 list (S4 has no separate task id
  found under this epic; likely folded into S2's scope).
- Whether the one-run trust-establishment lag also applies to a brand
  new `CODEX_HOME` with no prior `hooks.json` at all (this machine
  already had a trusted `SessionStart` entry from `herdr` before this
  spike started; only the newly-added entries were observed to lag).
