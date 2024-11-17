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

# Deprecations

Things that will continue to function this release, but we encourage you to move away from.
