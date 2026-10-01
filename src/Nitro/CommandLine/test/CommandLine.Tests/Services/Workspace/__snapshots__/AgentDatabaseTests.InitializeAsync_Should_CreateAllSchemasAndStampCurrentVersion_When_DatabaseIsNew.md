# InitializeAsync_Should_CreateAllSchemasAndStampCurrentVersion_When_DatabaseIsNew

## Version

```json
18
```

## Tables

```json
[
  "tasks",
  "messages",
  "agents",
  "ping_leases",
  "agent_deliveries",
  "agent_ping_gates",
  "mail_wake_outbox",
  "mail_wake_batches",
  "mail_wake_targets",
  "mail_wake_daemons"
]
```

## Indexes

```json
[
  "idx_mail_wake_outbox_due",
  "idx_mail_wake_batches_one_active_per_actor",
  "idx_mail_wake_batches_expires",
  "idx_agent_ping_gates_expires"
]
```

## AgentColumns

```json
[
  "name",
  "role",
  "harness",
  "harness_version",
  "session_id",
  "cwd",
  "workspace_path",
  "registered_at",
  "started_at",
  "last_seen_at",
  "ended_at",
  "deleted_at",
  "endpoint_kind",
  "endpoint_addr",
  "endpoint_secret",
  "block_budget_used",
  "last_ping_at",
  "last_ping_attempt",
  "last_ping_result",
  "last_ping_detail",
  "announcement_pending",
  "idle_push_armed"
]
```
