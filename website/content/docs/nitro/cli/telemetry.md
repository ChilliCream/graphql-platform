---
title: telemetry Command
description: "Inspect traces, logs, services, and telemetry attributes with the Nitro CLI, including filtering, time ranges, and cursor pagination."
---

Nitro: 16.7.0

The `nitro telemetry` commands inspect traces, logs, services, and telemetry attributes in a workspace.

All `telemetry` commands require authentication. Run `nitro login` first or pass `--api-key` (see [Global Options](./global-options.md)). Use `--workspace-id` or `NITRO_WORKSPACE_ID` to supply a workspace when there is no workspace in the current session.

See [Time ranges and pagination](#time-ranges-and-pagination) and [Filters](#filters) for the shared options.

# `nitro telemetry traces list`

List trace spans, newest first. By default, results include server and consumer spans and are limited to 20 entries.

```shell
nitro telemetry traces list
```

## Options

| Option                            | Env                  | Description                                                                                                          |
| --------------------------------- | -------------------- | -------------------------------------------------------------------------------------------------------------------- |
| `--workspace-id <workspace-id>`   | `NITRO_WORKSPACE_ID` | ID of the workspace when there is no workspace in the current session.                                               |
| `--service <service>`             |                      | Limit results to a service.                                                                                          |
| `--env <environment>`             |                      | Limit results to an environment. Can be repeated.                                                                    |
| `--filter <filter>`               |                      | Filter expression (see [Filters](#filters)).                                                                         |
| `--has-error`                     |                      | Only include spans with errors.                                                                                      |
| `--min-duration <milliseconds>`   |                      | Minimum span duration in milliseconds. Must not be negative.                                                         |
| `--span-kind <kind>`              |                      | One of `server`, `client`, `producer`, `consumer`, or `internal`. Can be repeated. Default: `server` and `consumer`. |
| `--search <text>`                 |                      | Search span names.                                                                                                   |
| `--since <timestamp-or-duration>` |                      | Earliest timestamp to include. Default: `30m`.                                                                       |
| `--until <timestamp-or-duration>` |                      | Latest timestamp to include. Default: now.                                                                           |
| `--limit <limit>`                 |                      | Maximum number of entries per page. Must be positive. Default: `20`.                                                 |
| `--cursor <cursor>`               | `NITRO_CURSOR`       | Pagination cursor to resume from.                                                                                    |

## Examples

Find slow, failing spans for the checkout service in production:

```shell
nitro telemetry traces list \
  --service checkout \
  --env production \
  --has-error \
  --min-duration 500 \
  --since 2h
```

Find spans with HTTP server errors:

```shell
nitro telemetry traces list --filter "http.response.status_code:>=500"
```

Fetch the next page using the same time range as the first request:

```shell
nitro telemetry traces list \
  --since "<start-timestamp>" \
  --until "<end-timestamp>" \
  --cursor "<cursor>"
```

# `nitro telemetry traces show`

Show a trace by its ID. The default output includes a summary, top operations, and a span tree. Pass `--output json` to return structured trace and span data. The output indicates when the server has truncated the spans.

```shell
nitro telemetry traces show "<trace-id>"
```

## Arguments

| Argument     | Description                        |
| ------------ | ---------------------------------- |
| `<trace-id>` | ID of the trace to show. Required. |

## Options

| Option                            | Env                   | Description                                                            |
| --------------------------------- | --------------------- | ---------------------------------------------------------------------- |
| `--workspace-id <workspace-id>`   | `NITRO_WORKSPACE_ID`  | ID of the workspace when there is no workspace in the current session. |
| `--span <span-id>`                |                       | Focus on the subtree rooted at a span ID.                              |
| `--since <timestamp-or-duration>` |                       | Accepted, but does not restrict trace lookup.                          |
| `--until <timestamp-or-duration>` |                       | Accepted, but does not restrict trace lookup.                          |
| `--output <json>`                 | `NITRO_OUTPUT_FORMAT` | Return structured JSON instead of the summary and span tree.           |

## Examples

Inspect a span subtree:

```shell
nitro telemetry traces show "<trace-id>" --span "<span-id>"
```

Export the trace as JSON:

```shell
nitro telemetry traces show "<trace-id>" --output json
```

# `nitro telemetry logs list`

List logs, newest first. Results are paginated and default to 50 entries.

```shell
nitro telemetry logs list
```

## Options

| Option                            | Env                  | Description                                                                     |
| --------------------------------- | -------------------- | ------------------------------------------------------------------------------- |
| `--workspace-id <workspace-id>`   | `NITRO_WORKSPACE_ID` | ID of the workspace when there is no workspace in the current session.          |
| `--service <service>`             |                      | Limit results to a service.                                                     |
| `--env <environment>`             |                      | Limit results to an environment. Can be repeated.                               |
| `--filter <filter>`               |                      | Filter expression (see [Filters](#filters)).                                    |
| `--severity <severity>`           |                      | Include logs at or above `trace`, `debug`, `info`, `warn`, `error`, or `fatal`. |
| `--trace-id <trace-id>`           |                      | Only include logs from a trace.                                                 |
| `--search <text>`                 |                      | Search log messages.                                                            |
| `--since <timestamp-or-duration>` |                      | Earliest timestamp to include. Default: `30m`.                                  |
| `--until <timestamp-or-duration>` |                      | Latest timestamp to include. Default: now.                                      |
| `--limit <limit>`                 |                      | Maximum number of entries per page. Must be positive. Default: `50`.            |
| `--cursor <cursor>`               | `NITRO_CURSOR`       | Pagination cursor to resume from.                                               |

## Examples

Find error and fatal logs for the checkout service from the last two hours:

```shell
nitro telemetry logs list --service checkout --severity error --since 2h
```

Find logs for a trace:

```shell
nitro telemetry logs list --trace-id "<trace-id>" --since 2h
```

# `nitro telemetry logs show`

Show a log by its ID. Returns JSON with the log body, attributes, resource attributes, scope, and any code location information, including when `--output json` is omitted.

```shell
nitro telemetry logs show "<log-id>"
```

## Arguments

| Argument   | Description                                                  |
| ---------- | ------------------------------------------------------------ |
| `<log-id>` | ID of the log to show, as returned by `logs list`. Required. |

## Options

| Option                          | Env                  | Description                                                            |
| ------------------------------- | -------------------- | ---------------------------------------------------------------------- |
| `--workspace-id <workspace-id>` | `NITRO_WORKSPACE_ID` | ID of the workspace when there is no workspace in the current session. |

# `nitro telemetry services list`

List services with their environments and latest observed versions. Results are paginated and default to 50 entries.

```shell
nitro telemetry services list
```

## Options

| Option                            | Env                  | Description                                                            |
| --------------------------------- | -------------------- | ---------------------------------------------------------------------- |
| `--workspace-id <workspace-id>`   | `NITRO_WORKSPACE_ID` | ID of the workspace when there is no workspace in the current session. |
| `--search <text>`                 |                      | Search service names.                                                  |
| `--env <environment>`             |                      | Limit results to an environment. Can be repeated.                      |
| `--filter <filter>`               |                      | Filter using trace attributes (see [Filters](#filters)).               |
| `--since <timestamp-or-duration>` |                      | Earliest timestamp to include. Default: `30m`.                         |
| `--until <timestamp-or-duration>` |                      | Latest timestamp to include. Default: now.                             |
| `--limit <limit>`                 |                      | Maximum number of entries per page. Must be positive. Default: `50`.   |
| `--cursor <cursor>`               | `NITRO_CURSOR`       | Pagination cursor to resume from.                                      |

## Examples

Find services with errors in production during the last day:

```shell
nitro telemetry services list --env production --filter "status:error" --since 1d
```

# `nitro telemetry services show`

Show a service by name. Returns JSON with its environments and version markers, including when `--output json` is omitted. Each version marker includes the version and the time it was first seen.

```shell
nitro telemetry services show "<name>"
```

## Arguments

| Argument | Description                            |
| -------- | -------------------------------------- |
| `<name>` | Name of the service to show. Required. |

## Options

| Option                            | Env                  | Description                                                            |
| --------------------------------- | -------------------- | ---------------------------------------------------------------------- |
| `--workspace-id <workspace-id>`   | `NITRO_WORKSPACE_ID` | ID of the workspace when there is no workspace in the current session. |
| `--env <environment>`             |                      | Limit results to an environment. Can be repeated.                      |
| `--since <timestamp-or-duration>` |                      | Earliest timestamp to include. Default: `30m`.                         |
| `--until <timestamp-or-duration>` |                      | Latest timestamp to include. Default: now.                             |

## Examples

Inspect the checkout service in production over the last week:

```shell
nitro telemetry services show checkout --env production --since 7d
```

# `nitro telemetry attributes keys`

List attribute keys for traces or logs. Results include each key and its attribute kind, are paginated, and default to 50 entries.

```shell
nitro telemetry attributes keys --signal traces
```

## Options

| Option                            | Env                  | Description                                                                                                                             |
| --------------------------------- | -------------------- | --------------------------------------------------------------------------------------------------------------------------------------- |
| `--workspace-id <workspace-id>`   | `NITRO_WORKSPACE_ID` | ID of the workspace when there is no workspace in the current session.                                                                  |
| `--signal <signal>`               |                      | Signal to inspect: `traces` or `logs`. Required.                                                                                        |
| `--kind <kind>`                   |                      | Limit results to an attribute kind: `body`, `event`, `field`, `link`, `log`, `metric`, `resource`, `scope`, or `span`. Can be repeated. |
| `--search <text>`                 |                      | Search attribute keys.                                                                                                                  |
| `--since <timestamp-or-duration>` |                      | Earliest timestamp to include. Default: `30m`.                                                                                          |
| `--until <timestamp-or-duration>` |                      | Latest timestamp to include. Default: now.                                                                                              |
| `--limit <limit>`                 |                      | Maximum number of entries per page. Must be positive. Default: `50`.                                                                    |
| `--cursor <cursor>`               | `NITRO_CURSOR`       | Pagination cursor to resume from.                                                                                                       |

## Examples

Find resource attribute keys containing `service` in trace data:

```shell
nitro telemetry attributes keys --signal traces --kind resource --search service
```

# `nitro telemetry attributes values`

List observed values for an attribute key. Values are returned as strings. Results are paginated and default to 50 entries.

```shell
nitro telemetry attributes values "<key>" --signal traces
```

## Arguments

| Argument | Description                         |
| -------- | ----------------------------------- |
| `<key>`  | Attribute key to inspect. Required. |

## Options

| Option                            | Env                  | Description                                                                                                             |
| --------------------------------- | -------------------- | ----------------------------------------------------------------------------------------------------------------------- |
| `--workspace-id <workspace-id>`   | `NITRO_WORKSPACE_ID` | ID of the workspace when there is no workspace in the current session.                                                  |
| `--signal <signal>`               |                      | Signal to inspect: `traces` or `logs`. Required.                                                                        |
| `--kind <kind>`                   |                      | Limit results to one attribute kind: `body`, `event`, `field`, `link`, `log`, `metric`, `resource`, `scope`, or `span`. |
| `--search <text>`                 |                      | Search attribute values.                                                                                                |
| `--since <timestamp-or-duration>` |                      | Earliest timestamp to include. Default: `30m`.                                                                          |
| `--until <timestamp-or-duration>` |                      | Latest timestamp to include. Default: now.                                                                              |
| `--limit <limit>`                 |                      | Maximum number of entries per page. Must be positive. Default: `50`.                                                    |
| `--cursor <cursor>`               | `NITRO_CURSOR`       | Pagination cursor to resume from.                                                                                       |

## Examples

Find service names recorded in logs during the last day:

```shell
nitro telemetry attributes values service.name --signal logs --kind resource --since 1d
```

# Time ranges and pagination

List commands and `services show` accept `--since` and `--until`. Both options accept an ISO 8601 timestamp or a duration relative to the current time, such as `30m`, `2h`, or `7d`. Duration units are `s`, `m`, `h`, and `d`. The default range is the last 30 minutes. `--since` must be earlier than `--until` and cannot be more than 60 days in the past.

All list commands return JSON with a `values` array and a `cursor`, including when `--output json` is omitted. Pass the returned cursor to `--cursor` to fetch the next page. The `NITRO_CURSOR` environment variable also supplies the cursor; an explicit `--cursor` takes precedence. A `null` cursor indicates that there are no more pages.

Keep the filters and time range the same when fetching subsequent pages. Use fixed timestamps for `--since` and `--until` to keep the range unchanged between requests.

# Filters

The `traces list`, `logs list`, and `services list` commands accept `--filter`. Quote the expression to keep the shell from interpreting spaces and operators.

| Expression                                  | Description                                                              |
| ------------------------------------------- | ------------------------------------------------------------------------ |
| `status:error`                              | Match an exact value.                                                    |
| `http.response.status_code:>=500`           | Compare a numeric value with `>`, `>=`, `<`, or `<=`.                    |
| `http.response.status_code:IN(500,502,503)` | Match any value in a set.                                                |
| `http.response.status_code:*`               | Match records where an attribute exists.                                 |
| `@resource.service.name:checkout*`          | Match a wildcard pattern on a resource attribute.                        |
| `status:error AND duration:>=500`           | Require both conditions. Whitespace between conditions also means `AND`. |
| `status:error OR duration:>=500`            | Require either condition. Use parentheses to group conditions.           |
| `-status:error`                             | Exclude matches.                                                         |

Free text searches span names for traces and log messages for logs. Scope prefixes select attributes: `@span.`, `@event.`, and `@resource.` for traces; `@log.`, `@body.`, and `@resource.` for logs. Service filters use the trace filter syntax.

Shortcut options such as `--service`, `--has-error`, `--severity`, and `--search` are combined with `--filter` using `AND`. Use `attributes keys` and `attributes values` to discover available attributes and values.
