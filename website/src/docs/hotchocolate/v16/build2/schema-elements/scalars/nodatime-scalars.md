---
title: "NodaTime Scalars"
---

Use NodaTime scalars when your GraphQL API should expose NodaTime date and time types instead of the default .NET date and time runtime types. The `HotChocolate.Types.NodaTime` package adds five v16 scalar implementations that follow the scalar specifications on [scalars.graphql.org](https://scalars.graphql.org/).

This page shows you how to install the package, register the scalars, model fields and arguments with NodaTime types, tune fractional-second precision, and troubleshoot common v16 migration issues.

# Add NodaTime support

Install the NodaTime scalar package in the same version range as your other `HotChocolate.*` packages:

<PackageInstallation packageName="HotChocolate.Types.NodaTime" />

Register the package on the request executor builder:

```csharp
// Program.cs
using HotChocolate.Types.NodaTime;

builder
    .AddGraphQL()
    .AddMutationType<Mutation>()
    .AddNodaTime();
```

`AddNodaTime()` registers these scalar types:

- `HotChocolate.Types.NodaTime.DateTimeType`
- `HotChocolate.Types.NodaTime.DurationType`
- `HotChocolate.Types.NodaTime.LocalDateType`
- `HotChocolate.Types.NodaTime.LocalDateTimeType`
- `HotChocolate.Types.NodaTime.LocalTimeType`

Each scalar serializes as a GraphQL string and exposes a `@specifiedBy` URL for its scalar specification.

# Choose the right NodaTime type

Start from the meaning of the value in your domain, then choose the NodaTime runtime type that represents that meaning.

| Domain value                                      | Use this NodaTime type | GraphQL scalar  | Example value            | Notes                                                                           |
| ------------------------------------------------- | ---------------------- | --------------- | ------------------------ | ------------------------------------------------------------------------------- |
| Event, message, or audit timestamp with an offset | `OffsetDateTime`       | `DateTime`      | `"2023-12-24T15:30:00Z"` | Use when the offset is part of the API value.                                   |
| Local appointment date and time                   | `LocalDateTime`        | `LocalDateTime` | `"2023-12-24T15:30:00"`  | No `Z` suffix and no numeric offset.                                            |
| Calendar date                                     | `LocalDate`            | `LocalDate`     | `"2023-12-24"`           | Use for birthdays, service dates, expiration dates, and other date-only values. |
| Time of day                                       | `LocalTime`            | `LocalTime`     | `"15:30:00"`             | Use for store hours, daily cutoffs, and recurring local times.                  |
| Elapsed amount of time                            | `Duration`             | `Duration`      | `"PT5M"`                 | Use for retry delays, grace periods, and timeouts.                              |

> If your domain requires `Instant`, `ZonedDateTime`, `OffsetDate`, `OffsetTime`, `DateTimeZone`, `IsoDayOfWeek`, or `Period`, the v16 NodaTime package does not include a scalar for that CLR type. Convert the value at your GraphQL boundary, or create a custom scalar for the API contract.

# Model fields and arguments with NodaTime

The following code-first example exposes a scheduling mutation. The CLR types determine the GraphQL scalar names after you register `AddNodaTime()`.

```csharp
// Types/ScheduleEntry.cs
using NodaTime;

public sealed class ScheduleEntry
{
    public required OffsetDateTime CreatedAt { get; init; }

    public required LocalDate ServiceDate { get; init; }

    public required LocalTime StartsAt { get; init; }

    public required Duration GracePeriod { get; init; }
}
```

```csharp
// Types/Mutation.cs
using NodaTime;

public sealed class Mutation
{
    public ScheduleEntry Reschedule(
        OffsetDateTime createdAt,
        LocalDate serviceDate,
        LocalTime startsAt,
        Duration gracePeriod)
    {
        return new ScheduleEntry
        {
            CreatedAt = createdAt,
            ServiceDate = serviceDate,
            StartsAt = startsAt,
            GracePeriod = gracePeriod
        };
    }
}
```

Expected SDL:

```graphql
type Mutation {
  reschedule(
    createdAt: DateTime!
    serviceDate: LocalDate!
    startsAt: LocalTime!
    gracePeriod: Duration!
  ): ScheduleEntry!
}

type ScheduleEntry {
  createdAt: DateTime!
  serviceDate: LocalDate!
  startsAt: LocalTime!
  gracePeriod: Duration!
}
```

Example operation:

```graphql
mutation Reschedule(
  $inputCreatedAt: DateTime!
  $serviceDate: LocalDate!
  $startsAt: LocalTime!
  $gracePeriod: Duration!
) {
  reschedule(
    createdAt: $inputCreatedAt
    serviceDate: $serviceDate
    startsAt: $startsAt
    gracePeriod: $gracePeriod
  ) {
    createdAt
    serviceDate
    startsAt
    gracePeriod
  }
}
```

Example variables:

```json
{
  "inputCreatedAt": "2023-12-24T15:30:00.123456789+01:00",
  "serviceDate": "2023-12-24",
  "startsAt": "15:30:00.123456789",
  "gracePeriod": "PT5M"
}
```

Nullability follows the same Hot Chocolate rules as other fields and arguments. Use nullable CLR types when the GraphQL field or argument should be nullable.

# Use the supported scalar reference

`HotChocolate.Types.NodaTime` includes exactly five NodaTime scalar implementations in v16.

| GraphQL scalar  | NodaTime runtime type     | Registered scalar type | Related BCL binding after `AddNodaTime()` | Example GraphQL value                   | Use when                                                 |
| --------------- | ------------------------- | ---------------------- | ----------------------------------------- | --------------------------------------- | -------------------------------------------------------- |
| `DateTime`      | `NodaTime.OffsetDateTime` | `DateTimeType`         | `DateTimeOffset`                          | `"2023-12-24T15:30:00.123456789+01:00"` | You expose a date and time with `Z` or a numeric offset. |
| `Duration`      | `NodaTime.Duration`       | `DurationType`         | None for `TimeSpan`                       | `"PT5M"`                                | You expose an elapsed duration.                          |
| `LocalDate`     | `NodaTime.LocalDate`      | `LocalDateType`        | `DateOnly`                                | `"2023-12-24"`                          | You expose a calendar date without time or offset.       |
| `LocalDateTime` | `NodaTime.LocalDateTime`  | `LocalDateTimeType`    | `DateTime`                                | `"2023-12-24T15:30:00.123456789"`       | You expose a local date and time without offset.         |
| `LocalTime`     | `NodaTime.LocalTime`      | `LocalTimeType`        | `TimeOnly`                                | `"15:30:00.123456789"`                  | You expose a time of day without date or offset.         |

The NodaTime scalars use the same GraphQL scalar names as the built-in date and time scalars, but they use NodaTime runtime types and support up to 9 fractional-second digits for `DateTime`, `LocalDateTime`, and `LocalTime`.

`Duration` is the exception to the BCL rebinding rule. `AddNodaTime()` does not bind `System.TimeSpan` to the NodaTime `DurationType` and does not add `TimeSpan` to `NodaTime.Duration` converters.

# Understand wire formats and precision

Clients send NodaTime scalar values as strings in GraphQL literals or JSON variables.

| Scalar          | Accepted shape                                              | Valid example            | Common invalid examples                                 | Precision notes                                                            |
| --------------- | ----------------------------------------------------------- | ------------------------ | ------------------------------------------------------- | -------------------------------------------------------------------------- |
| `DateTime`      | Date and time with `T`, followed by `Z` or a numeric offset | `"2023-12-24T15:30:00Z"` | `"2023-12-24T15:30:00"`, `"2023-12-24 15:30:00Z"`       | Fractional seconds can contain up to configured `InputPrecision`, max `9`. |
| `LocalDateTime` | Date and time with `T`, no offset                           | `"2023-12-24T15:30:00"`  | `"2023-12-24T15:30:00Z"`, `"2023-12-24T15:30:00+01:00"` | Fractional seconds can contain up to configured `InputPrecision`, max `9`. |
| `LocalDate`     | `YYYY-MM-DD`                                                | `"2023-12-24"`           | `"2023-12-24T15:30:00"`, `"2023-02-31"`                 | No fractional-second precision option.                                     |
| `LocalTime`     | `HH:mm:ss` with optional fractional seconds                 | `"15:30:00.123456789"`   | `"2023-12-24T15:30:00"`, `"15:30:00+01:00"`             | Fractional seconds can contain up to configured `InputPrecision`, max `9`. |
| `Duration`      | ISO 8601 duration string                                    | `"PT5M"`                 | `"5 minutes"`, `"00:05:00"`                             | `DateTimeOptions` does not apply.                                          |

Use uppercase `T` and `Z` in examples and client code. The v16 parsers accept lowercase `t` for `DateTime` and `LocalDateTime`, and lowercase `z` for `DateTime`, but uppercase values are clearer and match the canonical examples.

Output uses `OutputPrecision`. A value accepted with 9 fractional-second digits can serialize with fewer digits when you configure lower output precision.

# Configure fractional-second precision

`DateTime`, `LocalDateTime`, and `LocalTime` use `HotChocolate.Types.NodaTime.DateTimeOptions`.

| Option            | Default | Allowed values  | Applies to                               | Effect                                       |
| ----------------- | ------- | --------------- | ---------------------------------------- | -------------------------------------------- |
| `InputPrecision`  | `9`     | `0` through `9` | `DateTime`, `LocalDateTime`, `LocalTime` | Maximum accepted fractional-second digits.   |
| `OutputPrecision` | `9`     | `0` through `9` | `DateTime`, `LocalDateTime`, `LocalTime` | Maximum serialized fractional-second digits. |

Values greater than `9` throw during scalar configuration. `LocalDate` and `Duration` do not use these options.

If you need non-default precision for NodaTime CLR types, register the affected scalar types individually. Use this pattern when your GraphQL-facing members use NodaTime CLR types such as `OffsetDateTime`, `LocalDateTime`, and `LocalTime`. If your schema uses BCL types such as `DateTimeOffset`, `DateTime`, `DateOnly`, or `TimeOnly`, use the default `AddNodaTime()` registration or reproduce the runtime bindings and converters that `AddNodaTime()` adds.

```csharp
// Program.cs
using NodaTimeDateTimeOptions = HotChocolate.Types.NodaTime.DateTimeOptions;
using NodaTimeDateTimeType = HotChocolate.Types.NodaTime.DateTimeType;
using NodaTimeLocalDateTimeType = HotChocolate.Types.NodaTime.LocalDateTimeType;
using NodaTimeLocalTimeType = HotChocolate.Types.NodaTime.LocalTimeType;

builder
    .AddGraphQL()
    .AddMutationType<Mutation>()
    .AddType(new NodaTimeDateTimeType(new NodaTimeDateTimeOptions
    {
        InputPrecision = 9,
        OutputPrecision = 3
    }))
    .AddType(new NodaTimeLocalDateTimeType(new NodaTimeDateTimeOptions
    {
        InputPrecision = 9,
        OutputPrecision = 3
    }))
    .AddType(new NodaTimeLocalTimeType(new NodaTimeDateTimeOptions
    {
        InputPrecision = 9,
        OutputPrecision = 3
    }));
```

With these settings, an input such as `"2023-12-24T15:30:00.123456789+01:00"` is accepted for `DateTime`, but output is serialized as `"2023-12-24T15:30:00.123+01:00"`.

# Know how BCL types behave after AddNodaTime

`AddNodaTime()` changes the scalar implementations used by several BCL date and time types:

| CLR type         | GraphQL scalar after `AddNodaTime()` | NodaTime conversion                                                             |
| ---------------- | ------------------------------------ | ------------------------------------------------------------------------------- |
| `DateTimeOffset` | `DateTime`                           | Converts to and from `OffsetDateTime`.                                          |
| `DateTime`       | `LocalDateTime`                      | Converts to and from `LocalDateTime`.                                           |
| `DateOnly`       | `LocalDate`                          | Converts to and from `LocalDate`.                                               |
| `TimeOnly`       | `LocalTime`                          | Converts to and from `LocalTime`.                                               |
| `TimeSpan`       | Not rebound                          | Keep the built-in `Duration` scalar or convert to `NodaTime.Duration` yourself. |

This section is here to help you predict schema changes after registering NodaTime. For pure BCL scalar behavior, see [Built-in Scalars](./built-in-scalars).

# Review v15 upgrade notes

In v16, `HotChocolate.Types.NodaTime` was rewritten to align with the GraphQL scalar specifications. Parsing is stricter than legacy implementations, and only five scalar classes remain built in:

- `DateTimeType`
- `DurationType`
- `LocalDateType`
- `LocalDateTimeType`
- `LocalTimeType`

These legacy scalar types are no longer included:

- `DateTimeZoneType`
- `InstantType`
- `IsoDayOfWeekType`
- `OffsetDateType`
- `OffsetTimeType`
- `OffsetType`
- `PeriodType`
- `ZonedDateTimeType`

If you used one of the removed types, convert the value at the GraphQL boundary or expose your own scalar contract. See the [v15 to v16 migration guide](/docs/hotchocolate/v16/migrating/migrate-from-15-to-16) for the broader migration context.

# Troubleshoot common issues

## TimeSpan is not using the NodaTime Duration scalar

Cause: `AddNodaTime()` does not bind `System.TimeSpan` and does not register `TimeSpan` to `NodaTime.Duration` converters.

Fix options:

- Use `NodaTime.Duration` in GraphQL-facing models.
- Keep `TimeSpan` and use the built-in BCL `Duration` behavior described on [Built-in Scalars](./built-in-scalars).
- Convert between `TimeSpan` and `NodaTime.Duration` in application code before values reach the GraphQL schema.

## DateTime input without an offset is rejected

Cause: GraphQL `DateTime` represents an offset-aware date and time. It requires `Z` or a numeric offset.

Send one of these values instead:

```json
"2023-12-24T15:30:00Z"
```

```json
"2023-12-24T15:30:00+01:00"
```

Use `LocalDateTime` when the value intentionally has no offset.

## LocalDateTime input with Z or an offset is rejected

Cause: `LocalDateTime` models a local date and time without offset context.

Remove the offset:

```json
"2023-12-24T15:30:00"
```

Use `OffsetDateTime` with GraphQL `DateTime` when the offset matters.

## LocalTime input with a date or offset is rejected

Cause: `LocalTime` is a time-of-day value only.

Send a time-only value:

```json
"15:30:00"
```

```json
"15:30:00.123456789"
```

Use `LocalDateTime` or `DateTime` when the date or offset is part of the API value.

## Fractional seconds are rejected or shorter than expected

Cause: the input has more digits than `InputPrecision`, output uses a lower `OutputPrecision`, or the value has more than 9 fractional-second digits.

Keep inputs at or below the configured precision. Configure `OutputPrecision` only when clients need a shorter serialized format.

## InstantType or ZonedDateTimeType cannot be found

Cause: legacy NodaTime scalar types were removed from the v16 package.

Use one of the five supported scalars where possible. If your API must expose another leaf value contract, see [Custom Scalars](./custom-scalars).

# Next steps

- Use [Built-in Scalars](./built-in-scalars) for the default BCL scalar mappings.
- Use [Community Scalars](./community-scalars) for additional packaged scalar types.
- Use [Custom Scalars](./custom-scalars) when no built-in or package scalar matches your API contract.
