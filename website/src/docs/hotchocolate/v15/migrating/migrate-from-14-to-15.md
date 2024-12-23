---
title: Migrate Hot Chocolate from 14 to 15
---

This guide will walk you through the manual migration steps to update your Hot Chocolate GraphQL server to version 15.

Start by installing the latest `15.x.x` version of **all** of the `HotChocolate.*` packages referenced by your project.

> This guide is still a work in progress with more updates to follow.

# Breaking changes

Things that have been removed or had a change in behavior that may cause your code not to compile or lead to unexpected behavior at runtime if not addressed.

## Supported target frameworks

Support for .NET Standard 2.0, .NET 6, and .NET 7 has been removed.

## F# support removed

`HotChocolate.Types.FSharp` has been replaced by the community project [FSharp.HotChocolate](https://www.nuget.org/packages/FSharp.HotChocolate).

## Runtime type changes

- The runtime type for `LocalDateType` and `DateType` has been changed from `DateTime` to `DateOnly`.
- The runtime type for `LocalTimeType` has been changed from `DateTime` to `TimeOnly`.

## DateTime serialized in universal time for the Date type

`DateTime`s are now serialized in universal time for the `Date` type.

For example, the `DateTime` `2018-06-11 02:46:14` in a time zone of `04:00` will now serialize as `2018-06-10` and not `2018-06-11`.

Use the `LocalDate` type if you do not want the date to be converted to universal time.

## LocalDate, LocalTime, and Date scalars enforce a specific format

- `LocalDate`: `yyyy-MM-dd`
- `LocalTime`: `HH:mm:ss`
- `Date`: `yyyy-MM-dd`

Please ensure that your clients are sending date/time strings in the correct format to avoid errors.

# Deprecations

Things that will continue to function this release, but we encourage you to move away from.
