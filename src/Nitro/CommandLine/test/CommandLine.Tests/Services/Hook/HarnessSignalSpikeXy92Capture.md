# Spike xy9.2 raw capture: exact harness version and current-session signals

Captured 2026-08-24 on Linux 6.14 (host "beast"), by spike-xy92, read-only probes.
Decision table lives in the findings comment on perles-net-xy9.2. Redactions marked REDACTED.

## Claude Code (live-verified, version 2.1.241 running)

### Ancestry of a tool subprocess

```
$ pid=$$; while [ "$pid" != 0 ] && [ "$pid" != 1 ]; do tr '\0' ' ' </proc/$pid/cmdline; echo; pid=$(awk '{print $4}' /proc/$pid/stat); done
/usr/bin/zsh -c source /home/pascal/.claude/shell-snapshots/... (Bash tool shell)
claude                              <- PID 2476039, the harness
/usr/bin/zsh
/home/pascal/.local/bin/herdr server
...
```

### Environment visible to children (Bash tool subprocess)

```
AI_AGENT=claude-code_2-1-241_agent
CLAUDECODE=1
CLAUDE_CODE_ENTRYPOINT=cli
CLAUDE_CODE_EXECPATH=/home/pascal/.nvm/versions/node/v22.21.1/lib/node_modules/@anthropic-ai/claude-code/bin/claude.exe
CLAUDE_CODE_MESSAGING_SOCKET=/run/user/1000/cc-socks/2476039.sock
CLAUDE_CODE_MESSAGING_TOKEN=REDACTED
CLAUDE_CODE_SESSION_ID=e6b9afa6-db3c-41a3-94fe-02a683665222
CLAUDE_PID=2476039
```

### Session registry file, keyed by harness PID

```
$ cat ~/.claude/sessions/2476039.json
{"pid":2476039,"sessionId":"e6b9afa6-db3c-41a3-94fe-02a683665222","cwd":"/home/pascal/perles-net",
 "startedAt":1787575189062,"procStart":"44925761","version":"2.1.241","peerProtocol":1,
 "kind":"interactive","entrypoint":"cli","messagingSocketPath":"/run/user/1000/cc-socks/2476039.sock",
 "name":"perles-net-10","nameSource":"derived","status":"busy",...}
```

### PID plus process-start match

```
$ awk '{print $22}' /proc/2476039/stat   -> 44925761   (equals procStart, raw string equality)
$ grep btime /proc/stat                  -> btime 1787125930
$ getconf CLK_TCK                        -> 100
btime + 44925761/100 = 1787575187.61; sessions json startedAt = 1787575189.062 (1.5 s app-startup delta, expected)
$ readlink /proc/2476039/exe             -> .../node_modules/@anthropic-ai/claude-code/bin/claude.exe
```

### Version grammar

```
$ claude --version        -> 2.1.241 (Claude Code)          regex: ^(\d+\.\d+\.\d+) \(Claude Code\)$
$ AI_AGENT                -> claude-code_2-1-241_agent      regex: ^claude-code_(\d+)-(\d+)-(\d+)_agent$ (dashes are dots)
$ node -e "...EXECPATH/../package.json version"  -> 2.1.241
```

### Ambiguity evidence (cwd is not a key)

