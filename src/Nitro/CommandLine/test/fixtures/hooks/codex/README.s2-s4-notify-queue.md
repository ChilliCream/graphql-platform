# Codex CLI 0.149.0 notify/queue fixtures (spike S2+S4, perles-net-k3j.2)

Captured live on 2026-08-23 against `codex-cli 0.149.0`. See the
perles-net-k3j.2 findings comment for the full write-up and everything that
could not be verified. This is a separate capture from S1
(perles-net-k3j.1, `README.md` + `payload.session-start.json` etc in this
same directory, which covers `hooks.json` events); S2/S4 is about the
`notify` config.toml mechanism and `codex queue`, a different code path.

## Files

- `payload.notify.agent-turn-complete.json` -- one clean live `notify`
  payload, argv[1] (single JSON string argument, NOT stdin), from a fresh
  `codex exec` turn.
- `payload.notify.agent-turn-complete.after-queued-turn.json` -- the same
  thread's SECOND notify payload, captured after resuming that thread with a
  new prompt. Note `input-messages` is CUMULATIVE across the whole
  thread/session (all three user-turn inputs so far), not scoped to just the
  latest turn -- a correction to any assumption that this field is
  per-turn-only.
- `evidence.s2-notify-queue-loop-termination.txt` -- full trace: notify
  receipt, `codex queue` call and return, the queued message being consumed
  on the next resumed turn (as an extra prepended user-message item sharing
  that turn's single notify firing, not a separate turn/notify of its own),
  that turn's completion re-firing notify, and the message-id-keyed dedup
  ledger skipping the second firing (loop terminates, queue table empty
  afterward). Also covers foreign-notify chaining timing.
- `evidence.s4-cwd-and-session-id-correlation.txt` -- `cwd` resolution,
  thread-id/session_id correlation (via the rollout transcript, since the
  live SessionStart-hook correlation attempt did not fire this spike), and
  the UUIDv7 session/thread-id format finding.

## Headline facts

- **Payload fields observed live** (argv[1], single JSON string, not
  stdin): `type` (`"agent-turn-complete"`), `thread-id`, `turn-id`, `cwd`,
  `client` (**new field not listed in the plan's premise**, value
  `"codex_exec"` for every capture this spike -- likely varies by harness;
  not exercised for interactive/TUI sessions), `input-messages` (array,
  cumulative across the thread, not per-turn), `last-assistant-message`.
- **`codex queue --thread <id> --message <text>` writes into a durable,
  cross-process SQLite table** (`~/.codex/queue_1.sqlite`, table
  `queued_items`), decoupled from any live process. It is NOT an in-memory
  signal to a still-running process. For the `codex exec` harness (one-shot:
  the process always fully exits after its single turn, confirmed by `ps
  aux` showing no surviving process and by the rollout's `task_complete`
  record predating notify's own invocation), by the time notify runs there
  is no live process to re-enter; queuing writes durable state that sits
  until SOMETHING resumes that thread. This is a materially different (and
  less dangerous) timing story than "queue re-enters the still-running
  process synchronously from inside notify": there is no such re-entrancy
  for `codex exec`, because the process is already gone.
- **A queued message does not start its own turn.** It is delivered as an
  extra `user`-role item prepended ahead of whatever the next actual turn's
  prompt is (verified via the rollout transcript: both the queued digest and
  the new resume prompt appear as two sequential user messages under one
  shared `turn_id`), and that combined turn produces exactly ONE
  `agent-turn-complete` notify firing for both. `codex exec resume <thread>`
  with no prompt at all errors (`No prompt provided via stdin`, exit 1) --
  it does not silently flush the queue. Draining requires an externally
  supplied "next turn" (a resume with a prompt, or a persistent session's
  own next input), matching the plan's "next turn boundary" framing
  (`mail-notify-plan.md:323`).
- **No infinite loop, captured end-to-end**: turn 1 fires notify, a
  message-id-keyed ledger claims the (simulated) mail item and queues a
  digest; turn 2 (the queued digest's delivery) fires notify again; the
  ledger finds the same message-id already delivered and skips queueing;
  the queue table is empty afterward. See
  `evidence.s2-notify-queue-loop-termination.txt` for full timestamps and
  transcript excerpts. A SEPARATE run using a (thread-id, turn-id)-keyed
  guard instead of message-id DID keep queueing on every firing (each turn
  has a unique turn-id) -- termination in production depends entirely on the
  ledger being keyed by delivered mail message-id, exactly as the plan
  specifies for `session_deliveries`, not by turn/session identity.
- **Foreign-notify chaining is sequential and fast**: the wrapper's own work
  (receipt + `codex queue` call, ~355ms, dominated by the `codex queue`
  subprocess spawn) completes before invoking the foreign program (~3ms);
  total wrapper wall time end-to-end was ~375ms. The foreign program
  received the identical argv[1] payload.
- **`cwd` resolves the actual working directory** the `codex exec` process
  was invoked from.
- **thread-id and session_id are the same identifier** across every
  mechanism observed (notify payload `thread-id`, `codex exec --json`
  stream's `thread_id`, the rollout filename, and the rollout's
  `session_meta.payload.session_id`), for all 7 distinct sessions captured
  across S1 and S2 (6 in this spike, 1 in S1). Both are UUIDv7 (version
  nibble `7`, time-ordered leading bits), so the plan's composite-key
  "effectively-unique ids" assumption holds.

## What could not be determined this spike

- **Direct live SessionStart-hook-to-notify correlation.** Adding a second
  `hooks.json` SessionStart hook-group (co-existing with the pre-existing
  `herdr` entry, per S1's documented multi-group support) never acquired
  the silent one-run trust delay S1 observed for a new registration: across
  4 `codex exec` invocations (including from an already
  `trust_level = "trusted"` project directory), no new `[hooks.state]`
  entry was written and the hook never fired. Not root-caused this spike
  (possibly: trust delay applies only to genuinely new event registrations,
  not new hook-groups appended to an already-trusted event; or some other
  gating this spike didn't identify). Worked around via the rollout
  transcript's `session_meta.payload.session_id` instead (equally strong
  evidence, see above), but the hook-level correlation itself is unverified.
- **Interactive/persistent (non-`codex exec`) session behavior.** All
  captures this spike used `codex exec` / `codex exec resume`, which is
  one-shot per invocation. Whether a long-running interactive `codex`
  session (TUI, still alive and idle-waiting for input when notify fires)
  drains its queue immediately and automatically without any external
  trigger -- the actual "dangerous timing" scenario the ticket names, where
  a live process might re-enter synchronously -- was NOT tested: no
  headless pty-automation tool (`expect`, `pexpect`) was available in this
  environment, and building one from Python's raw `pty` module against a
  full-screen TUI was judged out of scope for this spike's timebox. `client`
  in every captured payload was `"codex_exec"`; an interactive session may
  report a different `client` value and may behave differently.
- **`client` field enumeration.** Only `"codex_exec"` was observed. Values
  for interactive sessions, `codex resume` (interactive), or other harnesses
  are unknown.
- **Notify firing on non-"agent-turn-complete" event types**, if any exist
  (only `type: "agent-turn-complete"` was ever observed; the plan's premise
  of a single notify event type was not contradicted, but no negative case
  was exercised).
- **Queue behavior when the target thread-id does not exist / is
  malformed** (e.g. a stale/garbage-collected session): not exercised.
- **Whether Codex's app-server daemon (`codex-code-mode-host` /
  `codex agents`) exposes a way to trigger queue drainage on a live process
  without a full `codex exec resume <thread> <prompt>` round-trip** (i.e. a
  true "wake it up now" primitive): not investigated, out of scope for this
  spike's `codex exec`-focused approach.
