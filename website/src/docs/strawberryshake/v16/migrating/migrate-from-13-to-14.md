---
title: Migrate Strawberry Shake from 13 to 14
---

This guide will walk you through the manual migration steps to update your Strawberry Shake GraphQL client to version 14.

# Breaking changes

Things that have been removed or had a change in behavior that may cause your code not to compile or lead to unexpected behavior at runtime if not addressed.

## Persisted Queries renamed to Persisted Operations

## CLI options renamed

| Old option name      | New option name          |
|----------------------|--------------------------|
| queryOutputDirectory | operationOutputDirectory |

## Directories renamed

| Old directory name | New directory name   |
|--------------------|----------------------|
| Generated/Queries  | Generated/Operations |

## Files renamed

| Old file name | New file name   |
|---------------|-----------------|
| queries.json  | operations.json |

### MSBuild properties renamed

| Old property name           | New property name               |
|-----------------------------|---------------------------------|
| GraphQLPersistedQueryOutput | GraphQLPersistedOperationOutput |
| GraphQLPersistedQueryFormat | GraphQLPersistedOperationFormat |
