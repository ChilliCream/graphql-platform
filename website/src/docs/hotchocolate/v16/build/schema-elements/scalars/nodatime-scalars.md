---
title: "NodaTime Scalars"
---

NodaTime scalars allow your GraphQL API to expose NodaTime date and time types instead of the default .NET types. The `HotChocolate.Types.NodaTime` package provides five scalar implementations in v16, each following the specifications at [scalars.graphql.org](https://scalars.graphql.org/).

This page explains how to install the package, register the scalars, model fields and arguments with NodaTime types, configure fractional-second precision, and resolve common migration issues in v16.

# Adding NodaTime Support

Install the NodaTime scalar package, ensuring its version matches your other `HotChocolate.*` packages:

<PackageInstallation packageName="HotChocolate.Types.NodaTime" />

Register the package with the request executor builder:

```csharp
// Program.cs
using HotChocolate.Types.NodaTime;

builder
    .AddGraphQL()
    .AddMutationType<Mutation>()
    .AddNodaTime();
```

The `AddNodaTime()` method registers these scalar types:

- `HotChocolate.Types.NodaTime.DateTimeType`
- `HotChocolate.Types.NodaTime.DurationType`
- `HotChocolate.Types.NodaTime.LocalDateType`
- `HotChocolate.Types.NodaTime.LocalDateTimeType`
- `HotChocolate.Types.NodaTime.LocalTimeType`

Each scalar serializes as a GraphQL string and includes a `@specifiedBy` URL referencing its scalar specification.

# Selecting the Appropriate NodaTime Type

Begin by considering the meaning of the value in your domain, then select the NodaTime runtime type that best represents it.

| Domain value                                      | Use this NodaTime type | GraphQL scalar  | Example value            | Notes                                                                           |
| ------------------------------------------------- | ---------------------- | --------------- | ------------------------ | ------------------------------------------------------------------------------- |
| Event, message, or audit timestamp with an offset | `OffsetDateTime`       | `DateTime`      | `"2023-12-24T15:30:00Z"` | Use when the offset is part of the API value.                                   |
| Local appointment date and time                   | `LocalDateTime`        | `LocalDateTime` | `"2023-12-24T15:30:00"`  | No `Z` suffix and no numeric offset.                                            |
| Calendar date                                     | `LocalDate`            | `LocalDate`     | `"2023-12-24"`           | Use for birthdays, service dates, expiration dates, and other date-only values. |
| Time of day                                       | `LocalTime`            | `LocalTime`     | `"15:30:00"`             | Use for store hours, daily cutoffs, and recurring local times.                  |
| Elapsed amount of time                            | `Duration`             | `Duration`      | `"PT5M"`                 | Use for retry delays, grace periods, and timeouts.                              |

> If your domain requires `Instant`, `ZonedDateTime`, `OffsetDate`, `OffsetTime`, `DateTimeZone`, `IsoDayOfWeek`, or `Period`, the v16 NodaTime package does not provide a scalar for that CLR type. Convert the value at your GraphQL boundary or implement a custom scalar for your API contract.

# Modeling Fields and Arguments with NodaTime

The following code-first example demonstrates a scheduling mutation. After registering `AddNodaTime()`, the CLR types determine the GraphQL scalar names.

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

Nullability follows the same rules as other fields and arguments in Hot Chocolate. Use nullable CLR types when the GraphQL field or argument should be nullable.

# Supported Scalar Reference

`HotChocolate.Types.NodaTime` provides exactly five NodaTime scalar implementations in v16.

| GraphQL scalar  | NodaTime runtime type     | Registered scalar type | Related BCL binding after `AddNodaTime()` | Example GraphQL value                   | Use when                                                 |
| --------------- | ------------------------- | ---------------------- | ----------------------------------------- | --------------------------------------- | -------------------------------------------------------- |
| `DateTime`      | `NodaTime.OffsetDateTime` | `DateTimeType`         | `DateTimeOffset`                          | `"2023-12-24T15:30:00.123456789+01:00"` | You expose a date and time with `Z` or a numeric offset. |
| `Duration`      | `NodaTime.Duration`       | `DurationType`         | None for `TimeSpan`                       | `"PT5M"`                                | You expose an elapsed duration.                          |
| `LocalDate`     | `NodaTime.LocalDate`      | `LocalDateType`        | `DateOnly`                                | `"2023-12-24"`                          | You expose a calendar date without time or offset.       |
| `LocalDateTime` | `NodaTime.LocalDateTime`  | `LocalDateTimeType`    | `DateTime`                                | `"2023-12-24T15:30:00.123456789"`       | You expose a local date and time without offset.         |
| `LocalTime`     | `NodaTime.LocalTime`      | `LocalTimeType`        | `TimeOnly`                                | `"15:30:00.123456789"`                  | You expose a time of day without date or offset.         |

NodaTime scalars use the same GraphQL scalar names as the built-in date and time scalars, but they rely on NodaTime runtime types and support up to 9 fractional-second digits for `DateTime`, `LocalDateTime`, and `LocalTime`.

`Duration` is an exception to the BCL rebinding rule. `AddNodaTime()` does not bind `System.TimeSpan` to the NodaTime `DurationType` and does not add converters between `TimeSpan` and `NodaTime.Duration`.

# Wire Formats and Precision

Clients provide NodaTime scalar values as strings in GraphQL literals or JSON variables.

| Scalar          | Accepted shape                                              | Valid example            | Common invalid examples                                 | Precision notes                                                            |
| --------------- | ----------------------------------------------------------- | ------------------------ | ------------------------------------------------------- | -------------------------------------------------------------------------- |
| `DateTime`      | Date and time with `T`, followed by `Z` or a numeric offset | `"2023-12-24T15:30:00Z"` | `"2023-12-24T15:30:00"`, `"2023-12-24 15:30:00Z"`       | Fractional seconds can contain up to configured `InputPrecision`, max `9`. |
| `LocalDateTime` | Date and time with `T`, no offset                           | `"2023-12-24T15:30:00"`  | `"2023-12-24T15:30:00Z"`, `"2023-12-24T15:30:00+01:00"` | Fractional seconds can contain up to configured `InputPrecision`, max `9`. |
| `LocalDate`     | `YYYY-MM-DD`                                                | `"2023-12-24"`           | `"2023-12-24T15:30:00"`, `"2023-02-31"`                 | No fractional-second precision option.                                     |
| `LocalTime`     | `HH:mm:ss` with optional fractional seconds                 | `"15:30:00.123456789"`   | `"2023-12-24T15:30:00"`, `"15:30:00+01:00"`             | Fractional seconds can contain up to configured `InputPrecision`, max `9`. |
| `Duration`      | ISO 8601 duration string                                    | `"PT5M"`                 | `"5 minutes"`, `"00:05:00"`                             | `DateTimeOptions` does not apply.                                          |

Use uppercase `T` and `Z` in examples and client code. The v16 parsers accept lowercase `t` for `DateTime` and `LocalDateTime`, and lowercase `z` for `DateTime`, but uppercase values are clearer and match canonical examples.

Output uses `OutputPrecision`. A value accepted with 9 fractional-second digits may serialize with fewer digits if you configure a lower output precision.

# Configuring Fractional-Second Precision

`DateTime`, `LocalDateTime`, and `LocalTime` use `HotChocolate.Types.NodaTime.DateTimeOptions` for precision settings.

| Option            | Default | Allowed values  | Applies to                               | Effect                                       |
| ----------------- | ------- | --------------- | ---------------------------------------- | -------------------------------------------- |
| `InputPrecision`  | `9`     | `0` through `9` | `DateTime`, `LocalDateTime`, `LocalTime` | Maximum accepted fractional-second digits.   |
| `OutputPrecision` | `9`     | `0` through `9` | `DateTime`, `LocalDateTime`, `LocalTime` | Maximum serialized fractional-second digits. |

Values above `9` will throw during scalar configuration. `LocalDate` and `Duration` do not use these options.

To use non-default precision for NodaTime CLR types, register the affected scalar types individually. This approach is suitable when your GraphQL-facing members use NodaTime CLR types such as `OffsetDateTime`, `LocalDateTime`, and `LocalTime`. If your schema uses BCL types like `DateTimeOffset`, `DateTime`, `DateOnly`, or `TimeOnly`, use the default `AddNodaTime()` registration or replicate the runtime bindings and converters that `AddNodaTime()` provides.

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

With these settings, an input like `"2023-12-24T15:30:00.123456789+01:00"` is accepted for `DateTime`, but output is serialized as `"2023-12-24T15:30:00.123+01:00"`.

# BCL Type Behavior After AddNodaTime

`AddNodaTime()` changes the scalar implementations used by several BCL date and time types:

| CLR type         | GraphQL scalar after `AddNodaTime()` | NodaTime conversion                                                             |
| ---------------- | ------------------------------------ | ------------------------------------------------------------------------------- |
| `DateTimeOffset` | `DateTime`                           | Converts to and from `OffsetDateTime`.                                          |
| `DateTime`       | `LocalDateTime`                      | Converts to and from `LocalDateTime`.                                           |
| `DateOnly`       | `LocalDate`                          | Converts to and from `LocalDate`.                                               |
| `TimeOnly`       | `LocalTime`                          | Converts to and from `LocalTime`.                                               |
| `TimeSpan`       | Not rebound                          | Keep the built-in `Duration` scalar or convert to `NodaTime.Duration` yourself. |

This section helps you anticipate schema changes after registering NodaTime. For information on pure BCL scalar behavior, see [Built-in Scalars](./built-in-scalars).

# v15 Upgrade Notes

In v16, `HotChocolate.Types.NodaTime` was rewritten to align with the GraphQL scalar specifications. Parsing is stricter than in previous versions, and only five scalar classes remain built in:

- `DateTimeType`
- `DurationType`
- `LocalDateType`
- `LocalDateTimeType`
- `LocalTimeType`

The following legacy scalar types are no longer included:

- `DateTimeZoneType`
- `InstantType`
- `IsoDayOfWeekType`
- `OffsetDateType`
- `OffsetTimeType`
- `OffsetType`
- `PeriodType`
- `ZonedDateTimeType`

If you previously used one of these removed types, convert the value at the GraphQL boundary or define your own scalar contract. For more details, see the [v15 to v16 migration guide](/docs/hotchocolate/v16/_leagcy/migrating/migrate-from-15-to-16).

# Troubleshooting Common Issues

## TimeSpan is not using the NodaTime Duration scalar

**Cause:** `AddNodaTime()` does not bind `System.TimeSpan` and does not register converters between `TimeSpan` and `NodaTime.Duration`.

**Solutions:**

- Use `NodaTime.Duration` in GraphQL-facing models.
- Keep `TimeSpan` and use the built-in BCL `Duration` behavior described in [Built-in Scalars](./built-in-scalars).
- Convert between `TimeSpan` and `NodaTime.Duration` in your application code before values reach the GraphQL schema.

## DateTime input without an offset is rejected

**Cause:** The GraphQL `DateTime` scalar represents an offset-aware date and time. It requires a `Z` or numeric offset.

**Send one of these values instead:**

```json
"2023-12-24T15:30:00Z"
```

```json
"2023-12-24T15:30:00+01:00"
```

Use `LocalDateTime` when the value intentionally has no offset.

## LocalDateTime input with Z or an offset is rejected

**Cause:** `LocalDateTime` models a local date and time without any offset context.

**Remove the offset:**

```json
"2023-12-24T15:30:00"
```

Use `OffsetDateTime` with the GraphQL `DateTime` scalar when the offset is important.

## LocalTime input with a date or offset is rejected

**Cause:** `LocalTime` represents only a time-of-day value.

**Send a time-only value:**

```json
"15:30:00"
```

```json
"15:30:00.123456789"
```

Use `LocalDateTime` or `DateTime` if the date or offset is part of the API value.

## Fractional seconds are rejected or shorter than expected

**Cause:** The input has more digits than allowed by `InputPrecision`, the output uses a lower `OutputPrecision`, or the value exceeds 9 fractional-second digits.

Keep inputs at or below the configured precision. Adjust `OutputPrecision` only when clients require a shorter serialized format.

## InstantType or ZonedDateTimeType cannot be found

**Cause:** Legacy NodaTime scalar types were removed from the v16 package.

Use one of the five supported scalars where possible. If your API must expose a different leaf value contract, see [Custom Scalars](./custom-scalars).

# Next Steps

- See [Built-in Scalars](./built-in-scalars) for the default BCL scalar mappings.
- Explore [Community Scalars](./community-scalars) for additional packaged scalar types.
- Refer to [Custom Scalars](./custom-scalars) if no built-in or package scalar matches your API contract.
