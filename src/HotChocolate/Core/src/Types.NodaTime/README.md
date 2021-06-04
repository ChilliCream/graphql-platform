# Introduction

Adds support for [NodaTime](https://github.com/nodatime/nodatime) types to Hot Chocolate 
so that they can be use to build your GraphQL schema.

Originally developed in [shoooe/hotchocolate-nodatime](https://github.com/shoooe/hotchocolate-nodatime) 
by [@shoooe](https://github.com/shoooe) 
and absorbed in to the Hot Chocolate repository with his permission.

# Usage

## .NET Core

Install [the package](https://www.nuget.org/packages/HotChocolate.Types.NodaTime) from NuGet:

```bash
dotnet add package HotChocolate.Types.NodaTime
```

Call `AddNodaTime` on your schema builder like so:

```c#
SchemaBuilder.New()
    // ...
    .AddNodaTime()
    .Create();
```

# Documentation

## DateTimeZone

Format: One Zone ID from [these](https://nodatime.org/TimeZones)

Literal example: `"Europe/Rome"`

References:
 - [NodaTime.DateTimeZone](https://nodatime.org/3.0.x/api/NodaTime.DateTimeZone.html)
 - [IANA (TZDB) time zone information](https://nodatime.org/TimeZones)

## Duration

Literal example: `"-123:07:53:10.019"`

References:
 - [NodaTime.Duration](https://nodatime.org/3.0.x/api/NodaTime.Duration.html)
 - [Patterns for Duration values](https://nodatime.org/3.0.x/userguide/duration-patterns)

## Instant

Literal example: `"2020-02-20T17:42:59Z"`

References:
 - [NodaTime.Instant](https://nodatime.org/3.0.x/api/NodaTime.Instant.html)
 - [Patterns for Instant values](https://nodatime.org/3.0.x/userguide/instant-patterns)

## IsoDayOfWeek

Literal example: `7`

References:
 - [NodaTime.IsoDayOfWeek](https://nodatime.org/3.0.x/api/NodaTime.IsoDayOfWeek.html)

## LocalDate

Literal example: `"2020-12-25"`

References:
 - [NodaTime.LocalDate](https://nodatime.org/3.0.x/api/NodaTime.LocalDate.html)
 - [Patterns for LocalDate values](https://nodatime.org/3.0.x/userguide/localdate-patterns)

## LocalDateTime

Literal example: `"2020-12-25T13:46:78"`

References:
 - [NodaTime.LocalDateTime](https://nodatime.org/3.0.x/api/NodaTime.LocalDateTime.html)
 - [Patterns for LocalDateTime values](https://nodatime.org/3.0.x/userguide/localdatetime-patterns)

## LocalDateTime

Literal examples: 
 - `"12:42:13"`
 - `"12:42:13.03101"`

References:
 - [NodaTime.LocalTime](https://nodatime.org/3.0.x/api/NodaTime.LocalTime.html)
 - [Patterns for LocalTime values](https://nodatime.org/3.0.x/userguide/localtime-patterns)

## OffsetDateTime

Literal examples: 
 - `"2020-12-25T13:46:78+02"`
 - `"2020-12-25T13:46:78+02:35"`

References:
 - [NodaTime.OffsetDateTime](https://nodatime.org/3.0.x/api/NodaTime.OffsetDateTime.html)
 - [Patterns for OffsetDateTime values](https://nodatime.org/3.0.x/userguide/offsetdatetime-patterns)

## OffsetDate

Literal examples: 
 - `"2020-12-25+02"`
 - `"2020-12-25+02:35"`

References:
 - [NodaTime.OffsetDate](https://nodatime.org/3.0.x/api/NodaTime.OffsetDate.html)
 - [Patterns for OffsetDate values](https://nodatime.org/3.0.x/userguide/offsetdate-patterns)

## OffsetTime

Literal examples: 
 - `"13:46:78+02"`
 - `"13:46:78+02:35"`

References:
 - [NodaTime.OffsetTime](https://nodatime.org/3.0.x/api/NodaTime.OffsetTime.html)
 - [Patterns for OffsetTime values](https://nodatime.org/3.0.x/userguide/offsettime-patterns)

## Offset

Literal examples: 
 - `"+02"`
 - `"+02:35"`

References:
 - [NodaTime.Offset](https://nodatime.org/3.0.x/api/NodaTime.Offset.html)
 - [Patterns for Offset values](https://nodatime.org/3.0.x/userguide/offset-patterns)

## Period

Literal examples: 
 - `"P-3W3D"`
 - `"PT139t"`
 - `"P-3W3DT139t"`

References:
 - [NodaTime.Period](https://nodatime.org/3.0.x/api/NodaTime.Period.html)
 - [Patterns for Period values](https://nodatime.org/3.0.x/userguide/period-patterns)

## ZonedDateTime

*There's nothing close to a standard for timezoned date-times. 
Therefore this library chooses to follow the order of the `ZonedDateTime` constructor 
and define a format with a `LocalDateTime` pattern followed by a timezone ID followed by an offset.
Feel free to override this behavior.*

Literal examples: 
 - `"2020-12-31T18:30:13 UTC +00"`
 - `"2020-12-31T19:40:13 Europe/Rome +01"`
 - `"2020-12-31T19:40:13 Asia/Kathmandu +05:45"`

References:
 - [NodaTime.Period](https://nodatime.org/3.0.x/api/NodaTime.ZonedDateTime.html)
 - [Patterns for Period values](https://nodatime.org/3.0.x/userguide/zoneddatetime-patterns)
