# Copilot CLI 1.0.75 hooks fixtures (spike S5, perles-net-k3j.4)

Captured live on 2026-08-23 against GitHub Copilot CLI 1.0.75
(`@github/copilot` npm package). `config-example.*.json` are the hook
config files authored to produce the corresponding `payload.*.json`
captures (command/paths redacted to `"..."`; the real probe commands
teed the hook's stdin to disk). See the perles-net-k3j.4 findings
comment for the full write-up, static-analysis evidence (from
`app.js`, `sdk/index.d.ts`, `copilot-sdk/docs/*.md`), and everything
that could not be verified.

Headline facts these fixtures demonstrate:
- Two independent key casings are accepted for the same event, and
  BOTH schemas are real, not a typo: canonical camelCase
  (`sessionStart`, `userPromptSubmitted`, `sessionEnd`, ...) yields a
  camelCase payload with epoch-millis `timestamp` and no event-name
  field; a Claude-Code-compatible alias set (`SessionStart`,
  `UserPromptSubmit`, `Stop`, `PreToolUse`, `PostToolUse`,
  `SubagentStop`, `PreCompact`, `PermissionRequest`, `Notification`,
  ...) yields a snake_case payload (`hook_event_name`, `session_id`,
  ISO-8601 `timestamp`) tagged internally as `_vsCodeCompat`. Both can
  fire for the same event if both keys are registered.
- User-scope and project-scope hook sources compose additively (not
  override): file-based hooks dir + embedded settings `hooks` key, at
  both scopes, all fire for the same event.
- Project-scope hooks (file dir, `.github/copilot/settings.json`, and
  the `.claude/settings.json` compat file) only fire when the project
  folder is listed in `~/.copilot/config.json`'s `trustedFolders`;
  untrusted folders are silently skipped in non-interactive `-p` mode
  (no error, no prompt).

## Correction to the plan's premise (important)

`.work/mail-notify-plan.md` states Copilot hooks have "No turn-end/stop
event". That is WRONG for file/settings-based hooks: `agentStop`
(canonical) / `Stop` (Claude-compat alias) exists, fires when a turn
ends (`stopReason`/`stop_reason: "end_turn"` observed), and per static
analysis of `app.js` its response schema is `{decision, reason}` -
the same shape as Claude's blocking `Stop` hook. Only the BLOCKING
effect of returning `decision: "block"` was not exercised live in this
spike (risk of an uncontrolled reprompt loop); firing and payload shape
are confirmed live, see `payload.agentStop.camelCase-key.json` and
`payload.Stop.claude-compat-alias-key.json`.