28 live rows in ~/.claude/sessions/*.json; 5 rows share cwd
/home/pascal/code/hc3/.work/hc2/website. cwd alone can never select a row.

## Codex CLI (live-verified, versions 0.147.0 / 0.149.0 / 0.149.1 running CONCURRENTLY)

### Running processes

```
$ pgrep -af codex (excerpt)
40274   codex resume 01a014f9-794c-7711-a60a-5645b90a1704       <- session id in cmdline for resumed sessions
1983671 codex                                                    <- fresh session, no id in cmdline
2478762 /home/pascal/.codex/packages/standalone/releases/0.149.1-x86_64-unknown-linux-musl/bin/codex-code-mode-host
255722  .../releases/0.147.0-x86_64-unknown-linux-musl/bin/codex-code-mode-host
```

### Per-process exact version via exe symlink

```
$ readlink /proc/40274/exe    -> .../releases/0.147.0-x86_64-unknown-linux-musl/bin/codex
$ readlink /proc/2477627/exe  -> .../releases/0.149.1-x86_64-unknown-linux-musl/bin/codex
$ readlink -f ~/.local/bin/codex -> .../releases/0.149.1-...   (PATH binary != version of older RUNNING sessions)
regex on exe path: /releases/(\d+\.\d+\.\d+(?:-[0-9A-Za-z.\-]+)?)-
```

### Session rollout file carries the exact version

```
~/.codex/sessions/YYYY/MM/DD/rollout-<ISO-ts>-<uuidv7>.jsonl, first line:
{"type":"session_meta","payload":{"session_id":"01a033d2-7795-73a1-9ba3-fb949b46879c",
 "timestamp":"2026-08-24T12:50:26.338Z","cwd":"/home/pascal/perles-net",
 "originator":"codex-tui","cli_version":"0.149.1","source":"cli",...}}
```

### Child environment (MCP server and code-mode-host children of codex 2477627)

Zero CODEX_* variables present. Only marker: PATH is prepended with
`.../releases/<version>-.../codex-path` and a `~/.codex/tmp/arg0/...` dir.
No session id, no pid, no version env var reaches these children.

### Ambiguity evidence

3+ rollout files dated 2026-08-24 whose session_meta cwd is /home/pascal/perles-net
(01a033c9-4ae8, 01a033d2-7795, 01a033c9-4ba3, ...). Newest-mtime-by-cwd is ambiguous.
Rollout meta records NO pid, so PID-reuse guarding needs nitro's own hook-time row.

### Version grammar

```
$ codex --version   -> codex-cli 0.149.1        regex: ^codex-cli (\d+\.\d+\.\d+)$
```

## Copilot CLI (installed 1.0.80; NO live CLI process, partial static verification)

### The package.json trap (why proxies are forbidden)

```
$ copilot --version
GitHub Copilot CLI 1.0.80.
Run 'copilot update' to check for updates.

@github/copilot/package.json            version 1.0.35
@github/copilot-linux-x64/package.json  version 1.0.35 (nested in @github/copilot/node_modules)
platform binary  .../copilot-linux-x64/copilot  mtime 2026-08-14, self-reports 1.0.80
```

`copilot update` replaced the binary IN PLACE; both package.json files still say 1.0.35.
npm metadata must never be used as the version source.

### Loader mechanics (static read of npm-loader.js)

npm-loader.js resolves `@github/copilot-${platform}-${arch}` and spawnSync's that
binary with stdio inherit; falls back to `node index.js` if unresolvable. So a live
copilot ancestor's /proc/<pid>/exe is expected to be the platform binary path
(NOT node), but this was not live-verified (gap: no CLI session running).
The exe path does NOT encode the version (in-place updates).

### Session state carries the exact version

```
~/.copilot/session-state/<sessionId>/events.jsonl first line:
{"type":"session.start","data":{"sessionId":"ac13b667-46e8-4a86-9953-f79a12b92c0d","version":1,
 "producer":"copilot-agent","copilotVersion":"1.0.80","startTime":"2026-08-23T17:04:17.458Z",
 "context":{"cwd":"...","gitRoot":"...","branch":"...","headCommit":"..."},...}}
~/.copilot/session-state/<sessionId>/workspace.yaml: id, cwd, git_root, branch, client_name,
 created_at, updated_at. No pid recorded. 134 session-state dirs exist; cwd is ambiguous.
```

### Name-collision hazard

8 live `copilot-language-server` processes (nvim/Mason, a different product) match a
naive name grep. Ancestor detection must match the exact exe path under
`@github/copilot*/copilot`, never the substring "copilot".

### Version grammar

```
$ copilot --version -> "GitHub Copilot CLI 1.0.80." plus advisory second line
regex on FIRST line only: ^GitHub Copilot CLI (\d+\.\d+\.\d+)\.$   (note trailing period)
```

## Cross-harness process-start facts

- /proc/<pid>/stat field 22 = start time in clock ticks since boot; CLK_TCK 100 here.
- Compare RAW TICKS for same-boot identity checks (string equality suffices, Claude
  stores it as the string "44925761"). Convert to wall clock only for display:
  btime (from /proc/stat) + ticks/CLK_TCK.
- ppid is stat field 4; walk terminates at pid 1 (or 0).
- /proc/<pid>/environ of a same-user process is readable (verified, 76 vars), but
  shows the env that process RECEIVED, not what it passes to children.

## Not verified (honesty list)

- Copilot: no live CLI session during the spike, so ancestor exe path, per-session
  process env, and any COPILOT_* vars passed to hook children are unverified.
- Codex: env of transient shell-exec children during a turn (possible CODEX_SANDBOX*
  vars) not captured; only long-lived MCP/code-mode-host children inspected.
- Claude: env inside HOOK subprocesses not captured live (capturing would require
  installing a hook, out of scope); inferred from the Bash tool child env, which the
  same process spawns.
- Windows and macOS: no /proc; start-time and ancestry matching semantics untested.
- Safety-classifier blocks: none occurred; every probe ran sandboxed and read-only.
