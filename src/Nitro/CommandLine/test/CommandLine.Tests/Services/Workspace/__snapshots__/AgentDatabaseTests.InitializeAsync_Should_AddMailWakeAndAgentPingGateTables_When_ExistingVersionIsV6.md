# InitializeAsync_Should_AddMailWakeAndAgentPingGateTables_When_ExistingVersionIsV6

## Version

```json
18
```

## Tables

```json
[
  "mail_wake_outbox",
  "mail_wake_batches",
  "mail_wake_targets",
  "mail_wake_daemons",
  "agent_ping_gates"
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

## AgentCount

```json
0
```

## MessageCount

```json
1
```

## RecipientCount

```json
1
```

## LeaseCount

```json
0
```
