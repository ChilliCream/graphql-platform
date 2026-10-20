# InitializeAsync_Should_PreserveEveryAgentDomainRow_When_ExistingVersionIsV18

## Version

```json
19
```

## Agents

```json
[
  {
    "name": "lena",
    "role": "",
    "harness": null,
    "harness_version": "",
    "session_id": null,
    "cwd": "",
    "workspace_path": "",
    "registered_at": "2026-01-10T12:00:00+00:00",
    "started_at": "2026-01-10T12:00:00+00:00",
    "last_seen_at": "2026-01-10T12:00:00+00:00",
    "ended_at": null,
    "deleted_at": null,
    "endpoint_kind": "none",
    "endpoint_addr": "",
    "endpoint_secret": null,
    "block_budget_used": 0,
    "last_ping_at": null,
    "last_ping_attempt": null,
    "last_ping_result": null,
    "last_ping_detail": null,
    "announcement_pending": 0,
    "idle_push_armed": 0
  },
  {
    "name": "maya",
    "role": "backend",
    "harness": "claude-code",
    "harness_version": "2.1.241",
    "session_id": "session-1",
    "cwd": "/repo",
    "workspace_path": "/repo/.git/nitro",
    "registered_at": "2026-01-10T12:00:00+00:00",
    "started_at": "2026-01-10T12:01:00+00:00",
    "last_seen_at": "2026-01-10T12:02:00+00:00",
    "ended_at": null,
    "deleted_at": null,
    "endpoint_kind": "claude-peer",
    "endpoint_addr": "peer-maya",
    "endpoint_secret": null,
    "block_budget_used": 2,
    "last_ping_at": "2026-01-10T12:03:00+00:00",
    "last_ping_attempt": "attempt-1",
    "last_ping_result": "endpoint-gone",
    "last_ping_detail": "socket gone",
    "announcement_pending": 1,
    "idle_push_armed": 1
  },
  {
    "name": "nova",
    "role": "reviewer",
    "harness": "opencode",
    "harness_version": "1.0.0",
    "session_id": "session-2",
    "cwd": "/repo",
    "workspace_path": "/repo/.git/nitro",
    "registered_at": "2026-01-10T12:00:00+00:00",
    "started_at": "2026-01-10T12:00:00+00:00",
    "last_seen_at": "2026-01-10T12:00:00+00:00",
    "ended_at": "2026-01-10T12:05:00+00:00",
    "deleted_at": "2026-01-10T12:06:00+00:00",
    "endpoint_kind": "opencode-server",
    "endpoint_addr": "http://127.0.0.1:4096",
    "endpoint_secret": "secret",
    "block_budget_used": 0,
    "last_ping_at": null,
    "last_ping_attempt": null,
    "last_ping_result": null,
    "last_ping_detail": null,
    "announcement_pending": 0,
    "idle_push_armed": 0
  }
]
```

## RowCounts

```json
{
  "tasks": 1,
  "messages": 1,
  "message_recipients": 1,
  "memory_journal": 1,
  "agent_deliveries": 1,
  "agent_ping_gates": 1,
  "ping_leases": 1,
  "mail_wake_outbox": 1,
  "mail_wake_batches": 1,
  "mail_wake_targets": 1
}
```

## AgentIndexes

```json
[
  {
    "name": "idx_agents_session",
    "sql": "CREATE UNIQUE INDEX idx_agents_session ON agents (harness, session_id) WHERE harness IS NOT NULL"
  }
]
```

## ForeignKeyViolations

```json
[]
```

## IntegrityCheck

```text
ok
```

## VentiAgentsAfterInsert

```json
[
  {
    "name": "vera",
    "harness": "venti",
    "session_id": "session-1"
  }
]
```

## OrphanDeliveryError

```text
SQLite Error 19: 'FOREIGN KEY constraint failed'.
```
