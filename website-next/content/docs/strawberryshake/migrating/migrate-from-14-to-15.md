---
title: Migrate Strawberry Shake from 14 to 15
description: "Upgrade Strawberry Shake from 14 to 15: the `Date` scalar runtime type changes from `DateTime` to `DateOnly`, plus other breaking changes to address."
---

This guide will walk you through the manual migration steps to update your Strawberry Shake GraphQL client to version 15.

# Breaking changes

Things that have been removed or had a change in behavior that may cause your code not to compile or lead to unexpected behavior at runtime if not addressed.

## Runtime type changes

The runtime type for the `Date` scalar is now `DateOnly` instead of `DateTime`.
